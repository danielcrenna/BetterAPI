// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Reflection;

namespace BetterAPI.Localization
{
    /// <summary>
    /// For each non-default supported culture, find all calls to _localizer.GetString(name) / _localizer[name],
    /// and fill in any missing entries.
    /// </summary>
    internal sealed class LocalizationStartupService : IHostedService
    {
        private readonly IStringLocalizer<LocalizationStartupService> _localizer;
        private readonly ILocalizationStore _store;
        private readonly IOptionsMonitor<RequestLocalizationOptions> _options;
        private readonly ILogger<LocalizationStartupService> _logger;
        private readonly HashSet<string> _translations;
        private readonly IDisposable _disposable;

        public LocalizationStartupService(IStringLocalizer<LocalizationStartupService> localizer, ILocalizationStore store, IOptionsMonitor<RequestLocalizationOptions> options, ILogger<LocalizationStartupService> logger)
        {
            _localizer = localizer;
            _store = store;
            _options = options;
            _logger = logger;
            _translations = ScanAssembliesForTranslations();

            _disposable = _options.OnChange(o =>
            {
                AddMissingTranslations(CancellationToken.None);
            });
        }

        private static readonly HashSet<MethodBase> GetStringMethods;

        static LocalizationStartupService()
        {
            GetStringMethods = new HashSet<MethodBase>();

            foreach (var method in typeof(IStringLocalizer).GetMethods())
                if (method.Name == "get_Item")
                    GetStringMethods.Add(method);

            foreach(var method in typeof(StringLocalizerExtensions).GetMethods())
                if (method.Name == "GetString")
                    GetStringMethods.Add(method);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            AddMissingTranslations(cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _translations.Clear();
            _disposable.Dispose();
            return Task.CompletedTask;
        }

        private void AddMissingTranslations(CancellationToken cancellationToken)
        {
            foreach (var culture in _options.CurrentValue.SupportedUICultures.Where(x => !_options.CurrentValue.DefaultRequestCulture.UICulture.Name.Equals(x.Name, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var name in _translations)
                {
                    if(_store.TryAddMissingTranslation(culture.Name, new LocalizedString(name, name, true), cancellationToken))
                        _logger.LogDebug(_localizer.GetString("Added missing translation for {CultureName}: '{Name}'"), name, culture.Name);
                }
            }
        }

        private static HashSet<string> ScanAssembliesForTranslations()
        {
            var translations = new HashSet<string>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Microsoft.Extensions.Localization.Abstractions")
                    continue; // skip internal calls to self

                foreach (var callerType in assembly.GetTypes())
                {
                    foreach (var callerMethod in callerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                                       BindingFlags.Static | BindingFlags.Instance |
                                                                       BindingFlags.CreateInstance))
                    {
                        if (GetStringMethods.Contains(callerMethod))
                            continue; // skip calling self

                        var callerBody = callerMethod.GetMethodBody();
                        if (callerBody == null)
                            continue; // no body

                        var instructions = MethodBodyReader.GetInstructions(callerMethod);

                        foreach (Instruction instruction in instructions)
                        {
                            if (instruction.Operand is MethodInfo methodInfo && GetStringMethods.Contains(methodInfo) &&
                                instruction.Previous.Operand is string value)
                            {
                                translations.Add(value);
                            }
                        }
                    }
                }
            }

            return translations;
        }
    }
}