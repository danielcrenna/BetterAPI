// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;

namespace BetterAPI
{
    /// <summary>
    /// Describes the contract required to fulfill API guidelines data operations against resources
    /// </summary>
    /// <typeparam name="T">The resource reference type</typeparam>
    public interface IResourceDataService<T> where T : class, IResource
    {
        IEnumerable<T> Get(CancellationToken cancellationToken);
        bool TryGetById(Guid id, out T? resource, CancellationToken cancellationToken);
        bool TryAdd(T model);
        bool TryDeleteById(Guid id, out T? deleted, out bool error);
    }
}