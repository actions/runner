using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitHub.Services.Content.Common
{
    public static class Extensions
    {
        private static Int32 encodingBufferSize = 4096;

        public static Stream ToStream(this String input)
        {
            return input.ToStream(StrictEncodingWithoutBOM.UTF8);
        }

        public static Stream ToStream(this String input, Encoding encoding)
        {
            MemoryStream output = new MemoryStream();
            using (StreamWriter writer = new StreamWriter(output, encoding, encodingBufferSize, true))
            {
                writer.Write(input);
                writer.Flush();
                output.Position = 0;
            }

            return output;
        }

        // This would be better as "ToString" but that conflicts with Object.ToString().
        public static String GetString(this Stream input)
        {
            // this doesn't require but won't reject a BOM
            using (StreamReader reader = new StreamReader(input, StrictEncodingWithBOM.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        public static IEnumerable<KeyValuePair<T3, T4>> KeyValueSelect<T1, T2, T3, T4>(this IEnumerable<KeyValuePair<T1, T2>> items,
            Func<KeyValuePair<T1, T2>, T3> keySelect, Func<KeyValuePair<T1, T2>, T4> valueSelect)
        {
            return items.Select(kvp => new KeyValuePair<T3, T4>(keySelect(kvp), valueSelect(kvp)));
        }

        public static IEnumerable<KeyValuePair<T3, T4>> BaseCast<T1, T2, T3, T4>(this IEnumerable<KeyValuePair<T1, T2>> items)
            where T1 : T3
            where T2 : T4
        {
            return items.KeyValueSelect(kvp => (T3)kvp.Key, kvp => (T4)kvp.Value);
        }

        public static IEnumerable<T2> Values<T1, T2>(this IEnumerable<KeyValuePair<T1, T2>> items)
        {
            return items.Select(kvp => kvp.Value);
        }

        public static IEnumerable<List<T>> GetPages<T>(this IEnumerable<T> source, int pageSize)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize must be > 0");
            }

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var currentPage = new List<T>(pageSize)
                    {
                        enumerator.Current
                    };

                    while (currentPage.Count < pageSize && enumerator.MoveNext())
                    {
                        currentPage.Add(enumerator.Current);
                    }

                    yield return currentPage;
                }
            }
        }

        [CLSCompliant(false)]
        public static ulong Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, ulong> selector)
        {
            return source.Aggregate<TSource, ulong>(0, (acc, item) => (acc + selector(item)));
        }

        public static IEnumerable<IDictionary<TKey, IEnumerable<TValue>>> GetDictionaryPages<TKey, TValue>(this IDictionary<TKey, IEnumerable<TValue>> source, int pageSize)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize must be > 0");
            }

            int currentCount = 0;

            var dictionary = new Dictionary<TKey, IEnumerable<TValue>>();

            foreach (var kvp in source)
            {
                IEnumerable<TValue> list = kvp.Value;

                while (list.Any())
                {
                    int neededElements = pageSize - currentCount;

                    var toAdd = list.Take(neededElements);
                    dictionary[kvp.Key] = toAdd;
                    list = list.Skip(neededElements);

                    currentCount += toAdd.Count();

                    if (currentCount == pageSize)
                    {
                        yield return dictionary;
                        dictionary = new Dictionary<TKey, IEnumerable<TValue>>();
                        currentCount = 0;
                    }
                }
            }

            if (currentCount != 0)
            {
                yield return dictionary;
            }
        }

        public static IDictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> kvps)
        {
            return kvps.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static IDictionary<TKey, IList<TValue>> ToDictionaryOfLists<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> groupings, int? maxToTakeInEachGroup = null)
        {
            return groupings.ToDictionary(grouping => grouping.Key,
                grouping => (IList<TValue>)(maxToTakeInEachGroup.HasValue ? grouping.Take(maxToTakeInEachGroup.Value) : grouping).ToList());
        }

        public static IDictionary<TKey, IEnumerable<TValue>> ToDictionaryOfEnumerables<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> groupings, int? maxToTakeInEachGroup = null)
        {
            return groupings.ToDictionary(grouping => grouping.Key,
                grouping => (maxToTakeInEachGroup.HasValue ? grouping.Take(maxToTakeInEachGroup.Value) : grouping));
        }

        public static IDictionary<TKey, IEnumerable<TValue>> ToDictionaryOfEnumerables<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> kvps, int? maxToTakeInEachGroup = null)
        {
            return kvps.ToDictionary(kvp => kvp.Key,
                kvp => (maxToTakeInEachGroup.HasValue ? kvp.Value.Take(maxToTakeInEachGroup.Value) : kvp.Value));
        }

        /// <summary>
        /// Creates a dictionary from a collection with the first key wins. If identical keys are present, an assertion is made to confirm that the values are the same.
        /// </summary>
        public static IDictionary<TKey, TElement> ToDictionaryFirstKeyWins<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
        {
            IEnumerable<KeyValuePair<TKey, TElement>> kvps = source.Select(s => new KeyValuePair<TKey, TElement>(keySelector(s), elementSelector(s)));
            return kvps.ToDictionaryFirstKeyWins();
        }

        /// <summary>
        /// Creates a dictionary from a collection with the first key wins. If identical keys are present, an assertion is made to confirm that the values are the same.
        /// </summary>
        public static IDictionary<TKey, TValue> ToDictionaryFirstKeyWins<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> kvps)
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (KeyValuePair<TKey, TValue> kvp in kvps)
            {
                if (!result.ContainsKey(kvp.Key))
                {
                    result.Add(kvp.Key, kvp.Value);
                }
                else
                {
                    if (!result[kvp.Key].Equals(kvp.Value))
                    {
                        throw new ArgumentException("Identical keys have different values.");
                    }
                }
            }
            return result;
        }

        /// <summary>
        ///     Attempt to remove an item from the ConcurrentDictionary.
        /// </summary>
        /// <remarks>
        ///     http://blogs.msdn.com/b/pfxteam/archive/2011/04/02/10149222.aspx
        /// </remarks>
        public static bool TryRemoveSpecific<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <summary>
        /// https://blogs.msdn.microsoft.com/pfxteam/2012/02/04/building-a-custom-getoradd-method-for-concurrentdictionarytkeytvalue/
        /// </summary>
        /// <returns>True if added</returns>
        public static bool GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, TValue value, out TValue finalValue)
        {
            while (true)
            {
                if (dict.TryGetValue(key, out finalValue))
                {
                    return false;
                }

                if (dict.TryAdd(key, value))
                {
                    finalValue = value;
                    return true;
                }
            }
        }

        /// <summary>
        /// https://blogs.msdn.microsoft.com/pfxteam/2012/02/04/building-a-custom-getoradd-method-for-concurrentdictionarytkeytvalue/
        /// </summary>
        /// <returns>True if added</returns>
        public static bool GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> generator, out TValue finalValue)
        {
            while (true)
            {
                if (dict.TryGetValue(key, out finalValue))
                {
                    return false;
                }

                finalValue = generator(key);
                if (dict.TryAdd(key, finalValue))
                {
                    return true;
                }
            }
        }

        public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("Values cannot be null when adding into conccurent bag.");
            }

            foreach (T value in values)
            {
                bag.Add(value);
            }
        }

        public static DateTimeOffset RoundDownUtc(this DateTimeOffset dateToRound, DateTimeOffset baseDate, TimeSpan roundAmount)
        {
            if (dateToRound.Offset != TimeSpan.Zero)
            {
                throw new ArgumentException($"{nameof(dateToRound)} is not UTC.");
            }

            if (baseDate.Offset != TimeSpan.Zero)
            {
                throw new ArgumentException($"{nameof(baseDate)} is not UTC.");
            }

            if (dateToRound < baseDate)
            {
                throw new ArgumentException($"{nameof(dateToRound)} is before {nameof(baseDate)}.");
            }

            if (roundAmount < TimeSpan.Zero)
            {
                throw new ArgumentException($"{nameof(roundAmount)} must be greater than zero.");
            }

            long ticksAfterBase = (dateToRound - baseDate).Ticks;
            long periodsAfterBase = ticksAfterBase / roundAmount.Ticks;
            long roundedTicksAfterBase = periodsAfterBase * roundAmount.Ticks;
            DateTimeOffset rounded = new DateTimeOffset(baseDate.Ticks + roundedTicksAfterBase, offset: TimeSpan.Zero);

            if (rounded > dateToRound)
            {
                throw new Exception($"{nameof(RoundDownUtc)}: {nameof(rounded)} {rounded} > {nameof(dateToRound)} {dateToRound}");
            }

            if (rounded <= dateToRound - roundAmount)
            {
                throw new Exception($"{nameof(RoundDownUtc)}: {nameof(rounded)} {rounded} <= {nameof(dateToRound)} {dateToRound} - {nameof(roundAmount)} {roundAmount}");
            }

            return rounded;
        }

        public static DateTimeOffset RoundUpUtc(this DateTimeOffset dateToRound, DateTimeOffset baseDate, TimeSpan roundAmount)
        {
            if (dateToRound.Offset != TimeSpan.Zero)
            {
                throw new ArgumentException($"{nameof(dateToRound)} is not UTC.");
            }

            if (baseDate.Offset != TimeSpan.Zero)
            {
                throw new ArgumentException($"{nameof(baseDate)} is not UTC.");
            }

            if (dateToRound < baseDate)
            {
                throw new ArgumentException($"{nameof(dateToRound)} is before {nameof(baseDate)}.");
            }

            if (roundAmount < TimeSpan.Zero)
            {
                throw new ArgumentException($"{nameof(roundAmount)} must be greater than zero.");
            }

            var dateToRoundDown = new DateTimeOffset(dateToRound.Ticks + roundAmount.Ticks - 1, offset: TimeSpan.Zero);
            DateTimeOffset rounded = dateToRoundDown.RoundDownUtc(baseDate, roundAmount);

            if (rounded < dateToRound)
            {
                throw new Exception($"{nameof(RoundUpUtc)}: {nameof(rounded)} {rounded} < {nameof(dateToRound)} {dateToRound}");
            }

            if (rounded >= dateToRound + roundAmount)
            {
                throw new Exception($"{nameof(RoundUpUtc)}: {nameof(rounded)} {rounded} >= {nameof(dateToRound)} {dateToRound} + {nameof(roundAmount)} {roundAmount}");
            }

            return rounded;
        }

        public static DateTime Max(this DateTime date1, DateTime date2)
        {
            return DateTime.FromFileTimeUtc(Math.Max(date1.ToFileTimeUtc(), date2.ToFileTimeUtc()));
        }
    }
}
