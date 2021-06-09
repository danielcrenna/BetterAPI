// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.Cryptography;
using BetterAPI.Data;
using BetterAPI.Http.Throttling;
using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Identity
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiIdentity(this IServiceCollection services)
        {
            Crypto.Initialize();
            services.AddAnonymousThrottle();
            services.AddResourceStore<User>(1);
            return services;
        }
    }
}
