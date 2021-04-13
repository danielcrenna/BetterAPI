﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using BetterAPI.Data;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    internal sealed class LightningLocalizationStore : LightningDataStore, ILocalizationStore
    {
        public LightningLocalizationStore(string path) : base(path) { }

        public LocalizedString GetText(string scope, string name, CancellationToken cancellationToken, params object[] args)
        {
            var entries = GetByKeyStruct<LocalizationEntry>(LocalizationKeyBuilder.LookupByName(name), cancellationToken);

            var result = entries
                .Select(x => new LocalizedString(x.Key, x.Value ?? x.Key, x.IsMissing, scope))
                .FirstOrDefault();

            return result ?? new LocalizedString(name, name, true, scope);
        }

        public IEnumerable<LocalizationEntry> GetAllTranslations(in bool includeParentCultures, CancellationToken cancellationToken)
        {
            // FIXME: index IsMissing
            // FIXME: use includeParentCultures

            var cultureName = CultureInfo.CurrentUICulture.Name;
            var entries = GetByKeyStruct<LocalizationEntry>(LocalizationKeyBuilder.LookupByCultureName(cultureName), cancellationToken)
                .Where(x => !x.IsMissing);

            return entries;
        }

        public IEnumerable<LocalizationEntry> GetAllMissingTranslations(in bool includeParentCultures, CancellationToken cancellationToken)
        {
            // FIXME: index IsMissing
            // FIXME: use includeParentCultures

            var cultureName = CultureInfo.CurrentUICulture.Name.ToUpperInvariant();
            var entries = GetByKeyStruct<LocalizationEntry>(LocalizationKeyBuilder.LookupByCultureName(cultureName), cancellationToken)
                .Where(x => x.IsMissing);

            return entries;
        }

        public IEnumerable<LocalizationEntry> GetAllMissingTranslations(string scope, in bool includeParentCultures, CancellationToken cancellationToken)
        {
            // FIXME: index IsMissing
            // FIXME: index Culture/Scope compound key
            // FIXME: use includeParentCultures

            var cultureName = CultureInfo.CurrentUICulture.Name.ToUpperInvariant();
            var entries = GetByKeyStruct<LocalizationEntry>(LocalizationKeyBuilder.LookupByCultureName(cultureName), cancellationToken)
                .Where(x => x.IsMissing && x.Scope.Equals(scope, StringComparison.OrdinalIgnoreCase));

            return entries;
        }

        public bool TryAddMissingTranslation(string cultureName, LocalizedString value, CancellationToken cancellationToken)
        {
            // FIXME: index on compound keys?

            var scope = value.SearchedLocation ?? string.Empty;

            if (!value.ResourceNotFound)
            {
                var message = GetText(scope, "Expecting a missing value, and this value was previously found.", cancellationToken);
                if (message.ResourceNotFound)
                    TryAddMissingTranslation(cultureName, message, cancellationToken);
                throw new InvalidOperationException(message);
            }

            var entries = GetByKeyStruct<LocalizationEntry>(LocalizationKeyBuilder.LookupByCultureName(cultureName),
                cancellationToken);

            if (entries.Any(x => x.Culture.Equals(cultureName, StringComparison.OrdinalIgnoreCase) && x.Key.Equals(value.Name, StringComparison.OrdinalIgnoreCase)))
                return false; // already have a key for this culture
            
            return TryAppend(new LocalizationEntry(Guid.NewGuid(), cultureName, value), cancellationToken);
        }
    }
}
