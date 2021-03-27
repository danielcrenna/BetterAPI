// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Mvc;

namespace BetterAPI.Extensions
{
    internal static class ActionContextExtensions
    {
        public static object GetResultBody(this ActionContext context, ObjectResult result)
        {
            return context.GetResultBody(result, out _);
        }

        public static object GetResultBody(this ActionContext context, ObjectResult result, out bool settable)
        {
            var body = result.Value;
            if (body != default)
            {
                settable = true;
                return body;
            }

            body = context.HttpContext.Items[Constants.ObjectResultValue] ??
                   throw new NullReferenceException("Could not locate expected result body");
            settable = false;
            return body;
        }
    }
}