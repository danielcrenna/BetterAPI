// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.Http.RemoteAddress
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRemoteAddressFilter(this IServiceCollection services)
        {
            services.AddTimestamps();
            services.AddCacheRegions();
            services.TryAddSingleton<RemoteAddressFilter>();
            return services;
        }
    }
}