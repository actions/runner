using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Services.Common;

namespace GitHub.Runner.Listener
{
    [ServiceLocator(Default = typeof(ErrorThrottler))]
    public interface IErrorThrottler : IRunnerService
    {
        void Reset();
        Task IncrementAndWaitAsync(CancellationToken token);
    }

    public sealed class ErrorThrottler : RunnerService, IErrorThrottler
    {
        internal static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(1);
        internal static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(1);
        internal static readonly TimeSpan BackoffCoefficient = TimeSpan.FromSeconds(1);
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
                attempt: _count - 2, // 0-based attempt
                minBackoff: MinBackoff,
                maxBackoff: MaxBackoff,
                deltaBackoff: BackoffCoefficient);
            Trace.Warning($"Back off {backoff.TotalSeconds} seconds before next attempt. Current consecutive error count: {_count}");
            await HostContext.Delay(backoff, token);
        }
    }
}
