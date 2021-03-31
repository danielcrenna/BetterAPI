﻿// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace BetterAPI.DataProtection
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ProtectedByPolicyAttribute : Attribute
    {
        public string PolicyName { get; }

        public ProtectedByPolicyAttribute(string policyName)
        {
            PolicyName = policyName;
        }
    }
}