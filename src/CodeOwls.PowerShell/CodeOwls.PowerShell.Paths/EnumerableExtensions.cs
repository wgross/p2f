using System;
using System.Collections.Generic;

namespace CodeOwls.PowerShell.Paths
{
    public static class EnumerableExtensions
    {
        public static void Foreach<T>(this IEnumerable<T> thisEnumerable, Action<T> action)
        {
            foreach (var e in thisEnumerable)
                action(e);
        }
    }
}