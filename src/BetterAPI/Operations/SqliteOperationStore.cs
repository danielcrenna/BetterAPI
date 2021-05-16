// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Operations
{
    public class SqliteOperationStore : IOperationStore
    {
        private readonly IStringLocalizer<SqliteOperationStore> _localizer;
        private readonly ILogger<SqliteOperationStore> _logger;

        public SqliteOperationStore(string filePath, IStringLocalizer<SqliteOperationStore> localizer, ILogger<SqliteOperationStore> logger)
        {
            _localizer = localizer;
            _logger = logger;
            CreateIfNotExists(filePath);
            FilePath = filePath;
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
                _logger.LogError(ErrorEvents.ErrorSavingResource, e, _localizer.GetString("Error creating operations table in SQLite"));
            }
        }

        private void Visit(SqliteConnection db, SqliteTransaction transaction)
        {
            
        }
    }
}