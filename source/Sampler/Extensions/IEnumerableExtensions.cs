using System;
using System.Collections.Generic;
using System.Linq;

namespace Octopus.Sampler.Extensions
{
    public static class IEnumerableExtensions
    {
        private static readonly Random Random = new Random();
        public static T SelectRandom<T>(this IEnumerable<T> source)
        {
            if (source == null) return default(T);

            var enumerable = source as T[] ?? source as ICollection<T> ?? source.ToArray();
            return enumerable.ElementAtOrDefault(Random.Next(0, enumerable.Count));
        }
    }
}