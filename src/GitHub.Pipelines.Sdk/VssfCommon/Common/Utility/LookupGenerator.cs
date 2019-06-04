using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Services.Common.Utility
{
    /// <summary>
    /// The <see cref="LookupGenerator"/> is a collection of functions to create 
    /// lookup delegates, storing any relevant state via closure.
    /// </summary>
    public static class LookupGenerator
    {
        public static Func<T, TOut> CreateLookupWithDefault<T, TOut>(
            TOut @default,
            params KeyValuePair<T, TOut>[] values)
        {
            return CreateLookupWithDefault(EqualityComparer<T>.Default, @default, values);
        }

        public static Func<T, TOut> CreateLookupWithDefault<T, TOut>(
            IEqualityComparer<T> comparer,
            TOut @default,
            params KeyValuePair<T, TOut>[] values)
        {
            var lookup = values.ToDictionary(pair => pair.Key, pair => pair.Value, comparer);

            return key => lookup.GetValueOrDefault(key, @default);
        }

        /// <summary>
        /// Creates a lookup function using closure encapsulation of an <see cref="IReadOnlyDictionary{T, TOut}"/>
        /// to handle the lookup table.  If the requested key is not found, the lookup will return the provided
        /// default.
        /// </summary>
        /// <typeparam name="T">The key of the lookup</typeparam>
        /// <typeparam name="TOut">The value stored in the lookup table</typeparam>
        /// <param name="default">A default value returned if the key is not found</param>
        /// <param name="dictionary">The lookup table, stored in the closure of the lookup function</param>
        /// <returns>A lookup function which returns the value at the requested key, or a default value.</returns>
        public static Func<T, TOut> CreateLookupWithDefault<T, TOut>(
            TOut @default,
            IReadOnlyDictionary<T, TOut> dictionary)
        {
            return key => dictionary.GetValueOrDefault(key, @default);
        }
    }
}
