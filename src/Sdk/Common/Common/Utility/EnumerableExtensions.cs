using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace GitHub.Services.Common
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns an empty <see cref="IEnumerable{T}"/> if the supplied source is null.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to return when not null.</param>
        /// <returns>The source sequence, or a new empty one if source was null.</returns>
        public static IEnumerable<T> AsEmptyIfNull<T>(this IEnumerable<T> source)
            => source ?? Enumerable.Empty<T>();

        /// <summary>
        /// If an enumerable is null, and it has a default constructor, return an empty collection by calling the
        /// default constructor.
        /// </summary>
        /// <typeparam name="TEnumerable">The type of the Enumerable</typeparam>
        /// <param name="source">A sequence of values to return when not null</param>
        /// <returns>The source sequence, or a new empty one if source was null.</returns>
        public static TEnumerable AsEmptyIfNull<TEnumerable>(this TEnumerable source) where TEnumerable : class, IEnumerable, new()
            => source ?? new TEnumerable();

        /// <summary>
        /// Splits a source <see cref="IEnumerable{T}"/> into several <see cref="IList{T}"/>s
        /// with a max size of batchSize.
        /// <remarks>Note that batchSize must be one or larger.</remarks>
        /// </summary>
        /// <param name="source">A sequence of values to split into smaller batches.</param>
        /// <param name="batchSize">The number of elements to place in each batch.</param>
        /// <returns>The original collection, split into batches.</returns>
        public static IEnumerable<IList<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            ArgumentUtility.CheckForNull(source, nameof(source));
            ArgumentUtility.CheckBoundsInclusive(batchSize, 1, int.MaxValue, nameof(batchSize));

            var nextBatch = new List<T>(batchSize);
            foreach (T item in source)
            {
                nextBatch.Add(item);
                if (nextBatch.Count == batchSize)
                {
                    yield return nextBatch;
                    nextBatch = new List<T>(batchSize);
                }
            }

            if (nextBatch.Count > 0)
            {
                yield return nextBatch;
            }
        }

        /// <summary>
        /// Splits an <see cref="IEnumerable{T}"/> into two partitions, determined by the supplied predicate.  Those
        /// that follow the predicate are returned in the first, with the remaining elements in the second.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">The source enumerable to partition.</param>
        /// <param name="predicate">The predicate applied to filter the items into their partitions.</param>
        /// <returns>An object containing the matching and nonmatching results.</returns>
        public static PartitionResults<T> Partition<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            ArgumentUtility.CheckForNull(source, nameof(source));
            ArgumentUtility.CheckForNull(predicate, nameof(predicate));

            var results = new PartitionResults<T>();

            foreach (var item in source)
            {
                if (predicate(item))
                {
                    results.MatchingPartition.Add(item);
                }
                else
                {
                    results.NonMatchingPartition.Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// Partitions items from a source IEnumerable into N+1 lists, where the first N lists are determened
        /// by the sequential check of the provided predicates, with the N+1 list containing those items
        /// which matched none of the provided predicates.
        /// </summary>
        /// <typeparam name="T">The type of the elements in source.</typeparam>
        /// <param name="source">The source containing the elements to partition</param>
        /// <param name="predicates">The predicates to determine which list the results end up in</param>
        /// <returns>An item containing the matching collections and a collection containing the non-matching items.</returns>
        public static MultiPartitionResults<T> Partition<T>(this IEnumerable<T> source, params Predicate<T>[] predicates)
        {
            ArgumentUtility.CheckForNull(source, nameof(source));
            ArgumentUtility.CheckForNull(predicates, nameof(predicates));

            var range = Enumerable.Range(0, predicates.Length).ToList();

            var results = new MultiPartitionResults<T>();
            results.MatchingPartitions.AddRange(range.Select(_ => new List<T>()));

            foreach (var item in source)
            {
                bool added = false;

                foreach (var predicateIndex in range.Where(predicateIndex => predicates[predicateIndex](item)))
                {
                    results.MatchingPartitions[predicateIndex].Add(item);
                    added = true;
                    break;
                }

                if (!added)
                {
                    results.NonMatchingPartition.Add(item);
                }
            }

            return results;
        }

        /// <summary>
        /// Merges two sorted IEnumerables using the given comparison function which
        /// defines a total ordering of the data.
        /// </summary>
        public static IEnumerable<T> Merge<T>(
            this IEnumerable<T> first,
            IEnumerable<T> second,
            IComparer<T> comparer)
        {
            return Merge(first, second, comparer == null ? (Func<T, T, int>)null : comparer.Compare);
        }

        /// <summary>
        /// Merges two sorted IEnumerables using the given comparison function which
        /// defines a total ordering of the data.
        /// </summary>
        public static IEnumerable<T> Merge<T>(
            this IEnumerable<T> first,
            IEnumerable<T> second,
            Func<T, T, int> comparer)
        {
            ArgumentUtility.CheckForNull(first, nameof(first));
            ArgumentUtility.CheckForNull(second, nameof(second));
            ArgumentUtility.CheckForNull(comparer, nameof(comparer));

            using (IEnumerator<T> e1 = first.GetEnumerator())
            using (IEnumerator<T> e2 = second.GetEnumerator())
            {
                bool e1Valid = e1.MoveNext();
                bool e2Valid = e2.MoveNext();

                while (e1Valid && e2Valid)
                {
                    if (comparer(e1.Current, e2.Current) <= 0)
                    {
                        yield return e1.Current;

                        e1Valid = e1.MoveNext();
                    }
                    else
                    {
                        yield return e2.Current;

                        e2Valid = e2.MoveNext();
                    }
                }

                while (e1Valid)
                {
                    yield return e1.Current;

                    e1Valid = e1.MoveNext();
                }

                while (e2Valid)
                {
                    yield return e2.Current;

                    e2Valid = e2.MoveNext();
                }
            }
        }

        /// <summary>
        /// Merges two sorted IEnumerables using the given comparison function which defines a total ordering of the data.  Unlike Merge, this method requires that
        /// both IEnumerables contain distinct elements.  Likewise, the returned IEnumerable will only contain distinct elements.  If the same element appears in both inputs,
        /// it will appear only once in the output.
        /// 
        /// Example:
        ///     first:  [1, 3, 5]
        ///     second: [4, 5, 7]
        ///     result: [1, 3, 4, 5, 7]
        /// </summary>
        public static IEnumerable<T> MergeDistinct<T>(
            this IEnumerable<T> first,
            IEnumerable<T> second,
            IComparer<T> comparer)
        {
            return MergeDistinct(first, second, comparer == null ? (Func<T, T, int>)null : comparer.Compare);
        }

        /// <summary>
        /// Merges two sorted IEnumerables using the given comparison function which defines a total ordering of the data.  Unlike Merge, this method requires that
        /// both IEnumerables contain distinct elements.  Likewise, the returned IEnumerable will only contain distinct elements.  If the same element appears in both inputs,
        /// it will appear only once in the output.
        /// 
        /// Example:
        ///     first:  [1, 3, 5]
        ///     second: [4, 5, 7]
        ///     result: [1, 3, 4, 5, 7]
        /// </summary>
        public static IEnumerable<T> MergeDistinct<T>(
            this IEnumerable<T> first,
            IEnumerable<T> second,
            Func<T, T, int> comparer)
        {
            ArgumentUtility.CheckForNull(first, nameof(first));
            ArgumentUtility.CheckForNull(second, nameof(second));
            ArgumentUtility.CheckForNull(comparer, nameof(comparer));

            using (IEnumerator<T> e1 = first.GetEnumerator())
            using (IEnumerator<T> e2 = second.GetEnumerator())
            {
                bool e1Valid = e1.MoveNext();
                bool e2Valid = e2.MoveNext();

                while (e1Valid && e2Valid)
                {
                    if (comparer(e1.Current, e2.Current) < 0)
                    {
                        yield return e1.Current;

                        e1Valid = e1.MoveNext();
                    }
                    else if (comparer(e1.Current, e2.Current) > 0)
                    {
                        yield return e2.Current;

                        e2Valid = e2.MoveNext();
                    }
                    else
                    {
                        yield return e1.Current;

                        e1Valid = e1.MoveNext();
                        e2Valid = e2.MoveNext();
                    }
                }

                while (e1Valid)
                {
                    yield return e1.Current;

                    e1Valid = e1.MoveNext();
                }

                while (e2Valid)
                {
                    yield return e2.Current;

                    e2Valid = e2.MoveNext();
                }
            }
        }

        /// <summary>
        /// Creates a HashSet based on the elements in <paramref name="source"/>.
        /// </summary>
        public static HashSet<T> ToHashSet<T>(
            IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        /// <summary>
        /// Creates a HashSet with equality comparer <paramref name="comparer"/> based on the elements
        /// in <paramref name="source"/>.
        /// </summary>
        public static HashSet<T> ToHashSet<T>(
            IEnumerable<T> source,
            IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(source, comparer);
        }

        /// <summary>
        /// Creates a HashSet based on the elements in <paramref name="source"/>, using transformation
        /// function <paramref name="selector"/>.
        /// </summary>
        public static HashSet<TOut> ToHashSet<TIn, TOut>(
            this IEnumerable<TIn> source,
            Func<TIn, TOut> selector)
        {
            return new HashSet<TOut>(source.Select(selector));
        }

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

        /// <summary>
        /// Executes the specified action to each of the items in the collection
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <param name="collection">The collection on which the action will be performed</param>
        /// <param name="action">The action to be performed</param>
        /// </summary>
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            ArgumentUtility.CheckForNull(action, nameof(action));
            ArgumentUtility.CheckForNull(collection, nameof(collection));

            foreach (T item in collection)
            {
                action(item);
            }
        }

        /// <summary>
        /// Add the item to the List if the condition is satisfied
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <param name="list">The collection on which the action will be performed</param>
        /// <param name="condition">The Condition under which the item will be added</param>
        /// <param name="element">The element to be added</param>
        public static void AddIf<T>(this List<T> list, bool condition, T element)
        {
            if (condition)
            {
                list.Add(element);
            }
        }

        /// <summary>
        /// Converts a collection of key-value string pairs to a NameValueCollection.
        /// </summary>
        /// <param name="pairs">The key-value string pairs.</param>
        /// <returns>The NameValueCollection.</returns>
        public static NameValueCollection ToNameValueCollection(this IEnumerable<KeyValuePair<string, string>> pairs)
        {
            NameValueCollection collection = new NameValueCollection();

            foreach (KeyValuePair<string, string> pair in pairs)
            {
                collection.Add(pair.Key, pair.Value);
            }

            return collection;
        }

        public static IList<P> PartitionSolveAndMergeBack<T, P>(this IList<T> source, Predicate<T> predicate, Func<IList<T>, IList<P>> matchingPartitionSolver, Func<IList<T>, IList<P>> nonMatchingPartitionSolver)
        {
            ArgumentUtility.CheckForNull(source, nameof(source));
            ArgumentUtility.CheckForNull(predicate, nameof(predicate));
            ArgumentUtility.CheckForNull(matchingPartitionSolver, nameof(matchingPartitionSolver));
            ArgumentUtility.CheckForNull(nonMatchingPartitionSolver, nameof(nonMatchingPartitionSolver));

            var partitionedSource = new PartitionResults<Tuple<int, T>>();

            for (int sourceCnt = 0; sourceCnt < source.Count; sourceCnt++)
            {
                var item = source[sourceCnt];

                if (predicate(item))
                {
                    partitionedSource.MatchingPartition.Add(new Tuple<int, T>(sourceCnt, item));
                }
                else
                {
                    partitionedSource.NonMatchingPartition.Add(new Tuple<int, T>(sourceCnt, item));
                }
            }

            var solvedResult = new List<P>(source.Count);
            if (partitionedSource.MatchingPartition.Any())
            {
                solvedResult.AddRange(matchingPartitionSolver(partitionedSource.MatchingPartition.Select(x => x.Item2).ToList()));
            }

            if (partitionedSource.NonMatchingPartition.Any())
            {
                solvedResult.AddRange(nonMatchingPartitionSolver(partitionedSource.NonMatchingPartition.Select(x => x.Item2).ToList()));
            }

            var result = Enumerable.Repeat(default(P), source.Count).ToList();

            if (solvedResult.Count != source.Count)
            {
                return solvedResult; // either we can throw here or just return solvedResult and ignore!
            }

            for (int resultCnt = 0; resultCnt < source.Count; resultCnt++)
            {
                if (resultCnt < partitionedSource.MatchingPartition.Count)
                {
                    result[partitionedSource.MatchingPartition[resultCnt].Item1] = solvedResult[resultCnt];
                }
                else
                {
                    result[partitionedSource.NonMatchingPartition[resultCnt - partitionedSource.MatchingPartition.Count].Item1] = solvedResult[resultCnt];
                }
            }

            return result;
        }
    }
}
