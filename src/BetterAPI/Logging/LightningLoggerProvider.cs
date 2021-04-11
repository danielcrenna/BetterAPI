// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Logging
{
    internal sealed class LightningLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, LightningLogger> _loggers =
            new ConcurrentDictionary<string, LightningLogger>();

        private readonly LightningLoggingStore _store;

        public LightningLoggerProvider(LightningLoggingStore store)
        {
            _store = store;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new LightningLogger(_store));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}