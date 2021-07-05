// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;

namespace BetterAPI.Data
{
    /// <summary>
    /// Describes the contract required to fulfill API guidelines data operations against resources.
    /// </summary>
    public interface IResourceDataService
    {
        bool SupportsSorting => false;
        bool SupportsFiltering => false;
        bool SupportsMaxPageSize  => false;
        bool SupportsCount => false;
        bool SupportsSkip => false;
        bool SupportsTop => false;
        bool SupportsSince => false;
        bool SupportsSearch => false;
        bool SupportsShaping => false;

        ResourceDataDistribution? GetResourceDataDistribution(int revision);
    }

    /// <summary>
    /// Describes the contract required to fulfill API guidelines data operations against resources.
    /// </summary>
    /// <typeparam name="T">The resource reference type</typeparam>
    public interface IResourceDataService<T> : IResourceDataService 
        where T : class, IResource
    {
        IEnumerable<T> Get(ResourceQuery query, CancellationToken cancellationToken);

        bool TryGetById(Guid id, out T? resource, out bool error, List<string>? fields, bool includeDeleted, CancellationToken cancellationToken);
        bool TryAdd(T model, out bool error, CancellationToken cancellationToken);
        bool TryUpdate(T previous, T next, out bool error, CancellationToken cancellationToken);
        bool TryDeleteById(Guid id, out T? deleted, out bool error, CancellationToken cancellationToken);
    }
}