// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace BetterAPI.Logging
{
    public static class LoggingBuilderExtensions
    {
        public static ILoggingBuilder AddLightingDb(this ILoggingBuilder builder, string path)
        {
            builder.AddConfiguration();
            builder.Services.TryAddSingleton(r => new LightningLoggingStore(path));
            builder.Services.TryAddSingleton<ILoggingStore>(r => r.GetRequiredService<LightningLoggingStore>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LightningLoggerProvider>(r => new LightningLoggerProvider(r.GetRequiredService<LightningLoggingStore>())));
            return builder;
        }
    }
}