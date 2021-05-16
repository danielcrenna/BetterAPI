// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace BetterAPI.Operations
{
    public sealed class OperationsHostedService : IHostedService
    {
        private readonly OperationsHost _host;

        public OperationsHostedService(OperationsHost host)
        {
            _host = host;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _host.Start(false, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _host.Stop(false, cancellationToken);
            return Task.CompletedTask;
        }
    }
}