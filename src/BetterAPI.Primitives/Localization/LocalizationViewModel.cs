// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Localization
{
    public class LocalizationViewModel : ScopedLocalizationViewModel
    {
        public string? Scope { get; set; }

        // ReSharper disable once UnusedMember.Global (Serialization)
        public LocalizationViewModel() { }

        public LocalizationViewModel(string scope, ScopedLocalizationViewModel model)
        {
            Scope = scope;
            Key = model.Key;
            Value = model.Value ?? model.Key;
            Culture = model.Culture;
        }

        public LocalizationViewModel(string culture, string scope, string key, string? value)
        {
            Key = key;
            Scope = scope;
            Value = value ?? key;
            Culture = culture;
        }
    }
}