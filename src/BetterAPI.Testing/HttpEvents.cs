// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;

namespace BetterAPI.Testing
{
    public static class HttpEvents
    {
        public static readonly EventId MatchedMock = new EventId(1, "RequestMatchedMock");
        public static readonly EventId UnmatchedMock = new EventId(2, "RequestUnmatchedMock");
    }
}