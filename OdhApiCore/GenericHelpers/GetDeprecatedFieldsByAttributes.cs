// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataModel.Annotations;

namespace OdhApiCore.GenericHelpers
{
    public class GetDeprecatedFieldsByAttributes
    {
        public static IEnumerable<DeprecationInfo> GetDeprecatedFields(Type type, string odhtype)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var deprecatedprops = props.Where(p =>
                p.GetCustomAttribute(typeof(SwaggerDeprecatedAttribute), true) != null
            );

            //foreach(var deprecatedprop in deprecatedprops)
            //{

            //}

            return deprecatedprops
                .Select(x => new DeprecationInfo()
                {
                    Type = odhtype,
                    Name = x.Name,
                    Description = x.GetCustomAttribute<SwaggerDeprecatedAttribute>().Description,
                    PropertyType = FriendlyName(x.PropertyType),
                    DeprecationDate =
                        x.GetCustomAttribute<SwaggerDeprecatedAttribute>().DeprecationDate,
                    RemovedAfter = x.GetCustomAttribute<SwaggerDeprecatedAttribute>().RemovedAfter,
                })
                .ToList();
        }

        public static string FriendlyName(Type type)
        {
            if (type.IsGenericType)
            {
                var namePrefix = type.Name.Split(
                    new[] { '`' },
                    StringSplitOptions.RemoveEmptyEntries
                )[0];
                var genericParameters = String.Join(
                    ", ",
                    type.GetGenericArguments().Select(FriendlyName)
                );
                return namePrefix + "<" + genericParameters + ">";
            }

            return type.Name;
        }
    }

    public class DeprecationInfo
    {
        public string? Type { get; set; }
        public string? Name { get; set; }
        public string? PropertyType { get; set; }
        public string? Description { get; set; }
        public DateTime? DeprecationDate { get; set; }
        public DateTime? RemovedAfter { get; set; }
    }
}
