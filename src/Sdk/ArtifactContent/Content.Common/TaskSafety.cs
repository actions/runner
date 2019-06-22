using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// Runs the task in the threadpool to prevent deadlocks
    /// </summary>
    /// <remarks>http://blogs.msdn.com/b/pfxteam/archive/2012/04/13/10293638.aspx</remarks>
    public static class TaskSafety
    {
        public static void SyncResultOnThreadPool(Func<Task> taskFunc, CancellationToken cancellationToken = default(CancellationToken))
        {
            Task.Run(taskFunc, cancellationToken).GetAwaiter().GetResult();
        }

        public static T SyncResultOnThreadPool<T>(Func<Task<T>> taskFunc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(taskFunc, cancellationToken).GetAwaiter().GetResult();
        }
    }
}
