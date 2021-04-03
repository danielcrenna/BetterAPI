// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Net.Mime;

namespace BetterAPI
{
    public static class ApiMediaTypeNames
    {
        public static class Application
        {
            public static readonly string Json = MediaTypeNames.Application.Json;
            public static readonly string Xml = MediaTypeNames.Application.Xml;
            public static readonly string ProblemJson = "application/problem+json";
        }
    }
}