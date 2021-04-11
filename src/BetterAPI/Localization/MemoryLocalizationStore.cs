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

        public LocalizedString GetText(string name, params object[] args)
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

        public IEnumerable<LocalizedString> GetAllTranslations(in bool includeParentCultures)
        {
            return _resources.Where(x => !x.IsMissing).Select(r => new LocalizedString(r.Key, r.Value ?? r.Key, false));
        }

        public IEnumerable<LocalizedString> GetAllMissingTranslations(in bool includeParentCultures)
        {
            return _resources.Where(x => x.IsMissing).Select(r => new LocalizedString(r.Key, r.Value ?? r.Key, true));
        }

        public bool TryAddMissingTranslation(string cultureName, LocalizedString value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (!value.ResourceNotFound)
            {
                var message = GetText("Expecting a missing value, and this value was previously found.");
                if (message.ResourceNotFound)
                    TryAddMissingTranslation(cultureName, message, cancellationToken);
                throw new InvalidOperationException(message);
            }

            var culture = CultureInfo.CurrentUICulture;
            if (_resources.Any(r => r.Culture == culture.Name && r.Key == value.Name))
                return false;

            _resources.Add(new LocalizationEntry(Guid.NewGuid(), culture.Name, value.Name, value.Value, true));
            return true;
        }
    }
}