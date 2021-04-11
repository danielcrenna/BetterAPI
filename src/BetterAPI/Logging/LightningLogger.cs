// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Logging
{
    internal sealed class LightningLogger : ILogger
    {
        private readonly LightningLoggingStore _store;

        public LightningLogger(LightningLoggingStore store)
        {
            _store = store;
        }

        public IDisposable? BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            _store.Append(logLevel, eventId, state, exception, formatter);
        }
    }
}