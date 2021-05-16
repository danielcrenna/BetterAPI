// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Data;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Operations
{
    public class ResourceDataServiceOperationStore : IOperationStore
    {
        private readonly IResourceDataService<Operation> _service;
        private readonly IStringLocalizer<ResourceDataServiceOperationStore> _localizer;
        private readonly ILogger<ResourceDataServiceOperationStore> _logger;

        public ResourceDataServiceOperationStore(
            IResourceDataService<Operation> service,
            IStringLocalizer<ResourceDataServiceOperationStore> localizer, 
            ILogger<ResourceDataServiceOperationStore> logger)
        {
            _service = service;
            _localizer = localizer;
            _logger = logger;
        }

        public Task<IEnumerable<Operation>> GetAsync(CancellationToken cancellationToken)
        {
            var query = new ResourceQuery();
            var operations = _service.Get(query, cancellationToken);
            return Task.FromResult(operations);
        }

        public Task<Operation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            if (_service.TryGetById(id, out var operation, cancellationToken))
                return Task.FromResult(operation);

            return Task.FromResult((Operation?) null);
        }
    }
}