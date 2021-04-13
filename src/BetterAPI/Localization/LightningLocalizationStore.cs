using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using BetterAPI.Data;
using LightningDB;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    internal sealed class LightningLocalizationStore : LightningDataStore, ILocalizationStore
    {
        public LightningLocalizationStore(string path) : base(path) { }

        public LocalizedString GetText(string scope, string name, CancellationToken cancellationToken, params object[] args)
        {
            // FIXME: pass cancellation token
            // FIXME: support partitions

            var entries = GetByKey(KeyBuilder.LookupByName(name.ToUpperInvariant()), cancellationToken);

            var result = entries
                .Select(x => new LocalizedString(x.Key, x.Value ?? x.Key, x.IsMissing, searchedLocation: null))
                .FirstOrDefault();

            return result ?? new LocalizedString(name, name, true, null);
        }

        public IEnumerable<LocalizationEntry> GetAllTranslations(in bool includeParentCultures, CancellationToken cancellationToken)
        {
            // FIXME: index IsMissing
            // FIXME: support partitions
            // FIXME: use includeParentCultures

            var cultureName = CultureInfo.CurrentUICulture.Name.ToUpperInvariant();
            var entries = GetByKey(KeyBuilder.LookupByCultureName(cultureName), cancellationToken)
                .Where(x => !x.IsMissing);

            return entries;
        }

        public IEnumerable<LocalizationEntry> GetAllMissingTranslations(in bool includeParentCultures, CancellationToken cancellationToken)
        {
            // FIXME: index IsMissing
            // FIXME: support partitions
            // FIXME: use includeParentCultures

            var cultureName = CultureInfo.CurrentUICulture.Name.ToUpperInvariant();
            var entries = GetByKey(KeyBuilder.LookupByCultureName(cultureName), cancellationToken)
                .Where(x => x.IsMissing);

            return entries;
        }

        public IEnumerable<LocalizationEntry> GetAllMissingTranslations(string scope, in bool includeParentCultures, CancellationToken cancellationToken)
        {
            // FIXME: index IsMissing
            // FIXME: index Scope
            // FIXME: use includeParentCultures

            var cultureName = CultureInfo.CurrentUICulture.Name.ToUpperInvariant();
            var entries = GetByKey(KeyBuilder.LookupByCultureName(cultureName), cancellationToken)
                .Where(x => x.IsMissing && x.Scope.Equals(scope, StringComparison.OrdinalIgnoreCase));

            return entries;
        }

        public bool TryAddMissingTranslation(string cultureName, LocalizedString value, CancellationToken cancellationToken)
        {
            // FIXME: index a compound key

            var scope = value.SearchedLocation ?? string.Empty;

            if (!value.ResourceNotFound)
            {
                var message = GetText(scope, "Expecting a missing value, and this value was previously found.", cancellationToken);
                if (message.ResourceNotFound)
                    TryAddMissingTranslation(cultureName, message, cancellationToken);
                throw new InvalidOperationException(message);
            }

            var entries = GetByKey(KeyBuilder.LookupByCultureName(cultureName.ToUpperInvariant()), cancellationToken);
            if (entries.Any(x => x.Culture.Equals(cultureName, StringComparison.OrdinalIgnoreCase) && 
                                 x.Key.Equals(value.Name, StringComparison.OrdinalIgnoreCase)))
                return false; // already have a key for this culture
            
            return TryAppendEntry(cultureName, value, cancellationToken);
        }

        private bool TryAppendEntry(string cultureName, LocalizedString value, CancellationToken cancellationToken)
        {
            var entry = new LocalizationEntry(Guid.NewGuid(), cultureName, value);

            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            var context = new LocalizationSerializationContext(bw);
            entry.Serialize(context);

            var buffer = ms.ToArray();

            return WithWritableTransaction((db, tx) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var key = entry.Id.ToByteArray();

                Index(db, tx, key, buffer);
                Index(db, tx, KeyBuilder.LookupById(key), key);
                Index(db, tx, KeyBuilder.IndexByName(entry.Key.ToUpperInvariant(), key), key);
                Index(db, tx, KeyBuilder.IndexByCultureName(cultureName.ToUpperInvariant(), key), key);
                Index(db, tx, KeyBuilder.IndexByScope((value.SearchedLocation ?? "".ToUpperInvariant()), key), key);

                return tx.Commit() == MDBResultCode.Success;
            });
        }

        private IEnumerable<LocalizationEntry> GetByKey(byte[] key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return WithReadOnlyCursor((cursor, tx) =>
            {
                var entries = new List<LocalizationEntry>();
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
                    if (entry == null)
                        break;

                    entries.Add((LocalizationEntry) entry);

                    r = cursor.Next();
                    if(r == MDBResultCode.Success)
                        (r, k, v) = cursor.GetCurrent();
                }

                return entries;
            });
        }

        private unsafe LocalizationEntry? GetByIndex(ReadOnlySpan<byte> index, LightningTransaction? parent, CancellationToken cancellationToken)
        {
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

            var buffer = value.AsSpan();

            fixed (byte* buf = &buffer.GetPinnableReference())
            {
                using var ms = new UnmanagedMemoryStream(buf, buffer.Length);
                using var br = new BinaryReader(ms);
                var context = new LocalizationDeserializationContext(br);

                var entry = new LocalizationEntry(context);
                return entry;
            }
        }
    }
}
