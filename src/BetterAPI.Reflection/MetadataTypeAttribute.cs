// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.Reflection
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class MetadataTypeAttribute : Attribute
    {
        public MetadataTypeAttribute(string profile, Type metadataType)
        {
            Profile = profile;
            MetadataType = metadataType;
        }

        public MetadataTypeAttribute(Type metadataType) : this("Default", metadataType)
        {
        }

        public string Profile { get; }

        public Type MetadataType { get; }
    }
}