// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BetterAPI
{
    internal sealed class ConfigureMvcOptions : IConfigureOptions<MvcOptions>
    {
        private readonly IOptions<ApiOptions> _options;

        public ConfigureMvcOptions(IOptions<ApiOptions> options)
        {
            _options = options;
        }

        public void Configure(MvcOptions options)
        {
            options.Conventions.Add(new ApiGuidelinesConventions(_options));
        }
    }
}