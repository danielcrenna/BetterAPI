// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Http;
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
            // We must first check if we have a terminal result
            var body = context.HttpContext.Items[Constants.TerminalObjectResultValue];
            if (body != default)
            {
                settable = false;
                return body;
            }

            // We can then check if there is a canonical result, so we don't need to deal
            // with modified results in unrelated middleware
            body = context.HttpContext.Items[Constants.CanonicalObjectResultValue];
            if (body != default)
            {
                settable = true;
                return body;
            }

            body = result.Value;
            if(body == default)
                throw new NullReferenceException("Could not locate expected result body");

            settable = true;
            return body;
        }

        /// <summary> Tests whether the current request is a GET request for a resource collection. </summary>
        public static bool IsCollectionQuery(this ActionContext context, out Type? underlyingType)
        {
            if (context.HttpContext.Request.Method != HttpMethods.Get)
            {
                underlyingType = null;
                return false;
            }

            if (context.ActionDescriptor.ReturnsEnumerableType(out underlyingType))
                return true;

            underlyingType = null;
            return false;
        }

        /// <summary> Tests whether the current request is a GET request for a resource. </summary>
        public static bool IsQuery(this ActionContext context, out Type? underlyingType)
        {
            if (context.HttpContext.Request.Method != HttpMethods.Get)
            {
                underlyingType = null;
                return false;
            }

            return context.ActionDescriptor.ReturnsType(out underlyingType);
        }
    }
}