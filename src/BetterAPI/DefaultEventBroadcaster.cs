// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.DeltaQueries;

namespace BetterAPI
{
    public sealed class DefaultEventBroadcaster : IEventBroadcaster
    {
        private readonly IDeltaQueryStore _deltas;

        public DefaultEventBroadcaster(IDeltaQueryStore deltas)
        {
            _deltas = deltas;
        }

        public void Created<T>(T model)
        {
            _deltas.TryPushAdd<T>(model);
        }
    }
}