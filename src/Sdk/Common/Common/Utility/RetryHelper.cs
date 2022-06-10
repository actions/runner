using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    [EditorBrowsable(EditorBrowsableState.Never)]

    public static class RetryHelper<T>
    {
        public static async Task<T> RetryWithTimeoutAsync(
            Func<Task<T>> retriableAction,
            TimeSpan minBackoff,
            TimeSpan maxBackoff,
            int maxTimeout = 5
        )
        {
            var remainingTime = TimeSpan.FromMinutes(maxTimeout);
            while (true)
            {
                try
                {
                    return await retriableAction();
                }
                catch
                {   
                    if (remainingTime > TimeSpan.Zero)
                    {
                        var backOff = BackoffTimerHelper.GetRandomBackoff(minBackoff, maxBackoff);
                        remainingTime -= backOff;
                        await Task.Delay(backOff);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
