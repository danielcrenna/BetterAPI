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
        LocalizedString GetText(string name, params object[] args);
        IEnumerable<LocalizedString> GetAllTranslations(in bool includeParentCultures);
        IEnumerable<LocalizedString> GetAllMissingTranslations(in bool includeParentCultures);
        bool TryAddMissingTranslation(string cultureName, LocalizedString value, CancellationToken cancellationToken);
    }
}