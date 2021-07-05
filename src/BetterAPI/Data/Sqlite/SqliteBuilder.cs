// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using BetterAPI.Filtering;
using BetterAPI.Reflection;
using BetterAPI.Sorting;

namespace BetterAPI.Data.Sqlite
{
    internal static class SqliteBuilder
    {
        #region DML

        public static Dictionary<string, object> InsertSql<T>(T resource, string resourceName, ITypeReadAccessor reads,
            AccessorMembers members, int revision, long? sequence, bool deleted, out string sql)
        {
            if (resource == null)
                throw new NullReferenceException();

            var hash = new Dictionary<string, object> {{"Sequence", sequence!}};

            sql = Pooling.StringBuilderPool.Scoped(sb =>
            {
                var fields = members.GetValueTypeFields();

                sb.Append("INSERT INTO '");
                sb.Append(resourceName);
                sb.Append("_V");
                sb.Append(revision);
                sb.Append("' (");
                for (var i = 0; i < fields.Length; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    var column = fields[i];
                    sb.Append("\"");
                    sb.Append(column.Name);
                    sb.Append("\"");
                }

                if (deleted)
                    sb.Append(", \"IsDeleted\"");
                
                sb.Append(", \"Sequence\") VALUES (");
                for (var i = 0; i < fields.Length; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    var column = fields[i];
                    sb.Append(":");
                    sb.Append(column.Name);
                }

                if(deleted)
                    sb.Append(", 1");

                sb.Append(", :Sequence)");

                foreach (var member in fields)
                {
                    if (!member.CanRead)
                        continue;
                    if (reads.TryGetValue(resource, member.Name, out var value))
                        hash.Add(member.Name, value);
                }
            });

            return hash;
        }

        #endregion

        private static Dictionary<string, (string, string)> BuildSqlHash(SqliteTableInfo tableInfo)
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
            if (!member.TryGetAttribute(out DefaultValueAttribute defaultValue) || defaultValue.Value == default)
                return "NULL";

            var type = member.Type.Name.ToUpperInvariant();
            return type == "STRING" ? $"\"{defaultValue.Value}\"" : defaultValue.Value?.ToString();
        }

        #region DDL

        public static string CreateTableSql(string resource, AccessorMembers members, int revision, bool fts)
        {
            // See: https://sqlite.org/fts5.html

            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                var fields = members.GetValueTypeFields();

                sb.Append("CREATE");
                if (fts)
                {
                    sb.Append(' ');
                    sb.Append("VIRTUAL");
                }

                sb.Append(' ');
                sb.Append("TABLE");
                sb.Append(' ');
                sb.Append("IF NOT EXISTS");
                sb.Append(' ');
                sb.Append('"');
                sb.Append(resource);
                if (!fts)
                {
                    sb.Append("_V");
                    sb.Append(revision);
                }

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
                for (var i = 0; i < fields.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                        sb.Append(' ');
                    }

                    var column = fields[i];
                    sb.Append('"');
                    sb.Append(column.Name);
                    sb.Append('"');

                    if (!fts)
                    {
                        sb.Append(' ');
                        sb.Append(ResolveColumnTypeToDbType(column));
                        sb.Append(' ');
                        sb.Append("DEFAULT");
                        sb.Append(' ');
                        sb.Append(ResolveColumnDefaultValue(column));
                    }
                }

                if (fields.Length > 0)
                {
                    sb.Append(',');
                    sb.Append(' ');
                }
                
                sb.Append('"');
                sb.Append("CreatedAt");
                sb.Append('"');
                if (!fts)
                {
                    sb.Append(' ');
                    sb.Append("INTEGER DEFAULT CURRENT_TIMESTAMP");
                }

                sb.Append(',');
                sb.Append(' ');
                sb.Append('"');
                sb.Append("IsDeleted");
                sb.Append('"');
                if (!fts)
                {
                    sb.Append(' ');
                    sb.Append("INTEGER DEFAULT 0");
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
                else
                {
                    sb.Append(',');
                    sb.Append(' ');
                    sb.Append("tokenize");
                    sb.Append('=');
                    sb.Append('"');
                    sb.Append("unicode61 remove_diacritics 2 tokenchars '-_.'");
                    sb.Append('"');
                    sb.Append(',');
                    sb.Append(' ');
                    sb.Append("content");
                    sb.Append('=');
                    sb.Append('"');
                    sb.Append(resource);
                    sb.Append('"');
                    sb.Append(',');
                    sb.Append(' ');
                    sb.Append("content_rowid");
                    sb.Append('=');
                    sb.Append('"');
                    sb.Append("Sequence");
                    sb.Append('"');
                }

                sb.Append(')');

                // https://www.sqlite.org/vtab.html
                // SEE: "2.1.3. WITHOUT ROWID Virtual Tables"
                // This should be possible since we have a single PK, but it crashes
                if (!fts) sb.Append(" WITHOUT ROWID");
            });
        }

        public static string CreateThroughTableSql(string parent, int parentRevision, string child, int childRevision)
        {
            // See: https://sqlite.org/fts5.html

            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append("CREATE");
                sb.Append(' ');
                sb.Append("TABLE");
                sb.Append(' ');
                sb.Append("IF NOT EXISTS");
                sb.Append(' ');
                sb.Append('"');
                sb.Append("Through");
                sb.Append('_');
                sb.Append(parent);
                sb.Append("_V");
                sb.Append(parentRevision);
                sb.Append('_');
                sb.Append(child);
                sb.Append("_V");
                sb.Append(childRevision);
                sb.Append('"');
                sb.Append(' ');
                sb.Append('(');

                sb.Append('"');
                sb.Append(parent);
                sb.Append("Id");
                sb.Append('"');
                sb.Append(' ');
                sb.Append("BLOB");
                sb.Append(' ');
                sb.Append("DEFAULT");
                sb.Append(' ');
                sb.Append("NULL");
                
                sb.Append(',');
                sb.Append(' ');

                sb.Append('"');
                sb.Append(child);
                sb.Append("Id");
                sb.Append('"');
                sb.Append(' ');
                sb.Append("BLOB");
                sb.Append(' ');
                sb.Append("DEFAULT");
                sb.Append(' ');
                sb.Append("NULL");

                // See: https://www.sqlite.org/lang_createtable.html#rowid
                // - "INTEGER PRIMARY KEY" is faster when explicit
                // - don't use ROWID, as our sequence spans multiple tables, and ROWID uses AUTO INCREMENT
                sb.Append(',');
                sb.Append(' ');
                sb.Append('"');
                sb.Append("Sequence");
                sb.Append('"');
                sb.Append(' ');
                sb.Append("INTEGER PRIMARY KEY");
                sb.Append(')');

                // https://www.sqlite.org/vtab.html
                // SEE: "2.1.3. WITHOUT ROWID Virtual Tables"
                // This should be possible since we have a single PK, but it crashes
                sb.Append(" WITHOUT ROWID");
            });
        }

        public static string AfterInsertTriggerSql(string resource, AccessorMembers members, int revision)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                var fields = members.GetValueTypeFields();

                sb.Append("CREATE TRIGGER IF NOT EXISTS ");
                sb.Append('"');
                sb.Append(resource);
                sb.Append("_V");
                sb.Append(revision);
                sb.Append("_AfterInsert");
                sb.Append('"');
                sb.Append(" AFTER INSERT ON ");
                sb.Append('"');
                sb.Append(resource);
                sb.Append("_V");
                sb.Append(revision);
                sb.Append('"');
                sb.Append(' ');
                sb.Append("BEGIN");
                sb.Append(' ');
                sb.Append("INSERT INTO ");
                sb.Append('"');
                sb.Append(resource);
                sb.Append("_Search");
                sb.Append('"');
                sb.Append(' ');
                sb.Append('(');
                sb.Append("rowid");
                sb.Append(',');
                sb.Append(' ');
                for (var i = 0; i < fields.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                        sb.Append(' ');
                    }

                    var column = fields[i];
                    sb.Append('"');
                    sb.Append(column.Name);
                    sb.Append('"');
                }

                sb.Append(')');
                sb.Append(' ');
                sb.Append("VALUES");
                sb.Append(' ');
                sb.Append('(');
                sb.Append("new.Sequence");
                sb.Append(',');
                sb.Append(' ');
                for (var i = 0; i < fields.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                        sb.Append(' ');
                    }

                    var column = fields[i];
                    sb.Append("new");
                    sb.Append('.');
                    sb.Append(column.Name);
                }

                sb.Append(')');
                sb.Append(';');
                sb.Append("END");
            });
        }

        public static string DropViewSql(string resource)
        {
            return $"DROP VIEW IF EXISTS \"{resource}\"";
        }

        public static string CreateViewSql(string resource, AccessorMembers members, int revision,
            IEnumerable<SqliteTableInfo> tableInfoList)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                var fields = members.GetValueTypeFields();

                sb.Append("CREATE VIEW \"");
                sb.Append(resource);
                sb.Append("\" (");
                for (var i = 0; i < fields.Length; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    var column = fields[i];
                    sb.Append("\"");
                    sb.Append(column.Name);
                    sb.Append("\"");
                }

                sb.Append(", \"CreatedAt\"");
                sb.Append(", \"IsDeleted\"");
                sb.Append(", \"Sequence\") AS SELECT ");

                for (var i = 0; i < fields.Length; i++)
                {
                    if (i != 0)
                        sb.Append(", ");
                    var column = fields[i];
                    sb.Append("\"");
                    sb.Append(column.Name);
                    sb.Append("\"");
                }

                sb.Append(", \"CreatedAt\"");
                sb.Append(", \"IsDeleted\"");
                sb.Append(", \"Sequence\" FROM");

                sb.Append(' ');
                sb.Append('\"');
                sb.Append(resource);
                sb.Append("_V");
                sb.Append(revision);
                sb.Append('\"');
                sb.Append(' ');

                foreach (var entry in tableInfoList)
                {
                    var hash = BuildSqlHash(entry);

                    sb.Append("UNION SELECT ");
                    var j = 0;
                    foreach (var column in fields)
                    {
                        if (j != 0)
                        {
                            sb.Append(',');
                            sb.Append(' ');
                        }

                        if (!hash.ContainsKey(column.Name))
                        {
                            sb.Append(ResolveColumnDefaultValue(column));
                            sb.Append(" AS \"");
                            sb.Append(column.Name);
                            sb.Append('"');
                            j++;
                            continue;
                        }

                        sb.Append('\"');
                        sb.Append(column.Name);
                        sb.Append('\"');
                        j++;
                    }

                    sb.Append(", \"CreatedAt\"");
                    sb.Append(", \"IsDeleted\"");
                    sb.Append(", \"Sequence\" FROM \"");

                    sb.Append(entry.name);
                    sb.Append("\" ");
                }

                sb.Append(" ORDER BY \"Sequence\" ASC ");
            });
        }

        public static string CreateIndexSql(string resource, string columnName, int revision, bool unique)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
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
                sb.Append(resource);
                sb.Append('_');
                sb.Append('V');
                sb.Append(revision);
                sb.Append("_");
                sb.Append(columnName);
                sb.Append('"');
                sb.Append(' ');
                sb.Append("ON");
                sb.Append(' ');
                sb.Append(resource);
                sb.Append("_V");
                sb.Append(revision);
                sb.Append('(');
                sb.Append('"');
                sb.Append(columnName);
                sb.Append('"');
                sb.Append(')');
                sb.Append(';');
            });
        }

        #endregion

        #region Queries

        public static string GetByIdSql(string resource, AccessorMembers members, List<string>? fields, bool includeDeleted)
        {
            // FIXME: missing field selections
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append(!includeDeleted
                    ? SelectFromSql(resource, fields, members, true)
                    : SelectTopFromSql(resource, 1, fields, members, true));
                
                sb.Append(' ');
                sb.Append("WHERE");
                sb.Append(' ');
                sb.Append('"');
                sb.Append(nameof(IResource.Id));
                sb.Append('"');
                sb.Append(' ');
                sb.Append('=');
                sb.Append(' ');
                sb.Append(':');
                sb.Append(nameof(IResource.Id));
                sb.Append(' ');

                if (!includeDeleted)
                {
                    sb.Append("AND");
                    sb.Append(' ');
                    sb.Append('"');
                    sb.Append("IsDeleted");
                    sb.Append('"');
                    sb.Append(' ');
                    sb.Append('=');
                    sb.Append('0');
                    sb.Append(' ');
                }

                sb.Append("ORDER");
                sb.Append(' ');
                sb.Append("BY");
                sb.Append(' ');
                sb.Append('"');
                sb.Append("Sequence");
                sb.Append('"');
                sb.Append(' ');
                sb.Append("DESC");
            });
        }

        public static string ExistsSql(string resource, bool includeDeleted)
        {
            // FIXME: missing field selections
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                // SELECT EXISTS(SELECT 1 FROM myTbl WHERE u_tag="tag");
                sb.Append("SELECT");
                sb.Append(' ');
                sb.Append("EXISTS");
                sb.Append(' ');
                sb.Append('(');

                sb.Append("SELECT");
                sb.Append(' ');
                sb.Append('1');
                sb.Append(' ');
                sb.Append("FROM");
                sb.Append(' ');
                sb.Append('"');
                sb.Append(resource);
                sb.Append('"');
                sb.Append(' ');
                sb.Append("WHERE");
                sb.Append(' ');
                sb.Append('"');
                sb.Append(nameof(IResource.Id));
                sb.Append('"');
                sb.Append(' ');
                sb.Append('=');
                sb.Append(' ');
                sb.Append(':');
                sb.Append(nameof(IResource.Id));
                sb.Append(')');
            });
        }

        public static string GetTableInfo()
        {
            const string? sql = "SELECT * " +
                                "FROM sqlite_master " +
                                "WHERE type='table' " +
                                "AND name LIKE :name " +
                                "ORDER BY name DESC ";

            return sql;
        }

        public static string GetMaxSequence(string resource, int revision)
        {
            return $"SELECT MAX(\"Sequence\") FROM \"{resource}_V{revision}\"";
        }

        public static string SelectSql(string resource, AccessorMembers members, ResourceQuery query, out bool hasWhere)
        {
            var isSearch = !string.IsNullOrWhiteSpace(query.SearchQuery);
            hasWhere = isSearch;

            var sb = Pooling.StringBuilderPool.Get();
            try
            {
                sb.Append(isSearch
                    ? SearchSql(resource, query, members)
                    : SelectFromSql(resource, query.Fields, members, false));

                sb.Append(' ');

                // FIXME: group by ID and select most recent row
                // FIXME: prevent zombie rows, as deletes are filtered out

                if (!query.IncludeDeleted)
                {
                    sb.Append("WHERE");
                    sb.Append(' ');
                    sb.Append('"');
                    sb.Append("IsDeleted");
                    sb.Append('"');
                    sb.Append(' ');
                    sb.Append('=');
                    sb.Append(' ');
                    sb.Append('0');
                    sb.Append(' ');
                    hasWhere = true;
                }

                if (query.Filters != default)
                {
                    foreach (var (name, @operator, @value) in query.Filters)
                    {
                        if (members.TryGetValue(name, out var member))
                        {
                            sb.Append(hasWhere ? "AND" : "WHERE");
                            hasWhere = true;
                        }

                        sb.Append(' ');
                        sb.Append('"');
                        sb.Append(member.Name);
                        sb.Append('"');
                        sb.Append(' ');

                        switch (@operator)
                        {
                            case FilterOperator.Equal:
                                sb.Append('=');
                                break;
                            case FilterOperator.NotEqual:
                                sb.Append("<>");
                                break;
                            case FilterOperator.GreaterThan:
                                sb.Append('>');
                                break;
                            case FilterOperator.GreaterThanOrEqual:
                                sb.Append(">=");
                                break;
                            case FilterOperator.LessThan:
                                sb.Append('<');
                                break;
                            case FilterOperator.LessThanOrEqual:
                                sb.Append("<=");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        sb.Append(' ');

                        var quoted = member.Type == typeof(string) || member.Type == typeof(DateTimeOffset) || member.Type == typeof(Guid);
                        if (quoted)
                            sb.Append('"');

                        sb.Append(value);

                        if (quoted)
                            sb.Append('"');
                    }
                }

                var sql = sb.ToString();
                return sql;
            }
            finally
            {
                Pooling.StringBuilderPool.Return(sb);
            }
        }

        private static string SearchSql(string resource, ResourceQuery query, AccessorMembers members)
        {
            //
            // See: https://sqlite.org/fts5.html
            // See: https://lunrjs.com/guides/searching.html
            // See: https://www.sqlitetutorial.net/sqlite-full-text-search/
            //

            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append(SelectFromSql(resource, query.Fields, members, true));
                sb.Append(' ');
                sb.Append("WHERE");
                sb.Append(' ');
                sb.Append('"');
                sb.Append(resource);
                sb.Append("_Search");
                sb.Append('"');
                sb.Append(' ');
                sb.Append("MATCH");
                sb.Append(' ');
                sb.Append('"');
                sb.Append(query.SearchQuery);
                sb.Append('"');
            });
        }

        public static string CountSql(string query, string orderBy)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append("SELECT COUNT(*) FROM (");
                sb.Append(query);
                sb.Append(orderBy);
                sb.Append(')');
            });
        }

        public static string PageSql(ResourceQuery query, string viewName, string orderBy, bool hasWhere)
        {
            // Perf: Using OFFSET forces skip-reading records, so we'll settle with a sub-select
            //       over IDs, which is at least less data to scan and part of the index

            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append(hasWhere ? "AND" : "WHERE");
                sb.Append(' ');
                sb.Append($"\"{nameof(IResource.Id)}\" NOT IN (");
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
            });
        }

        public static string SelectFromSql(string resource, List<string>? fields, AccessorMembers members, bool fts)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append("SELECT");
                sb.Append(' ');

                SelectFieldsFromSql(resource, fields, members, fts, sb);
            });
        }

        public static string SelectTopFromSql(string resource, int n, List<string>? fields, AccessorMembers members, bool fts)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append("SELECT");
                sb.Append(' ');
                sb.Append("TOP");
                sb.Append(' ');
                sb.Append(n);
                sb.Append(' ');

                SelectFieldsFromSql(resource, fields, members, fts, sb);
            });
        }

        private static void SelectFieldsFromSql(string resource, IReadOnlyCollection<string>? fields, AccessorMembers members, bool fts, StringBuilder sb)
        {
            if (fields == null)
            {
                sb.Append('*');
            }
            else
            {
                var count = 0;
                foreach (var field in fields)
                {
                    if (!members.TryGetValue(field, out var member))
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
            sb.Append(resource);
            if (fts)
            {
                sb.Append('_');
                sb.Append("Search");
            }

            sb.Append('"');
        }

        public static string OrderBySql(ResourceQuery query)
        {
            return Pooling.StringBuilderPool.Scoped(sb =>
            {
                if (query.Sorting != default && query.Sorting.Count > 0)
                {
                    sb.Append("ORDER");
                    sb.Append(' ');
                    sb.Append("BY");
                    sb.Append(' ');

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
            });
        }

        #endregion
    }
}