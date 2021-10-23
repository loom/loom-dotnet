using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Loom
{
    public static class CollectionExtensions
    {
        private static readonly Random _random = new();

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return from e in source
                   orderby _random.Next()
                   select e;
        }

        public static T Sample<T>(this IEnumerable<T> source)
        {
            return source.Shuffle().First();
        }

        public static IEnumerable<T> Sample<T>(this IEnumerable<T> source, int count)
        {
            return source.Shuffle().Take(count);
        }

        public static bool TryGetValue(this IDictionary dictionary, object key, out object value)
        {
            if (dictionary.Contains(key))
            {
                value = dictionary[key];
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
