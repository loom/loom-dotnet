namespace Loom
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class CollectionExtensions
    {
        private static readonly Random _random = new Random();

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
    }
}
