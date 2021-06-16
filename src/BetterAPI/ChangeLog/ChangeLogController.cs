// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BetterAPI.Data;
using BetterAPI.Http;
using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.ChangeLog
{
    [InternalController]
    [Display(Name = "Changes", Description = "Provides operational changes declared in the application's change log.")]
    public class ChangeLogController : Controller
    {
        private readonly ChangeLogBuilder _changeLog;

        public ChangeLogController(ChangeLogBuilder changeLog)
        {
            _changeLog = changeLog;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var versions = _changeLog.Versions;

            var changes = new Dictionary<string, List<ResourceDataDistribution>>();

            foreach (var (version, manifest) in versions)
            {
                var key = version.ToString();

                foreach (var (name, type) in manifest)
                {
                    var service = HttpContext.RequestServices.GetService(typeof(IResourceDataService<>).MakeGenericType(type));
                    if (service == default || !(service is IResourceDataService data))
                        continue;

                    var revision = _changeLog.GetRevisionForResourceAndApiVersion(name, version);
                    var distribution = data.GetResourceDataDistribution(revision);
                    if (distribution == default || string.IsNullOrWhiteSpace(distribution.Partition))
                        continue;

                    if (!changes.TryGetValue(key, out var values))
                        changes.Add(key, values = new List<ResourceDataDistribution>());

                    values.Add(distribution);
                }
            }

            return Ok(new {Value = changes});
        }
    }
}