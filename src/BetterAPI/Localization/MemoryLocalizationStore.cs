// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    public class MemoryLocalizationStore : ILocalizationStore
    {
        private readonly IList<LocalizationKey> _resources;

        public MemoryLocalizationStore()
        {
            _resources = new List<LocalizationKey>();
        }

        public LocalizedString GetText(string name, params object[] args)
        {
            var culture = CultureInfo.CurrentUICulture;

            if (args.Length != 0)
            {
                var value = _resources.SingleOrDefault(r => r.Culture == culture.Name && r.Key == name).Value;
                var result = new LocalizedString(name, string.Format(value ?? name, args), value == null);
                if (result.ResourceNotFound)
                    TryAddMissingTranslation(result);
                return result;
            }
            else
            {
                var value = _resources.SingleOrDefault(r => r.Culture == culture.Name && r.Key == name).Value;
                var result = new LocalizedString(name, value ?? name, value == null);
                if (result.ResourceNotFound)
                    TryAddMissingTranslation(result);
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

        public bool TryAddMissingTranslation(LocalizedString value)
        {
            // FIXME: this exception message is technically not being translated due to a circular dependency
            if(!value.ResourceNotFound)
                throw new InvalidOperationException("Expecting a missing value, and this value was previously found.");

            var culture = CultureInfo.CurrentUICulture;
            if (_resources.Any(r => r.Culture == culture.Name && r.Key == value.Name))
                return false;

            _resources.Add(new LocalizationKey(culture.Name, value.Name, value.Value, true));
            return true;
        }
    }
}