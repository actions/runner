using System;
using System.ComponentModel;

namespace GitHub.Services.Common
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class BackoffTimerHelper
    {
        public static TimeSpan GetRandomBackoff(
            TimeSpan minBackoff,
            TimeSpan maxBackoff,
            TimeSpan? previousBackoff = null)
        {
            Random random = null;
            if (previousBackoff.HasValue)
            {
                random = new Random((Int32)previousBackoff.Value.TotalMilliseconds);
            }
            else
            {
                random = new Random();
            }

            return TimeSpan.FromMilliseconds(random.Next((Int32)minBackoff.TotalMilliseconds, (Int32)maxBackoff.TotalMilliseconds));
        }

        public static TimeSpan GetExponentialBackoff(
            Int32 attempt,
            TimeSpan minBackoff,
            TimeSpan maxBackoff,
            TimeSpan deltaBackoff)
        {
            Double randomBackoff = (Double)new Random().Next((Int32)(deltaBackoff.TotalMilliseconds * 0.8), (Int32)(deltaBackoff.TotalMilliseconds * 1.2));
            Double additionalBackoff = attempt < 0 ? (Math.Pow(2.0, (Double)attempt)) * randomBackoff : (Math.Pow(2.0, (Double)attempt) - 1.0) * randomBackoff;
            return TimeSpan.FromMilliseconds(Math.Min(minBackoff.TotalMilliseconds + additionalBackoff, maxBackoff.TotalMilliseconds));
        }
    }
}
