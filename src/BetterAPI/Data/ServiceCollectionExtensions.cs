// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Reflection;
using BetterAPI.ChangeLog;
using BetterAPI.Data.Sqlite;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Data
{
    internal static class ServiceCollectionExtensions
    {
        private static readonly MethodInfo? AddResourceStoreMethod;

        static ServiceCollectionExtensions()
        {
            AddResourceStoreMethod = typeof(ServiceCollectionExtensions).GetMethod(nameof(AddResourceStoreImpl),
                BindingFlags.NonPublic | BindingFlags.Static) ?? throw new NullReferenceException();
        }

        /// <summary>
        /// Create a resource-based sub-system for operations, backed by SQLite.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="revision"></param>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public static IServiceCollection AddResourceStore<T>(this IServiceCollection services, int revision = 1, string? collectionName = default) where T : class, IResource
        {
            return AddResourceStoreImpl<T>(services, revision, collectionName!);
        }

        private static IServiceCollection AddResourceStoreImpl<T>(IServiceCollection services, int revision,
            string collectionName) where T : class, IResource
        {
            // FIXME: switch to LMDB / configurable backing stores
            services.AddSqliteResource<T>(revision, collectionName);
            return services;
        }

        internal static IServiceCollection AddResourceStoreInternal(this IServiceCollection services, Type type, int revision = 1, string? collectionName = default)
        {
            var methodToCall = AddResourceStoreMethod?.MakeGenericMethod(type);
            methodToCall?.Invoke(null, new object[] { services, revision, collectionName!});
            return services;
        }

        /// <summary>
        /// Create a resource-based sub-system for operations, backed by SQLite.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="services"></param>
        /// <param name="revision"></param>
        /// <param name="collectionName"></param>
        /// <returns></returns>
        public static IServiceCollection AddSqliteResource<T>(this IServiceCollection services, int revision = 1, string? collectionName = default) where T : class, IResource
        {
            collectionName ??= typeof(T).Name.Pluralize().ToLowerInvariant();
            services.TryAddSingleton(r => new SqliteResourceDataService<T>($"{collectionName}.db", revision, r.GetRequiredService<ChangeLogBuilder>(), r.GetRequiredService<IStringLocalizer<SqliteResourceDataService<T>>>(), r.GetRequiredService<ILogger<SqliteResourceDataService<T>>>()));
            services.TryAddSingleton<IResourceDataService<T>>(r => r.GetRequiredService<SqliteResourceDataService<T>>());
            services.TryAddTransient<IResourceDataService>(r => r.GetRequiredService<SqliteResourceDataService<T>>());
            return services;
        }
    }
}
