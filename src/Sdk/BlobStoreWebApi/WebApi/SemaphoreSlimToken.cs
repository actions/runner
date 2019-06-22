using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.BlobStore.WebApi
{
    /// <summary>
    ///     Disposable token for guarenteed release via a using() statement
    /// </summary>
    public struct SemaphoreSlimToken : IDisposable
    {
        private SemaphoreSlim _semaphore;

        private SemaphoreSlimToken(SemaphoreSlim semaphore)
            : this()
        {
            _semaphore = semaphore;
        }

        /// <summary>
        ///     Wait on a SemaphoreSlim and return a token that, when disposed, calls Release() on the SemaphoreSlim
        /// </summary>
        /// <param name="semaphore">The semaphore to wait on</param>
        /// <returns>A token that, when disposed, calls Release() on the SemaphoreSlim</returns>
        public static async Task<SemaphoreSlimToken> Wait(SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            return new SemaphoreSlimToken(semaphore);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_semaphore != null)
            {
                _semaphore.Release();
                _semaphore = null;
            }
        }

        // ReSharper disable UnusedParameter.Global

        /// <summary>
        ///     Equality operator.
        /// </summary>
        public static bool operator ==(SemaphoreSlimToken left, SemaphoreSlimToken right)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        ///     Inequality operator.
        /// </summary>
        public static bool operator !=(SemaphoreSlimToken left, SemaphoreSlimToken right)
        {
            throw new InvalidOperationException();
        }

        // ReSharper restore UnusedParameter.Global

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            throw new InvalidOperationException();
        }
    }
}
