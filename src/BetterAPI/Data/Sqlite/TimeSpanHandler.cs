// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Data;
using Dapper;

namespace BetterAPI.Data.Sqlite
{
    internal sealed class TimeSpanHandler : SqlMapper.TypeHandler<TimeSpan?>
    {
        public static readonly TimeSpanHandler Default = new TimeSpanHandler();

        public override void SetValue(IDbDataParameter parameter, TimeSpan? value)
        {
            if (value.HasValue)
            {
                parameter.Value = value.Value;
            }
            else
            {
                parameter.Value = DBNull.Value;
            }
        }

        public override TimeSpan? Parse(object? value)
        {
            return value switch
            {
                null => null,
                TimeSpan timeSpan => timeSpan,
                _ => TimeSpan.Parse(value.ToString() ?? "0")
            };
        }
    }
}