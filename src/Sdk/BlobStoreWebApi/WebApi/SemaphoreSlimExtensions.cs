using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.BlobStore.WebApi
{
    /// <summary>
    ///     Static class that provides a disposable token for guaranteed release via a using() statement
    /// </summary>
    public static class SemaphoreSlimExtensions
    {
        /// <summary>
        ///     Get a disposable token for guaranteed release via a using() statement.
        /// </summary>
        public static Task<SemaphoreSlimToken> WaitToken(this SemaphoreSlim semaphore)
        {
            return SemaphoreSlimToken.Wait(semaphore);
        }
    }
}
