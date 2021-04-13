// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace BetterAPI.Testing
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection TryRemoveEnumerable<TService, TImplementation>(this IServiceCollection services)
        {
            var remove = new HashSet<ServiceDescriptor>();

            foreach(var descriptor in services.Where(x => x.ServiceType == typeof(TService) && x.ImplementationType == typeof(TImplementation)))
                remove.Add(descriptor);

            foreach (var descriptor in remove)
                services.Remove(descriptor);

            return services;
        }
    }
}