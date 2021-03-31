// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BetterAPI
{
    /// <summary>
    /// See: https://docs.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-5.0#use-apibehavioroptionsclienterrormapping
    /// </summary>
    internal sealed class ConfigureApiBehaviorOptions : IConfigureOptions<ApiBehaviorOptions>
    {
        private readonly IOptions<ProblemDetailsOptions> _options;

        public ConfigureApiBehaviorOptions(IOptions<ProblemDetailsOptions> options)
        {
            _options = options;
        }

        public void Configure(ApiBehaviorOptions options)
        {
            const int statusCode = (int)HttpStatusCode.BadRequest;

            options.ClientErrorMapping[statusCode] = new ClientErrorData
            {
                Title = "Bad Request",
                Link = $"{_options.Value.BaseUrl}{statusCode}"
            };
        }
    }
}