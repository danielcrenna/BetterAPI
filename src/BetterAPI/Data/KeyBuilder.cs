// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BetterAPI.Extensions;
using BetterAPI.Reflection;
using Microsoft.Extensions.Logging;
using WyHash;

namespace BetterAPI.Data
{
    public static class KeyBuilder
    {
        private static readonly Dictionary<Type, byte[]> IdPrefixes = new Dictionary<Type, byte[]>();
        private static readonly Dictionary<(Type, string), byte[]> KeyPrefixes = new Dictionary<(Type, string), byte[]>();

        private static string BuildKeyPrefixString(string key) => key[0] + ":";
        private static string BuildTypePrefixString(MemberInfo type) => type.Name[0] + ":";
        
        /// <summary>
        /// Builds a key for indexing a lookup between the index key and the reference back to the serialized content by ID index. 
        /// The key is in the format [KEY_PREFIX][KEY][ID_PREFIX][ID].
        ///
        /// Lookups should omit the [ID_PREFIX][ID] portion and scan the range starting at [KEY_PREFIX][KEY] to find all IDs whose properties possess the key's value,
        /// and then call GetByIndex to deserialize the entry directly from the ID index.
        /// </summary>
        public static bool TryGetIndexKey(this AccessorMember member, ITypeReadAccessor accessor, object target, byte[] id, out byte[]? indexKey, ILogger? logger = default)
        {
            if (!member.CanRead || !member.HasAttribute<IndexAttribute>())
            {
                indexKey = default;
                return false;
            }

            var type = member.DeclaringType ?? target.GetType();

            if (!accessor.TryGetValue(target, member.Name, out var value))
            {
                indexKey = default;
                return false;
            }

            indexKey = IndexKey(type, member.Name, value.ToString(), id, logger);
            return true;
        }

        public static ReadOnlySpan<byte> PrepareValue(ReadOnlySpan<char> value)
        {
            var buffer = new char[value.Length];
            var result = new byte[sizeof(ulong)];

            value.ToUpperInvariant(buffer);

            if(!BitConverter.TryWriteBytes(result, WyHash64.ComputeHash64(buffer)))
                throw new InvalidOperationException();

            return result;
        }
        
        /// <summary>
        /// Produces the [ID_PREFIX] for a given type.
        /// </summary>
        public static byte[] GetIdPrefix(Type type)
        {
            if (!IdPrefixes.TryGetValue(type, out var prefix))
                IdPrefixes.Add(type, prefix = Encoding.UTF8.GetBytes(BuildTypePrefixString(type)));
            return prefix;
        }
        
        /// <summary>
        /// Produces the [KEY_PREFIX] for a given type and key name.
        /// </summary>
        public static byte[] GetKeyPrefix(Type type, string key)
        {
            key = key.ToUpperInvariant();
            if (!KeyPrefixes.TryGetValue((type, key), out var prefix))
                KeyPrefixes.Add((type, key), prefix = GetIdPrefix(type).Concat(Encoding.UTF8.GetBytes(BuildKeyPrefixString(key))));
            return prefix;
        }

        /// <summary>
        /// Produces the [KEY_PREFIX][KEY] for a given type, key name, and key value.
        /// </summary>
        public static byte[] LookupKey(Type type, string key, string? value, ILogger? logger)
        {
            var lookupKey = GetKeyPrefix(type, key).Concat(PrepareValue(value));

            logger?.LogTrace(() => "Lookup {Type}.{Member}.{Value} => {KEY_PREFIX}{KEY}",
                () => type.Name,
                () => key,
                () => value ?? "<NULL>",
                () => BuildTypePrefixString(type) + BuildKeyPrefixString(key),
                () => Convert.ToBase64String(PrepareValue(value)),
                () => lookupKey.Length);

            return lookupKey;
        }

        /// <summary>
        /// Produces the [KEY_PREFIX][KEY][ID_PREFIX][ID] for a given type, key name, key value, and ID value.
        /// </summary>
        public static byte[] IndexKey(Type type, string key, string? value, byte[] id, ILogger? logger = default)
        {
            var indexKey = GetKeyPrefix(type, key).Concat(PrepareValue(value)).Concat(GetIdPrefix(type)).Concat(id);

            logger?.LogTrace(() => "Index {Type}.{Member}.{Value} => {KEY_PREFIX}{KEY}{ID_PREFIX}{ID} ({KeySize})",
                () => type.Name,
                () => key,
                () => value ?? "<NULL>",
                () => BuildTypePrefixString(type) + BuildKeyPrefixString(key),
                () => Convert.ToBase64String(PrepareValue(value)),
                () => BuildTypePrefixString(type), 
                () => new Guid(id),
                () => indexKey.Length);

            return indexKey;
        }

        ///// <summary>
        ///// Builds a key for indexing a lookup between the index key and the reference back to the serialized content by ID index. 
        ///// The key is in the format [KEY_PREFIX_N][KEY_N]...[ID_PREFIX][ID].
        /////
        ///// Lookups should omit the [ID_PREFIX][ID] portion and scan the range starting at [KEY_PREFIX][KEY]... to find all IDs whose properties possess the key values,
        ///// and then call GetByIndex to deserialize the entry directly from the ID index.
        ///// </summary>
        //public static bool TryGetIndexKey(this IEnumerable<AccessorMember> members, ITypeReadAccessor accessor, object target, byte[] id, out byte[]? indexKey, ILogger? logger = default)
        //{
        //    if (!member.CanRead || !member.HasAttribute<IndexAttribute>())
        //    {
        //        indexKey = default;
        //        return false;
        //    }

        //    var type = member.DeclaringType ?? target.GetType();

        //    if (!accessor.TryGetValue(target, member.Name, out var value))
        //    {
        //        indexKey = default;
        //        return false;
        //    }

        //    indexKey = IndexKey(type, member.Name, value.ToString(), id, logger);
        //    return true;
        //}

        ///// <summary>
        ///// Produces the [KEY_PREFIX][KEY]...[ID_PREFIX][ID] for a given type, key names, key values, and ID value.
        ///// </summary>
        //public static byte[] IndexKey(Type type, string[] keys, string?[] values, byte[] id, ILogger? logger = default)
        //{
        //    var indexKey = GetKeyPrefix(type, key).Concat(PrepareValue(value)).Concat(GetIdPrefix(type)).Concat(id);

        //    logger?.LogTrace(() => "Index {Type}.{Member}.{Value} => {KEY_PREFIX}{KEY}{ID_PREFIX}{ID} ({KeySize})",
        //        () => type.Name,
        //        () => key,
        //        () => value ?? "<NULL>",
        //        () => BuildTypePrefixString(type) + BuildKeyPrefixString(key),
        //        () => Convert.ToBase64String(PrepareValue(value)),
        //        () => BuildTypePrefixString(type), 
        //        () => new Guid(id),
        //        () => indexKey.Length);

        //    return indexKey;
        //}
    }
}