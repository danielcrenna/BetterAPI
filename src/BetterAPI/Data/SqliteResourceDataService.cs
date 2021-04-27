// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BetterAPI.Paging;
using BetterAPI.Reflection;
using BetterAPI.Sorting;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterAPI.Data
{
    public sealed class SqliteResourceDataService<T> : IResourceDataService<T>
        where T : class, IResource
    {
        private readonly int _revision;
        private readonly IOptionsMonitor<PagingOptions> _options;
        private readonly ILogger<SqliteResourceDataService<T>> _logger;
        private readonly AccessorMembers _members;
        private readonly ITypeReadAccessor _reads;
        private readonly ITypeWriteAccessor _writes;

        public bool SupportsSorting => true;
        public bool SupportsMaxPageSize => true;
        public bool SupportsCount => true;
        public bool SupportsSkip => true;
        public bool SupportsTop => true;

        static SqliteResourceDataService()
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());
        }

        public SqliteResourceDataService(string filePath, int revision, IOptionsMonitor<PagingOptions> options, ILogger<SqliteResourceDataService<T>> logger)
        {
            _revision = revision;
            _options = options;
            _logger = logger;

            _reads = ReadAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public, out _members);
            _writes = WriteAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public);

            CreateIfNotExists(filePath);
            FilePath = filePath;
        }

        public IEnumerable<T> Get(ResourceQuery query, CancellationToken cancellationToken)
        {
            Debug.Assert(query.PageOffset.HasValue);
            Debug.Assert(query.PageSize.HasValue);
            
            var viewName = _reads.Type.Name;

            var db = OpenConnection();

            var sb = new StringBuilder();
            sb.Append($"SELECT * FROM \"{viewName}\"");

            var orderBy = BuildOrderBy(query);

            // Perf: Using OFFSET forces skip-reading records, so we'll settle with a sub-select
            //       over IDs, which is at least less data to scan and part of the index
            sb.Append($" WHERE \"{nameof(IResource.Id)}\" NOT IN (");
            sb.Append($"SELECT \"{nameof(IResource.Id)}\" FROM {viewName} ");
            sb.Append(orderBy);
            sb.Append(" LIMIT ");
            sb.Append(query.PageOffset);
            sb.Append(") ");
            sb.Append(orderBy);
            sb.Append(" LIMIT ");
            sb.Append(query.PageSize);

            var sql = sb.ToString();
            var result = db.Query<T>(sql);

            if (query.CountTotalRows)
            {
                var count = new StringBuilder();
                count.Append("SELECT COUNT(*) FROM (");
                count.Append($"SELECT * FROM {viewName} ");
                count.Append(orderBy);
                count.Append(')');

                sql = count.ToString();
                var total = db.QuerySingle<int>(sql);
                query.TotalRows = total;
            }

            return result;
        }

        private static StringBuilder BuildOrderBy(ResourceQuery query)
        {
            var sb = new StringBuilder();

            if (query.Sorting != default && query.Sorting.Count > 0)
            {
                sb.Append("ORDER BY ");

                var count = 0;
                foreach (var (member, direction) in query.Sorting)
                {
                    sb.Append('"');
                    sb.Append(member.Name);
                    sb.Append('"');
                    sb.Append(' ');
                    sb.Append(direction == SortDirection.Descending ? "DESC" : "ASC");
                    
                    count++;

                    if (count < query.Sorting.Count)
                        sb.Append(", ");
                }
            }

            return sb;
        }

        public bool TryGetById(Guid id, out T? resource, CancellationToken cancellationToken)
        {
            var db = OpenConnection();
            var result = db.QuerySingleOrDefault<T?>($"SELECT * FROM \"{_reads.Type.Name}\" WHERE \"{nameof(IResource.Id)}\" = :Id", new { Id = id });
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
                _logger.LogError(ErrorEvents.ErrorSavingResource, e, "Error saving resource to SQLite");
                return false;
            }
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
                _logger.LogError(ErrorEvents.ErrorSavingResource, e, "Error creating resource table in SQLite");
            }
        }

        private void Visit(IDbConnection db, IDbTransaction t)
        {
            var tableInfoList = db.Query<TableInfo>(
                    "SELECT * " +
                    "FROM sqlite_master " +
                    "WHERE type='table' " +
                    "AND name LIKE :name " +
                    "ORDER BY name DESC ", new {name = $"{_reads.Type.Name}%"}, t)
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
                    throw new FormatException("invalid revision mask");

                if (_revision <= revision)
                    return;

                CreateTableRevision(db, t, revision);
                RebuildView(db, t, tableInfoList, revision);
            }
        }

        private void CreateTableRevision(IDbConnection db, IDbTransaction t, int revision)
        {
            var sql = CreateTableSql(revision);
            db.Execute(sql, transaction: t);
        }

        private void InsertRecord(T resource, int revision, IDbConnection db, IDbTransaction t)
        {
            var previous = revision == 1 ? 1 : revision - 1;

            var sequence =
                db.QuerySingleOrDefault<long?>($"SELECT MAX(\"Sequence\") FROM \"{_reads.Type.Name}_V{previous}\"",
                        transaction: t)
                    .GetValueOrDefault(0);

            sequence++;

            if(resource.Id == Guid.Empty)
                _writes.TrySetValue(resource, nameof(IResource.Id), Guid.NewGuid());

            var insert = new StringBuilder();
            insert.Append("INSERT INTO '");
            insert.Append(_reads.Type.Name);
            insert.Append("_V");
            insert.Append(revision);
            insert.Append("' (");
            for (var i = 0; i < _members.Count; i++)
            {
                if (i != 0)
                    insert.Append(", ");
                var column = _members[i];
                insert.Append("\"");
                insert.Append(column.Name);
                insert.Append("\"");
            }

            insert.Append(", \"Sequence\") VALUES (");
            for (var i = 0; i < _members.Count; i++)
            {
                if (i != 0)
                    insert.Append(", ");
                var column = _members[i];
                insert.Append(":");
                insert.Append(column.Name);
            }

            insert.Append(", :Sequence)");

            var hash = new Dictionary<string, object> {{"Sequence", sequence}};
            foreach (var member in _members)
            {
                if(_reads.TryGetValue(resource, member.Name, out var value))
                    hash.Add(member.Name, value);
            }

            var sql = insert.ToString();
            db.Execute(sql, hash, t);
        }

        private void RebuildView(IDbConnection db, IDbTransaction t, IEnumerable<TableInfo> tableInfoList, int revision)
        {
            db.Execute($"DROP VIEW IF EXISTS \"{_reads.Type.Name}\"");

            var view = new StringBuilder();
            view.Append("CREATE VIEW \"");
            view.Append(_reads.Type.Name);
            view.Append("\" (");
            for (var i = 0; i < _members.Count; i++)
            {
                if (i != 0)
                    view.Append(", ");
                var column = _members[i];
                view.Append("\"");
                view.Append(column.Name);
                view.Append("\"");
            }

            view.Append(", \"Sequence\") AS SELECT ");
            for (var i = 0; i < _members.Count; i++)
            {
                if (i != 0)
                    view.Append(", ");
                var column = _members[i];
                view.Append("\"");
                view.Append(column.Name);
                view.Append("\"");
            }

            view.Append(", \"Sequence\" FROM \"");
            view.Append(_reads.Type.Name);
            view.Append("_V");
            view.Append(revision);
            view.Append("\" ");

            foreach (var entry in tableInfoList)
            {
                var hash = BuildSqlHash(entry);

                view.Append("UNION SELECT ");
                var j = 0;
                foreach (var column in _members)
                {
                    if (j != 0)
                        view.Append(", ");

                    if (!hash.ContainsKey(column.Name))
                    {
                        view.Append(ResolveColumnDefaultValue(column));
                        view.Append(" AS \"");
                        view.Append(column.Name);
                        view.Append("\"");
                        j++;
                        continue;
                    }

                    view.Append("\"");
                    view.Append(column.Name);
                    view.Append("\"");
                    j++;
                }

                view.Append(", \"Sequence\" FROM \"");
                view.Append(entry.name);
                view.Append("\" ");
            }

            view.Append(" ORDER BY \"Sequence\" ASC ");
            var viewSql = view.ToString();
            db.Execute(viewSql, transaction: t);
        }

        private static Dictionary<string, (string, string)> BuildSqlHash(TableInfo tableInfo)
        {
            var clauses = Regex.Replace(tableInfo.sql, @"[^\(]+\(([^\)]+)\)", "$1",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

            var columns = clauses[1..]
                .Split(',', StringSplitOptions.RemoveEmptyEntries);

            var sqlHash = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in columns)
            {
                var pair = column.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var name = pair[0].Trim('\"');
                var type = pair[1];
                var defaultValue = pair[3];
                sqlHash.Add(name, (type, defaultValue));
            }

            return sqlHash;
        }

        private string CreateTableSql(int revision)
        {
            var create = new StringBuilder();
            create.Append("CREATE TABLE \"");
            create.Append(_reads.Type.Name);
            create.Append("_V");
            create.Append(revision);
            create.Append("\" (");
            for (var i = 0; i < _members.Count; i++)
            {
                if (i != 0)
                    create.Append(", ");
                var column = _members[i];
                create.Append(" \"");
                create.Append(column.Name);
                create.Append("\" ");
                create.Append(ResolveColumnTypeToDbType(column));
                create.Append(" DEFAULT ");
                create.Append(ResolveColumnDefaultValue(column));
            }

            // See: https://www.sqlite.org/lang_createtable.html#rowid
            // - "INTEGER PRIMARY KEY" is faster when explicit
            // - don't use ROWID, as our sequence spans multiple tables, and ROWID uses AUTO INCREMENT
            create.Append(", \"Sequence\" INTEGER PRIMARY KEY) WITHOUT ROWID");
            var createSql = create.ToString();
            return createSql;
        }

        private static string ResolveColumnTypeToDbType(AccessorMember member)
        {
            var type = member.Type.Name.ToUpperInvariant();
            return type switch
            {
                "INT" => "INTEGER",
                "STRING" => "TEXT",
                _ => "BLOB"
            };
        }

        private static string? ResolveColumnDefaultValue(AccessorMember member)
        {
            if(!member.TryGetAttribute(out DefaultValueAttribute defaultValue) || defaultValue.Value == default)
                return "NULL";

            // FIXME: Dapper crashes if the default value starts with "?"
            var type = member.Type.Name.ToUpperInvariant();
            return type == "STRING" ? $"\"{defaultValue.Value}\"" : defaultValue.Value?.ToString();
        }

        private IDbConnection OpenConnection()
        {
            var db = new SqliteConnection($"Data Source={FilePath}");
            db.Open();
            return db;
        }

        public struct TableInfo
        {
            // ReSharper disable once InconsistentNaming
            public string name;

            // ReSharper disable once InconsistentNaming
            public string sql;
        }
    }
}