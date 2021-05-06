// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;

namespace BetterAPI.Localization
{
    internal sealed class ApiStringLocalizer : IStringLocalizer
    {
        private readonly ILocalizationStore _store;
        private readonly IHttpContextAccessor _accessor;
        private readonly string _scope;
        
        public ApiStringLocalizer(ILocalizationStore store, IHttpContextAccessor accessor, string scope)
        {
            _store = store;
            _accessor = accessor;
            _scope = scope;
        }
        
        public LocalizedString this[string name]
        {
            get
            {
                var text = _store.GetText(_scope, name, _accessor.HttpContext?.RequestAborted ?? CancellationToken.None);
                return text;
            }
        }

        public LocalizedString this[string name, params object[] args]
        {
            get
            {
                var text = _store.GetText(_scope, name, _accessor.HttpContext?.RequestAborted ?? CancellationToken.None, args);
                return text;
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return _store.GetAllTranslationsByCurrentCulture(includeParentCultures, _accessor.HttpContext?.RequestAborted ?? CancellationToken.None)
                .Select(x => x.AsLocalizedString);
        }
    }
}