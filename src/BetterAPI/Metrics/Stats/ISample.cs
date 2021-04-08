// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;

namespace BetterAPI.Metrics.Stats
{
    /// <summary>
    ///     A statistically representative sample of a data stream
    /// </summary>
    public interface ISample
    {
        /// <summary>
        ///     Returns the number of values recorded
        /// </summary>
        int Count { get; }

        /// <summary>
        ///     Returns a copy of the sample's values
        /// </summary>
        ICollection<long> Values { get; }

        /// <summary>
        ///     Clears all recorded values
        /// </summary>
        void Clear();

        /// <summary>
        ///     Adds a new recorded value to the sample
        /// </summary>
        void Update(long value);
    }
}