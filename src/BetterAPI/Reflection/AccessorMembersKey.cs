// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Reflection
{
    internal readonly struct AccessorMembersKey : IEquatable<AccessorMembersKey>
    {
        public Type Type { get; }
        public AccessorMemberScope Scope { get; }
        public AccessorMemberTypes Types { get; }

        public AccessorMembersKey(Type type, AccessorMemberTypes types, AccessorMemberScope scope)
        {
            Type = type;
            Scope = scope;
            Types = types;
        }

        public bool Equals(AccessorMembersKey other)
        {
            return Type == other.Type && Scope == other.Scope && Types == other.Types;
        }

        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && obj is AccessorMembersKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Type != null ? Type.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (int) Scope;
                hashCode = (hashCode * 397) ^ (int) Types;
                return hashCode;
            }
        }

        public static bool operator ==(AccessorMembersKey left, AccessorMembersKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AccessorMembersKey left, AccessorMembersKey right)
        {
            return !left.Equals(right);
        }
    }
}