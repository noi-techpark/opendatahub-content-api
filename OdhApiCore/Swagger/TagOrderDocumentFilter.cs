// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace OdhApiCore.Swagger
{
    public class TagOrderDocumentFilter : IDocumentFilter
    {
        private static readonly List<string> Order = new()
        {
            // Main
            "MetaData",
            "Accommodation", "Announcement", "Article", "Common", "Event",
            "ODHActivityPoi", "Publisher", "Source", "SpatialData",
            "Tag", "Trip", "UrbanGreen", "Venue", "Weather", "WebcamInfo",
            // Secondary
            "Search", "Distinct", "Deprecated", "PushResponse",
            "Geo", "GeoConverter", "Location", "FileUpload", "JsonLD",
            // Obsolete
            "EventShort", "ODHTag",
        };

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Collect all tags actually used in operations
            var usedTags = swaggerDoc.Paths.Values
                .SelectMany(p => p.Operations.Values)
                .SelectMany(o => o.Tags)
                .Select(t => t.Name)
                .Distinct()
                .ToHashSet();

            // Build ordered tag list: known tags first (in defined order), then any unknown ones alphabetically
            var ordered = Order
                .Where(usedTags.Contains)
                .Select(name => new OpenApiTag { Name = name })
                .ToList();

            var unknown = usedTags
                .Where(t => !Order.Contains(t))
                .OrderBy(t => t)
                .Select(name => new OpenApiTag { Name = name });

            ordered.AddRange(unknown);
            swaggerDoc.Tags = ordered;

            // Also reorder paths so they group correctly within the spec
            var sortedPaths = swaggerDoc.Paths
                .OrderBy(p => p.Value.Operations.Values
                    .SelectMany(o => o.Tags)
                    .Select(t => IndexOf(t.Name))
                    .DefaultIfEmpty(int.MaxValue)
                    .Min())
                .ToList();

            swaggerDoc.Paths.Clear();
            foreach (var path in sortedPaths)
                swaggerDoc.Paths.Add(path.Key, path.Value);
        }

        private static int IndexOf(string tag)
        {
            var i = Order.IndexOf(tag);
            return i >= 0 ? i : int.MaxValue;
        }
    }
}
