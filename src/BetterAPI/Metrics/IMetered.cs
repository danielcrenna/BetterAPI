// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

namespace BetterAPI.Metrics
{
    public interface IMetered
    {
        /// <summary>
        ///     Returns the meter's rate unit
        /// </summary>
        /// <returns></returns>
        TimeUnit RateUnit { get; }

        /// <summary>
        ///     Returns the type of events the meter is measuring
        /// </summary>
        /// <returns></returns>
        string EventType { get; }

        /// <summary>
        ///     Returns the number of events which have been marked
        /// </summary>
        /// <returns></returns>
        long Count { get; }

        /// <summary>
        ///     Returns the fifteen-minute exponentially-weighted moving average rate at
        ///     which events have occured since the meter was created
        ///     <remarks>
        ///         This rate has the same exponential decay factor as the fifteen-minute load
        ///         average in the top Unix command.
        ///     </remarks>
        /// </summary>
        double FifteenMinuteRate { get; }

        /// <summary>
        ///     Returns the five-minute exponentially-weighted moving average rate at
        ///     which events have occured since the meter was created
        ///     <remarks>
        ///         This rate has the same exponential decay factor as the five-minute load
        ///         average in the top Unix command.
        ///     </remarks>
        /// </summary>
        double FiveMinuteRate { get; }

        /// <summary>
        ///     Returns the mean rate at which events have occured since the meter was created
        /// </summary>
        double MeanRate { get; }

        /// <summary>
        ///     Returns the one-minute exponentially-weighted moving average rate at
        ///     which events have occured since the meter was created
        ///     <remarks>
        ///         This rate has the same exponential decay factor as the one-minute load
        ///         average in the top Unix command.
        ///     </remarks>
        /// </summary>
        /// <returns></returns>
        double OneMinuteRate { get; }
    }
}