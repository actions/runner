using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    public static class TaskCancellationExtensions
    {
        private struct Void { }

        /// <summary>
        /// Some APIs (e.g. HttpClient) don't honor cancellation tokens.  This wrapper adds an extra layer of cancellation checking.
        /// </summary>
        public static Task EnforceCancellation(
            this Task task,
            CancellationToken cancellationToken,
            Func<string> makeMessage = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = -1)
        {
            Func<Task<Void>> task2 = async () =>
            {
                await task.ConfigureAwait(false);
                return new Void();
            };

            return task2().EnforceCancellation<Void>(cancellationToken, makeMessage, file, member, line);
        }

        /// <summary>
        /// Some APIs (e.g. HttpClient) don't honor cancellation tokens.  This wrapper adds an extra layer of cancellation checking.
        /// </summary>
        public static async Task<TResult> EnforceCancellation<TResult>(
            this Task<TResult> task,
            CancellationToken cancellationToken,
            Func<string> makeMessage = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = -1)
        {
            ArgumentUtility.CheckForNull(task, nameof(task));

            // IsCompleted will return true when the task is in one of the three final states: RanToCompletion, Faulted, or Canceled.
            if (task.IsCompleted)
            {
                return await task;
            }

            var cancellationTcs = new TaskCompletionSource<bool>(RUN_CONTINUATIONS_ASYNCHRONOUSLY);
            using (cancellationToken.Register(() => cancellationTcs.SetResult(false)))
            {
                var completedTask = await Task.WhenAny(task, cancellationTcs.Task).ConfigureAwait(false);
                if (completedTask == task)
                {
                    return await task;
                }
            }

            // Even if our actual task actually did honor the cancellation token, there's still a race that our WaitForCancellation
            // task may have handled the cancellation more quickly.
            if (!cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("Task ended but cancellation token is not marked for cancellation.");
            }

            // However, we'd ideally like to throw the cancellation exception from the original task if we can.
            // Thus, we'll give that task a few seconds to coallesce (e.g. write to a log) before we give up on it.
            int seconds = 3;
            var lastChanceTcs = new TaskCompletionSource<bool>(RUN_CONTINUATIONS_ASYNCHRONOUSLY);
            using (var lastChanceTimer = new CancellationTokenSource(TimeSpan.FromSeconds(seconds)))
            using (lastChanceTimer.Token.Register(() => lastChanceTcs.SetResult(false)))
            {
                var completedTask = await Task.WhenAny(task, lastChanceTcs.Task).ConfigureAwait(false);
                if (completedTask == task)
                {
                    return await task;
                }
            }

            // At this point, we've given up on waiting for this task.
            ObserveExceptionIfNeeded(task);

            string errorString = $"Task in function {member} at {file}:{line} was still active {seconds} seconds after operation was cancelled.";
            if (makeMessage != null)
            {
                errorString += $" {makeMessage()}";
            }

            throw new OperationCanceledException(errorString, cancellationToken);
        }

        private static void ObserveExceptionIfNeeded(Task task)
        {
            task.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// This is a flag exposed by TaskCreationOptions and TaskContinuationOptions but it's not in .Net 4.5
        /// In Azure we have latest .Net loaded which will consume this flag.
        /// Client environments using earlier .Net would ignore it.
        /// </summary>
        private const int RUN_CONTINUATIONS_ASYNCHRONOUSLY = 0x40;
    }
}
