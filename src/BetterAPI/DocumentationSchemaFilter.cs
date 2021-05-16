// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System.Linq;
using BetterAPI.DataProtection;
using BetterAPI.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BetterAPI
{
    internal sealed class DocumentationSchemaFilter : ISchemaFilter
    {
        private readonly IAuthorizationService _authorization;
        private readonly IHttpContextAccessor _http;
        private readonly ChangeLogBuilder _builder;

        public DocumentationSchemaFilter(IAuthorizationService authorization, IHttpContextAccessor http, ChangeLogBuilder builder)
        {
            _authorization = authorization;
            _http = http;
            _builder = builder;
        }

        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            DocumentXmlSchemas(schema, context);
            
            if (!typeof(IResource).IsAssignableFrom(context.Type))
                return;

            foreach (var member in AccessorMembers.Create(context.Type, AccessorMemberTypes.Properties, AccessorMemberScope.Public))
            {
                if (!member.TryGetAttribute<ProtectedByPolicyAttribute>(out var attribute))
                    continue;

                var user = _http.ResolveCurrentPrincipal();
                if (!user.Claims.Any() || !_authorization.AuthorizeAsync(user, null, attribute.PolicyName)
                    .ConfigureAwait(false).GetAwaiter().GetResult().Succeeded)
                {
                    var propertyName = char.ToLowerInvariant(member.Name[0]) + member.Name.Substring(1);
                    schema.Properties.Remove(propertyName);
                }
            }

            if (_builder.TryGetResourceName(context.Type, out var name))
            {
                schema.Title = name;
            }
        }

        /// <summary>
        /// Fixes issues with Swagger-generated examples when the media type is XML.
        /// </summary>
        private static void DocumentXmlSchemas(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsValueType || schema.Type == "string")
                return; // ignore value types

            schema.Xml = new OpenApiXml {Name = context.Type.Name};

            if (schema.Type == "array" && schema.Items.Reference != null)
            {
                schema.Xml = new OpenApiXml
                {
                    Name = $"ArrayOf{schema.Items.Reference.Id}",
                    Wrapped = true,
                };
            }

            if (schema.Properties.Count > 0)
            {
                foreach (var (name, property) in schema.Properties)
                {
                    if (property.Type != "array")
                        continue;
                    property.Xml = new OpenApiXml {Name = name, Wrapped = true};
                    property.Items.Xml = new OpenApiXml {Name = property.Items.Type};
                }
            }
        }
    }
}