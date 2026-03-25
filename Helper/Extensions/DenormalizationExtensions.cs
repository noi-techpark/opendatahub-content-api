using DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Helper
{
    public static class DenormalizationExtensions
    {
        /// <summary>
        /// Denormalizes an EventLinked record by expanding a specified collection property,
        /// returning one EventLinked clone per item in that collection.
        /// </summary>
        public static IEnumerable<EventLinked> DenormalizeBy<TItem>(
            this EventLinked source,
            Func<EventLinked, ICollection<TItem>?> collectionSelector,
            Action<EventLinked, ICollection<TItem>?> collectionSetter)
        {
            var collection = collectionSelector(source);

            if (collection == null || !collection.Any())
            {
                yield return source;
                yield break;
            }

            foreach (var item in collection)
            {
                var clone = source.DeepClone();
                collectionSetter(clone, new List<TItem> { item });
                yield return clone;
            }
        }

        /// <summary>
        /// Deep clone via JSON serialization (simple and safe for data models).
        /// </summary>
        private static EventLinked DeepClone(this EventLinked source)
        {
            var json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<EventLinked>(json)!;
        }

        public static IEnumerable<T> DenormalizeBy<T, TItem>(
        this T source,
        Func<T, ICollection<TItem>?> collectionSelector,
        Action<T, ICollection<TItem>?> collectionSetter)
        where T : class
        {
            var collection = collectionSelector(source);

            if (collection == null || !collection.Any())
            {
                yield return source;
                yield break;
            }

            foreach (var item in collection)
            {
                var clone = source.DeepClone();
                collectionSetter(clone, new List<TItem> { item });
                yield return clone;
            }
        }

        private static T DeepClone<T>(this T source) where T : class
        {
            var json = JsonSerializer.Serialize(source);
            return JsonSerializer.Deserialize<T>(json)!;
        }
    }
}
