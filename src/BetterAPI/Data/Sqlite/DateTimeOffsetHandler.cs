﻿// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Data;
using Dapper;

namespace BetterAPI.Data.Sqlite
{
    internal sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset?>
    {
        public static readonly DateTimeOffsetHandler Default = new DateTimeOffsetHandler();

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
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

        public override DateTimeOffset? Parse(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case DateTimeOffset offset:
                    return offset;
                default:
                    return Convert.ToDateTime(value);
            }
        }
    }
}