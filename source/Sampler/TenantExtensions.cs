using System;
using System.Collections.Generic;
using System.Linq;
using Octopus.Client.Model;

namespace Octopus.Sampler
{
    public static class TenantExtensions
    {
        // ReSharper disable once InconsistentNaming
        public static bool IsVIP(this TenantResource tenant)
        {
            return tenant.Name.ToLowerInvariant().IndexOfAny(new[] { 'v' }) >= 0;
        }

        public static bool IsTrial(this TenantResource tenant)
        {
            return tenant.Name.ToLowerInvariant().IndexOfAny(new[] { 'u' }) >= 0;
        }

        public static bool IsBetaTester(this TenantResource tenant)
        {
            return tenant.Name.ToLowerInvariant().IndexOfAny(new[] { 'b' }) >= 0;
        }

        public static bool IsEarlyAdopter(this TenantResource tenant)
        {
            return tenant.Name.ToLowerInvariant().IndexOfAny(new[] { 'e' }) >= 0;
        }

        static readonly Random Randomizer = new Random();
        public static T GetRandom<T>(this IEnumerable<T> source)
        {
            var enumerable = source as T[] ?? source.ToArray();
            return enumerable.ElementAt(Randomizer.Next(0, enumerable.Count() - 1));
        }
    }
}