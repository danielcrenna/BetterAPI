// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Text.Json;

namespace BetterAPI.Shaping
{
    public interface IShaped
    {
        object? Body { get; }
        IList<string> Fields { get; }
        void WriteInner(Utf8JsonWriter writer, IShaped value, JsonSerializerOptions options);
    }
}