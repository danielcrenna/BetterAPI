﻿// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace BetterAPI.Guidelines.Caching
{
    public sealed class FileCacheDependency : ICacheDependency
    {
        private readonly List<string> _filters = new List<string>();
        private readonly IFileProvider _provider;

        /// <param name="fileName">
        ///     The fully qualified name of the file, or the relative file name. Do not end the path with the
        ///     directory separator character.
        /// </param>
        public FileCacheDependency(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            _provider = new PhysicalFileProvider(fileInfo.DirectoryName);
            _filters.Add(fileName);
        }

        /// <param name="rootDirectory">The root directory. This should be an absolute path.</param>
        /// <param name="exclusionFilters">Specifies which files or directories are excluded.</param>
        /// <param name="filters">
        ///     Filter strings used to determine what files or folders to monitor. Example: **/*.cs, *.*,
        ///     subFolder/**/*.cshtml.
        /// </param>
        public FileCacheDependency(string rootDirectory, ExclusionFilters exclusionFilters = ExclusionFilters.Sensitive,
            params string[] filters)
        {
            _provider = new PhysicalFileProvider(rootDirectory, exclusionFilters);
            _filters.AddRange(filters);
        }

        public FileCacheDependency(IFileProvider provider, params string[] filters)
        {
            _provider = provider;
            _filters.AddRange(filters);
        }

        public IChangeToken GetChangeToken()
        {
            switch (_filters.Count)
            {
                case 0:
                    return NullChangeToken.Singleton;
                case 1:
                    return _provider.Watch(_filters[0]);
                default:
                    return new CompositeChangeToken(_filters.Select(f => _provider.Watch(f)).ToList());
            }
        }

        public void Dispose() { }
    }
}