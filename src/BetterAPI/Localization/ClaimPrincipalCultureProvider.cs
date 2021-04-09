// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Localization;

namespace BetterAPI.Localization
{
    internal sealed class ClaimPrincipalCultureProvider : RequestCultureProvider
    {
        public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        {
            // see: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/request-features?view=aspnetcore-5.0
            var feature = httpContext.Features.Get<IHttpAuthenticationFeature>();

            // ReSharper disable once ConstantConditionalAccessQualifier (it lies!)
            if(feature?.User == null)
                return Task.FromResult(null as ProviderCultureResult);

            var claim = feature.User.FindFirst(x => x.Type == ClaimTypes.Locality);
            if (claim == null)
                return Task.FromResult(null as ProviderCultureResult);
            
            // FIXME: we probably should support multiple claims, and/or use custom claim names
            var culture = claim.Value;
            var uiCulture = claim.Value;
            
            return Task.FromResult(new ProviderCultureResult(culture, uiCulture))!;
        }
    }
}