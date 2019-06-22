using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    /// <remarks>
    /// We need to be careful with ConfigureAwait and callbacks. See:
    /// http://stackoverflow.com/questions/30036459/should-we-use-configureawaitfalse-in-libraries-that-call-async-callbacks
    /// </remarks>>
    public static class AsyncEnumeratorExtensions
    {
        public static ForkedAsyncEnumerator<T> Fork<T>(
            this IAsyncEnumerator<T> enumerator,
            int forkCount,
            int boundedCapacity,
            CancellationToken cancellationToken)
        {
            return new ForkedAsyncEnumerator<T>(enumerator, forkCount, boundedCapacity, cancellationToken);
        }

        public static IAsyncEnumerator<IReadOnlyCollection<T>> GetPages<T>(
            this IAsyncEnumerator<T> enumerator,
            int pageSize)
        {
            return new AsyncEnumerator<IReadOnlyCollection<T>>(
                boundedCapacity: 2, // double-buffering
                producerTask: async (valueAdderAsync, cancellationToken) =>
                {
                    using (enumerator)
                    {
                        bool readMore = true;
                        while (readMore && await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                        {
                            var currentPage = new List<T>(pageSize)
                            {
                                enumerator.Current
                            };

                            while (currentPage.Count < pageSize &&
                                   await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                            {
                                currentPage.Add(enumerator.Current);
                            }

                            readMore = await valueAdderAsync(currentPage, cancellationToken).ConfigureAwait(false);
                        }
                    }
                });
        }

        public static IAsyncEnumerator<T2> Select<T1, T2>(
            this IAsyncEnumerator<T1> enumerator,
            Func<T1, T2> selector)
        {
            return new AsyncEnumeratorSelect<T1, T2>(enumerator, selector);
        }

        public static IAsyncEnumerator<T2> SelectAsync<T1, T2>(
            this IAsyncEnumerator<T1> enumerator,
            Func<T1, Task<T2>> selector)
        {
            return new AsyncEnumeratorSelectAsync<T1, T2>(enumerator, selector);
        }

        public static IAsyncEnumerator<T2> SelectMany<T1, T2>(
            this IAsyncEnumerator<T1> enumerator, 
            Func<T1, IAsyncEnumerator<T2>> selector)
        {
            return new AsyncEnumeratorSelectMany<T1, T2>(enumerator, selector);
        }

        public static IAsyncEnumerator<T> SelectMany<T>(
            this IAsyncEnumerator<IEnumerable<T>> enumerator)
        {
            return enumerator.SelectMany(x => x);
        }

        public static IAsyncEnumerator<T> SelectMany<T>(
            this IAsyncEnumerator<IAsyncEnumerator<T>> enumerator)
        {
            return enumerator.SelectMany(x => x);
        }

        public static IAsyncEnumerator<T2> SelectManyAsync<T1, T2>(
            this IAsyncEnumerator<T1> enumerator, 
            Func<T1, CancellationToken, Task<IAsyncEnumerator<T2>>> selector)
        {
            return new AsyncEnumeratorSelectManyAsync<T1, T2>(enumerator, selector);
        }

        /// <remarks>
        /// The items in the inner IEnumerable sequences are produced synchronously. If
        /// you want to produce the inner items asynchronously you can warp the result
        /// of the selector in an asynchronous enumerator as follows:
        ///
        /// enumerator.SelectMany(x => new AsyncEnumerator(selector(x))
        /// </remarks>
        public static IAsyncEnumerator<T2> SelectMany<T1, T2>(
            this IAsyncEnumerator<T1> enumerator, 
            Func<T1, IEnumerable<T2>> selector)
        {
            return new AsyncEnumeratorSelectMany<T1, T2>(enumerator, x => new UnbufferedAsyncEnumerator<T2>(selector(x)));
        }

        public static IAsyncEnumerator<TOut> SelectWithState<TState, TIn, TOut>(
            this IAsyncEnumerator<TIn> enumerator,
            Func<TState, TIn, Tuple<TState, TOut>> update,
            TState initialState)
        {
            return enumerator.Select(x =>
            {
                var c = update(initialState, x);
                initialState = c.Item1;
                return c.Item2;
            });
        }

        public static IAsyncEnumerator<T> Where<T>(
            this IAsyncEnumerator<T> enumerator,
            Func<T, bool> selector)
        {
            return new AsyncEnumeratorWhere<T>(enumerator, selector);
        }

        // This is a lazy Skip. When the first item is requested the
        // enumerator will fast forward to the (n+1)th item in time O(n) by
        // requesting and discarding the first n elements of the original
        // enumeration.
        //
        public static IAsyncEnumerator<T> Skip<T>(
            this IAsyncEnumerator<T> enumerator,
            long n)
        {
            var i = 0;
            return enumerator.Where(x => ++i > n);
        }

        // Lazy take with short cut. Enumeration stops after the requested
        // number of elements and further elements aren't requested.
        //
        public static IAsyncEnumerator<T> Take<T>(
            this IAsyncEnumerator<T> enumerator,
            long n)
        {
            return new AsyncEnumeratorWhere<T>(enumerator, selector: _ => true, take: n);
        }
             
        public static async Task<T> SingleOrDefaultAsync<T>(this IAsyncEnumerator<T> enumerator, CancellationToken token)
        {
            using (enumerator)
            {
                bool any = await enumerator.MoveNextAsync(token).ConfigureAwait(false);
                if (!any)
                {
                    return default(T);
                }

                T singleItem = enumerator.Current;

                bool more = await enumerator.MoveNextAsync(token).ConfigureAwait(false);
                if (more)
                {
                    throw new ArgumentException("Enumerator had more than one item.");
                }

                return singleItem;
            }
        }

        public static IAsyncEnumerator<T> Concat<T>(
            this IAsyncEnumerator<T> firstAsyncEnumerator,
            IAsyncEnumerator<T> secondAsyncEnumerator)
        {
            return Enumerable.Repeat(firstAsyncEnumerator, 1)
                .Concat(Enumerable.Repeat(secondAsyncEnumerator,1))
                .CollectOrdered();
        }

        public static IAsyncEnumerator<T> Concat<T>(
            this IAsyncEnumerator<T> firstAsyncEnumerator,
            IEnumerable<IAsyncEnumerator<T>> otherEnumerators)
        {
            return Enumerable.Repeat(firstAsyncEnumerator, 1)
                .Concat(otherEnumerators)
                .CollectOrdered();
        }

        public static IAsyncEnumerator<T> CollectOrdered<T>(
            this IEnumerable<IAsyncEnumerator<T>> enumerators)
        {
            return new AsyncEnumerator<T>(
                boundedCapacity: 2,
                producerTask: async (valueAdderAsync, cancelToken) =>
                {
                    bool keepGoing = true;
                    foreach (var enumerator in enumerators)
                    {
                        await enumerator.DoWhileAsyncNoContext(cancelToken, async v =>
                        {
                            keepGoing = await valueAdderAsync(v, cancelToken).ConfigureAwait(false);
                            return keepGoing;
                        });

                        if (!keepGoing)
                        {
                            break;
                        }
                    }
                });
        }

        // Merges multiple sorted AsyncEnumerator into a single sorted AsyncEnumerator
        public static IAsyncEnumerator<T> CollectSortOrdered<T>(
            this IEnumerable<IAsyncEnumerator<T>> sourceEnumerators,
            int? boundedCapacity,
            IComparer<T> itemComparer)
        {
            if (sourceEnumerators.Count() == 1) // Shortcut optimization for single sorted enumerator
            {
                return sourceEnumerators.First();
            }

            return new AsyncEnumerator<T>(
                boundedCapacity: boundedCapacity,
                producerTask: async (valueAdderAsync, cancelToken) =>
                {
                    T previousValue = default(T);
                    bool isFirstItem = true;

                    try
                    {
                        // The SortedDictionary is used like a MinHeap
                        var sortedEnumerators = new SortedDictionary<T, Stack<IAsyncEnumerator<T>>>(itemComparer);

                        foreach (var enumerator in sourceEnumerators)
                        {
                            if (await enumerator.MoveNextAsync(cancelToken))
                            {
                                if (!sortedEnumerators.TryGetValue(enumerator.Current, out var enumerators))
                                {
                                    enumerators = new Stack<IAsyncEnumerator<T>>(1);
                                    sortedEnumerators.Add(enumerator.Current, enumerators);
                                }
                                enumerators.Push(enumerator);
                            }
                        }

                        while (sortedEnumerators.Any())
                        {
                            // Pop Min Element
                            var minEnumeratorsKvp = sortedEnumerators.First();
                            var minEnumerators = minEnumeratorsKvp.Value;

                            var minEnumerator = minEnumerators.Pop();

                            if (!minEnumerators.Any())
                            {
                                sortedEnumerators.Remove(minEnumeratorsKvp.Key);
                            }

                            // Verifying Order
                            if (!isFirstItem &&
                                itemComparer.Compare(previousValue, minEnumerator.Current) > 0)
                            {
                                throw new InvalidOperationException($"Enumeration is not in sorted order, prev: {previousValue}, curr: {minEnumerator.Current}");
                            }
                            isFirstItem = false;
                            previousValue = minEnumerator.Current;

                            if (!await valueAdderAsync(minEnumerator.Current, cancelToken).ConfigureAwait(false))
                            {
                                return;
                            }

                            if (await minEnumerator.MoveNextAsync(cancelToken))
                            {
                                if (!sortedEnumerators.TryGetValue(minEnumerator.Current, out var enumerators))
                                {
                                    // Perf Optimization: Unless there is a duplicate value, the minEnumerators will be empty
                                    // And its safe to reuse the stack, as it is removed from tree
                                    enumerators = !minEnumerators.Any() ? minEnumerators : new Stack<IAsyncEnumerator<T>>(1);
                                    sortedEnumerators.Add(minEnumerator.Current, enumerators);
                                }
                                enumerators.Push(minEnumerator);
                            }
                        }
                    }
                    finally 
                    {
                        var exceptions = new List<Exception>();
                        foreach (var enumerator in sourceEnumerators)
                        {
                            try
                            {
                                enumerator.Dispose();
                            }
                            catch (Exception e)
                            {
                                exceptions.Add(e);
                            }
                        }
                        if (exceptions.Count == 1)
                        {
                            throw exceptions[0];
                        }
                        else if (exceptions.Count > 1)
                        {
                            throw new AggregateException("Multiple exceptions while disposing enumerators", exceptions);
                        }
                    }
                });
        }

        public static IAsyncEnumerator<TValue> CollectUnordered<TValue>(
            this IEnumerable<IAsyncEnumerator<TValue>> enumerators,
            Int32? boundedCapacity)
        {
            return new AsyncEnumerator<IAsyncEnumerator<TValue>, TValue>(
                enumerators,
                boundedCapacity,
                (enumerator, valueAdderAsync, cancelToken) => enumerator.DoWhileAsyncNoContext(cancelToken, t => valueAdderAsync(t, cancelToken)));
        }

        public static IAsyncEnumerator<TValue> CollectUnordered<TValue>(Int32? boundedCapacity, params IAsyncEnumerator<TValue>[] enumerators)
        {
            return enumerators.CollectUnordered(boundedCapacity);
        }

        public static async Task<T> SingleAsync<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken)
        {
            using (enumerator)
            {
                bool any = await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false);
                if (!any)
                {
                    throw new ArgumentException("Enumerator had no items.");
                }

                T singleItem = enumerator.Current;

                bool more = await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(false);
                if (more)
                {
                    throw new ArgumentException("Enumerator had more than one item.");
                }

                return singleItem;
            }
        }
        
        public static Task ForEachAsyncNoContext<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken, Action<T> loopBody)
        {
            return ForEachAsync(enumerator, false, cancellationToken, loopBody);
        }
        
        public static Task ForEachAsyncNoContext<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken, Func<T, Task> loopBodyAsync)
        {
            return ForEachAsync(enumerator, false, cancellationToken, loopBodyAsync);
        }

        public static Task ForEachAsyncCaptureContext<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken, Action<T> loopBody)
        {
            return ForEachAsync(enumerator, true, cancellationToken, loopBody);
        }

        public static Task ForEachAsyncCaptureContext<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken, Func<T, Task> loopBodyAsync)
        {
            return ForEachAsync(enumerator, true, cancellationToken, loopBodyAsync);
        }

        public static async Task ForEachAsync<T>(this IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext, CancellationToken cancellationToken, Action<T> loopBody)
        {
            enumerator.AssertNotEnumerated();
            using (enumerator)
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext))
                {
                    loopBody(enumerator.Current);
                }
            }
        }

        public static async Task ForEachAsync<T>(this IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext, CancellationToken cancellationToken, Func<T, Task> loopBodyAsync)
        {
            enumerator.AssertNotEnumerated();
            using (enumerator)
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext))
                {
                    await loopBodyAsync(enumerator.Current).ConfigureAwait(continueOnCapturedContext);
                }
            }
        }

        // Just like Take(n), but stops on reaching n and returns result as a List
        public static async Task<List<T>> TakeAsList<T>(
            this IAsyncEnumerator<T> enumerator,
            long n,
            CancellationToken cancellationToken)
        {
            var list = new List<T>();
            if (n > 0)
            {
                long count = n;
                await enumerator.DoWhileAsyncNoContext(cancellationToken, item => {
                    list.Add(item);
                    return (--count) > 0;
                }).ConfigureAwait(false);
            }
            return list;
        }

        public static Task DoWhileAsyncNoContext<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken, Func<T, bool> loopBody)
        {
            return DoWhileAsync(enumerator, false, cancellationToken, loopBody);
        }

        public static Task DoWhileAsyncCaptureContext<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken, Func<T, bool> loopBody)
        {
            return DoWhileAsync(enumerator, true, cancellationToken, loopBody);
        }

        public static Task DoWhileAsyncNoContext<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken, Func<T, Task<bool>> loopBodyAsync)
        {
            return DoWhileAsync(enumerator, false, cancellationToken, loopBodyAsync);
        }

        public static Task DoWhileAsyncCaptureContext<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken, Func<T, Task<bool>> loopBodyAsync)
        {
            return DoWhileAsync(enumerator, true, cancellationToken, loopBodyAsync);
        }

        public static async Task DoWhileAsync<T>(this IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext, CancellationToken cancellationToken, Func<T, bool> loopBody)
        {
            enumerator.AssertNotEnumerated();
            using (enumerator)
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext))
                {
                    if (!loopBody(enumerator.Current))
                    {
                        break;
                    }
                }
            }
        }

        public static async Task DoWhileAsync<T>(this IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext, CancellationToken cancellationToken, Func<T, Task<bool>> loopBodyAsync)
        {
            enumerator.AssertNotEnumerated();
            using (enumerator)
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext))
                {
                    if (!(await loopBodyAsync(enumerator.Current).ConfigureAwait(continueOnCapturedContext)))
                    {
                        break;
                    }
                }
            }
        }

        public static Task<bool> AllAsyncCaptureContext<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken, Func<T, bool> predicateFunc)
        {
            return AllAsync(enumerator, true, cancellationToken, predicateFunc);
        }

        public static Task<bool> AllAsyncNoContext<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken, Func<T, bool> predicateFunc)
        {
            return AllAsync(enumerator, false, cancellationToken, predicateFunc);
        }

        public static async Task<bool> AllAsync<T>(this IAsyncEnumerator<T> enumerator, bool continueOnCapturedContext, CancellationToken cancellationToken, Func<T, bool> predicateFunc)
        {
            enumerator.AssertNotEnumerated();
            using (enumerator)
            {
                while (await enumerator.MoveNextAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext))
                {
                    if (!predicateFunc(enumerator.Current))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerator<T> enumerator, CancellationToken cancellationToken)
        {
            var list = new List<T>();
            await enumerator.ForEachAsyncNoContext(cancellationToken, item => list.Add(item)).ConfigureAwait(false);
            return list;
        }

        public static async Task<IDictionary<K, V>> ToDictionaryAsync<K, V>(this IAsyncEnumerator<KeyValuePair<K, V>> enumerator, CancellationToken cancellationToken)
        {
            var list = new Dictionary<K, V>();
            await enumerator.ForEachAsyncNoContext(cancellationToken, item => list.Add(item.Key, item.Value)).ConfigureAwait(false);
            return list;
        }

        /// <summary>
        /// Because AsyncEnumerator can be very lazy, there are times (e.g. before returning from a controller) that we want to validate the pipeline. 
        /// To do so, we can just make sure that we get at least one valid result. If there is an error, it will throw here and get a helpful call stack.
        /// </summary>
        public static async Task<IAsyncEnumerator<T>> WrapAndProbeAsync<T>(this IAsyncEnumerator<T> enumeratorToProbe, CancellationToken cancellationToken)
        {
            bool any = await enumeratorToProbe.MoveNextAsync(cancellationToken).ConfigureAwait(false);
            if (any)
            {
                return AsyncEnumerator.CollectOrdered(
                    new AsyncEnumerator<T>(new[] { enumeratorToProbe.Current }),
                    new ResetStartAsyncEnumerator<T>(enumeratorToProbe));
            }
            else
            {
                return new AsyncEnumerator<T>(Enumerable.Empty<T>());
            }
        }

        public static void AssertNotEnumerated<T>(this IAsyncEnumerator<T> enumerator)
        {
            if (enumerator.EnumerationStarted)
            {
                throw new EnumeratorAlreadyStartedException();
            }
        }

        // This class is a "fake" IAsyncEnumerator. It's for only for internal use. If you want 
        // a "real" use `new AsyncEnumerator(IEnumerable enumerable)`.
        //
        // The latter introduces a DataFlow buffer of length one to provide asyncronous production 
        // of the items in the IEnumerable. This class can be used to avoid the overhead of
        // the internal buffer. However, the items in the enumeration are produced synchronously.
        //
        private class UnbufferedAsyncEnumerator<T> : IAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> baseEnumerator;

            public UnbufferedAsyncEnumerator(IEnumerable<T> enumerable)
            {
                this.baseEnumerator = enumerable.GetEnumerator();
            }

            public T Current => baseEnumerator.Current;

            public bool EnumerationStarted { get; private set; }

            public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                EnumerationStarted = true;
                return Task.FromResult(baseEnumerator.MoveNext());
            }

            public void Dispose(bool disposing)
            {
                if (disposing)
                {
                    baseEnumerator.Dispose();
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        private class AsyncEnumeratorComparer<T> : IComparer<IAsyncEnumerator<T>>
        {
            private readonly IComparer<T> itemComparer;

            public AsyncEnumeratorComparer(IComparer<T> itemComparer)
            {
                this.itemComparer = itemComparer;
            }

            public int Compare(IAsyncEnumerator<T> x, IAsyncEnumerator<T> y)
            {
                if (!x.EnumerationStarted || !y.EnumerationStarted)
                {
                    throw new ArgumentException("Enumerator has not started enumeration for comparing");
                }

                return itemComparer.Compare(x.Current, y.Current);
            }
        }
    }
}
