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
            // default: https://tools.ietf.org/html/rfc7231#section-6.5.1
            options.ClientErrorMapping[(int)HttpStatusCode.BadRequest] = new ClientErrorData
            {
                Title = "Bad Request",
                Link = $"{_options.Value.BaseUrl}{(int)HttpStatusCode.BadRequest}"
            };

            // default: "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            options.ClientErrorMapping[(int)HttpStatusCode.NotFound] = new ClientErrorData
            {
                Title = "Not Found",
                Link = $"{_options.Value.BaseUrl}{(int)HttpStatusCode.NotFound}"
            };
        }
    }
}