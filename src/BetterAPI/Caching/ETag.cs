// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Caching
{
    public readonly struct ETag : IEquatable<ETag>
    {
        public readonly ETagType Type;
        public readonly string Value;

        public ETag(ETagType type, string value)
        {
            Type = type;
            Value = value;
        }

        /// <summary> A format-valid but invalid weak ETag </summary>
        public static readonly ETag InvalidWeak = new ETag(ETagType.Weak, "\"00000000000000000000000000000000\"");

        /// <summary> Weak ETag for JSON: [] </summary>
        public static readonly ETag WeakEmptyJsonArray =
            new ETag(ETagType.Weak, "\"D751713988987E9331980363E24189CE\"");

        public static implicit operator string(ETag etag)
        {
            return etag.ToString();
        }

        public bool Equals(ETag other)
        {
            return Type == other.Type && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is ETag other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add((int) Type);
            hashCode.Add(Value, StringComparer.OrdinalIgnoreCase);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(ETag left, ETag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ETag left, ETag right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return Type switch
            {
                ETagType.Weak => "W/" + Value,
                ETagType.Strong => Value,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}