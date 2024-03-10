using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1591

namespace IdentityServer4.Extensions
{
    public static class IEnumerableExtensions
    {
        public static bool IsNullOrEmptyList<T>(this IEnumerable<T> list)
        {
            if (list == null)
            {
                return true;
            }

            if (!list.Any())
            {
                return true;
            }

            return false;
        }
    }
}