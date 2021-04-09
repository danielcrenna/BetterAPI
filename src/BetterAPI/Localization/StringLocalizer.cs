// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    public class StringLocalizer : IStringLocalizer
    {
        private readonly ILocalizationStore _store;

        public StringLocalizer(ILocalizationStore store)
        {
            _store = store;
        }
        
        public LocalizedString this[string name] => _store.GetText(name);

        public LocalizedString this[string name, params object[] args] => _store.GetText(name, args);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return _store.GetAllTranslations(includeParentCultures);
        }
    }
}