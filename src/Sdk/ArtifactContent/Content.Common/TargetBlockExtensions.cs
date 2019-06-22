using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GitHub.Services.Content.Common
{
    /// <summary>
    /// Indicates that the target block is in an invalid state when failing to receive an item. This can happen under certain circumstances:
    /// 1) the target block is bounded and the buffer is full;
    /// 2) a race condition happens so that when checking the completion state the original exception has not been set yet.
    /// </summary>
    public class IllegalTargetBlockStateException : InvalidOperationException
    {
        public IllegalTargetBlockStateException(string message) : base(message)
        {
        }
    }

    /// <remarks>
    /// As of 10/25/2018, we're still using the "ancient" DataFlow package which customers are being discouraged from using.
    /// See https://github.com/dotnet/corefx/issues/24547 "Can something be done about the Microsoft.Tpl.Dataflow package?"
    /// Ours per /.nuget/externals/UnifiedDependencies.xml: <package id="Microsoft.Tpl.Dataflow" version="4.5.24" availableAtBuildTime="true" availableAtDeployTime="true" />
    ///      https://dev.azure.com/mseng/AzureDevOps/_packaging?feed=Codex-Deps&package=Microsoft.Tpl.Dataflow&version=4.5.24&protocolType=NuGet&_a=package&view=versions
    /// Old: https://www.nuget.org/packages/Microsoft.Tpl.Dataflow/ (v4.5.24 last updated 12/10/2014)
    /// New: https://www.nuget.org/packages/System.Threading.Tasks.Dataflow/ (v4.8.0 last updated 8/11/2017)
    /// 
    /// An assumption for the use of target block throughout Artifact service codebase is we do not apply bounding
    /// (note it is NOT max parallelism). As long as bounding is unlimited, Post() should not return false unless
    /// the block is marked as complete, or it's faulted by a queued task. And Send() should not be affected by 
    /// bounding at all.
    /// </remarks>
    public static class TargetBlockExtensions
    {
        /// <summary>
        /// Throws <see cref="IllegalTargetBlockStateException"/> if the action block is in an invalid state when a new item is added. 
        /// </summary>
        public static void PostOrThrow<T>(this ITargetBlock<T> targetBlock, T input, CancellationToken token)
        {
            if (!targetBlock.Post(input))
            {
                ThrowError(targetBlock, "post", token);
            }
        }

        /// <summary>
        /// Throws <see cref="IllegalTargetBlockStateException"/> if the action block is in an invalid state when a new item is added. 
        /// </summary>
        public static Task SendOrThrowAsync<T, T2>(this ITargetBlock<T> targetBlock, ITargetBlock<T2> finalBlock, T input, CancellationToken token)
        {
            if (targetBlock == finalBlock)
            {
                return targetBlock.SendOrThrowAsync(input, token);
            }
            else
            {
                return finalBlock.CancelProducerIfDataflowFaulted(linkedToken =>
                    targetBlock.SendOrThrowAsync(input, linkedToken.Token), token);
            }
        }

        public static Task SendOrThrowAsync<T>(this ITargetBlock<T> targetBlock, T input, DataflowNetworkCancellationToken token)
        {
            return targetBlock.SendOrThrowAsync(input, token.Token);
        }

        public static Task SendOrThrowSingleBlockNetworkAsync<T>(this ActionBlock<T> actionBlock, T input, CancellationToken token)
        {
            ITargetBlock<T> targetBlock = actionBlock;
            return targetBlock.SendOrThrowAsync(input, token);
        }

        private static async Task SendOrThrowAsync<T>(this ITargetBlock<T> targetBlock, T input, CancellationToken token)
        {
            if (!await targetBlock.SendUnsafeAsync(input, token).ConfigureAwait(false))
            {
                ThrowError(targetBlock, "send", token);
            }
        }

        public static Task<bool> SendUnsafeAsync<TInput>(this ITargetBlock<TInput> target, TInput item)
        {
            return DataflowBlock.SendAsync(target, item);
        }

        public static Task<bool> SendUnsafeAsync<TInput>(this ITargetBlock<TInput> target, TInput item, CancellationToken cancellationToken)
        {
            return DataflowBlock.SendAsync(target, item, cancellationToken);
        }

        /// <summary>
        /// Posts all inputs to the targetBlock, marks the targetBlock complete, and returns the completion task of the targetBlock.
        /// <b>WARNING</b>: as this method's name suggests, the action block must be unbounded.
        /// </summary>
        public static Task PostAllToUnboundedAndCompleteAsync<T>(this ITargetBlock<T> targetBlock, IEnumerable<T> inputs, CancellationToken cancellationToken)
        {
            foreach (T input in inputs)
            {
                try
                {
                    targetBlock.PostOrThrow(input, cancellationToken);
                }
                catch (InvalidOperationException) when (targetBlock.Completion.IsFaulted)
                {
                    // If this exception is due to the action block already in faulted state (error from previous item), then propagate the original exception 
                    break;
                }
            }

            targetBlock.Complete();
            return targetBlock.Completion;
        }

        public struct DataflowNetworkCancellationToken
        {
            public readonly CancellationToken Token;

            internal DataflowNetworkCancellationToken(CancellationToken cancellationToken)
            {
                Token = cancellationToken;
            }
        }

        internal static async Task CancelProducerIfDataflowFaulted<T>(this ITargetBlock<T> finalBlock, Func<DataflowNetworkCancellationToken, Task> producerAsync, CancellationToken token)
        {
            using (var cts = new CancellationTokenSource())
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, token))
            {
                var dataflowNetworkCancelToken = new DataflowNetworkCancellationToken(linkedCts.Token);
                Task finalBlockCompletion = finalBlock.Completion;
                Task producerTask = producerAsync(dataflowNetworkCancelToken);

                Task completedTask = await Task.WhenAny(finalBlockCompletion, producerTask).ConfigureAwait(false);
                if (completedTask == finalBlockCompletion)
                {
                    // At this point, the final block is complete. This means that the producer will get stuck if it
                    // tries to continuing sending to that network. So we send a cancellation to the sender.

                    // Do not cancel linkedCts, reason:
                    // if we have linkedCts.Cancel() then cts.IsCancelled is always false
                    // linkedCts.IsCancelled means either final block faulted OR the provided token was cancelled
                    cts.Cancel();

                    // Now that the producer is cancelled, we should await it (which should complete as soon as the cancellation
                    // is observed) and the final block as well (which has already run to termination).

                    // We await both tasks so that we get exceptions from both.

                    await Task.WhenAll(producerTask, finalBlockCompletion).ConfigureAwait(false);
                }
                else
                {
                    await producerTask.ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Sends all inputs to the actionBlock, marks the actionBlock as complete, and awaits completion of the actionBlock.
        /// </summary>
        public static Task SendAllAndCompleteSingleBlockNetworkAsync<T1>(this ActionBlock<T1> actionBlock, IEnumerable<T1> inputs, CancellationToken token)
        {
            return actionBlock.SendAllAndCompleteAsync(inputs, token);
        }

        private static async Task SendAllAndCompleteAsync<T1>(this ITargetBlock<T1> targetBlock, IEnumerable<T1> inputs, CancellationToken token)
        {
            foreach (T1 input in inputs)
            {
                try
                {
                    await targetBlock.SendOrThrowAsync(input, token).ConfigureAwait(false);
                }
                catch (InvalidOperationException) when (targetBlock.Completion.IsFaulted)
                {
                    // If this exception is due to the action block already in faulted state (error from previous item), then propagate the original exception 
                    break;
                }
            }

            targetBlock.Complete();
            await targetBlock.Completion.ConfigureAwait(false);
        }

        /// <summary>
        /// Sends all inputs to the targetBlock, marks the targetBlock as complete, and awaits completion of the targetBlock.
        /// If during the course of sending inputs, a subsequent block in the network faults (due to an exception), then the exception will be thrown.
        /// <b>WARNING</b>: if the action block is bounded, this method may flood the buffer and will have to wait indefinitely for vacancy to send more items in.
        /// </summary>
        public static async Task SendAllAndCompleteAsync<T1, T2>(this ITargetBlock<T1> targetBlock, IEnumerable<T1> inputs, ITargetBlock<T2> finalBlock, CancellationToken token)
        {
            if (targetBlock == finalBlock)
            {
                await targetBlock.SendAllAndCompleteAsync(inputs, token);
                return;
            }

            await finalBlock.CancelProducerIfDataflowFaulted(producerAsync: async linkedToken =>
            {
                foreach (T1 input in inputs)
                {
                    try
                    {
                        await targetBlock.SendOrThrowAsync(input, linkedToken).ConfigureAwait(false);
                    }
                    catch (InvalidOperationException) when (targetBlock.Completion.IsFaulted)
                    {
                        // If this exception is due to the action block already in faulted state (error from previous item), then propagate the original exception 
                        break;
                    }
                }
            }, token).ConfigureAwait(false);

            targetBlock.Complete();
            await finalBlock.Completion.ConfigureAwait(false);
        }

        /// <summary>
        /// Drain the block, expose and throw the real error
        /// </summary>
        private static void ThrowError<T>(ITargetBlock<T> actionBlock, string verb, CancellationToken token)
        {
            var completion = actionBlock.Completion;

            // 1) To mitigate a race condition inside the block, drain the input for 5 seconds.
            if (!(completion.IsCanceled || completion.IsFaulted || completion.IsCompleted))
            {
                int index = Task.WaitAny(completion, Task.Delay(TimeSpan.FromSeconds(5)));
                if (index == 1)
                {
                    if (!(completion.IsCanceled || completion.IsFaulted))
                    {
                        throw new IllegalTargetBlockStateException(
                            $"Could not {verb} to ActionBlock, which nonetheless is not cancelled or faulted, and has not completed after waiting for 5 seconds.");
                    }
                }
            }

            // 2) Expose the real error.
            if (completion.IsCanceled)
            {
                throw new TaskCanceledException(
                    $"Could not {verb} to ActionBlock. The block is cancelled.{(token.IsCancellationRequested ? " The cancellation has been requested on the passed token." : "")}");
            }
            else if (completion.IsFaulted)
            {
                throw new InvalidOperationException(
                    $"Could not {verb} to faulted ActionBlock. Error: {completion.Exception.Message}", completion.Exception);
            }
            else if (completion.IsCompleted)
            {
                // This should not happen if the block is properly used.
                throw new InvalidOperationException($"Could not {verb} to ActionBlock. The block is completed.");
            }
            else
            {
                // A strange case we need to investigate if reproducing.
                string extra = String.Equals("post", verb) ? " This might be caused by bounded capacity setting." : "";
                throw new IllegalTargetBlockStateException($"Could not {verb} to ActionBlock, which nonetheless is not completed, cancelled, or faulted." + extra);
            }
        }

        // The below extensions are intentionally designed to cause compile errors (ambiguous calls) at the callsite.

        private const string SendAsyncHangPatternExplanation = "Use of ITargetBlock<TInput>.SendAsync can lead to a hang when used in a Dataflow with BoundedCapacity and faults. Call SendUnsafeAsync if this was intended.";

        /// <summary>
        /// <see cref="SendAsyncHangPatternExplanation"/>
        /// </summary>
        [Obsolete(SendAsyncHangPatternExplanation, error: true)]
        public static Task<bool> SendAsync<TInput>(this ITargetBlock<TInput> target, TInput item, CancellationToken cancellationToken, object thisIsUnsafe1 = null)
        {
            throw new InvalidOperationException(SendAsyncHangPatternExplanation);
        }

        /// <summary>
        /// <see cref="SendAsyncHangPatternExplanation"/>
        /// </summary>
        [Obsolete(SendAsyncHangPatternExplanation, error: true)]
        public static Task<bool> SendAsync<TInput>(this ITargetBlock<TInput> target, TInput item, CancellationToken cancellationToken, string thisIsUnsafe2 = null)
        {
            throw new InvalidOperationException(SendAsyncHangPatternExplanation);
        }

        /// <summary>
        /// <see cref="SendAsyncHangPatternExplanation"/>
        /// </summary>
        [Obsolete(SendAsyncHangPatternExplanation, error: true)]
        public static Task<bool> SendAsync<TInput>(this ITargetBlock<TInput> target, TInput item, object thisIsUnsafe = null)
        {
            throw new InvalidOperationException(SendAsyncHangPatternExplanation);
        }

        /// <summary>
        /// <see cref="SendAsyncHangPatternExplanation"/>
        /// </summary>
        [Obsolete(SendAsyncHangPatternExplanation, error: true)]
        public static Task<bool> SendAsync<TInput>(this ITargetBlock<TInput> target, TInput item, string thisIsUnsafe2 = null)
        {
            throw new InvalidOperationException(SendAsyncHangPatternExplanation);
        }
    }
}
