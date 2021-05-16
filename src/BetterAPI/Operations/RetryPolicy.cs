// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using BetterAPI.Operations.Internal;

namespace BetterAPI.Operations
{
    /// <summary>
    ///     A retry policy, for use with <see cref="PushQueue{T}" />
    /// </summary>
    public class RetryPolicy
    {
        private readonly IDictionary<int, RetryDecision> _rules;
        private RetryDecision _default = RetryDecision.Requeue;

        public RetryPolicy()
        {
            _rules = new Dictionary<int, RetryDecision>();
            RequeueInterval = a => TimeSpan.FromSeconds(5 + Math.Pow(a, 4));
        }

        public Func<int, TimeSpan> RequeueInterval { get; set; }

        public void Default(RetryDecision action)
        {
            _default = action;
        }

        public void After(int tries, RetryDecision action)
        {
            _rules.Add(tries, action);
        }

        public void Clear()
        {
            _rules.Clear();
        }

        public RetryDecision DecideOn<T>(T @event, int attempts)
        {
            foreach (var threshold in _rules.Keys.OrderBy(k => k).Where(threshold => attempts >= threshold))
            {
                return _rules[threshold];
            }

            return _default;
        }
    }
}