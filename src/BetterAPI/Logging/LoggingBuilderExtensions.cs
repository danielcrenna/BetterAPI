// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace BetterAPI.Logging
{
    public static class LoggingBuilderExtensions
    {
        public static ILoggingBuilder AddLightingDb(this ILoggingBuilder builder, Func<string> getPathFunc)
        {
            builder.AddConfiguration();
            builder.Services.AddSingleton<LightningLoggingStore>();
            builder.Services.AddSingleton<ILoggingStore>(r => r.GetRequiredService<LightningLoggingStore>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LightningLoggerProvider>(r =>
                new LightningLoggerProvider(r.GetRequiredService<LightningLoggingStore>(), getPathFunc)));
            return builder;
        }
    }
}