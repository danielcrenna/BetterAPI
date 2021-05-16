﻿// Copyright (c) Daniel Crenna. All rights reserved.
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
using BetterAPI.Caching;
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
        private readonly ChangeLogBuilder _builder;
        private readonly IStringLocalizer<SqliteResourceDataService<T>> _localizer;
        private readonly ILogger<SqliteResourceDataService<T>> _logger;
        private readonly AccessorMembers _members;
        private readonly ITypeReadAccessor _reads;

        public bool SupportsSorting => true;
        public bool SupportsMaxPageSize => true;
        public bool SupportsCount => true;
        public bool SupportsSkip => true;
        public bool SupportsTop => true;
        public bool SupportsShaping => true;
        public bool SupportsSearch => true;

        public SqliteResourceDataService(string filePath, int revision, ChangeLogBuilder builder, IStringLocalizer<SqliteResourceDataService<T>> localizer, ILogger<SqliteResourceDataService<T>> logger)
        {
            _revision = revision;
            _builder = builder;
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

            var db = OpenConnection();
            var viewName = GetResourceName();
            var orderBy = SqliteBuilder.OrderBySql(query);

            var sql = SqliteBuilder.SelectSql(viewName, _members, query, out var hasWhere);
            var pageSql = sql + SqliteBuilder.PageSql(query, viewName, orderBy, hasWhere);
            IEnumerable<T> result = db.Query<T>(pageSql);

            if (!query.CountTotalRows)
                return result;

            var countSql = SqliteBuilder.CountSql(sql, orderBy);
            var total = db.QuerySingle<int>(countSql);
            query.TotalRows = total;

            return result;
        }
        
        public bool TryGetById(Guid id, out T? resource, CancellationToken cancellationToken)
        {
            var db = OpenConnection();
            var viewName = GetResourceName();
            var result = db.QuerySingleOrDefault<T?>(SqliteBuilder.GetById(viewName), new { Id = id });
            resource = result;
            return resource != default;
        }

        public bool TryAdd(T model)
        {
            var db = OpenConnection();
            var t = db.BeginTransaction();

            try
            {
                InsertRecord(model, _revision, db, t);
                t.Commit();
                return true;
            }
            catch (Exception e)
            {
                t.Rollback();
                _logger.LogError(ErrorEvents.ErrorSavingResource, e, _localizer.GetString("Error saving resource to SQLite"));
                return false;
            }
        }

        public bool TryUpdate(T model)
        {
            throw new NotImplementedException();
        }

        public bool TryDeleteById(Guid id, out T? deleted, out bool error)
        {
            throw new NotImplementedException();
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
                .AsList();

            int revision;
            if (tableInfoList.Count == 0)
            {
                revision = 1;
                CreateTableRevision(db, t, revision);
                RebuildView(db, t, tableInfoList, revision);
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

                CreateTableRevision(db, t, _revision);
                RebuildView(db, t, tableInfoList, _revision);
            }
        }

        private void CreateTableRevision(IDbConnection db, IDbTransaction t, int revision)
        {
            var viewName = GetResourceName();

            db.Execute(SqliteBuilder.CreateTableSql(viewName, _members, revision, false), transaction: t);

            if (SupportsSearch)
            {
                db.Execute(SqliteBuilder.AfterInsertTriggerSql(viewName, _members, revision), transaction: t);
            }
            
            foreach (var member in _members.GetDiscreteFields())
            {
                if (member.Name.Equals(nameof(IResource.Id)))
                {
                    IndexMember(db, t, revision, member, true);
                }

                if (member.HasAttribute<IndexAttribute>() || member.HasAttribute<LastModifiedAttribute>())
                {
                    IndexMember(db, t, revision, member, false);
                }
            }

            if (SupportsSearch)
            {
                db.Execute(SqliteBuilder.CreateTableSql(viewName, _members, revision, true), transaction: t);
            }
        }

        private void IndexMember(IDbConnection db, IDbTransaction t, int revision, AccessorMember member, bool unique)
        {
            db.Execute(SqliteBuilder.CreateIndexSql(GetResourceName(), member, revision, unique), transaction: t);
        }
        
        private void InsertRecord(T resource, int revision, IDbConnection db, IDbTransaction t)
        {
            var sequence = GetNextSequence(revision, db, t);
            var viewName = GetResourceName();
            var hash = SqliteBuilder.InsertSql(resource, viewName, _reads, _members, revision, sequence, out string sql);
            db.Execute(sql, hash, t);
        }

        private long GetNextSequence(int revision, IDbConnection db, IDbTransaction t)
        {
            var previous = revision == 1 ? 1 : revision - 1;

            var viewName = GetResourceName();

            var sequence = db.QuerySingleOrDefault<long?>(SqliteBuilder.GetMaxSequence(viewName, revision),
                transaction: t);

            // account for the corner case of multiple revisions before the first insertion
            while (previous != 1 && !sequence.HasValue)
            {
                sequence = db.QuerySingleOrDefault<long?>(
                    SqliteBuilder.GetMaxSequence(viewName, previous),
                    transaction: t);

                previous--;
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
            if(_builder.TryGetResourceName(_reads.Type, out var resourceName) && resourceName != default)
                return resourceName;

            return _reads.Type.Name;
        }
    }
}