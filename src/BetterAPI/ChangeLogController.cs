// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace BetterAPI
{
    [Route("api/changelog")]
    public class ChangeLogController : Controller
    {
        private readonly ChangeLogBuilder _builder;

        public ChangeLogController(ChangeLogBuilder builder)
        {
            _builder = builder;
        }

        public IActionResult GetChangeLog()
        {
            var changes = _builder.Versions.ToDictionary(kv => kv.Key.ToString(), kv => (IEnumerable<string>) kv.Value.Keys);
            return Ok(new {Value = changes});
        }
    }
}