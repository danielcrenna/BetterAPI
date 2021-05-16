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
    internal sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value)
            => Guid.Parse((string) value);

        public override void SetValue(IDbDataParameter parameter, Guid value)
            => parameter.Value = value.ToString();
    }
}