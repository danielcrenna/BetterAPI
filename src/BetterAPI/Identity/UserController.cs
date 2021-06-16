// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.ComponentModel.DataAnnotations;
using BetterAPI.ChangeLog;
using BetterAPI.Cryptography;
using BetterAPI.Data;
using BetterAPI.Events;
using BetterAPI.Http;
using BetterAPI.Paging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetterAPI.Identity
{
    [InternalController]
    [Display(Name = "Users", Description = "Manages the creation of API users, their roles, and authorization levels.")]
    public sealed class UserController : ResourceController<User>
    {
        public UserController(
            IResourceDataService<User> service, 
            IPageQueryStore store, 
            IResourceEventBroadcaster resourceEvents,
            ChangeLogBuilder changeLog,
            IStringLocalizer<UserController> localizer,
            IOptionsSnapshot<ApiOptions> options, 
            ILogger<UserController> logger) 
            : base(localizer, service, store, resourceEvents, changeLog, options, logger)
        {
            
        }

        public override bool BeforeSave(User model, out IActionResult? error)
        {
            if (model.PublicKey == null || model.PublicKey.Length == 0)
            {
                unsafe
                {
                    Crypto.GenerateKeyPair(out var pk, out var sk);
                    model.PublicKey = pk;
                }
            }

            return base.BeforeSave(model, out error);
        }
    }
}
