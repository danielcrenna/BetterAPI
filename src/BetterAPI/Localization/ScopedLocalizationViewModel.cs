// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Localization
{
    public class ScopedLocalizationViewModel
    {
        public string? Culture { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }

        // ReSharper disable once UnusedMember.Global (Serialization)
        public ScopedLocalizationViewModel() { }

        public ScopedLocalizationViewModel(string culture, string key, string? value)
        {
            Key = key;
            Value = value ?? key;
            Culture = culture;
        }
    }
}