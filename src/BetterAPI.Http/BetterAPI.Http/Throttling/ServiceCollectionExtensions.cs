// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.Http.Throttling
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Protects anonymous/public endpoints from abuse by enforcing a throttle.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAnonymousThrottle(this IServiceCollection services)
        {
            services.AddTimestamps();
            services.AddMemoryCache(o => { });
            services.TryAddSingleton<ThrottleFilter>();
            return services;
        }
    }
}