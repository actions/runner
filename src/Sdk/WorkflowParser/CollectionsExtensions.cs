using System.Collections.Generic;

namespace GitHub.Actions.WorkflowParser
{
    internal static class CollectionsExtensions
    {
        /// <summary>
        /// Adds all of the given values to this collection.
        /// Can be used with dictionaries, which implement <see cref="ICollection{T}"/> and <see cref="IEnumerable{T}"/> where T is <see cref="KeyValuePair{TKey, TValue}"/>.
        /// </summary>
        public static TCollection AddRange<T, TCollection>(this TCollection collection, IEnumerable<T> values)
            where TCollection : ICollection<T>
        {
            foreach (var value in values)
            {
                collection.Add(value);
            }

            return collection;
        }
    }
}
