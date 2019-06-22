using System;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ForkedAsyncEnumerator<T> : IDisposable
    {
        private readonly IAsyncEnumerator<T> source;
        private readonly BufferBlock<T>[] buffers;
        private readonly Task sourceDrainerTask;

        public readonly IAsyncEnumerator<T>[] ForksForParallelConsupmtion;

        public ForkedAsyncEnumerator(IAsyncEnumerator<T> source,int forkCount, int boundedCapacity, CancellationToken token)
        {
            this.source = source;

            buffers = Enumerable.Range(0, forkCount).Select(_ => new BufferBlock<T>(new DataflowBlockOptions()
            {
                BoundedCapacity = boundedCapacity,
                CancellationToken = token,
            })).ToArray();

            sourceDrainerTask = Task.Run(async () =>
            {
                try
                {
                    await source.ForEachAsyncNoContext(token, async t =>
                    {
                        foreach (BufferBlock<T> buffer in buffers)
                        {
                            await buffer.SendOrThrowAsync(buffer, t, token).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
                }
                finally
                {
                    foreach (BufferBlock<T> buffer in buffers)
                    {
                        buffer.Complete();
                    }
                }
            },
            token);

            ForksForParallelConsupmtion = buffers.Select(buffer => CreateFromBuffer(boundedCapacity, buffer, sourceDrainerTask)).ToArray();
        }

        private static IAsyncEnumerator<T> CreateFromBuffer(int boundedCapacity, BufferBlock<T> bufferBlock, Task checkForErrorTask)
        {
            return new AsyncEnumerator<T>(
                boundedCapacity,
                async (valueAdderAsync, cancellationToken) =>
                {
                    while (await bufferBlock.OutputAvailableAsync(cancellationToken).ConfigureAwait(false))
                    {
                        T x = await bufferBlock.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                        if (!(await valueAdderAsync(x, cancellationToken).ConfigureAwait(false)))
                        {
                            break;
                        }
                    }

                    await checkForErrorTask.ConfigureAwait(false);
                });
        }

        public void Dispose()
        {
            foreach (IAsyncEnumerator<T> e in ForksForParallelConsupmtion)
            {
                e.Dispose();
            }
            source.Dispose();
            sourceDrainerTask.Wait();
        }
    }
}
