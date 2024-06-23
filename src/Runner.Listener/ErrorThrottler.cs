using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Services.Common;

namespace GitHub.Runner.Listener
{
    public interface IErrorThrottler : IRunnerService
    {
        void Reset();
        Task IncrementAndWaitAsync(CancellationToken token);
    }

    public sealed class ErrorThrottler : RunnerService, IErrorThrottler
    {
        private static readonly TimeSpan s_minBackoff = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan s_maxBackoff = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan s_backoffCoefficient = TimeSpan.FromSeconds(1);
        private int _count = 0;

        public void Reset()
        {
            _count = 0;
        }

        public async Task IncrementAndWaitAsync(CancellationToken token)
        {
            if (++_count <= 1)
            {
                return;
            }

            TimeSpan backoff = BackoffTimerHelper.GetExponentialBackoff(
                attempt: _count - 1,
                minBackoff: s_minBackoff,
                maxBackoff: s_maxBackoff,
                deltaBackoff: s_backoffCoefficient);
            Trace.Warning($"Back off {backoff.TotalSeconds} seconds before next attempt.");
            await Task.Delay(backoff, token);
        }
    }
}