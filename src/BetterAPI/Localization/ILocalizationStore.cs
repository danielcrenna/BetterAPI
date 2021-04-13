// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    public interface ILocalizationStore
    {
        LocalizedString GetText(string scope, string name, CancellationToken cancellationToken, params object[] args);
        IEnumerable<LocalizationEntry> GetAllTranslations(in bool includeParentCultures, CancellationToken cancellationToken);
        IEnumerable<LocalizationEntry> GetAllMissingTranslations(in bool includeParentCultures, CancellationToken cancellationToken);
        IEnumerable<LocalizationEntry> GetAllMissingTranslations(string scope, in bool includeParentCultures, CancellationToken cancellationToken);
        bool TryAddMissingTranslation(string cultureName, LocalizedString value, CancellationToken cancellationToken);
    }
}