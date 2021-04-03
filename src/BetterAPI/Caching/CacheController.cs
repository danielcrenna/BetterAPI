// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Caching
{
    [Route("cache")]
    [DoNotHttpCache]
    // [ApiExplorerSettings(GroupName = "ops")]
    // [ApiController]
    public sealed class CacheController : Controller
    {
        private readonly ICacheManager _cacheManager;

        public CacheController(ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
        }

        [HttpOptions]
        public IActionResult GetUsageInfo()
        {
            return new ObjectResult(_cacheManager);
        }

        [HttpGet("keys")]
        public IActionResult GetKeys()
        {
            return new ObjectResult(_cacheManager.IntrospectKeys());
        }
    }
}