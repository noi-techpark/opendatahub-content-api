// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.Converters
{
    public static class ListConverter
    {
        /// <summary>
        /// Converts List<string> to List<int> - throws exception on invalid values
        /// </summary>
        public static List<int> ConvertToIntList(List<string> stringList)
        {
            if (stringList == null)
                return null;

            return stringList.Select(int.Parse).ToList();
        }

        /// <summary>
        /// Converts List<string> to List<int> - skips invalid values
        /// </summary>
        public static List<int> ConvertToIntListSafe(List<string> stringList)
        {
            if (stringList == null)
                return new List<int>();

            return stringList
                .Where(s => int.TryParse(s, out _))
                .Select(int.Parse)
                .ToList();
        }

        /// <summary>
        /// Converts List<string> to List<int> - returns default value for invalid entries
        /// </summary>
        public static List<int> ConvertToIntListWithDefault(List<string> stringList, int defaultValue = 0)
        {
            if (stringList == null)
                return new List<int>();

            return stringList.Select(s => int.TryParse(s, out int result) ? result : defaultValue).ToList();
        }

        /// <summary>
        /// Converts List<string> to List<int> with culture-specific parsing
        /// </summary>
        public static List<int> ConvertToIntListWithCulture(List<string> stringList, CultureInfo culture)
        {
            if (stringList == null)
                return null;

            return stringList.Select(s => int.Parse(s, culture)).ToList();
        }

        /// <summary>
        /// Converts List<string> to List<int> - returns result with success/failure info
        /// </summary>
        public static (List<int> converted, List<string> failed) ConvertToIntListWithErrors(List<string> stringList)
        {
            var converted = new List<int>();
            var failed = new List<string>();

            if (stringList == null)
                return (converted, failed);

            foreach (var str in stringList)
            {
                if (int.TryParse(str, out int result))
                {
                    converted.Add(result);
                }
                else
                {
                    failed.Add(str);
                }
            }

            return (converted, failed);
        }

        /// <summary>
        /// Converts List<string> to List<int> - handles null/empty strings
        /// </summary>
        public static List<int> ConvertToIntListHandleNulls(List<string> stringList, int defaultValue = 0)
        {
            if (stringList == null)
                return new List<int>();

            return stringList.Select(s =>
            {
                if (string.IsNullOrWhiteSpace(s))
                    return defaultValue;

                return int.TryParse(s.Trim(), out int result) ? result : defaultValue;
            }).ToList();
        }

        /// <summary>
        /// Generic converter for any type that implements TryParse pattern
        /// </summary>
        public static List<T> ConvertList<T>(List<string> stringList, TryParseDelegate<T> tryParseMethod, T defaultValue = default(T))
        {
            if (stringList == null)
                return new List<T>();

            return stringList.Select(s =>
                tryParseMethod(s, out T result) ? result : defaultValue
            ).ToList();
        }

        // Delegate for TryParse methods
        public delegate bool TryParseDelegate<T>(string input, out T result);
    }

    // Extension methods for more natural usage
    public static class ListExtensions
    {
        /// <summary>
        /// Extension method: list.ToIntList()
        /// </summary>
        public static List<int> ToIntList(this List<string> stringList)
        {
            return ListConverter.ConvertToIntListSafe(stringList);
        }

        /// <summary>
        /// Extension method: list.ToIntList(defaultValue)
        /// </summary>
        public static List<int> ToIntList(this List<string> stringList, int defaultValue)
        {
            return ListConverter.ConvertToIntListWithDefault(stringList, defaultValue);
        }

        /// <summary>
        /// Extension method for IEnumerable<string>
        /// </summary>
        public static List<int> ToIntList(this IEnumerable<string> stringEnumerable)
        {
            return stringEnumerable?.ToList().ToIntList() ?? new List<int>();
        }
    }
}
