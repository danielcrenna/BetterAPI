// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Extensions;
using BetterAPI.Reflection;
using BetterAPI.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Reflection;

namespace BetterAPI.Localization
{
    /// <summary>
    /// For each non-default supported culture, find all calls to _localizer.GetString(name) / _localizer[name], and fill in any missing entries.
    /// </summary>
    internal sealed class LocalizationStartupService : IHostedService
    {
        private readonly IStringLocalizer<LocalizationStartupService> _localizer;
        private readonly ILocalizationStore _store;
        private readonly IOptionsMonitor<RequestLocalizationOptions> _options;
        private readonly ILogger<LocalizationStartupService> _logger;
        private readonly Dictionary<string, List<string>> _translations;
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
                foreach (var (scope, names) in _translations)
                {
                    using (_logger.BeginScope(scope))
                    {
                        foreach (var name in names)
                        {
                            if(_store.TryAddMissingTranslation(culture.Name, new LocalizedString(name, name, true, scope), cancellationToken))
                                _logger.LogDebug(_localizer.GetString("Added missing translation for {CultureName}: '{Name}'"), name, culture.Name);
                        }
                    }
                }
            }
        }

        private static Dictionary<string, List<string>> ScanAssembliesForTranslations()
        {
            var translations = new Dictionary<string, List<string>>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Microsoft.Extensions.Localization.Abstractions")
                    continue; // skip internal calls to self

                foreach (var callerType in assembly.GetTypes())
                {
                    ScanMethodsForLocalizerInvocations(callerType, translations);

                    ScanMembersForAttributes(callerType, translations);
                }
            }

            return translations;
        }

        private static void ScanMembersForAttributes(Type callerType, IDictionary<string, List<string>> translations)
        {
            foreach (var member in AccessorMembers.Create(callerType, AccessorMemberTypes.Properties,
                AccessorMemberScope.Public))
            {
                if (member.TryGetAttribute(out DisplayAttribute display))
                {
                    var scope = callerType.Name;

                    if (!translations.TryGetValue(scope, out var list))
                        translations.Add(scope, list = new List<string>());

                    var description = display.Description;
                    if (!string.IsNullOrWhiteSpace(description))
                        list.Add(description);

                    var prompt = display.Prompt;
                    if (!string.IsNullOrWhiteSpace(prompt))
                        list.Add(prompt);

                    var name = display.Name;
                    if (!string.IsNullOrWhiteSpace(name))
                        list.Add(name);

                    var groupName = display.GroupName;
                    if (!string.IsNullOrWhiteSpace(groupName))
                        list.Add(groupName);
                }

                if (member.TryGetAttribute(out OneOfAttribute oneOf))
                {
                    var scope = callerType.Name;

                    if (!translations.TryGetValue(scope, out var list))
                        translations.Add(scope, list = new List<string>());

                    if(!string.IsNullOrWhiteSpace(oneOf.ErrorMessage))
                        list.Add(oneOf.ErrorMessage);

                    foreach (var value in oneOf.OneOfStrings)
                    {
                        if(!string.IsNullOrWhiteSpace(value))
                            list.Add(value);
                    }
                }
            }
        }

        private static void ScanMethodsForLocalizerInvocations(Type callerType, Dictionary<string, List<string>> translations)
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
                        string scope;
                        if (instruction.Previous.Previous != null &&
                            instruction.Previous.Previous.Operand is FieldInfo field)
                        {
                            scope = field.DeclaringType != null && field.DeclaringType.IsGenericType
                                ? field.DeclaringType.NormalizeResourceControllerName().Replace("_T", string.Empty)
                                : field.DeclaringType?.Name ?? string.Empty;
                        }
                        else
                            scope = string.Empty;

                        if (!translations.TryGetValue(scope, out var list))
                            translations.Add(scope, list = new List<string>());

                        list.Add(value);
                    }
                }
            }
        }
    }
}