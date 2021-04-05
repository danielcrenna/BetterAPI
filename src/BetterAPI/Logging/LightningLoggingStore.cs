// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using LightningDB;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Logging
{
    internal sealed class LightningLoggingStore : LightningDataStore, ILoggingStore
    {
        public bool Append<TState>(LogLevel logLevel, in EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var id = Guid.NewGuid();

            var entry = new LoggingEntry(id, exception)
            {
                LogLevel = logLevel,
                EventId = eventId,
                Message = formatter(state, exception)
            };

            if (state is IReadOnlyList<KeyValuePair<string, object>> values)
            {
                entry.Data = values.Select(x => new KeyValuePair<string, string?>(x.Key, x.Value?.ToString())).ToList();
            }

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            var context = new LoggingSerializeContext(bw);
            entry.Serialize(context);

            var value = ms.ToArray();

            return WithWritableTransaction((db, tx) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var key = id.ToByteArray();

                tx.Put(db, key, value, PutOptions.NoOverwrite);
                tx.Put(db, KeyBuilder.BuildLogByEntryKey(key), key, PutOptions.NoOverwrite);
                tx.Put(db, KeyBuilder.BuildLogByDataKey("LOGLEVEL", GetLogLevelString(entry.LogLevel)), key, PutOptions.NoDuplicateData);

                if (entry.Data != null)
                    foreach (var (k, v) in entry.Data)
                        tx.Put(db, KeyBuilder.BuildLogByDataKey(k.ToUpperInvariant(), v?.ToUpperInvariant()), key, PutOptions.NoDuplicateData);

                return tx.Commit() == MDBResultCode.Success;
            });
        }

        private static string GetLogLevelString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return "TRACE";
                case LogLevel.Debug:
                    return "DEBUG";
                case LogLevel.Information:
                    return "INFORMATION";
                case LogLevel.Warning:
                    return "WARNING";
                case LogLevel.Error:
                    return "ERROR";
                case LogLevel.Critical:
                    return "CRITICAL";
                case LogLevel.None:
                    return "NONE";
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        public IEnumerable<LoggingEntry> Get(CancellationToken cancellationToken = default)
        {
            return GetByKey(KeyBuilder.GetAllLogEntriesKey(), cancellationToken);
        }
        
        public IEnumerable<LoggingEntry> GetByData(string key, CancellationToken cancellationToken = default)
        {
            return GetByKey(KeyBuilder.BuildLogByDataKey(key), cancellationToken);
        }

        public IEnumerable<LoggingEntry> GetByData(string key, string? value, CancellationToken cancellationToken = default)
        {
            return GetByKey(KeyBuilder.BuildLogByDataKey(key, value), cancellationToken);
        }

        private IEnumerable<LoggingEntry> GetByKey(byte[] key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return WithReadOnlyCursor((cursor, tx) =>
            {
                var entries = new List<LoggingEntry>();
                var sr = cursor.SetRange(key);
                if (sr != MDBResultCode.Success)
                    return entries;

                var (r, k, v) = cursor.GetCurrent();

                while (r == MDBResultCode.Success && !cancellationToken.IsCancellationRequested)
                {
                    if (!k.AsSpan().StartsWith(key))
                        break;

                    var index = v.AsSpan();
                    var entry = GetByIndex(index, tx, cancellationToken);
                    if (entry == default)
                        break;

                    entries.Add(entry);

                    r = cursor.Next();
                    if(r == MDBResultCode.Success)
                        (r, k, v) = cursor.GetCurrent();
                }

                return entries;
            });
        }

        private unsafe LoggingEntry? GetByIndex(ReadOnlySpan<byte> index, LightningTransaction? parent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var tx =
                Env.Value.BeginTransaction(parent == null
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

            var buffer = value.AsSpan();

            fixed (byte* buf = &buffer.GetPinnableReference())
            {
                using var ms = new UnmanagedMemoryStream(buf, buffer.Length);
                using var br = new BinaryReader(ms);
                var context = new LoggingDeserializeContext(br);

                var uuid = br.ReadGuid();
                var entry = new LoggingEntry(uuid, context);
                return entry;
            }
        }

        private T WithReadOnlyCursor<T>(Func<LightningCursor, LightningTransaction, T> func)
        {
            using var tx = Env.Value.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase(configuration: Config);
            using var cursor = tx.CreateCursor(db);
            var result = func.Invoke(cursor, tx);
            return result;
        }

        private T WithWritableTransaction<T>(Func<LightningDatabase, LightningTransaction, T> func)
        {
            using var tx = Env.Value.BeginTransaction(TransactionBeginFlags.None);
            using var db = tx.OpenDatabase(configuration: Config);
            var result = func.Invoke(db, tx);
            return result;
        }
    }
}