// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataModel.Annotations
{
    //[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    //public class SwaggerDeprecatedAttribute : Attribute
    //{
    //    public SwaggerDeprecatedAttribute(string? description = null)
    //    {
    //        Description = description;
    //    }

    //    public string Description { get; }
    //}

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class SwaggerDeprecatedAttribute : Attribute
    {
        public SwaggerDeprecatedAttribute(
            string? description = null,
            string? deprecationdate = null,
            string? removedafter = null
        )
        {
            Description = description;

            if (DateTime.TryParse(deprecationdate, out DateTime deprecationdatetemp))
                DeprecationDate = deprecationdatetemp;
            else
                DeprecationDate = null;

            if (DateTime.TryParse(removedafter, out DateTime removedaftertemp))
                RemovedAfter = removedaftertemp;
            else
                RemovedAfter = null;
        }

        public string Description { get; }
        public DateTime? DeprecationDate { get; }
        public DateTime? RemovedAfter { get; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class SwaggerEnumAttribute : Attribute
    {
        public SwaggerEnumAttribute(string[] enumValues)
        {
            EnumValues = enumValues;
        }

        public IEnumerable<string> EnumValues { get; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class GetOnlyJsonPropertyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
    public class SwaggerReferenceAttribute : Attribute
    {
        public SwaggerReferenceAttribute(Type referenceto, string? description = null)
        {
            Description = description;
            ReferenceTo = referenceto;
        }

        public string Description { get; }
        public Type ReferenceTo { get; }
    }
    
    [AttributeUsage(AttributeTargets.Property)]
    public class PolymorphicDictionaryAttribute : Attribute
    {
        public Dictionary<string, Type> TypeMapping { get; }

        public PolymorphicDictionaryAttribute(params object[] keyTypePairs)
        {
            TypeMapping = new Dictionary<string, Type>();

            for (int i = 0; i < keyTypePairs.Length; i += 2)
            {
                if (i + 1 < keyTypePairs.Length)
                {
                    var key = keyTypePairs[i].ToString();
                    var type = keyTypePairs[i + 1] as Type;
                    if (key != null && type != null)
                    {
                        TypeMapping[key] = type;
                    }
                }
            }
        }
    }
}
