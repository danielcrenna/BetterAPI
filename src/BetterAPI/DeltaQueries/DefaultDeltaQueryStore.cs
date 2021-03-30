// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.WebUtilities;

namespace BetterAPI.DeltaQueries
{
    public sealed class DefaultDeltaQueryStore : IDeltaQueryStore
    {
        private readonly IDictionary<string, DeltaTrackingInfo> _links;
        private readonly IDictionary<Type, IList<DeltaTrackingInfo>> _types;

        private readonly Func<DateTimeOffset> _timestamps;

        public DefaultDeltaQueryStore(Func<DateTimeOffset> timestamps)
        {
            _timestamps = timestamps;
            _links = new ConcurrentDictionary<string, DeltaTrackingInfo>();
            _types = new ConcurrentDictionary<Type, IList<DeltaTrackingInfo>>();
        }

        public string BuildDeltaLinkForQuery(Type type)
        {
            if (type == default) throw new ArgumentNullException(nameof(type));

            var now = _timestamps();

            var query = Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append(type.FullName);
                sb.Append('_');
                sb.Append(now.Ticks);
            });

            var deltaLink = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(query));
            var deltaTrackingInfo = new DeltaTrackingInfo(type) { TrackingDateTime = now };
            
            // Update pull and push tracking
            //
            _links[deltaLink] = deltaTrackingInfo;
            if(!_types.TryGetValue(type, out var list))
                _types.TryAdd(type, list = new List<DeltaTrackingInfo>());
            list.Add(deltaTrackingInfo);

            return deltaLink;
        }

        public bool TryGetDelta<T>(string deltaLink, out IEnumerable<Delta<T>>? changes)
        {
            // FIXME: We should be able to decide between forcing a query re-execution,
            //        in cases where we don't control the underlying data store, and changes
            //        can occur outside the application, and relying on proactive push deltas,
            //        for performance reasons.

            if (!_links.TryGetValue(deltaLink, out _))
            {
                changes = default;
                return false; // invalid delta link provided 
            }

            // FIXME: rebuild and execute the query, and play against changes
            changes = Enumerable.Empty<Delta<T>>();
            return true; 
        }

        public void TryPushAdd<T>(T added)
        {
            if(!_types.TryGetValue(typeof(T), out var deltaTrackingInfos))
                return; // no tracking for this type

            // FIXME: do something useful here (i.e. push deltas)
            foreach (var deltaTrackingInfo in deltaTrackingInfos)
            {
                
            }
        }
    }
}