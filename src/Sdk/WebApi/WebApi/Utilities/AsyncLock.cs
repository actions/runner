using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.WebApi.Utilities
{
    /// <summary>
    /// From: http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx
    /// </summary>
    internal sealed class AsyncLock
    {
        public AsyncLock()
        {
            m_releaser = Task.FromResult((IDisposable)new Releaser(this));
        }

        public Task<IDisposable> LockAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Don't pass cancellationToken to the semaphore. If we can't acquire the semaphore immediately
            // we'll still get the waitTask returned immediately (with IsCompleted = false)
            // and then we'll end up in the else block where we add a continuation to the waitTask which will honor the cancellationToken
            Task waitTask = m_semaphore.WaitAsync();

            if (waitTask.IsCompleted)
            {
                return m_releaser;
            }
            else
            {
                return waitTask.ContinueWith(
                    (task, state) => (IDisposable)state,
                    m_releaser.Result,
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }

        private readonly SemaphoreSlim m_semaphore = new SemaphoreSlim(1, 1);
        private readonly Task<IDisposable> m_releaser;

        private sealed class Releaser : IDisposable
        {
            internal Releaser(AsyncLock toRelease)
            {
                m_toRelease = toRelease;
            }

            public void Dispose()
            {
                m_toRelease.m_semaphore.Release();
            }

            private readonly AsyncLock m_toRelease;
        }
    }
}
