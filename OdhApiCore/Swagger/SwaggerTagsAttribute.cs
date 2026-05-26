// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Http.Metadata;
using System;
using System.Collections.Generic;

namespace OdhApiCore.Swagger
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class SwaggerTagsAttribute : Attribute, ITagsMetadata
    {
        public SwaggerTagsAttribute(params string[] tags) => Tags = tags;
        public IReadOnlyList<string> Tags { get; }
    }
}
