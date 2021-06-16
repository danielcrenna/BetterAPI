// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BetterAPI.DeltaQueries;

namespace BetterAPI.Events
{
    /// <summary>
    /// Keeps delta queries up to date when resources change.
    /// </summary>
    public sealed class DeltaQueryResourceEventBroadcaster : IResourceEventBroadcaster
    {
        private readonly IDeltaQueryStore _deltas;

        public DeltaQueryResourceEventBroadcaster(IDeltaQueryStore deltas)
        {
            _deltas = deltas;
        }

        public void Created<T>(T resource) where T : IResource
        {
            _deltas.TryPushAdd(resource);
        }

        public void Updated<T>(T resource) where T : IResource
        {
            throw new System.NotImplementedException();
        }
    }
}