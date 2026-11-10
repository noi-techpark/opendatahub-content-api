// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DataModel.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Helper.JsonHelpers
{
    public class GetOnlyContractResolver : DefaultContractResolver
    {
        //Use this shared instance, the resolver caches the contracts (and the generated reflection code) per instance,
        //creating a new one for every deserialization recompiles all of it and lets the memory grow
        public static readonly GetOnlyContractResolver Instance = new GetOnlyContractResolver();

        protected override JsonProperty CreateProperty(
            MemberInfo member,
            MemberSerialization memberSerialization
        )
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property != null && property.Readable && property.AttributeProvider != null)
            {
                var attributes = property.AttributeProvider.GetAttributes(
                    typeof(GetOnlyJsonPropertyAttribute),
                    true
                );
                if (attributes != null && attributes.Count > 0)
                    property.Readable = false;
            }
            return property;
        }
    }
}
