// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Threading;
using System.Threading.Tasks;
using LightningDB;

namespace BetterAPI.Data
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

        protected LightningEnvironment Env;

        protected LightningDataStore(string path)
        {
            var config = new EnvironmentConfiguration
            {
                MaxDatabases = DefaultMaxDatabases,
                MaxReaders = DefaultMaxReaders,
                MapSize = DefaultMapSize
            };
            Env = new LightningEnvironment(path, config);
            Env.Open();
            CreateIfNotExists(Env);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
            using var tx = Env.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            var count = tx.GetEntriesCount(db); // entries also contains handles to databases
            return Task.FromResult(count);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            Env.Dispose();
        }

        public static void Index(LightningDatabase db, LightningTransaction tx, byte[] key, byte[] value)
        {
            // IMPORTANT:
            // lmdb DUP_SORT still imposes the MaxKeySizeBytes on the length of the key + value,
            // so it's less ambiguous if we handle "multiple values for a single key" semantics ourselves
            //

            if (key.Length > MaxKeySizeBytes)
            {
                // FIXME: localize this
                var message = $"Index key length is {key.Length} but the maximum key size is {MaxKeySizeBytes}";
                throw new InvalidOperationException(message);
            }

            tx.Put(db, key, value, PutOptions.NoOverwrite);
        }

        protected T WithReadOnlyCursor<T>(Func<LightningCursor, LightningTransaction, T> func)
        {
            using var tx = Env.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);
            var result = func.Invoke(cursor, tx);
            return result;
        }

        protected T WithWritableTransaction<T>(Func<LightningDatabase, LightningTransaction, T> func)
        {
            using var tx = Env.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase(configuration: Config);
            var result = func.Invoke(db, tx);
            return result;
        }
    }
}