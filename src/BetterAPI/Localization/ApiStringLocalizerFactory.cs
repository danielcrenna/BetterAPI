// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    internal sealed class ApiStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly ILocalizationStore _store;
        private readonly IHttpContextAccessor _accessor;

        public ApiStringLocalizerFactory(ILocalizationStore store, IHttpContextAccessor accessor)
        {
            _store = store;
            _accessor = accessor;
        }

        public IStringLocalizer Create(Type resourceSource)
            => Create(resourceSource.Namespace ?? string.Empty, resourceSource.Name);

        public IStringLocalizer Create(string baseName, string location)
            => new ApiStringLocalizer(_store, _accessor, location);
    }
}