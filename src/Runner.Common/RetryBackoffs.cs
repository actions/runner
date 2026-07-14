using System;

namespace GitHub.Runner.Common
{
    public static class RetryBackoffs
    {
        public static Func<int, int, Exception, TimeSpan> Fixed(TimeSpan delay)
        {
            if (delay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delay));
            }

            return (_, _, _) => delay;
        }

        public static Func<int, int, Exception, TimeSpan> Random(TimeSpan minDelay, TimeSpan maxDelay)
        {
            ValidateRange(minDelay, maxDelay);

            return (_, _, _) => NextDelay(minDelay, maxDelay);
        }

        public static Func<int, int, Exception, TimeSpan> Exponential(TimeSpan minDelay, TimeSpan maxDelay, TimeSpan deltaDelay)
        {
            ValidateRange(minDelay, maxDelay);
            if (deltaDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaDelay));
            }

            return (attempt, _, _) =>
            {
                var exponent = Math.Max(0, attempt);
                var additional = (Math.Pow(2.0, exponent) - 1.0) * deltaDelay.TotalMilliseconds;
                var delayMs = Math.Min(minDelay.TotalMilliseconds + additional, maxDelay.TotalMilliseconds);
                return TimeSpan.FromMilliseconds(Math.Max(0, delayMs));
            };
        }

        public static Func<int, int, Exception, TimeSpan> ExponentialFullJitter(TimeSpan minDelay, TimeSpan maxDelay, TimeSpan deltaDelay)
        {
            ValidateRange(minDelay, maxDelay);
            if (deltaDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaDelay));
            }

            return (attempt, _, _) =>
            {
                var exponent = Math.Max(0, attempt);
                var additional = (Math.Pow(2.0, exponent) - 1.0) * deltaDelay.TotalMilliseconds;
                var capMs = Math.Min(minDelay.TotalMilliseconds + additional, maxDelay.TotalMilliseconds);
                var floorMs = minDelay.TotalMilliseconds;

                if (capMs <= floorMs)
                {
                    return TimeSpan.FromMilliseconds(floorMs);
                }

                var jitterMs = global::System.Random.Shared.NextDouble() * (capMs - floorMs);
                return TimeSpan.FromMilliseconds(floorMs + jitterMs);
            };
        }

        public static Func<int, int, Exception, TimeSpan> DecorrelatedJitter(TimeSpan minDelay, TimeSpan maxDelay)
        {
            ValidateRange(minDelay, maxDelay);

            var previous = minDelay;
            return (_, _, _) =>
            {
                var upperBoundMs = Math.Min(maxDelay.TotalMilliseconds, previous.TotalMilliseconds * 3);
                if (upperBoundMs <= minDelay.TotalMilliseconds)
                {
                    previous = minDelay;
                    return previous;
                }

                var nextMs = minDelay.TotalMilliseconds + (global::System.Random.Shared.NextDouble() * (upperBoundMs - minDelay.TotalMilliseconds));
                previous = TimeSpan.FromMilliseconds(nextMs);
                return previous;
            };
        }

        private static void ValidateRange(TimeSpan minDelay, TimeSpan maxDelay)
        {
            if (minDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(minDelay));
            }

            if (maxDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelay));
            }

            if (maxDelay < minDelay)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelay));
            }
        }

        private static TimeSpan NextDelay(TimeSpan minDelay, TimeSpan maxDelay)
        {
            if (minDelay == maxDelay)
            {
                return minDelay;
            }

            var minMs = minDelay.TotalMilliseconds;
            var rangeMs = maxDelay.TotalMilliseconds - minMs;
            var jitterMs = global::System.Random.Shared.NextDouble() * rangeMs;
            return TimeSpan.FromMilliseconds(minMs + jitterMs);
        }
    }
}
