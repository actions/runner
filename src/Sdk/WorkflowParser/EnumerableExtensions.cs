#nullable disable // Temporary: should be removed and issues fixed manually

using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Actions.WorkflowParser
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Creates a HashSet with equality comparer <paramref name="comparer"/> based on the elements
        /// in <paramref name="source"/>, using transformation function <paramref name="selector"/>.
        /// </summary>
        public static HashSet<TOut> ToHashSet<TIn, TOut>(
            this IEnumerable<TIn> source,
            Func<TIn, TOut> selector,
            IEqualityComparer<TOut> comparer)
        {
            return new HashSet<TOut>(source.Select(selector), comparer);
        }
    }
}
