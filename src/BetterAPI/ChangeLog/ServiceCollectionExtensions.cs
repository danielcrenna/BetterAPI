// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.ChangeLog
{
    public static class ServiceCollectionExtensions
    {
        private static ChangeLogBuilder? _changeLogBuilder;

        public static ChangeLogBuilder GetChangeLog(this IServiceCollection services)
        {
            _changeLogBuilder ??= new ChangeLogBuilder(services);
            return _changeLogBuilder;
        }

        public static ChangeLogBuilder AddChangeLog(this IServiceCollection services, Action<ChangeLogBuilder>? builderAction = default)
        {
            var builder = GetChangeLog(services);
            services.TryAddSingleton(builder);
            builderAction?.Invoke(builder);
            builder.Build();
            return builder;
        }
    }
}