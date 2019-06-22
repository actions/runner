using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GitHub.Services.Content.Common
{
    /*
     * Dataflow blocks swallows all operation canceled exceptions not linked to the cancellation token passed.
     * (eg. the OperationCanceledException does not come from the CancellationToken, cancellationToken.ThrowIfCancellationRequested())
     * https://stackoverflow.com/questions/37219648/operationcanceledexception-thrown-in-tpl-dataflow-block-gets-swallowed
     * https://github.com/dotnet/corefx/blob/master/src/System.Threading.Tasks.Dataflow/src/Blocks/ActionBlock.cs#L133-L150
     * We designed our system with the intention of having dataflow blocks by default throwing on all exceptions. As a result, we wrapped all
     * functions within dataflow blocks to throw a TimeoutException instead when an OperationCanceledExceptions are thrown.
    */
    public static class NonSwallowingActionBlock
    {
        public static ActionBlock<T> Create<T>(Action<T> action)
        {
            Action<T> nonSwallowingAction = CreateNonSwallowingAction(action);
            return new ActionBlock<T>(nonSwallowingAction);
        }

        public static ActionBlock<T> Create<T>(Func<T, Task> action)
        {
            Func<T, Task> nonSwallowingAction = CreateNonSwallowingFunc(action);
            return new ActionBlock<T>(nonSwallowingAction);
        }
        
        public static ActionBlock<T> Create<T>(Action<T> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            Action<T> nonSwallowingAction = CreateNonSwallowingAction(action);
            return new ActionBlock<T>(nonSwallowingAction, dataflowBlockOptions);
        }

        public static ActionBlock<T> Create<T>(Func<T, Task> action, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            Func<T, Task> nonSwallowingAction = CreateNonSwallowingFunc(action);
            return new ActionBlock<T>(nonSwallowingAction, dataflowBlockOptions);
        }

        private static Action<T> CreateNonSwallowingAction<T>(Action<T> action)
        {
            Action<T> nonSwallowingAction = (input) =>
            {
                try
                {
                    action(input);
                }
                catch (OperationCanceledException oce)
                {
                    throw NonSwallowingTransformBlockUtils.CreateTimeoutException(oce, nameof(ActionBlock<T>));
                }
            };
            return nonSwallowingAction;
        }

        private static Func<T, Task> CreateNonSwallowingFunc<T>(Func<T, Task> action)
        {
            Func<T, Task> nonSwallowingAction = async (input) =>
            {
                try
                {
                    await action(input).ConfigureAwait(false);
                }
                catch (OperationCanceledException oce)
                {
                    throw NonSwallowingTransformBlockUtils.CreateTimeoutException(oce, nameof(ActionBlock<T>));
                }
            };
            return nonSwallowingAction;
        }
    }
}
