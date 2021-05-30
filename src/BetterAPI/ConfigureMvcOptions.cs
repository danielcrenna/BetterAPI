// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BetterAPI
{
    /// <summary>
    /// The convention itself must be in DI, it can't be constructed here, or it will never fire.
    /// This is because the usual IMvcBuilder configureAction creates a delegate, which will look for our convention in DI.
    /// If it's constructed here, it is never found and therefore never executed.
    /// 
    /// See: https://github.com/aspnet/Mvc/issues/6214#issuecomment-297880860
    /// </summary>
    internal sealed class ConfigureMvcOptions : IConfigureOptions<MvcOptions>
    {
        private readonly ApiGuidelinesConvention _convention;

        public ConfigureMvcOptions(ApiGuidelinesConvention convention)
        {
            _convention = convention;
        }

        public void Configure(MvcOptions options)
        {
            options.Conventions.Add(_convention);
        }
    }
}