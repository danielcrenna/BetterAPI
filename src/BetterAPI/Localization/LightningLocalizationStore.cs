using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using BetterAPI.Data;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Localization
{
    internal sealed class LightningLocalizationStore : LightningDataStore, ILocalizationStore
    {
        private readonly ILogger<LightningLocalizationStore> _logger;

        public LightningLocalizationStore(string path, ILogger<LightningLocalizationStore> logger) : base(path)
        {
            _logger = logger;
        }

        public LocalizedString GetText(string scope, string name, CancellationToken cancellationToken, params object[] args)
        {
            var entries = GetByKeyStruct<LocalizationEntry>(LocalizationKeyBuilder.LookupByKey(name), cancellationToken);

            var result = entries
                .Select(x => new LocalizedString(x.Key, x.Value ?? x.Key, x.IsMissing, scope))
                .FirstOrDefault();

            if (args.Length <= 0)
                return result ?? new LocalizedString(name, name, true, scope);

            if(result == null)
                return new LocalizedString(name, string.Format(name, args), true, scope);

            return new LocalizedString(result.Name, string.Format(result.Value, args), result.ResourceNotFound,
                result.SearchedLocation);
        }

        public IEnumerable<LocalizationEntry> GetAllTranslations(CancellationToken cancellationToken)
        {
            var entries = GetStruct<LocalizationEntry>(cancellationToken);
            return entries;
        }

        public IEnumerable<LocalizationEntry> GetAllTranslationsByCurrentCulture(in bool includeParentCultures, CancellationToken cancellationToken)
        {
            // FIXME: index IsMissing
            // FIXME: use includeParentCultures

            var cultureName = CultureInfo.CurrentUICulture.Name;
            var entries = GetByKeyStruct<LocalizationEntry>(LocalizationKeyBuilder.LookupByCulture(cultureName), cancellationToken)
                .Where(x => !x.IsMissing);

            return entries;
        }

        public IEnumerable<LocalizationEntry> GetAllMissingTranslationsByCurrentCulture(in bool includeParentCultures, CancellationToken cancellationToken)
        {
            // FIXME: index IsMissing
            // FIXME: use includeParentCultures

            var cultureName = CultureInfo.CurrentUICulture.Name.ToUpperInvariant();
            var entries = GetByKeyStruct<LocalizationEntry>(LocalizationKeyBuilder.LookupByCulture(cultureName), cancellationToken)
                .Where(x => x.IsMissing);

            return entries;
        }

        public IEnumerable<LocalizationEntry> GetAllMissingTranslationsByCurrentCulture(string scope, in bool includeParentCultures, CancellationToken cancellationToken)
        {
            // FIXME: index IsMissing
            // FIXME: index Culture/Scope compound key
            // FIXME: use includeParentCultures

            var cultureName = CultureInfo.CurrentUICulture.Name.ToUpperInvariant();
            var entries = GetByKeyStruct<LocalizationEntry>(LocalizationKeyBuilder.LookupByCulture(cultureName), cancellationToken)
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

            var entries = GetByKeyStruct<LocalizationEntry>(LocalizationKeyBuilder.LookupByCulture(cultureName),
                cancellationToken);

            if (entries.Any(x => x.Culture.Equals(cultureName, StringComparison.OrdinalIgnoreCase) && x.Key.Equals(value.Name, StringComparison.OrdinalIgnoreCase)))
                return false; // already have a key for this culture
            
            return TryAppend(new LocalizationEntry(Guid.NewGuid(), cultureName, value), cancellationToken, _logger);
        }

        public bool MarkAsUnused(string key, CancellationToken cancellationToken)
        {
            // FIXME: implement
            return false;
        }
    }
}
