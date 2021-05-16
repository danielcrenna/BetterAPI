// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using BetterAPI.Operations.Internal;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterAPI.Operations
{
    public sealed class OperationsHost : IDisposable
    {
        private readonly IStringLocalizer<OperationsHost> _localizer;
        private readonly IOptionsMonitor<OperationOptions> _options;
        private readonly ILogger<OperationsHost> _logger;

        private readonly ConcurrentDictionary<int, TaskScheduler> _schedulers;
        private readonly ConcurrentDictionary<TaskScheduler, TaskFactory> _factories;
        
        private PushQueue<IEnumerable<Operation>>? _background;
        private CancellationTokenSource? _cancel;
        private PushQueue<IEnumerable<Operation>>? _maintenance;
        private QueuedTaskScheduler? _scheduler;

        public OperationsHost(IStringLocalizer<OperationsHost> localizer, IOptionsMonitor<OperationOptions> options, ILogger<OperationsHost> logger)
        {
            _localizer = localizer;
            _options = options;
            _logger = logger;
            _cancel = new CancellationTokenSource();
            _scheduler ??= new QueuedTaskScheduler(ResolveConcurrency());

            _schedulers = new ConcurrentDictionary<int, TaskScheduler>();
            _factories = new ConcurrentDictionary<TaskScheduler, TaskFactory>();
            _cancel = new CancellationTokenSource();

            // dispatch thread
            _background = new PushQueue<IEnumerable<Operation>>();
            //_background.Attach(WithPendingTasks);
            //_background.AttachBacklog(WithOverflowTasks);
            //_background.AttachUndeliverable(WithFailedTasks);

            // maintenance thread
            _maintenance = new PushQueue<IEnumerable<Operation>>();
            //_maintenance.Attach(WithHangingTasks);
            //_maintenance.AttachBacklog(WithHangingTasks);
            //_maintenance.AttachUndeliverable(WithFailedTasks);

            options.OnChange(OnOptionsChanged);
        }

        // ReSharper disable once EmptyDestructor
        ~OperationsHost()
        {

        }

        private int ResolveConcurrency()
        {
            return _options.CurrentValue.Concurrency == 0
                ? Environment.ProcessorCount
                : _options.CurrentValue.Concurrency;
        }
        
        public void Start(bool immediate, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            _logger.LogInformation(_localizer.GetString("Starting operations host"));

            _scheduler ??= new QueuedTaskScheduler(ResolveConcurrency());

            //_background.Produce(EnqueueTasks, TimeSpan.FromSeconds(_options.CurrentValue.SleepIntervalSeconds));
            //_background.Start(immediate);

            //_maintenance.Produce(HangingTasks, TimeSpan.FromMinutes(1));
            //_maintenance.Start(immediate);
        }

        public void Stop(bool immediate, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(_localizer.GetString("Stopping operations host"));
            
            _scheduler?.Dispose();
            _scheduler = null;

            _background?.Stop(immediate);
            _maintenance?.Stop(immediate);
        }

        private void OnOptionsChanged(OperationOptions changed)
        {
            _logger.LogInformation(_localizer.GetString("Operation options changed, recycling the host."));
            Stop(false);
            Start(false);
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (_cancel != null)
            {
                _cancel.Cancel();
                _cancel.Token.WaitHandle.WaitOne();
                _cancel.Dispose();
                _cancel = null;
            }

            _factories.Clear();
            _schedulers.Clear();

            _scheduler?.Dispose();
            _scheduler = null;

            _background?.Dispose();
            _background = null;

            _maintenance?.Dispose();
            _maintenance = null;
        }
    }
}