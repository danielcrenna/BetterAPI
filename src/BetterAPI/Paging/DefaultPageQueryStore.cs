// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Text;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.WebUtilities;

namespace BetterAPI.Paging
{
    public sealed class DefaultPageQueryStore : IPageQueryStore
    {
        private readonly Func<DateTimeOffset> _timestamps;

        public DefaultPageQueryStore(Func<DateTimeOffset> timestamps)
        {
            _timestamps = timestamps;
        }

        public string BuildNextLinkForQuery(Type type)
        {
            if (type == default)
                throw new ArgumentNullException(nameof(type));

            var now = _timestamps();

            var query = Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append(type.FullName);
                sb.Append('_');
                sb.Append(now.Ticks);
            });

            var nextLink = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(query));
            return nextLink;
        }
    }
}