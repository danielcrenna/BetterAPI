// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading;
using System.Threading.Tasks;
using LightningDB;

namespace BetterAPI.Logging
{
    internal abstract class LightningDataStore : IDisposable
    {
        // ReSharper disable once UnusedMember.Global
        protected const ushort MaxKeySizeBytes = 511;

        private const int DefaultMaxReaders = 126;
        private const int DefaultMaxDatabases = 5;
        private const long DefaultMapSize = 10_485_760;

        protected static readonly DatabaseConfiguration Config = new DatabaseConfiguration
            {Flags = DatabaseOpenFlags.None};

        protected Lazy<LightningEnvironment> Env;
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Init(string path)
        {
            Env = new Lazy<LightningEnvironment>(() =>
            {
                var config = new EnvironmentConfiguration
                {
                    MaxDatabases = DefaultMaxDatabases,
                    MaxReaders = DefaultMaxReaders,
                    MapSize = DefaultMapSize
                };
                var environment = new LightningEnvironment(path, config);
                environment.Open();
                CreateIfNotExists(environment);
                return environment;
            });
        }

        private static void CreateIfNotExists(LightningEnvironment environment)
        {
            using var tx = environment.BeginTransaction();
            try
            {
                using (tx.OpenDatabase(null, Config))
                {
                    tx.Commit();
                }
            }
            catch (LightningException)
            {
                using (tx.OpenDatabase(null, new DatabaseConfiguration {Flags = DatabaseOpenFlags.Create}))
                {
                    tx.Commit();
                }
            }
        }

        public Task<long> GetLengthAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromResult(0L);
            using var tx = Env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            var count = tx.GetEntriesCount(db); // entries also contains handles to databases
            return Task.FromResult(count);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            if (Env.IsValueCreated)
                Env.Value.Dispose();
        }
    }
}