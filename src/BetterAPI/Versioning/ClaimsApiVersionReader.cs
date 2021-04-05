// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace BetterAPI.Versioning
{
    internal sealed class ClaimsApiVersionReader : IApiVersionReader
    {
        public void AddParameters(IApiVersionParameterDescriptionContext context)
        {
            
        }

        public string? Read(HttpRequest request)
        {
            return null;
        }
    }
}