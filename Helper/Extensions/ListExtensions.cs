// SPDX-FileCopyrightText: NOI Techpark <digital@noi.bz.it>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helper.Extensions
{
    public static class ListExtensions
    {
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static void TryAddOrUpdateOnList(
            this ICollection<string> smgtags,
            ICollection<string> tagsToAdd
        )
        {
            foreach (var tag in tagsToAdd)
            {
                smgtags.TryAddOrUpdateOnList(tag);
            }
        }

        public static void TryRemoveOnList(this ICollection<string> smgtags, string tagToRemove)
        {
            if (smgtags.Contains(tagToRemove))
                smgtags.Remove(tagToRemove);
        }

        public static void TryRemoveOnList(
            this ICollection<string> smgtags,
            ICollection<string> tagsToRemove
        )
        {
            foreach (var tag in tagsToRemove)
            {
                smgtags.TryRemoveOnList(tag);
            }
        }

        public static void TryAddOrUpdateOnList(this ICollection<string> smgtags, string tagToAdd)
        {
            if (!smgtags.Contains(tagToAdd))
                smgtags.Add(tagToAdd);
        }

        public static void RemoveEmptyStrings(this ICollection<string>? smgtags)
        {
            if (smgtags != null && smgtags.Count > 0)
            {
                // If it's a List, use RemoveAll for better performance
                if (smgtags is List<string> list)
                {
                    list.RemoveAll(string.IsNullOrEmpty);
                }
                else
                {
                    // For other ICollection types, remove items individually
                    var emptyStrings = smgtags.Where(string.IsNullOrEmpty).ToList();
                    foreach (var empty in emptyStrings)
                    {
                        smgtags.Remove(empty);
                    }
                }
            }
        }

        public static IEnumerable<string> UnionIfNotNull(
            this ICollection<string>? sourceunion,
            ICollection<string>? listtounion
        )
        {
            if (sourceunion != null && listtounion != null)
                return sourceunion.Union(listtounion);
            else if (sourceunion == null && listtounion != null)
                return listtounion;
            else if (sourceunion != null && listtounion == null)
                return sourceunion;
            else
                return new List<string>();
        }

        public static ICollection<string>? ConverListToLowerCase(this ICollection<string>? smgtags)
        {
            if (smgtags != null && smgtags.Count > 0)
                return smgtags.Select(d => d.ToLower()).ToList();
            else
                return smgtags;
        }

        /// <summary>
        /// Removes all items from list1 that are present in list2 (case-insensitive comparison)
        /// </summary>
        /// <param name="list1">The list to remove items from</param>
        /// <param name="list2">The list containing items to be removed from list1</param>
        public static List<string>? RemoveItemsPresentInOtherList(List<string> list1, List<string> list2)
        {
            if (list1 == null || list2 == null || list2.Count == 0)
                return list1;

            // Create a HashSet from list2 with case-insensitive comparison for O(1) lookups
            var list2Set = new HashSet<string>(list2, StringComparer.OrdinalIgnoreCase);

            // Remove items from list1 that exist in list2 (case-insensitive)
            list1.RemoveAll(item => list2Set.Contains(item));

            return list1;
        }
    }
}
