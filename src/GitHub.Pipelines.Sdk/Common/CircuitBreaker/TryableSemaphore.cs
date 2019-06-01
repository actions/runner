using System;

namespace Microsoft.VisualStudio.Services.CircuitBreaker
{
    public interface ITryableSemaphore
    {
        bool TryAcquire(int numberOfPermits);

        void Release();

        int GetNumberOfPermitsUsed();
    }

    /// <summary>
    /// Semaphore that only supports TryAcquire and never blocks and that supports a dynamic permit count.
    /// </summary>
    public class TryableSemaphore : ITryableSemaphore
    {
        /// <summary>
        /// </summary>
        private readonly AtomicLong count = new AtomicLong(0);

        /// <summary>
        /// Tries to acquire the semaphore.
        /// </summary>
        /// <example>
        /// if (s.TryAcquire())
        /// {
        ///     try
        ///     {
        ///         // do work that is protected by 's'
        ///     }
        ///     finally
        ///     {
        ///         s.Release();
        ///     }
        /// }
        /// </example>
        /// <returns></returns>
        public bool TryAcquire(int numberOfPermits)
        {
            long currentCount = count.IncrementAndGet();
            if (currentCount > numberOfPermits)
            {
                count.DecrementAndGet();
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Releases the acquired semaphore.
        /// </summary>
        /// <example>
        /// if (s.TryAcquire())
        /// {
        ///     try
        ///     {
        ///         // do work that is protected by 's'
        ///     }
        ///     finally
        ///     {
        ///         s.Release();
        ///     }
        /// }
        /// </example>
        public void Release()
        {
            count.DecrementAndGet();
        }

        public int GetNumberOfPermitsUsed()
        {
            return (int)count.Value;
        }
    }

    /// <summary>
    /// Semaphore that never blocks
    /// </summary>
    public class TryableSemaphoreNoOpImpl : ITryableSemaphore
    {
        public bool TryAcquire(int numberOfPermits)
        {
            return true;
        }

        public void Release()
        {
        }

        public int GetNumberOfPermitsUsed()
        {
            return 0;
        }
    }
}
