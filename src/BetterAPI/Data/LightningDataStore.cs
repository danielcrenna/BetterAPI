// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Reflection;
using LightningDB;
using Microsoft.Extensions.Logging;

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
            // lmdb's DUP_SORT still imposes the MaxKeySizeBytes on the length of the key + value,
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

        protected T WithReadOnlyCursor<T>(Func<LightningCursor, LightningTransaction, T> func, ILogger? logger = default)
        {
            using var tx = Env.BeginTransaction(TransactionBeginFlags.ReadOnly);
            try
            {
                using var db = tx.OpenDatabase(configuration: Config);
                using var cursor = tx.CreateCursor(db);
                var result = func.Invoke(cursor, tx);
                return result;
            }
            catch (Exception e)
            {
                logger?.LogError(ErrorEvents.ErrorSavingResource, e, "Unable to read from lmdb transaction", e);
                tx.Reset();
                throw;
            }
        }

        protected T WithWritableTransaction<T>(Func<LightningDatabase, LightningTransaction, T> func, ILogger? logger = default)
        {
            using var tx = Env.BeginTransaction(TransactionBeginFlags.None);
            try
            {
                using var db = tx.OpenDatabase(configuration: Config);
                var result = func.Invoke(db, tx); // NOTE: function is expected to call tx.Commit()
                return result;
            }
            catch (Exception e)
            {
                logger?.LogError(ErrorEvents.ErrorSavingResource, e, "Unable to write to lmdb transaction", e);
                tx.Reset();
                throw;
            }
        }

        protected IEnumerable<T?> GetByKey<T>(byte[] key, CancellationToken cancellationToken) where T : class
        {
            cancellationToken.ThrowIfCancellationRequested();

            return WithReadOnlyCursor((cursor, tx) =>
            {
                var entries = new List<T>();
                var sr = cursor.SetRange(key);
                if (sr != MDBResultCode.Success)
                    return entries;

                var (r, k, v) = cursor.GetCurrent();

                while (r == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
                {
                    if (!k.AsSpan().StartsWith(key))
                        break;

                    var index = v.AsSpan();
                    var entry = GetByIndex<T>(index, tx, cancellationToken);
                    if (entry == null)
                        break;

                    entries.Add(entry);

                    r = cursor.Next();
                    if(r == MDBResultCode.Success)
                        (r, k, v) = cursor.GetCurrent();
                }

                return entries;
            });
        }

        protected IEnumerable<T> GetByKeyStruct<T>(byte[] key, CancellationToken cancellationToken) where T : struct
        {
            cancellationToken.ThrowIfCancellationRequested();

            return WithReadOnlyCursor((cursor, tx) =>
            {
                var entries = new List<T>();
                var sr = cursor.SetRange(key);
                if (sr != MDBResultCode.Success)
                    return entries;

                var (r, k, v) = cursor.GetCurrent();

                while (r == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
                {
                    if (!k.AsSpan().StartsWith(key))
                        break;

                    var index = v.AsSpan();
                    var entry = GetByIndexStruct<T>(index, tx, cancellationToken);
                    if (entry == null)
                        break;

                    entries.Add(entry.Value);

                    r = cursor.Next();
                    if(r == MDBResultCode.Success)
                        (r, k, v) = cursor.GetCurrent();
                }

                return entries;
            });
        }

        protected T? GetByIndex<T>(ReadOnlySpan<byte> index, LightningTransaction? parent, CancellationToken cancellationToken) where T : class
        {
            return (T?) GetByIndexImpl(typeof(T), index, parent, cancellationToken);
        }

        protected T? GetByIndexStruct<T>(ReadOnlySpan<byte> index, LightningTransaction? parent, CancellationToken cancellationToken) where T : struct
        {
            return (T?) GetByIndexImpl(typeof(T), index, parent, cancellationToken);
        }

        private object? GetByIndexImpl(Type type, ReadOnlySpan<byte> index, LightningTransaction? parent, CancellationToken cancellationToken)
        {
            // FIXME: localize this
            if(!LightningSerializeContext.Serializers.TryGetValue(type, out var functions))
                throw new SerializationException($"No serialization functions registered for '{type.Name}'");

            cancellationToken.ThrowIfCancellationRequested();

            using var tx =
                Env.BeginTransaction(parent == null
                    ? TransactionBeginFlags.ReadOnly
                    : TransactionBeginFlags.None);

            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);

            var (sr, _, _) = cursor.SetKey(index);
            if (sr != MDBResultCode.Success)
                return default;

            var (gr, _, value) = cursor.GetCurrent();
            if (gr != MDBResultCode.Success)
                return default;
            
            return functions.deserialize(value);
        }

        protected bool TryAppend<T>(T resource, CancellationToken cancellationToken, ILogger? logger = default)
        {
            if (cancellationToken.IsCancellationRequested || resource == null)
                return false;

            // FIXME: localize this
            if(!LightningSerializeContext.Serializers.TryGetValue(typeof(T), out var functions))
                throw new SerializationException($"No serialization functions registered for '{typeof(T).Name}'");

            // FIXME: localize this
            var accessor = ReadAccessor.Create(typeof(T), AccessorMemberTypes.Properties, AccessorMemberScope.Public, out var members);
            if(members == null || !members.TryGetValue(nameof(IResource.Id), out var idMember) || idMember.Type != typeof(Guid))
                throw new NotSupportedException("Currently, the data store only accepts entries that have a Guid 'Id' property");

            // FIXME: localize this
            if(!accessor.TryGetValue(resource, nameof(IResource.Id), out var key) || !(key is Guid guid))
                throw new InvalidOperationException("Unable to read resource's Guid 'Id' property");

            var id = guid.ToByteArray();
            var buffer = functions.serialize(resource);
            
            return WithWritableTransaction((db, tx) =>
            {
                Index(db, tx, id, buffer);

                //foreach (var combination in members.GetCombinations())
                //{

                //}

                foreach (var member in members)
                {
                    if(!member.TryGetIndexKey(accessor, resource, id, out var index, logger) || index == default)
                        continue;

                    Index(db, tx, index, id);
                }

                return tx.Commit() == MDBResultCode.Success;
            }, logger);
        }
    }
}