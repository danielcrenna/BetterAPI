// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace BetterAPI
{
    public sealed class MemoryResourceDataService<T> : IResourceDataService<T> where T : class, IResource
    {
        private readonly IDictionary<Guid, T> _store =
            new ConcurrentDictionary<Guid, T>();

        private readonly IDictionary<Guid, T> _deleted =
            new ConcurrentDictionary<Guid, T>();

        public IEnumerable<T> Get(CancellationToken cancellationToken)
        {
            return _store.Values;
        }

        public bool TryGetById(Guid id, out T? model, CancellationToken cancellationToken)
        {
            if (!_store.TryGetValue(id, out var stored))
            {
                model = default;
                return false;
            }

            model = stored;
            return true;
        }

        public bool TryAdd(T model)
        {
            var added = _store.TryAdd(model.Id, model);
            if (added && _deleted.ContainsKey(model.Id))
                _deleted.Remove(model.Id);
            return added;
        }

        public bool TryDeleteById(Guid id, out T? deleted, out bool error)
        {
            if (_deleted.TryGetValue(id, out deleted))
            {
                error = false;
                return false; // Gone
            }

            if (!_store.TryGetValue(id, out var toDelete))
            {
                deleted = default;
                error = false;
                return false; // NotFound
            }

            if (!_store.Remove(id))
            {
                deleted = default;
                error = true;
                return false; // InternalServerError
            }

            _deleted.Add(id, toDelete);
            deleted = toDelete;
            error = false;
            return true;
        }
    }
}