// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Http.Interception;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Events
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventServices(this IServiceCollection services, IWebHostEnvironment environment)
        {
            services.AddRequestInterception(o => o.Enabled = true);

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceEventBroadcaster, DeltaQueryResourceEventBroadcaster>());

            if (environment.IsDevelopment())
                services.TryAddEnumerable(ServiceDescriptor.Singleton<IRequestEventBroadcaster, LogRequestEventBroadcaster>(r => new LogRequestEventBroadcaster(
                    r.GetRequiredService<IStringLocalizer<LogRequestEventBroadcaster>>(), LogLevel.Information)));

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IRequestEventBroadcaster, SnapshotRequestEventBroadcaster>());

            return services;
        }
    }
}
