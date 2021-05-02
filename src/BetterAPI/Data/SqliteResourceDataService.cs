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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using BetterAPI.Caching;
using BetterAPI.Reflection;
using BetterAPI.Sorting;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Data
{
    public sealed class SqliteResourceDataService<T> : IResourceDataService<T>
        where T : class, IResource
    {
        private readonly int _revision;
        private readonly ILogger<SqliteResourceDataService<T>> _logger;
        private readonly AccessorMembers _members;
        private readonly ITypeReadAccessor _reads;
        private readonly ITypeWriteAccessor _writes;

        public bool SupportsSort => true;
        public bool SupportsMaxPageSize => true;
        public bool SupportsCount => true;
        public bool SupportsSkip => true;
        public bool SupportsTop => true;
        public bool SupportsShaping => true;
        public bool SupportsSearch => true;

        static SqliteResourceDataService()
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());
        }

        public SqliteResourceDataService(string filePath, int revision, ILogger<SqliteResourceDataService<T>> logger)
        {
            _revision = revision;
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
            sb.Append(BuildSelectFields(query));
            sb.Append(' ');

            var orderBy = BuildOrderBy(query);

            // Perf: Using OFFSET forces skip-reading records, so we'll settle with a sub-select
            //       over IDs, which is at least less data to scan and part of the index
            sb.Append($"WHERE \"{nameof(IResource.Id)}\" NOT IN (");
            sb.Append($"SELECT \"{nameof(IResource.Id)}\" FROM {viewName} ");
            sb.Append(orderBy);
            sb.Append(' ');
            sb.Append("LIMIT");
            sb.Append(' ');
            sb.Append(query.PageOffset);
            sb.Append(") ");
            sb.Append(orderBy);
            sb.Append(' ');
            sb.Append("LIMIT");
            sb.Append(' ');
            sb.Append(query.PageSize);

            var sql = sb.ToString();
            var result = db.Query<T>(sql);

            if (query.CountTotalRows)
            {
                var count = new StringBuilder();
                count.Append("SELECT COUNT(*) FROM (");
                count.Append(BuildSelectFields(query));
                count.Append(' ');
                count.Append(orderBy);
                count.Append(')');

                sql = count.ToString();
                var total = db.QuerySingle<int>(sql);
                query.TotalRows = total;
            }

            return result;
        }

        private StringBuilder BuildSelectFields(ResourceQuery query)
        {
            var sb = new StringBuilder("SELECT ");

            if (query.Fields == null)
            {
                sb.Append('*');
            }
            else
            {
                var count = 0;
                foreach(var field in query.Fields)
                {
                    if (!_members.TryGetValue(field, out var member))
                        continue;

                    if (count != 0)
                        sb.Append(", ");
                    sb.Append('"');
                    sb.Append(member.Name);
                    sb.Append('"');
                    count++;
                }
            }

            sb.Append(' ');
            sb.Append("FROM");
            sb.Append(' ');
            sb.Append('"');
            sb.Append(_reads.Type.Name);
            sb.Append('"');

            return sb;
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
                // must commit even if we don't create anything, or we'll lock the db
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
                    throw new FormatException("invalid revision mask");

                if (_revision <= revision)
                    return;

                CreateTableRevision(db, t, revision);
                RebuildView(db, t, tableInfoList, revision);
            }
        }

        private void CreateTableRevision(IDbConnection db, IDbTransaction t, int revision)
        {
            db.Execute(CreateTableSql(revision, false), transaction: t);

            foreach (var member in _members)
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
                // See: https://sqlite.org/fts5.html
                db.Execute(CreateTableSql(revision, true), transaction: t);
            }
        }

        private void IndexMember(IDbConnection db, IDbTransaction t, int revision, AccessorMember member, bool unique)
        {
            db.Execute(CreateIndexSql(revision, member, unique), transaction: t);
        }

        private string CreateIndexSql(int revision, AccessorMember member, bool unique)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE");
            if (unique)
            {
                sb.Append(' ');
                sb.Append("UNIQUE");
            }
            sb.Append(' ');
            sb.Append("INDEX");
            sb.Append(' ');
            sb.Append('"');
            sb.Append(_reads.Type.Name);
            sb.Append('_');
            sb.Append('V');
            sb.Append(revision);
            sb.Append("_");
            sb.Append(member.Name);
            sb.Append('"');
            sb.Append(' ');
            sb.Append("ON");
            sb.Append(' ');
            sb.Append(_reads.Type.Name);
            sb.Append("_V");
            sb.Append(revision);
            sb.Append('(');
            sb.Append('"');
            sb.Append(member.Name);
            sb.Append('"');
            sb.Append(')');
            sb.Append(';');

            var indexSql = sb.ToString();
            return indexSql;
        }

        private void InsertRecord(T resource, int revision, IDbConnection db, IDbTransaction t)
        {
            var previous = revision == 1 ? 1 : revision - 1;

            var sequence = db.QuerySingleOrDefault<long?>(
                $"SELECT MAX(\"Sequence\") FROM \"{_reads.Type.Name}_V{revision}\"",
                transaction: t);

            // account for the corner case of multiple revisions before the first insertion
            while(previous != 1 && !sequence.HasValue)
            {
                sequence = db.QuerySingleOrDefault<long?>(
                    $"SELECT MAX(\"Sequence\") FROM \"{_reads.Type.Name}_V{previous}\"",
                    transaction: t);

                previous--;
            }

            sequence = sequence.GetValueOrDefault(0);
            sequence++;

            
            var hash = InsertSql(resource, revision, sequence, false, out string sql);
            db.Execute(sql, hash, t);

            if (SupportsSearch)
            {
                hash = InsertSql(resource, revision, sequence, true, out sql);
                db.Execute(sql, hash, t);
            }
        }

        private Dictionary<string, object> InsertSql(T resource, int revision, long? sequence, bool fts, out string sql)
        {
            var sb = new StringBuilder();
            sb.Append("INSERT INTO '");
            sb.Append(_reads.Type.Name);
            sb.Append("_V");
            sb.Append(revision);
            if (fts)
            {
                sb.Append('_');
                sb.Append("Search");
            }
            sb.Append("' (");
            for (var i = 0; i < _members.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                var column = _members[i];
                sb.Append("\"");
                sb.Append(column.Name);
                sb.Append("\"");
            }
            sb.Append(", \"Sequence\") VALUES (");
            for (var i = 0; i < _members.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                var column = _members[i];
                sb.Append(":");
                sb.Append(column.Name);
            }
            sb.Append(", :Sequence)");
            sql = sb.ToString();

            var hash = new Dictionary<string, object> {{"Sequence", sequence!}};
            foreach (var member in _members)
            {
                if (!member.CanRead)
                    continue;
                if (_reads.TryGetValue(resource, member.Name, out var value))
                    hash.Add(member.Name, value);
            }
            return hash;
        }

        private void RebuildView(IDbConnection db, IDbTransaction t, IEnumerable<TableInfo> tableInfoList, int revision)
        {
            db.Execute($"DROP VIEW IF EXISTS \"{_reads.Type.Name}\"");

            var sb = new StringBuilder();
            sb.Append("CREATE VIEW \"");
            sb.Append(_reads.Type.Name);
            sb.Append("\" (");
            for (var i = 0; i < _members.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                var column = _members[i];
                sb.Append("\"");
                sb.Append(column.Name);
                sb.Append("\"");
            }

            sb.Append(", \"Sequence\") AS SELECT ");
            for (var i = 0; i < _members.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                var column = _members[i];
                sb.Append("\"");
                sb.Append(column.Name);
                sb.Append("\"");
            }

            sb.Append(", \"Sequence\" FROM \"");
            sb.Append(_reads.Type.Name);
            sb.Append("_V");
            sb.Append(revision);
            sb.Append("\" ");

            foreach (var entry in tableInfoList)
            {
                var hash = BuildSqlHash(entry);

                sb.Append("UNION SELECT ");
                var j = 0;
                foreach (var column in _members)
                {
                    if (j != 0)
                        sb.Append(", ");

                    if (!hash.ContainsKey(column.Name))
                    {
                        sb.Append(ResolveColumnDefaultValue(column));
                        sb.Append(" AS \"");
                        sb.Append(column.Name);
                        sb.Append("\"");
                        j++;
                        continue;
                    }

                    sb.Append("\"");
                    sb.Append(column.Name);
                    sb.Append("\"");
                    j++;
                }

                sb.Append(", \"Sequence\" FROM \"");
                sb.Append(entry.name);
                sb.Append("\" ");
            }

            sb.Append(" ORDER BY \"Sequence\" ASC ");
            var viewSql = sb.ToString();
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

        private string CreateTableSql(int revision, bool fts)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE");
            if (fts)
            {
                sb.Append(' ');
                sb.Append("VIRTUAL");
            }
            sb.Append(' ');
            sb.Append("TABLE");
            sb.Append(' ');
            sb.Append('"');
            sb.Append(_reads.Type.Name);
            sb.Append("_V");
            sb.Append(revision);
            if (fts)
            {
                sb.Append('_');
                sb.Append("Search");
            }
            sb.Append('"');
            sb.Append(' ');
            if (fts)
            {
                sb.Append("USING");
                sb.Append(' ');
                sb.Append("FTS5");
                sb.Append(' ');
            }
            sb.Append('(');
            for (var i = 0; i < _members.Count; i++)
            {
                if (i != 0)
                    sb.Append(", ");

                var column = _members[i];
                sb.Append(" \"");
                sb.Append(column.Name);
                sb.Append("\" ");

                if (!fts)
                {
                    sb.Append(ResolveColumnTypeToDbType(column));
                    sb.Append(" DEFAULT ");
                    sb.Append(ResolveColumnDefaultValue(column));
                }
            }

            // See: https://www.sqlite.org/lang_createtable.html#rowid
            // - "INTEGER PRIMARY KEY" is faster when explicit
            // - don't use ROWID, as our sequence spans multiple tables, and ROWID uses AUTO INCREMENT
            sb.Append(',');
            sb.Append(' ');
            sb.Append('"');
            sb.Append("Sequence");
            sb.Append('"');
            if (!fts)
            {
                sb.Append(' ');
                sb.Append("INTEGER PRIMARY KEY");
            }
            sb.Append(')');

            // https://www.sqlite.org/vtab.html
            // SEE: "2.1.3. WITHOUT ROWID Virtual Tables"
            // This should be possible since we have a single PK, but it crashes
            if (!fts)
            {
                sb.Append(" WITHOUT ROWID");
            }

            var sql = sb.ToString();
            return sql;
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