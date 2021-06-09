// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///     Adds timestamp generation for services that require them. The default implementation is the local server's wall time.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddTimestamps(this IServiceCollection services)
        {
            services.TryAdd(ServiceDescriptor.Singleton<Func<DateTimeOffset>>(r => () => DateTimeOffset.Now));
            return services;
        }
    }
}
