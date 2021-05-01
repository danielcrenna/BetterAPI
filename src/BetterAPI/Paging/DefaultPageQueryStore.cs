// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BetterAPI.Data;
using BetterAPI.Reflection;
using BetterAPI.Sorting;
using HashidsNet;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace BetterAPI.Paging
{
    /// <summary>
    /// The default page query store generates opaque URLs that are reversible and stateless, in that all query information
    /// is obtainable from the generated continuation token. This sacrifices CPU and token length to reduce round-trip time.
    /// </summary>
    public sealed class DefaultPageQueryStore : IPageQueryStore
    {
        private readonly Func<DateTimeOffset> _timestamps;
        private readonly ILogger<DefaultPageQueryStore> _logger;

        public DefaultPageQueryStore(Func<DateTimeOffset> timestamps, ILogger<DefaultPageQueryStore> logger)
        {
            _timestamps = timestamps;
            _logger = logger;
        }

        public string BuildNextLinkForQuery(Type type, ResourceQuery query)
        {
            if (type == default)
                throw new ArgumentNullException(nameof(type));

            var now = _timestamps();
            var data = SerializeResourceQuery(query);
            var queryHash = Pooling.StringBuilderPool.Scoped(sb =>
            {
                sb.Append(type.FullName);
                sb.Append('_');
                sb.Append(now.Ticks);
                sb.Append('_');
            });

            var nextLink = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(queryHash).Concat(data));
            return nextLink;
        }

        public ResourceQuery? GetQueryFromHash(string queryHash)
        {
            var nextLink = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(queryHash));

            var tokens = nextLink.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length != 3)
                return null;

            var serialized = tokens[2];
            var deserialized = DeserializeResourceQuery(Encoding.UTF8.GetBytes(serialized));

            return deserialized;
        }

        private static byte[] SerializeResourceQuery(ResourceQuery query)
        {
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            bw.WriteNullableInt32(query.PageOffset);
            bw.WriteNullableInt32(query.PageSize);
            bw.WriteNullableInt32(query.MaxPageSize);

            bw.Write(query.CountTotalRows);
            bw.WriteNullableInt32(query.TotalRows);

            if (bw.WriteBoolean(query.Fields != null))
            {
                bw.Write(query.Fields!.Count);
                foreach (var field in query.Fields!)
                {
                    bw.Write(field);
                }
            }

            if (bw.WriteBoolean(query.Sorting != null))
            {
                bw.Write(query.Sorting!.Count);
                foreach (var (accessorMember, sortDirection) in query.Sorting!)
                {
                    bw.WriteNullableString(accessorMember.DeclaringType?.AssemblyQualifiedName);
                    bw.Write(accessorMember.Name);
                    bw.Write((byte) sortDirection);
                }
            }

            var data = ms.ToArray();
            return data;
        }

        private static ResourceQuery DeserializeResourceQuery(byte[] buffer)
        {
            var query = new ResourceQuery();
            
            var ms = new MemoryStream(buffer);
            var br = new BinaryReader(ms);

            query.PageOffset = br.ReadNullableInt32();
            query.PageSize = br.ReadNullableInt32();
            query.MaxPageSize = br.ReadNullableInt32();

            query.CountTotalRows = br.ReadBoolean();
            query.TotalRows = br.ReadNullableInt32();

            if (br.ReadBoolean())
            {
                var fields = br.ReadInt32();
                query.Fields = new List<string>(fields);
                for (var i = 0; i < fields; i++)
                {
                    query.Fields.Add(br.ReadString());
                }
            }

            if (br.ReadBoolean())
            {
                var sorts = br.ReadInt32();
                query.Sorting = new List<(AccessorMember, SortDirection)>(sorts);
                for (var i = 0; i < sorts; i++)
                {
                    var declaringTypeName = br.ReadNullableString();
                    if (declaringTypeName != null)
                    {
                        var declaringType = Type.GetType(declaringTypeName);
                        var memberName = br.ReadString();
                        var direction = (SortDirection) br.ReadByte();

                        if (declaringType == null)
                            continue;

                        var members = AccessorMembers.Create(declaringType, AccessorMemberTypes.Properties,
                            AccessorMemberScope.Public);

                        if (members.TryGetValue(memberName, out var member))
                        {
                            query.Sorting.Add((member, direction));
                        }
                    }
                }
            }

            return query;
        }
    }
}