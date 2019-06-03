using System.Collections.Generic;

namespace GitHub.Services.Common
{
    public static class CollectionsExtensions
    {
        /// <summary>
        /// Adds all of the given values to this collection.
        /// Can be used with dictionaries, which implement <see cref="ICollection{T}"/> and <see cref="IEnumerable{T}"/> where T is <see cref="KeyValuePair{TKey, TValue}"/>.
        /// For dictionaries, also see <see cref="DictionaryExtensions.SetRange{K, V, TDictionary}(TDictionary, IEnumerable{KeyValuePair{K, V}})"/>
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

        /// <summary>
        /// Adds all of the given values to this collection if and only if the values object is not null.
        /// See <see cref="AddRange{T, TCollection}(TCollection, IEnumerable{T})"/> for more details.
        /// </summary>
        public static TCollection AddRangeIfRangeNotNull<T, TCollection>(this TCollection collection, IEnumerable<T> values)
            where TCollection : ICollection<T>
        {
            if (values != null)
            {
                collection.AddRange(values);
            }

            return collection;
        }
    }
}
