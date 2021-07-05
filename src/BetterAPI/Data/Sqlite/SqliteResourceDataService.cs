// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using BetterAPI.ChangeLog;
using BetterAPI.Reflection;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Data.Sqlite
{
    public sealed class SqliteResourceDataService<T> : Sqlite, IResourceDataService<T>
        where T : class, IResource
    {
        private readonly int _revision;
        private readonly ChangeLogBuilder _changeLog;
        private readonly IStringLocalizer<SqliteResourceDataService<T>> _localizer;
        private readonly ILogger<SqliteResourceDataService<T>> _logger;
        private readonly AccessorMembers _members;
        private readonly ITypeReadAccessor _reads;

        public bool SupportsSorting => true;
        public bool SupportsFiltering => true;
        public bool SupportsMaxPageSize => true;
        public bool SupportsCount => true;
        public bool SupportsSkip => true;
        public bool SupportsTop => true;
        public bool SupportsShaping => true;
        public bool SupportsSearch => true;

        public SqliteResourceDataService(IStringLocalizer<SqliteResourceDataService<T>> localizer, string filePath,
            int revision, ChangeLogBuilder changeLog, ILogger<SqliteResourceDataService<T>> logger)
        {
            _revision = revision;
            _changeLog = changeLog;
            _localizer = localizer;
            _logger = logger;
            _reads = ReadAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public, out _members);
            
            CreateIfNotExists(filePath);
            FilePath = filePath;
        }

        public IEnumerable<T> Get(ResourceQuery query, CancellationToken cancellationToken)
        {
            Debug.Assert(query.PageOffset.HasValue);
            Debug.Assert(query.PageSize.HasValue);
            
            var viewName = GetResourceName();
            var orderBy = SqliteBuilder.OrderBySql(query);

            var sql = SqliteBuilder.SelectSql(viewName, _members, query, out var hasWhere);
            var pageSql = sql + SqliteBuilder.PageSql(query, viewName, orderBy, hasWhere);

            var db = OpenConnection();
            IEnumerable<T> result = db.Query<T>(pageSql);

            if (!query.CountTotalRows)
                return result;

            var countSql = SqliteBuilder.CountSql(sql, orderBy);
            var total = db.QuerySingle<int>(countSql);
            query.TotalRows = total;

            return result;
        }
        
        public bool TryGetById(Guid id, out T? resource, out bool error, List<string>? fields, bool includeDeleted, CancellationToken cancellationToken)
        {
            try
            {
                var db = OpenConnection();
                var viewName = GetResourceName();
                var result = db.QuerySingleOrDefault<T?>(SqliteBuilder.GetByIdSql(viewName, _members, fields, includeDeleted), new { Id = id });
                resource = result;
                error = false;
                return resource != default;
            }
            catch (Exception e)
            {
                _logger.LogError(ErrorEvents.ErrorRetrievingResource, e, _localizer.GetString("Error retrieving resource from SQLite"));
                resource = default;
                error = true;
                return false;
            }
        }

        public bool Exists(Guid id, bool includeDeleted, CancellationToken cancellationToken)
        {
            var db = OpenConnection();
            var viewName = GetResourceName();
            var result = db.QuerySingleOrDefault<int?>(SqliteBuilder.ExistsSql(viewName, includeDeleted), new { Id = id });
            return result != default;
        }

        public bool TryAdd(T model, out bool error, CancellationToken cancellationToken)
        {
            var db = OpenConnection();
            var t = db.BeginTransaction();

            try
            {
                InsertRecord(model, _revision, false, db, t);
                t.Commit();
                error = false;
                return true;
            }
            catch (Exception e)
            {
                t.Rollback();
                _logger.LogError(ErrorEvents.ErrorSavingResource, e, _localizer.GetString("Error saving resource to SQLite"));
                error = true;
                return false;
            }
        }
        
        public bool TryUpdate(T previous, T next, out bool error, CancellationToken cancellationToken)
        {
            var accessor = WriteAccessor.Create(next, AccessorMemberTypes.Properties, AccessorMemberScope.Public, out var members);

            if(members == null || !members.TryGetValue(nameof(IResource.Id), out var idMember) || idMember.Type != typeof(Guid))
                throw new NotSupportedException(_localizer.GetString("Currently, the data store only accepts entries that have a Guid 'Id' property"));

            if(!accessor.TrySetValue(next, nameof(IResource.Id), previous.Id))
                throw new NotSupportedException(_localizer.GetString("Could not set the resource's Guid 'Id' property"));

            return TryAdd(next, out error, cancellationToken);
        }

        public bool TryDeleteById(Guid id, out T? deleted, out bool error, CancellationToken cancellationToken)
        {
            if (!TryGetById(id, out deleted, out error, null, true, cancellationToken) || deleted == default)
            {
                deleted = default;
                return false;
            }

            var db = OpenConnection();
            var t = db.BeginTransaction();

            try
            {
                InsertRecord(deleted, _revision, true, db, t);
                t.Commit();
                error = false;
                return true;
            }
            catch (Exception e)
            {
                t.Rollback();
                _logger.LogError(ErrorEvents.ErrorSavingResource, e, _localizer.GetString("Error saving resource to SQLite"));
                error = true;
                return false;
            }
        }
        
        public string FilePath { get; }

        private void CreateIfNotExists(string filePath)
        {
            var baseDirectory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(baseDirectory))
                Directory.CreateDirectory(baseDirectory);

            var db = new SqliteConnection($"Data Source={filePath}");
            db.Open();
            var t = db.BeginTransaction();

            try
            {
                Visit(db, t);
                t.Commit(); 
            }
            catch (Exception e)
            {
                t.Rollback();
                _logger.LogError(ErrorEvents.ErrorSavingResource, e, _localizer.GetString("Error creating resource table in SQLite"));
            }
        }

        private void Visit(IDbConnection db, IDbTransaction t)
        {
            var viewName = GetResourceName();

            var tableInfoList = db.Query<SqliteTableInfo>(SqliteBuilder.GetTableInfo(), new {name = $"{viewName}%"}, t)
                .Where(x => !x.name.Contains("_Search"))
                .AsList() ?? throw new NullReferenceException();

            int revision;
            if (tableInfoList.Count == 0)
            {
                revision = 1;
                Visit(db, t, revision, tableInfoList);
            }
            else
            {
                var tableInfo = tableInfoList[0];

                var revisionString = Regex.Split(tableInfo.name, "([0-9])+$",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled)[1];

                if (!int.TryParse(revisionString, out revision))
                    throw new FormatException(_localizer.GetString("invalid revision number"));

                if (_revision <= revision)
                    return;

                Visit(db, t, _revision, tableInfoList);
            }
        }

        private void Visit(IDbConnection db, IDbTransaction t, int revision, IEnumerable<SqliteTableInfo> tableInfoList)
        {
            CreateTableRevision(db, t, revision);
            CreateThroughTableRevisions(db, t, revision);
            RebuildView(db, t, tableInfoList, revision);
        }

        private void CreateTableRevision(IDbConnection db, IDbTransaction t, int revision)
        {
            var viewName = GetResourceName();

            {
                var sql = SqliteBuilder.CreateTableSql(viewName, _members, revision, false);
                db.Execute(sql, transaction: t);
            }

            if (SupportsSearch)
            {
                var sql = SqliteBuilder.AfterInsertTriggerSql(viewName, _members, revision);
                db.Execute(sql, transaction: t);
            }
            
            foreach (var member in _members.GetValueTypeFields())
            {
                if (member.Name.Equals(nameof(IResource.Id)))
                {
                    IndexMember(db, t, revision, member, true);
                }

                if (member.HasAttribute<IndexAttribute>())
                {
                    IndexMember(db, t, revision, member, false);
                }
            }

            // FIXME: deal with user field name collisions
            {
                var sql = SqliteBuilder.CreateIndexSql(GetResourceName(), "IsDeleted", revision, false);
                db.Execute(sql, transaction: t);
            }

            if (SupportsSearch)
            {
                var sql = SqliteBuilder.CreateTableSql(viewName, _members, revision, true);
                db.Execute(sql, transaction: t);
            }
        }

        private void CreateThroughTableRevisions(IDbConnection db, IDbTransaction t, int revision)
        {
            var viewName = GetResourceName();
            
            // Versioning:
            //
            // - Find the API version for the current resource revision
            // - For any embedded resource collections, find the revision for this API version

            var version = _changeLog.GetApiVersionForResourceAndRevision(viewName, revision);
            
            foreach (var member in _members)
            {
                if (!member.Type.ImplementsGeneric(typeof(IEnumerable<>)) || !member.Type.IsGenericType)
                    continue; // not a collection

                var arguments = member.Type.GetGenericArguments();
                var embeddedCollectionType = arguments[0];

                if (!typeof(IResource).IsAssignableFrom(embeddedCollectionType))
                    continue; // not a resource collection

                if (!_changeLog.TryGetResourceNameForType(embeddedCollectionType, out var embeddedViewName) || embeddedViewName == default)
                    embeddedViewName = embeddedCollectionType.Name;

                var embeddedViewRevision = _changeLog.GetRevisionForResourceAndApiVersion(embeddedViewName, version);
                var sql = SqliteBuilder.CreateThroughTableSql(viewName, revision, embeddedViewName, embeddedViewRevision);
                db.Execute(sql, transaction: t);
            }
        }

        private void IndexMember(IDbConnection db, IDbTransaction t, int revision, AccessorMember member, bool unique)
        {
            var sql = SqliteBuilder.CreateIndexSql(GetResourceName(), member.Name, revision, unique);
            db.Execute(sql, transaction: t);
        }
        
        private void InsertRecord(T resource, int revision, bool deleted, IDbConnection db, IDbTransaction t)
        {
            var sequence = GetNextSequence(revision, db, t);
            var viewName = GetResourceName();
            var hash = SqliteBuilder.InsertSql(resource, viewName, _reads, _members, revision, sequence, deleted, out string sql);
            db.Execute(sql, hash, t);
        }

        private long GetNextSequence(int revision, IDbConnection db, IDbTransaction t)
        {
            var previous = revision == 1 ? 1 : revision - 1;

            var viewName = GetResourceName();

            // check current partition first
            var sequence = db.QuerySingleOrDefault<long?>(SqliteBuilder.GetMaxSequence(viewName, revision),
                transaction: t);

            while (!sequence.HasValue)
            {
                sequence = db.QuerySingleOrDefault<long?>(
                    SqliteBuilder.GetMaxSequence(viewName, previous),
                    transaction: t);

                previous--;

                // account for multiple revisions before the first insertion
                if (previous == 0)
                    sequence = 0;
            }

            var nextSequence = sequence.GetValueOrDefault(0);
            nextSequence++;
            return nextSequence;
        }
        
        private void RebuildView(IDbConnection db, IDbTransaction t, IEnumerable<SqliteTableInfo> tableInfoList, int revision)
        {
            var viewName = GetResourceName();
            db.Execute(SqliteBuilder.DropViewSql(viewName), transaction: t);
            db.Execute(SqliteBuilder.CreateViewSql(viewName, _members, revision, tableInfoList), transaction: t);
        }

        private IDbConnection OpenConnection()
        {
            var db = new SqliteConnection($"Data Source={FilePath}");
            db.Open();
            return db;
        }
        
        private string GetResourceName()
        {
            if(_changeLog.TryGetResourceNameForType(_reads.Type, out var resourceName) && resourceName != default)
                return resourceName;

            return _reads.Type.Name;
        }

        public ResourceDataDistribution? GetResourceDataDistribution(int revision)
        {
            var viewName = GetResourceName();

            using var db = new SqliteConnection($"Data Source={FilePath}");
            db.Open();
            using var t = db.BeginTransaction();

            var tableInfoList = db.Query<SqliteTableInfo>(SqliteBuilder.GetTableInfo(), new {name = $"{viewName}%"}, t)
                .Where(x => !x.name.Contains("_Search"))
                .AsList() ?? throw new NullReferenceException();

            foreach (var tableInfo in tableInfoList)
            {
                var revisionString = Regex.Split(tableInfo.name, "([0-9])+$",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled)[1];

                if (!int.TryParse(revisionString, out var tableRevision) || tableRevision != revision)
                    continue;

                var count = db.QuerySingle<long>($"SELECT COUNT(*) FROM \"{tableInfo.name}\"", transaction: t);
                var result = new ResourceDataDistribution {Partition = tableInfo.name, RowCount = count};
                return result;
            }

            return default;
        }
    }
}