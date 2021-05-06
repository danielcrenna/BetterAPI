// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    public class MemoryLocalizationStore : ILocalizationStore
    {
        private readonly IList<LocalizationEntry> _resources;

        public MemoryLocalizationStore()
        {
            _resources = new List<LocalizationEntry>();
        }

        public LocalizedString GetText(string scope, string name, CancellationToken cancellationToken, params object[] args)
        {
            var culture = CultureInfo.CurrentUICulture;

            if (args.Length != 0)
            {
                var value = _resources.SingleOrDefault(r => r.Culture == culture.Name && r.Key == name).Value;
                var result = new LocalizedString(name, string.Format(value ?? name, args), value == null);
                return result;
            }
            else
            {
                var value = _resources.SingleOrDefault(r => r.Culture == culture.Name && r.Key == name).Value;
                var result = new LocalizedString(name, value ?? name, value == null);
                return result;
            }
        }

        public IEnumerable<LocalizationEntry> GetAllTranslations(CancellationToken cancellationToken)
        {
            return _resources.Where(x => !x.IsMissing);
        }

        public IEnumerable<LocalizationEntry> GetAllTranslationsByCurrentCulture(in bool includeParentCultures, CancellationToken cancellationToken)
        {
            return _resources.Where(x => !x.IsMissing && x.Culture.Equals(CultureInfo.CurrentUICulture.Name));
        }

        public IEnumerable<LocalizationEntry> GetAllMissingTranslationsByCurrentCulture(in bool includeParentCultures, CancellationToken cancellationToken)
        {
            return _resources.Where(x => x.IsMissing && x.Culture.Equals(CultureInfo.CurrentUICulture.Name));
        }

        public IEnumerable<LocalizationEntry> GetAllMissingTranslationsByCurrentCulture(string scope, in bool includeParentCultures, CancellationToken cancellationToken)
        {
            return _resources.Where(x => x.IsMissing && x.Scope.Equals(scope, StringComparison.OrdinalIgnoreCase) && x.Culture.Equals(CultureInfo.CurrentUICulture.Name));
        }

        public bool TryAddMissingTranslation(string cultureName, LocalizedString value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scope = value.SearchedLocation ?? string.Empty;
            
            if (!value.ResourceNotFound)
            {
                var message = GetText(scope, "Expecting a missing value, and this value was previously found.", cancellationToken);
                if (message.ResourceNotFound)
                    TryAddMissingTranslation(cultureName, message, cancellationToken);
                throw new InvalidOperationException(message);
            }

            var culture = CultureInfo.CurrentUICulture;
            if (_resources.Any(r => r.Culture == culture.Name && r.Key == value.Name))
                return false;

            _resources.Add(new LocalizationEntry(Guid.NewGuid(), culture.Name, value));
            return true;
        }

        public bool MarkAsUnused(string key, CancellationToken cancellationToken)
        {
            return false;
        }
    }
}