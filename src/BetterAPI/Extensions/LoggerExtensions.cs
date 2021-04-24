// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Extensions
{
    /// <summary>
    /// Extension methods that prevent a logger from inadvertently evaluating its message before determining if the logger
    /// is present and enabled at the specified log level.
    ///
    /// High volumes of logs that won't reach the log writer can put tremendous pressure on the GC, and can cause
    /// service interruptions.
    ///
    /// See: https://docs.microsoft.com/en-us/dotnet/core/diagnostics/logging-tracing#performance-considerations
    /// </summary>
    internal static class LoggerExtensions
    {
        #region Debug

        public static void LogDebug(this ILogger? logger, Func<string> message, params Func<object>[] args)
        {
            logger?.SafeLog(LogLevel.Debug, message, args);
        }

        public static void LogDebug(this ILogger? logger, Func<string> message)
        {
            logger?.SafeLog(LogLevel.Debug, message);
        }

        public static void LogDebug(this ILogger? logger, Func<string> message, Exception exception)
        {
            logger?.SafeLog(LogLevel.Debug, message, exception);
        }

        public static void LogDebug(this ILogger? logger, Func<string> message, Exception exception, params Func<object>[] args)
        {
            logger?.SafeLog(LogLevel.Debug, message, exception, args);
        }

        #endregion

        #region Trace

        public static void LogTrace(this ILogger? logger, Func<string> message, params Func<object>[] args)
        {
            logger?.SafeLog(LogLevel.Trace, message, args);
        }

        public static void LogTrace(this ILogger? logger, Func<string> message)
        {
            logger?.SafeLog(LogLevel.Trace, message);
        }

        public static void LogTrace(this ILogger? logger, Func<string> message, Exception exception)
        {
            logger?.SafeLog(LogLevel.Trace, message, exception);
        }

        public static void LogTrace(this ILogger? logger, Func<string> message, Exception exception, params Func<object>[] args)
        {
            logger?.SafeLog(LogLevel.Trace, message, exception, args);
        }

        #endregion

        private static void SafeLog(this ILogger logger, LogLevel logLevel, Func<string> message, Exception exception, params Func<object>[] args)
        {
            if (!logger.IsEnabled(logLevel))
                return;

            logger.Log(logLevel, exception, message(), args.Select(x => x()).ToArray());
        }

        private static void SafeLog(this ILogger logger, LogLevel logLevel, Func<string> message, params Func<object>[] args)
        {
            if (!logger.IsEnabled(logLevel))
                return;

            logger.Log(logLevel, message(), args.Select(x => x()).ToArray());
        }

        private static void SafeLog(this ILogger logger, LogLevel logLevel, Func<string> message, Exception exception)
        {
            if (!logger.IsEnabled(logLevel))
                return;

            logger.Log(logLevel, exception, message());
        }

        private static void SafeLog(this ILogger logger, LogLevel logLevel, Func<string> message)
        {
            if (!logger.IsEnabled(logLevel))
                return;

            logger.Log(logLevel, message());
        }
    }
}