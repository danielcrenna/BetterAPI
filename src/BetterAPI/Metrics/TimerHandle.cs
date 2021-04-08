// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;

namespace BetterAPI.Metrics
{
    /// <summary>
    ///     Wraps a timing closure with additional data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TimerHandle<T>
    {
        private readonly Stopwatch _stopwatch;

        public TimerHandle(Stopwatch stopwatch)
        {
            _stopwatch = stopwatch;
            StartedAt = default;
            StoppedAt = default;
            Value = default;
        }

        public DateTimeOffset? StartedAt { get; private set; }
        public DateTimeOffset? StoppedAt { get; private set; }
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public bool IsStarted => StartedAt.HasValue;
        public bool IsStopped => StoppedAt.HasValue;
        public bool IsRunning => IsStarted && !IsStopped;

        public T Value { get; set; }

        public void Start()
        {
            _stopwatch.Start();
            StartedAt = DateTimeOffset.UtcNow;
        }

        public void Stop()
        {
            _stopwatch.Stop();
            StoppedAt = DateTimeOffset.UtcNow;
        }

        public static implicit operator T(TimerHandle<T> h)
        {
            return h.Value;
        }
    }
}