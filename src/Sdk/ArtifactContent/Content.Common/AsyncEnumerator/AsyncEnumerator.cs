using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GitHub.Services.Content.Common
{
    public static class AsyncEnumerator
    {
        public static readonly int? UnboundedCapacity = null;

        public static IAsyncEnumerator<T> CollectUnordered<T>(
            int? boundedCapacity,
            params IAsyncEnumerator<T>[] enumerators)
        {
            return enumerators.CollectUnordered(boundedCapacity);
        }

        public static IAsyncEnumerator<T> CollectOrdered<T>(
            params IAsyncEnumerator<T>[] enumerators)
        {
            return enumerators.CollectOrdered();
        }
    }

    public delegate Task<bool> TryAddValueAsyncFunc<TValue>(TValue valueToAdd, CancellationToken token);

    public class AsyncEnumerator<TValue> : AsyncEnumerator<object, TValue>
    {
        private static readonly object[] SingleObjectArray = {null};

        public AsyncEnumerator(int? boundedCapacity,
            Func<TryAddValueAsyncFunc<TValue>, CancellationToken, Task> producerTask)
            : base(SingleObjectArray,
                boundedCapacity,
                (dummyObject, valueAdderAsync, cancelToken) => producerTask(valueAdderAsync, cancelToken))
        {
        }

        public AsyncEnumerator(IEnumerable<TValue> items)
            : this(items.GetEnumerator())
        {
        }

        public AsyncEnumerator(IEnumerator<TValue> items)
            : base(SingleObjectArray,
                1,
                (dummyObject, valueAdderAsync, cancelToken) => ValueAdderAsync(valueAdderAsync, cancelToken, items))
        {
        }

        private static async Task ValueAdderAsync(TryAddValueAsyncFunc<TValue> valueAdderAsync, CancellationToken token,
            IEnumerator<TValue> items)
        {
            while (items.MoveNext())
            {
                if (!await valueAdderAsync(items.Current, token).ConfigureAwait(false))
                {
                    break;
                }
            }
        }
    }

    public class AsyncEnumerator<TProducerSource, TValue> : IAsyncEnumerator<TValue>
    {
        private readonly CancellationTokenSource cancellationSource;
        private readonly BufferBlock<TValue> bufferBlock;
        private readonly Task checkForErrorsTask;

        private bool checkForErrorsTaskObserved = false;
        private bool disposed = false;
        private TValue current;

        public AsyncEnumerator(
            IEnumerable<TProducerSource> producerSources,
            int? boundedCapacity,
            Func<TProducerSource, TryAddValueAsyncFunc<TValue>, CancellationToken, Task> producerTask)
            : this(
                producerSources,
                maxConcurrentProducers: null,
                boundedCapacity: boundedCapacity,
                producerTask: producerTask)
        {
        }

        public AsyncEnumerator(
            IEnumerable<TProducerSource> producerSources,
            int? maxConcurrentProducers,
            int? boundedCapacity,
            Func<TProducerSource, TryAddValueAsyncFunc<TValue>, CancellationToken, Task> producerTask)
        {
            cancellationSource = new CancellationTokenSource();
            bufferBlock = boundedCapacity.HasValue
                ? new BufferBlock<TValue>(new DataflowBlockOptions {BoundedCapacity = boundedCapacity.Value})
                : new BufferBlock<TValue>();

            var producers = NonSwallowingActionBlock.Create<TProducerSource>(
                source => producerTask(source, TryAddValue, cancellationSource.Token),
                new ExecutionDataflowBlockOptions()
                {
                    CancellationToken = cancellationSource.Token,
                    MaxDegreeOfParallelism = maxConcurrentProducers ?? Environment.ProcessorCount,
                });

            producers.PostAllToUnboundedAndCompleteAsync(producerSources, cancellationSource.Token);

            this.checkForErrorsTask = WaitForProducersAsync(producers.Completion);
        }

        public bool EnumerationStarted { get; private set; }

        public TValue Current
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Cannot get current value: AsyncEnumerator is disposed.");
                }

                return current;
            }
        }

        public async Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Cannot get current value: AsyncEnumerator is disposed.");
            }

            EnumerationStarted = true;

            bool more = await bufferBlock.OutputAvailableAsync(cancellationToken).ConfigureAwait(false);
            if (more)
            {
                current = await bufferBlock.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                try
                {
                    await checkForErrorsTask.ConfigureAwait(false);
                }
                finally
                {
                    checkForErrorsTaskObserved = true;
                }
            }

            return more;
        }

        public int CurrentBufferCount => bufferBlock.Count;

        //TODO make internal
        protected virtual void BeginWaitForCheckForErrorsTask()
        {

        }

        private async Task WaitForProducersAsync(Task producersTask)
        {
            try
            {
                await producersTask.ConfigureAwait(false);
            }
            finally
            {
                bufferBlock.Complete();
            }
        }

        private async Task<bool> TryAddValue(TValue valueToAdd, CancellationToken token)
        {
            // Check before throw
            if (token.IsCancellationRequested)
            {
                return false;
            }

            await bufferBlock.SendOrThrowAsync(bufferBlock, valueToAdd, token).ConfigureAwait(false);

            return true;
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                if (!checkForErrorsTaskObserved)
                { 
                    bool cancelledByDispose;
                    if (cancellationSource.IsCancellationRequested)
                    {
                        cancelledByDispose = false;
                    }
                    else
                    {
                        cancellationSource.Cancel(throwOnFirstException: false);
                        cancelledByDispose = true;
                    }
                
                    BeginWaitForCheckForErrorsTask();
                    try
                    {
                        checkForErrorsTask.GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException) when (cancelledByDispose)
                    {
                    }
                    // catch when dataflow producers block wraps OperationCanceledException with TimeoutException
                    catch (TimeoutException e) when (cancelledByDispose && e.InnerException is OperationCanceledException)
                    {
                    }
                    finally
                    {
                        cancellationSource.Dispose();
                        checkForErrorsTaskObserved = true;
                    }
                }
                
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
