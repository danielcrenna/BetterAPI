// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using BetterAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI
{
    public sealed class ChangeLogBuilder
    {
        public IServiceCollection Services { get; }

        private readonly Dictionary<string, Type> _pendingTypes;
        private readonly IDictionary<ApiVersion, Dictionary<string, Type>> _versions;

        internal ISet<Type>? ResourceTypes { get; private set; }

        public ChangeLogBuilder(IServiceCollection services)
        {
            _pendingTypes = new Dictionary<string, Type>();
            _versions = new Dictionary<ApiVersion, Dictionary<string, Type>>();
            Services = services;
        }

        public ChangeLogBuilder AddResource<T>(string? name = default) where T : class, IResource
        {
            name ??= typeof(T).Name;
            _pendingTypes[name] = typeof(T);

            Services.TryAddSingleton(r => new SqliteResourceDataService<T>("resources.db", 1, r.GetRequiredService<IStringLocalizer<SqliteResourceDataService<T>>>(), r.GetRequiredService<ILogger<SqliteResourceDataService<T>>>()));
            Services.TryAddSingleton<IResourceDataService<T>>(r => r.GetRequiredService<SqliteResourceDataService<T>>());
            Services.TryAddTransient<IResourceDataService>(r => r.GetRequiredService<SqliteResourceDataService<T>>());

            return this;
        }

        public ChangeLogBuilder ShipVersion(ApiVersion version)
        {
            if(!_versions.TryGetValue(version, out var manifest))
                _versions.Add(version, manifest = new Dictionary<string, Type>());
            foreach (var (k, v) in _pendingTypes)
                manifest.Add(k, v);
            _pendingTypes.Clear();
            return this;
        }
        
        public void Build()
        {
            if (_versions.Count == 0)
                ShipVersion(ApiVersion.Default);
            ResourceTypes ??= _versions.SelectMany(x => x.Value.Values).Distinct().ToHashSet();
        }

        internal bool TryGetResourceName(Type type, out string? name)
        {
            foreach (var version in _versions)
            {
                foreach (var (key, value) in version.Value)
                {
                    if (value != type)
                        continue;
                    name = key;
                    return true;
                }
            }

            name = default;
            return false;
        }
    }
}