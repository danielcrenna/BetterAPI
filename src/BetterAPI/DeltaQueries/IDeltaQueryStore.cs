// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;

namespace BetterAPI.DeltaQueries
{
    public interface IDeltaQueryStore
    {
        string? BuildDeltaLinkForQuery(Type context);
        bool TryGetDelta<T>(string deltaLink, out IEnumerable<Delta<T>>? changes);
        void TryPushAdd<T>(T model);
    }
}