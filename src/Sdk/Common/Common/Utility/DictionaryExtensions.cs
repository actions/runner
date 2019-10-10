using GitHub.Services.Common.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Linq;

namespace GitHub.Services.Common
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Adds a new value to the dictionary or updates the value if the entry already exists.
        /// Returns the updated value inserted into the dictionary.
        /// </summary>
        public static V AddOrUpdate<K, V>(this IDictionary<K, V> dictionary,
            K key, V addValue, Func<V, V, V> updateValueFactory)
        {
            if (dictionary.TryGetValue(key, out V returnValue))
            {
                addValue = updateValueFactory(returnValue, addValue);
            }

            dictionary[key] = addValue;
            return addValue;
        }

        /// <summary>
        /// Returns the value in an IDictionary at the given key, or the default
        /// value for that type if it is not present.
        /// </summary>
        public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dictionary, K key, V @default = default(V))
        {
            V value;
            return dictionary.TryGetValue(key, out value) ? value : @default;
        }

        /// <summary>
        /// Returns the value in an IReadOnlyDictionary at the given key, or the default
        /// value for that type if it is not present.
        /// </summary>
        public static V GetValueOrDefault<K, V>(this IReadOnlyDictionary<K, V> dictionary, K key, V @default = default(V))
        {
            V value;
            return dictionary.TryGetValue(key, out value) ? value : @default;
        }

        /// <summary>
        /// Returns the value in a Dictionary at the given key, or the default
        /// value for that type if it is not present.
        /// </summary>
        /// <remarks>
        /// This overload is necessary to prevent Ambiguous Match issues, as Dictionary implements both
        /// IDictionary and IReadonlyDictionary, but neither interface implements the other
        /// </remarks>
        public static V GetValueOrDefault<K, V>(this Dictionary<K, V> dictionary, K key, V @default = default(V))
        {
            V value;
            return dictionary.TryGetValue(key, out value) ? value : @default;
        }

        /// <summary>
        /// Returns the value in an IDictionary at the given key, or the default
        /// nullable value for that type if it is not present.
        /// </summary>
        public static V? GetNullableValueOrDefault<K, V>(this IDictionary<K, V> dictionary, K key, V? @default = default(V?)) where V : struct
        {
            V value;
            return dictionary.TryGetValue(key, out value) ? value : @default;
        }

        /// <summary>
        /// Returns the value in an IReadOnlyDictionary at the given key, or the default
        /// nullable value for that type if it is not present.
        /// </summary>
        public static V? GetNullableValueOrDefault<K, V>(this IReadOnlyDictionary<K, V> dictionary, K key, V? @default = default(V?)) where V : struct
        {
            V value;
            return dictionary.TryGetValue(key, out value) ? value : @default;
        }

        /// <summary>
        /// Returns the value in a Dictionary at the given key, or the default
        /// nullable value for that type if it is not present.
        /// </summary>
        /// <remarks>
        /// This overload is necessary to prevent Ambiguous Match issues, as Dictionary implements both
        /// IDictionary and IReadonlyDictionary, but neither interface implements the other
        /// </remarks>
        public static V? GetNullableValueOrDefault<K, V>(this Dictionary<K, V> dictionary, K key, V? @default = default(V?)) where V : struct
        {
            V value;
            return dictionary.TryGetValue(key, out value) ? value : @default;
        }

        /// <summary>
        /// Returns the value in an IReadonlyDictionary with values of type <see cref="object"/>  
        /// casted as values of requested type, or the defualt if the key is not found or 
        /// if the value was found but not compatabile with the requested type.
        /// </summary>
        /// <typeparam name="K">The key type</typeparam>
        /// <typeparam name="V">The requested type of the stored value</typeparam>
        /// <param name="dictionary">the dictionary to perform the lookup on</param>
        /// <param name="key">The key to lookup</param>
        /// <param name="default">Optional: the default value to return if not found</param>
        /// <returns>The value at the key, or the default if it is not found or of the wrong type</returns>
        public static V GetCastedValueOrDefault<K, V>(this IReadOnlyDictionary<K, object> dictionary, K key, V @default = default(V))
        {
            object value;
            return dictionary.TryGetValue(key, out value) && value is V ? (V)value : @default;
        }

        /// <summary>
        /// Returns the value in an IDictionary with values of type <see cref="object"/>  
        /// casted as values of requested type, or the defualt if the key is not found or 
        /// if the value was found but not compatabile with the requested type.
        /// </summary>
        /// <typeparam name="K">The key type</typeparam>
        /// <typeparam name="V">The requested type of the stored value</typeparam>
        /// <param name="dictionary">the dictionary to perform the lookup on</param>
        /// <param name="key">The key to lookup</param>
        /// <param name="default">Optional: the default value to return if not found</param>
        /// <returns>The value at the key, or the default if it is not found or of the wrong type</returns>
        public static V GetCastedValueOrDefault<K, V>(this IDictionary<K, object> dictionary, K key, V @default = default(V))
        {
            object value;
            return dictionary.TryGetValue(key, out value) && value is V ? (V)value : @default;
        }

        /// <summary>
        /// Returns the value in a Dictionary with values of type <see cref="object"/>  
        /// casted as values of requested type, or the defualt if the key is not found or 
        /// if the value was found but not compatabile with the requested type.
        /// </summary>
        /// <remarks>
        /// This overload is necessary to prevent Ambiguous Match issues, as Dictionary implements both
        /// IDictionary and IReadonlyDictionary, but neither interface implements the other
        /// </remarks>
        /// <typeparam name="K">The key type</typeparam>
        /// <typeparam name="V">The requested type of the stored value</typeparam>
        /// <param name="dictionary">the dictionary to perform the lookup on</param>
        /// <param name="key">The key to lookup</param>
        /// <param name="default">Optional: the default value to return if not found</param>
        /// <returns>The value at the key, or the default if it is not found or of the wrong type</returns>
        public static V GetCastedValueOrDefault<K, V>(this Dictionary<K, object> dictionary, K key, V @default = default(V))
        {
            return ((IReadOnlyDictionary<K, object>)dictionary).GetCastedValueOrDefault(key, @default);
        }

        /// <summary>
        /// Returns the value in an IDictionary at the given key, or creates a new value using the default constructor, adds it at the given key, and returns the new value.
        /// </summary>
        public static V GetOrAddValue<K, V>(this IDictionary<K, V> dictionary, K key) where V : new()
        {
            V value = default(V);

            if (!dictionary.TryGetValue(key, out value))
            {
                value = new V();
                dictionary.Add(key, value);
            }

            return value;
        }

        /// <summary>
        /// Returns the value in an IDictionary at the given key, or creates a new value using the given delegate, adds it at the given key, and returns the new value.
        /// </summary>
        public static V GetOrAddValue<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> createValueToAdd)
        {
            V value = default(V);

            if (!dictionary.TryGetValue(key, out value))
            {
                value = createValueToAdd();
                dictionary.Add(key, value);
            }

            return value;
        }

        /// <summary>
        /// Adds all of the given key-value pairs (such as from another dictionary, since IDictionary implements IEnumerable<KeyValuePair>) to this dictionary.
        /// Overwrites preexisting values of the same key.
        /// To avoid overwriting values, use <see cref="CollectionsExtensions.AddRange{T, TCollection}(TCollection, IEnumerable{T})"/>.
        /// </summary>
        /// <returns>this dictionary</returns>
        public static TDictionary SetRange<K, V, TDictionary>(this TDictionary dictionary, IEnumerable<KeyValuePair<K, V>> keyValuePairs)
            where TDictionary : IDictionary<K, V>
        {
            foreach (var keyValuePair in keyValuePairs)
            {
                dictionary[keyValuePair.Key] = keyValuePair.Value;
            }

            return dictionary;
        }

        /// <summary>
        /// Adds all of the given key-value pairs if and only if the key-value pairs object is not null.
        /// See <see cref="SetRange{K, V, TDictionary}(TDictionary, IEnumerable{KeyValuePair{K, V}})"/> for more details.
        /// </summary>
        /// <returns>this dictionary</returns>
        public static TDictionary SetRangeIfRangeNotNull<K, V, TDictionary>(this TDictionary dictionary, IEnumerable<KeyValuePair<K, V>> keyValuePairs)
            where TDictionary : IDictionary<K, V>
        {
            if (keyValuePairs != null)
            {
                dictionary.SetRange(keyValuePairs);
            }

            return dictionary;
        }

        /// <summary>
        /// Adds all of the given key-value pairs to this lazily initialized dictionary if and only if the key-value pairs object is not null or empty.
        /// Does not initialize the dictionary otherwise.
        /// See <see cref="SetRange{K, V, TDictionary}(TDictionary, IEnumerable{KeyValuePair{K, V}})"/> for more details.
        /// </summary>
        /// <returns>this dictionary</returns>
        public static Lazy<TDictionary> SetRangeIfRangeNotNullOrEmpty<K, V, TDictionary>(this Lazy<TDictionary> lazyDictionary, IEnumerable<KeyValuePair<K, V>> keyValuePairs)
            where TDictionary : IDictionary<K, V>
        {
            if (keyValuePairs != null && keyValuePairs.Any())
            {
                lazyDictionary.Value.SetRange(keyValuePairs);
            }

            return lazyDictionary;
        }

        /// <summary>
        /// Tries to add a key to the dictionary, if it does not already exist.
        /// </summary>
        /// <param name="dictionary">The <see cref="IDictionary{TKey,TValue}"/> instance where <c>TValue</c> is <c>object</c></param>
        /// <param name="key">The key to add</param>
        /// <param name="value">The value to add</param>
        /// <returns><c>true</c> if the key was added with the specified value. If the key already exists, the method returns <c>false</c> without updating the value.</returns>
        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                return false;
            }

            dictionary.Add(key, value);
            return true;
        }

        /// <summary>
        /// Tries to add all of the given key-values pairs to the dictionary, if they do not already exist. 
        /// </summary>
        /// <param name="dictionary">The <see cref="IDictionary{TKey,TValue}"/> instance where <c>TValue</c> is <c>object</c></param>
        /// <param name="keyValuePairs">The values to try and add to the dictionary</param>
        /// <returns><c>true</c> if the all of the values were added. If any of the keys exists, the method returns <c>false</c> without updating the value.</returns>
        public static bool TryAddRange<TKey, TValue, TDictionary>(this TDictionary dictionary, IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs) where TDictionary : IDictionary<TKey, TValue>
        {
            bool rangeAdded = true;
            foreach (var keyValuePair in keyValuePairs)
            {
                rangeAdded &= dictionary.TryAdd(keyValuePair.Key, keyValuePair.Value);
            }

            return rangeAdded;
        }

        /// <summary>
        /// Gets the value of <typeparamref name="T"/> associated with the specified key or <c>default</c> value if
        /// either the key is not present or the value is not of type <typeparamref name="T"/>. 
        /// </summary>
        /// <typeparam name="T">The type of the value associated with the specified key.</typeparam>
        /// <param name="dictionary">The <see cref="IDictionary{TKey,TValue}"/> instance where <c>TValue</c> is <c>object</c>.</param>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
        /// <returns><c>true</c> if key was found, value is non-null, and value is of type <typeparamref name="T"/>; otherwise false.</returns>
        public static bool TryGetValue<T>(this IDictionary<string, object> dictionary, string key, out T value)
        {
            object valueObj;
            if (dictionary.TryGetValue(key, out valueObj))
            {
                //Handle Guids specially
                if (typeof(T) == typeof(Guid))
                {
                    Guid guidVal;
                    if (dictionary.TryGetGuid(key, out guidVal))
                    {
                        value = (T)(object)guidVal;
                        return true;
                    }
                }

                //Handle Enums specially
                if (typeof(T).GetTypeInfo().IsEnum)
                {
                    if (dictionary.TryGetEnum(key, out value))
                    {
                        return true;
                    }
                }

                if (valueObj is T)
                {
                    value = (T)valueObj;
                    return true;
                }
            }

            value = default(T);
            return false;
        }

        /// <summary>
        /// Gets the value of T associated with the specified key if the value can be converted to T according to <see cref="PropertyValidation"/>.
        /// </summary>
        /// <typeparam name="T">the type of the value associated with the specified key</typeparam>
        /// <param name="dictionary">the dictionary from which we should retrieve the value</param>
        /// <param name="key">the key of the value to retrieve</param>
        /// <param name="value">when this method returns, the value associated with the specified key, if the key is found and the value is convertible to T,
        /// or default of T, if not</param>
        /// <returns>true if the value was retrieved successfully, otherwise false</returns>
        public static bool TryGetValidatedValue<T>(this IDictionary<string, object> dictionary, string key, out T value, bool allowNull = true)
        {
            value = default(T);
            //try to convert to T. T *must* be something with
            //TypeCode != TypeCode.object (and not DBNull) OR
            //byte[] or guid or object.
            if (!PropertyValidation.IsValidConvertibleType(typeof(T)))
            {
                return false;
            }

            //special case guid...
            if (typeof(T) == typeof(Guid))
            {
                Guid guidVal;
                if (dictionary.TryGetGuid(key, out guidVal))
                {
                    value = (T)(object)guidVal;
                    return true;
                }
            }
            else
            {
                object objValue = null;
                if (dictionary.TryGetValue(key, out objValue))
                {
                    if (objValue == null)
                    {
                        //we found it and it is
                        //null, which may be okay depending on the allowNull flag
                        //value is already = default(T)
                        return allowNull;
                    }

                    if (typeof(T).GetTypeInfo().IsAssignableFrom(objValue.GetType().GetTypeInfo()))
                    {
                        value = (T)objValue;
                        return true;
                    }

                    if (typeof(T).GetTypeInfo().IsEnum)
                    {
                        if (dictionary.TryGetEnum(key, out value))
                        {
                            return true;
                        }
                    }

                    if (objValue is string)
                    {
                        TypeCode typeCode = Type.GetTypeCode(typeof(T));

                        try
                        {
                            value = (T)Convert.ChangeType(objValue, typeCode, CultureInfo.CurrentCulture);
                            return true;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the Enum value associated with the specified key if the value can be converted to an Enum.
        /// </summary>
        public static bool TryGetEnum<T>(this IDictionary<string, object> dictionary, string key, out T value)
        {
            value = default(T);

            object objValue = null;

            if (dictionary.TryGetValue(key, out objValue))
            {
                if (objValue is string)
                {
                    try
                    {
                        value = (T)Enum.Parse(typeof(T), (string)objValue, true);
                        return true;
                    }
                    catch (ArgumentException)
                    {
                        // Provided string is not a member of enumeration
                    }
                }
                else
                {
                    try
                    {
                        value = (T)objValue;
                        return true;
                    }
                    catch (InvalidCastException)
                    {
                        // Value cannot be cast to the enum
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the Guid value associated with the specified key if the value can be converted to a Guid.
        /// </summary>
        public static bool TryGetGuid(this IDictionary<string, object> dictionary, string key, out Guid value)
        {
            value = Guid.Empty;

            object objValue = null;

            if (dictionary.TryGetValue(key, out objValue))
            {
                if (objValue is Guid)
                {
                    value = (Guid)objValue;
                    return true;
                }
                else if (objValue is string)
                {
                    return Guid.TryParse((string)objValue, out value);
                }
            }

            return false;
        }

        /// <summary>
        /// Copies the values from this <see cref="IDictionary{TKey, TValue}"/> into a destination <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="source">The source dictionary from which to from.</param>
        /// <param name="dest">The destination dictionary to which to copy to.</param>
        /// <param name="filter">Optional filtering predicate.</param>
        /// <returns>The destination dictionary.</returns>
        /// <remarks>
        /// If <paramref name="dest"/> is <c>null</c>, no changes are made.
        /// </remarks>
        public static IDictionary<TKey, TValue> Copy<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> dest, Predicate<TKey> filter)
        {
            if (dest == null)
            {
                return dest;
            }

            foreach (var key in source.Keys)
            {
                if (filter == null || filter(key))
                {
                    dest[key] = source[key];
                }
            }

            return dest;
        }

        /// <summary>
        /// Copies the values from this <see cref="IDictionary{TKey, TValue}"/> into a destination <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="source">The source dictionary from which to from.</param>
        /// <param name="dest">The destination dictionary to which to copy to.</param>
        /// <returns>The destination dictionary.</returns>
        /// <remarks>
        /// If <paramref name="dest"/> is <c>null</c>, no changes are made.
        /// </remarks>
        public static IDictionary<TKey, TValue> Copy<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> dest)
        {
            return source.Copy(dest, filter: null);
        }

        /// <summary>
        /// Sets the given key-value pair if and only if the value is not null.
        /// </summary>
        public static IDictionary<TKey, TValue> SetIfNotNull<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value)
            where TValue : class
        {
            if (value != null)
            {
                dictionary[key] = value;
            }

            return dictionary;
        }

        /// <summary>
        /// Sets the given key-value pair on this lazily initialized dictionary if and only if the value is not null.
        /// Does not initialize the dictionary otherwise.
        /// </summary>
        public static Lazy<IDictionary<TKey, TValue>> SetIfNotNull<TKey, TValue>(
            this Lazy<IDictionary<TKey, TValue>> dictionary,
            TKey key,
            TValue value)
            where TValue : class
        {
            if (value != null)
            {
                dictionary.Value[key] = value;
            }

            return dictionary;
        }

        /// <summary>
        /// Adds the given key-value pair to this dictionary if the value is nonnull
        /// and does not conflict with a preexisting value for the same key.
        /// No-ops if the value is null.
        /// No-ops if the preexisting value for the same key is equal to the given value.
        /// Throws <see cref="ArgumentException"/> if the preexisting value for the same key is not equal to the given value.
        /// </summary>
        public static IDictionary<TKey, TValue> SetIfNotNullAndNotConflicting<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value,
            string valuePropertyName = "value",
            string dictionaryName = "dictionary")
            where TValue : class
        {
            if (value == null)
            {
                return dictionary;
            }

            dictionary.CheckForConflict(key, value, valuePropertyName, dictionaryName, ignoreDefaultValue: true);

            dictionary[key] = value;

            return dictionary;
        }

        /// <summary>
        /// Adds the given key-value pair to this dictionary if the value does not conflict with a preexisting value for the same key.
        /// No-ops if the preexisting value for the same key is equal to the given value.
        /// Throws <see cref="ArgumentException"/> if the preexisting value for the same key is not equal to the given value.
        /// </summary>
        public static IDictionary<TKey, TValue> SetIfNotConflicting<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value,
            string valuePropertyName = "value",
            string dictionaryName = "dictionary")
        {
            dictionary.CheckForConflict(key, value, valuePropertyName, dictionaryName, ignoreDefaultValue: false);

            dictionary[key] = value;

            return dictionary;
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if this IDictionary contains a preexisting value for the same key which is not equal to the given key.
        /// </summary>
        public static void CheckForConflict<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value,
            string valuePropertyName = "value",
            string dictionaryName = "dictionary",
            bool ignoreDefaultValue = true)
        {
            if (Equals(value, default(TValue)) && ignoreDefaultValue)
            {
                return;
            }

            TValue previousValue = default(TValue);

            if (!dictionary.TryGetValue(key, out previousValue))
            {
                return;
            }

            if (Equals(previousValue, default(TValue)) && ignoreDefaultValue)
            {
                return;
            }

            if (Equals(value, previousValue))
            {
                return;
            }

            throw new ArgumentException(
                String.Format(CultureInfo.CurrentCulture,
                              "Parameter {0} = '{1}' inconsistent with {2}['{3}'] => '{4}'",
                              valuePropertyName, value, dictionaryName, key, previousValue));
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if this IReadOnlyDictionary contains a preexisting value for the same key which is not equal to the given key.
        /// </summary>
        public static void CheckForConflict<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value,
            string valuePropertyName = "value",
            string dictionaryName = "dictionary",
            bool ignoreDefaultValue = true)
        {
            if (Equals(value, default(TValue)) && ignoreDefaultValue)
            {
                return;
            }

            TValue previousValue = default(TValue);

            if (!dictionary.TryGetValue(key, out previousValue))
            {
                return;
            }

            if (Equals(previousValue, default(TValue)) && ignoreDefaultValue)
            {
                return;
            }

            if (Equals(value, previousValue))
            {
                return;
            }

            throw new ArgumentException(
                String.Format(CultureInfo.CurrentCulture,
                              "Parameter {0} = \"{1}\" is inconsistent with {2}[\"{3}\"] => \"{4}\"",
                              valuePropertyName, value, dictionaryName, key, previousValue));
        }
    }
}
