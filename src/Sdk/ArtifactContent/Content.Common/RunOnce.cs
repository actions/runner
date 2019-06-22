using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    public class RunOnce<TKey>
    {
        private struct Void { }
        private readonly RunOnce<TKey, Void> inner;

        public RunOnce(bool consolidateExceptions)
        {
            inner = new RunOnce<TKey, Void>(consolidateExceptions);
        }

        public RunOnce(bool consolidateExceptions, IEqualityComparer<TKey> comparer)
        {
            inner = new RunOnce<TKey, Void>(consolidateExceptions);
        }

        public Task RunOnceAsync(TKey key, Func<Task> taskFunc)
        {
            return inner.RunOnceAsync(key, async () =>
            {
                await taskFunc().ConfigureAwait(false);
                return new Void();
            });
        }
    }

    public class RunOnce<TKey, TResult>
    {
        private readonly ConcurrentDictionary<TKey, Task<TResult>> taskDictionary;
        private readonly bool consolidateExceptions;

        public IEnumerable<TResult> CompletedValues => taskDictionary.Values.Where(v => v.IsCompleted).Select(v => v.Result).ToArray();

        public RunOnce(bool consolidateExceptions)
            : this(consolidateExceptions, EqualityComparer<TKey>.Default)
        {
        }

        public RunOnce(bool consolidateExceptions, IEqualityComparer<TKey> comparer)
        {
            this.consolidateExceptions = consolidateExceptions;
            taskDictionary = new ConcurrentDictionary<TKey, Task<TResult>>(comparer);
        }

        public async Task<TResult> RunOnceAsync(TKey key, Func<Task<TResult>> taskFunc)
        {
            while (true)
            {
                // ConfigureAwait(true) to maintain SynchronizationContext across attempts
                var (success, value) = await TryRunOnceInternalAsync(key, taskFunc).ConfigureAwait(true);
                if (!success)
                {
                    continue;
                }

                return value;
            }
        }

        public async Task<(bool success, TResult value)> TryRunOnceInternalAsync(TKey key, Func<Task<TResult>> taskFunc)
        {
            var tcs = new SafeTaskCompletionSource<TResult>(); // No TaskCreationOptions.RunContinuationsAsynchronously (.NET 4.5.1 < 4.6.1)

            Task<TResult> task;
            if (taskDictionary.GetOrAdd(key, tcs.Task, out task))
            {
                // We grabbed an open slot, so now run.
                return (true, await ExecuteAsync(key, taskFunc, tcs));
            }
            else
            {
                tcs.MarkTaskAsUnused();

                try
                {
                    return (true, await task.ConfigureAwait(false));
                }
                catch when (!consolidateExceptions)
                {
                    return (false, default(TResult));
                }
            }
        }

        private async Task<TResult> ExecuteAsync(TKey key, Func<Task<TResult>> taskFunc, SafeTaskCompletionSource<TResult> tcs)
        {
            try
            {
                TResult value = await taskFunc().ConfigureAwait(false);
                var _doNotAwait = Task.Run(() => tcs.SetResult(value));
                return value;
            }
            catch (Exception e)
            {
                // Make sure to cleanup before we call tcs.SetException.
                if (!this.consolidateExceptions)
                {
                    Remove(taskDictionary, key, tcs.Task);
                }
                tcs.SetException(e);
                throw;
            }
        }

        private static void Remove(ConcurrentDictionary<TKey, Task<TResult>> taskDictionary, TKey key, Task<TResult> task)
        {
            ((ICollection<KeyValuePair<TKey, Task<TResult>>>)taskDictionary).Remove(new KeyValuePair<TKey, Task<TResult>>(key, task));
        }
    }
}
