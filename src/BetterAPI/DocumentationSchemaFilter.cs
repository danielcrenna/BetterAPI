// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BetterAPI
{
    internal sealed class DocumentationSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            DocumentXmlSchemas(schema, context);
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