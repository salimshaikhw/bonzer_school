using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Extensions
{
    public static class CollectionExtensions
    {
        public static List<List<T>> Split<T>(this List<T> source, int group)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / group)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static bool IsAny<T>(this IEnumerable<T> collection)
        {
            return collection != null && collection.Any();
        }
    }
}
