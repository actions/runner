using System;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public class RetryExecutor
    {
        private const int DefaultMaximumRetryCount = 5;

        private const int DefaultMillisecondsToSleepBetweenRetries = 1000;

        public int MaximumRetryCount { get; set; }

        public int MillisecondsToSleepBetweenRetries { get; set; }

        public Func<Exception, bool> ShouldRetryAction { get; set; }

        protected Action<int> SleepAction { get; set; }

        public RetryExecutor()
        {
            this.MaximumRetryCount = DefaultMaximumRetryCount;
            this.MillisecondsToSleepBetweenRetries = DefaultMillisecondsToSleepBetweenRetries;
            this.ShouldRetryAction = ex => true;
            this.SleepAction = i => Task.Delay(i);
        }

        public void Execute(Action action)
        {
            ArgUtil.NotNull(action, nameof(action));

            for (var retryCount = 0; retryCount < this.MaximumRetryCount; retryCount++)
            {
                try
                {
                    action();
                    break;
                }
                catch (Exception ex)
                {
                    if (retryCount == MaximumRetryCount - 1 || !ShouldRetryAction(ex))
                    {
                        throw;
                    }

                    this.SleepAction(this.MillisecondsToSleepBetweenRetries);
                }
            }
        }

        public void Execute<T>(Action<T> action, T parameter)
        {
            ArgUtil.NotNull(action, nameof(action));

            Execute(() => action(parameter));
        }

        public TResult Execute<T, TResult>(Func<T, TResult> action, T parameter)
        {
            ArgUtil.NotNull(action, nameof(action));

            var result = default(TResult);
            this.Execute(() => result = action(parameter));
            return result;
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            ArgUtil.NotNull(action, nameof(action));

            for (var retryCount = 0; retryCount < this.MaximumRetryCount; retryCount++)
            {
                try
                {
                    await action();
                    break;
                }
                catch (Exception ex)
                {
                    if (retryCount == this.MaximumRetryCount - 1 || !this.ShouldRetryAction(ex))
                    {
                        throw;
                    }

                    SleepAction(this.MillisecondsToSleepBetweenRetries);
                }
            }
        }

        public async Task ExecuteAsync<T>(Func<T, Task> action, T parameter)
        {
            ArgUtil.NotNull(action, nameof(action));
            await this.ExecuteAsync(async () => await action(parameter));
        }

        public async Task<TResult> ExecuteAsync<T, TResult>(Func<T, Task<TResult>> action, T parameter)
        {
            ArgUtil.NotNull(action, nameof(action));

            var result = default(TResult);
            await this.ExecuteAsync(async () => result = await action(parameter));
            return result;
        }
    }
}