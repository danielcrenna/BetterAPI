// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    public class StringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly ILocalizationStore _store;

        public StringLocalizerFactory(ILocalizationStore store)
        {
            _store = store;
        }

        public IStringLocalizer Create(Type resourceSource)
            => new StringLocalizer(_store);

        public IStringLocalizer Create(string baseName, string location)
            => new StringLocalizer(_store);
    }
}