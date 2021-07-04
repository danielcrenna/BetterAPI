// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using BetterAPI.Data;
using BetterAPI.DataProtection;
using BetterAPI.Reflection;
using BetterAPI.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BetterAPI.ChangeLog
{
    public sealed class ChangeLogBuilder
    {
        public IServiceCollection Services { get; }

        private readonly Dictionary<string, Type> _pendingTypes;
        private readonly IDictionary<ApiVersion, Dictionary<string, Type>> _versions;
        private bool _addMissingResources;

        internal ISet<Type>? ResourceTypes { get; private set; }
        internal IImmutableDictionary<ApiVersion, Dictionary<string, Type>> Versions { get; private set; }

        public ChangeLogBuilder(IServiceCollection services)
        {
            _pendingTypes = new Dictionary<string, Type>();
            _versions = new Dictionary<ApiVersion, Dictionary<string, Type>>();

            Versions = _versions.ToImmutableDictionary();
            Services = services;
        }

        #region Options

        /// <summary>
        /// During validation, add any missing resources detected by the runtime when the change log is built.
        ///     <remarks>
        ///         Note that auto-added types will not get the benefit of an explicit resource name, so it is usually preferable to add them explicitly.
        ///     </remarks> 
        /// </summary>
        public ChangeLogBuilder AddMissingResources()
        {
            _addMissingResources = true;
            return this;
        }

        /// <summary>
        /// During validation, throw an exception on any missing resources detected by the runtime when the change log is built.
        /// This is the default, as it prevents accidental addition of resources without an explicit resource name.
        /// </summary>
        public ChangeLogBuilder IgnoreMissingResources()
        {
            _addMissingResources = false;
            return this;
        }

        #endregion

        public ChangeLogBuilder AddResource<T>(string? name = default) where T : class, IResource
        {
            name ??= ResolveMissingResourceName<T>();
            _pendingTypes[name] = typeof(T);

            // get the revision number for this resource name
            var revision = 1;
            foreach (var version in _versions)
            {
                foreach (var item in version.Value)
                {
                    if (item.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                        revision++;
                }
            }

            Services.TryAddScoped<ResourceController<T>>(); // needed for embedded collections
            Services.AddResourceStore<T>(revision, "resources");
            return this;
        }
        
        #region Shipping

        public ChangeLogBuilder ShipVersion(ApiVersion version)
        {
            if(!_versions.TryGetValue(version, out var manifest))
                _versions.Add(version, manifest = new Dictionary<string, Type>());
            foreach (var (k, v) in _pendingTypes)
                manifest.Add(k, v);
            _pendingTypes.Clear();
            return this;
        }

        public ChangeLogBuilder ShipVersionOne(DateTime? groupVersion = default)
        {
            return ShipVersion(ApiVersion.Default);
        }

        public ChangeLogBuilder ShipNextMinorVersion(DateTime? groupVersion = default)
        {
            var currentVersion = _versions.Keys.Max();
            if (currentVersion == null)
            {
                // FIXME: localize
                throw new ChangeLogException("Cannot increment a next minor version when no previous version exists");
            }

            var nextMinorVersion = groupVersion.HasValue
                ? new ApiVersion(groupVersion.Value, currentVersion.MajorVersion.GetValueOrDefault(1),
                    1 + currentVersion.MinorVersion.GetValueOrDefault(0))
                : new ApiVersion(currentVersion.MajorVersion.GetValueOrDefault(1),
                    1 + currentVersion.MinorVersion.GetValueOrDefault(0));

            return ShipVersion(nextMinorVersion);
        }

        public ChangeLogBuilder ShipNextMajorVersion(DateTime? groupVersion = default)
        {
            var currentVersion = _versions.Keys.Max();
            if (currentVersion == null)
            {
                // FIXME: localize
                throw new ChangeLogException("Cannot increment a next major version when no previous version exists");
            }

            var nextMajorVersion = groupVersion.HasValue
                ? new ApiVersion(groupVersion.Value, 1 + currentVersion.MajorVersion.GetValueOrDefault(1),
                    currentVersion.MinorVersion.GetValueOrDefault(0))
                : new ApiVersion(1 + currentVersion.MajorVersion.GetValueOrDefault(1),
                    currentVersion.MinorVersion.GetValueOrDefault(0));

            return ShipVersion(nextMajorVersion);
        }

        #endregion
        
        public void Build()
        {
            if (_versions.Count == 0)
                ShipVersion(ApiVersion.Default);
            
            var types = _versions.Values.SelectMany(x => x.Values).ToHashSet();

            var versions = _versions;
            if (_addMissingResources)
            {
                // we might modify this during validation, so use a copy
                versions = new Dictionary<ApiVersion, Dictionary<string, Type>>();
                foreach(var (key, value) in _versions)
                    versions.Add(key, new Dictionary<string, Type>(value));
            }

            foreach (var (version, manifest) in versions)
            {
                foreach (var (resourceName, resourceType) in manifest)
                {
                    ValidateTypeExistsInChangeLog(resourceName, version, resourceType, types, _addMissingResources);
                }
            }

            ResourceTypes = _versions.SelectMany(x => x.Value.Values).Distinct().ToHashSet();
            Versions = _versions.ToImmutableDictionary();
        }

        private void AddMissingResource(ApiVersion version, Type type, string? name = default)
        {
            if (!typeof(IResource).IsAssignableFrom(type))
                // FIXME: localize
                throw new ChangeLogException("You cannot add a resource type that does not inherit from 'IResource'");

            name ??= ResolveMissingResourceName(type);

            // get the revision number for ths resource name (from prior versions to the missing resource only)
            var revision = 1;
            foreach (var (previousVersion, manifest) in _versions.OrderBy(x => x.Key))
            {
                if (version <= previousVersion)
                    break;

                foreach (var item in manifest)
                {
                    if (item.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                        revision++;
                }
            }

            // add the missing resource store
            Services.AddResourceStoreInternal(type, revision, "resources");

            // needed for embedded collections
            Services.TryAddScoped(typeof(ResourceController<>).MakeGenericType(type));

            // add the missing entry in the version manifest
            _versions[version].Add(name, type);
        }

        private void ValidateTypeExistsInChangeLog(string resourceName, ApiVersion version, Type type, IReadOnlySet<Type> typesToSearch, bool addMissingResources = true)
        {
            if(!typesToSearch.Contains(type))
            {
                if (addMissingResources)
                {
                    AddMissingResource(version, type);
                }
                else
                {
                    // FIXME: localize
                    var message = string.Format("Resource '{0}' binds type '{1}', but this type was not found in the change log. Did you forget to add it?", resourceName, type.Name);
                    throw new ChangeLogException(message);    
                }
            }

            var members = AccessorMembers.Create(type, AccessorMemberTypes.Properties, AccessorMemberScope.Public);
            foreach (var member in members)
            {
                if (typeof(IResource).IsAssignableFrom(member.Type))
                {
                    ValidateTypeExistsInChangeLog(resourceName, version, member.Type, typesToSearch, addMissingResources);
                }

                if (member.Type.ImplementsGeneric(typeof(IEnumerable<>)) && member.Type.IsGenericType)
                {
                    var arguments = member.Type.GetGenericArguments();
                    var underlyingType = arguments[0];
                    if (typeof(IResource).IsAssignableFrom(underlyingType))
                    {
                        ValidateTypeExistsInChangeLog(resourceName, version, underlyingType, typesToSearch, addMissingResources);
                    }
                }
            }
        }
        
        private static string ResolveMissingResourceName<T>() where T : class, IResource => typeof(T).TryGetAttribute(true, out ResourceNameAttribute resourceName) ? resourceName.Name : typeof(T).Name;

        private static string ResolveMissingResourceName(MemberInfo type) => type.TryGetAttribute(true, out ResourceNameAttribute resourceName) ? resourceName.Name : type.Name;

        internal bool TryGetResourceNameForType(Type type, out string? name)
        {
            foreach (var version in _versions)
            {
                foreach (var (key, value) in version.Value)
                {
                    if (value != type)
                        continue;
                    name = key;
                    return true;
                }
            }

            name = default;
            return false;
        }

        internal bool TryGetApiVersionForType(Type type, out ApiVersion? apiVersion)
        {
            foreach (var (version, manifest) in _versions)
            {
                foreach (var (_, value) in manifest)
                {
                    if (value != type)
                        continue;
                    apiVersion = version;
                    return true;
                }
            }

            apiVersion = default;
            return false;
        }

        internal ApiVersion GetApiVersionForResourceAndRevision(string resourceName, in int revision)
        {
            var found = revision;
            foreach (var (version, manifest) in _versions)
            {
                foreach (var entry in manifest)
                {
                    if (!entry.Key.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    found--;
                    if (found == 0)
                        return version;
                }
            }
            return ApiVersion.Default;
        }

        public int GetRevisionForResourceAndApiVersion(string resourceName, ApiVersion version)
        {
            var revision = 0;
            foreach (var (apiVersion, manifest) in _versions)
            {
                if (apiVersion > version)
                    return revision;

                foreach (var entry in manifest)
                {
                    if (!entry.Key.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    revision++;
                }
            }
            return revision;
        }

        public ResourceFormat BuildResourceFormat<T>(ApiVersion version)
        {
            var type = typeof(T);
            var format = new ResourceFormat
            {
                AssemblyName = Path.GetFileName(type.Assembly.Location),
                Type = type.FullName
            };

            if (TryGetResourceNameForType(type, out var resourceName))
                format.Name = resourceName;

            format.Version = new ResourceVersion
            {
                DisplayName = version.ToString(),
                GroupVersion = version.GroupVersion,
                MajorVersion = version.MajorVersion,
                MinorVersion = version.MinorVersion,
                Status = version.Status
            };

            // already ordered by display attributes, if they exist
            var members = AccessorMembers.Create(type, AccessorMemberTypes.Properties, AccessorMemberScope.Public);
            
            foreach (var member in members)
            {
                var field = new ResourceField
                {
                    Type = ResolveTypeName(member)
                };

                if (member.Name == "Id")
                    field.IsHidden = true;

                if (!member.CanWrite)
                    field.IsReadOnly = true;

                if (member.TryGetAttribute(out DisplayAttribute display))
                {
                    field.Name = display.GetName() ?? member.Name;
                    field.Description = display.GetDescription();
                    field.Prompt = display.GetPrompt();
                    field.Order = display.GetOrder().GetValueOrDefault(0);
                }
                else
                {
                    field.Name = member.Name;
                }

                if (member.TryGetAttribute(out OneOfAttribute oneOf))
                    field.Options.AddRange(oneOf.OneOfStrings);

                if (member.TryGetAttribute(out ProtectedByPolicyAttribute policy))
                    field.PolicyName = policy.PolicyName;

                format.Fields.Add(field);
            }

            return format;
        }
        
        private static string ResolveTypeName(AccessorMember member)
        {
            if (member.HasAttribute<OneOfAttribute>())
                return "enum";

            return member.Type.Name;
        }
    }
}