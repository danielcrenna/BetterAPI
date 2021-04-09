// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Localization
{
    public readonly struct LocalizationKey
    {
        public string Culture { get; }
        public string Key { get; }
        public string? Value { get; }
        public bool IsMissing { get; }

        public LocalizationKey(string culture, string key, string? value, bool missing)
        {
            Culture = culture;
            Key = key;
            Value = value;
            IsMissing = missing;
        }
    }
}