// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BetterAPI.Http
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]
    public sealed class FromFilterAttribute : BindingBehaviorAttribute
    {
        public FromFilterAttribute() : base(BindingBehavior.Never)
        {
        }
    }
}