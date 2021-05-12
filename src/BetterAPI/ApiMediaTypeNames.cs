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
            public const string Json = MediaTypeNames.Application.Json;
            public const string Xml = MediaTypeNames.Application.Xml;

            public const string ProblemJson = "application/problem+json";
            public const string ProblemXml = "application/problem+json";

            public const string JsonMergePatch = "application/json+merge-patch";
            public const string XmlMergePatch = "application/xml+merge-patch";

            public const string JsonPatchJson = "application/json-patch+json";
            public const string JsonPatchXml = "application/json-patch+xml";
        }
    }
}