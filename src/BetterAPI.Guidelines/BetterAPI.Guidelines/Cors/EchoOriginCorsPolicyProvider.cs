// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace BetterAPI.Guidelines.Cors
{
    internal sealed class EchoOriginCorsPolicyProvider : ICorsPolicyProvider
    {
        private readonly ICorsPolicyProvider _inner;

        public EchoOriginCorsPolicyProvider(ICorsPolicyProvider inner)
        {
            _inner = inner;
        }

        public async Task<CorsPolicy> GetPolicyAsync(HttpContext context, string policyName)
        {
            var host = context.Request.Host.Host;
            var policy = await _inner.GetPolicyAsync(context, policyName);
            policy.Origins.Clear();
            policy.Origins.Add(host);
            return policy;
        }
    }
}