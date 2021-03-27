// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Builder;

namespace BetterAPI.Guidelines
{
    internal sealed class AppBuilderServiceProvider : IServiceProvider
    {
        private readonly IApplicationBuilder _app;
        private readonly IServiceProvider _inner;

        public AppBuilderServiceProvider(IApplicationBuilder app, IServiceProvider inner)
        {
            _app = app;
            _inner = inner;
        }

        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(IApplicationBuilder) || serviceType == _app.GetType()
                ? _app
                : _inner.GetService(serviceType);
        }
    }
}