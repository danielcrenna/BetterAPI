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

namespace BetterAPI.Operations.Internal
{
    internal static class TaskExtensions
    {
        private static readonly IDictionary<TaskScheduler, TaskFactory> TaskFactories =
            new ConcurrentDictionary<TaskScheduler, TaskFactory>();

        public static Task Run(this TaskScheduler scheduler, Action action, CancellationToken cancellationToken)
        {
            return WithTaskFactory(scheduler).StartNew(action, cancellationToken);
        }

        public static Task Run(this TaskScheduler scheduler, Func<Task> func, CancellationToken cancellationToken)
        {
            return WithTaskFactory(scheduler).StartNew(func, cancellationToken).Unwrap();
        }

        public static TaskFactory WithTaskFactory(this TaskScheduler scheduler)
        {
            if (!TaskFactories.TryGetValue(scheduler, out var tf))
                TaskFactories.Add(scheduler,
                    tf = new TaskFactory(CancellationToken.None, TaskCreationOptions.DenyChildAttach,
                        TaskContinuationOptions.None, scheduler));

            return tf;
        }
    }
}