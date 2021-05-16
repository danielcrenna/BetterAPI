// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Serialization
{
    public interface IObjectDeserializer
    {
        object BufferToObject(ReadOnlySpan<byte> buffer, Type type, ITypeResolver typeResolver,
            IReadObjectSink emitter = null);

        T BufferToObject<T>(ReadOnlySpan<byte> buffer, ITypeResolver typeResolver, IReadObjectSink emitter = null);
    }
}