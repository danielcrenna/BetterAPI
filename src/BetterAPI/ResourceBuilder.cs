// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using BetterAPI.Data;
using BetterAPI.Paging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterAPI
{
    public sealed class ChangeLogBuilder
    {
        private readonly string _resourceName;

        public IServiceCollection Services { get; }

        private readonly IDictionary<ApiVersion, List<Type>> _versions;

        public ChangeLogBuilder(string resourceName, IServiceCollection services)
        {
            _resourceName = resourceName;
            _versions = new Dictionary<ApiVersion, List<Type>>();
            Services = services;
        }

        public ChangeLogBuilder Add<T>() where T : class, IResource
        {
            //Services.TryAddSingleton<MemoryResourceDataService<T>>();
            //Services.TryAddSingleton<IResourceDataService<T>>(r => r.GetRequiredService<MemoryResourceDataService<T>>());

            Services.TryAddSingleton(r => new SqliteResourceDataService<T>("resources.db", 1,  r.GetRequiredService<ILogger<SqliteResourceDataService<T>>>()));
            Services.TryAddSingleton<IResourceDataService<T>>(r => r.GetRequiredService<SqliteResourceDataService<T>>());
            return this;
        }

        public ChangeLogBuilder ShipVersion(ApiVersion version)
        {
            if(!_versions.TryGetValue(version, out var list))
                _versions.Add(version, list = new List<Type>());

            return this;
        }

        public void Build()
        {
            if (_versions.Count == 0)
                ShipVersion(ApiVersion.Default);
        }
    }
}