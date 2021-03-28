// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Enveloping
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEnveloping(this IServiceCollection services)
        {
            services.AddScoped<EnvelopeActionFilter>();
            services.AddMvc(o => { o.Filters.AddService<EnvelopeActionFilter>(int.MinValue); });
            return services;
        }
    }
}