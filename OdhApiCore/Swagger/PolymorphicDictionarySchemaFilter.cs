// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using DataModel.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

public class PolymorphicDictionarySchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.MemberInfo == null) return;

        var attribute = context.MemberInfo.GetCustomAttribute<PolymorphicDictionaryAttribute>();
        if (attribute == null) return;

        // Clear the existing schema for the property
        schema.Type = "object";
        schema.Properties.Clear();

        // Create properties for each key with its specific type
        foreach (var kvp in attribute.TypeMapping)
        {
            var key = kvp.Key;
            var type = kvp.Value;

            // Generate schema for the type
            var typeSchema = context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

            schema.Properties[key] = new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.Schema,
                    Id = type.Name
                },
                Nullable = true
            };
        }

        // Mark all properties as not required since it's a dictionary
        schema.Required.Clear();
        schema.AdditionalPropertiesAllowed = false;
    }
}