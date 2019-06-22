using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GitHub.Services.Content.Common
{
    // Dataflow blocks swallow all operation canceled exceptions linked to the cancellation token passed
    // https://stackoverflow.com/questions/37219648/operationcanceledexception-thrown-in-tpl-dataflow-block-gets-swallowed
    // We designed our system with the intention of having dataflow blocks by default throwing on all exceptions
    // https://github.com/dotnet/corefx/blob/master/src/System.Threading.Tasks.Dataflow/src/Blocks/ActionBlock.cs#L133-L150
    public static class NonSwallowingTransformBlock
    {
        public static TransformBlock<TInput, TOutput> Create<TInput, TOutput>(Func<TInput, TOutput> transform)
        {
            Func<TInput, TOutput> nonSwallowingTransform = NonSwallowingTransformBlockUtils.CreateNonSwallowingFunc(transform, nameof(TransformBlock<TInput, TOutput>));
            return new TransformBlock<TInput, TOutput>(nonSwallowingTransform);
        }

        public static TransformBlock<TInput, TOutput> Create<TInput, TOutput>(Func<TInput, Task<TOutput>> transform)
        {
            Func<TInput, Task<TOutput>> nonSwallowingTransform = NonSwallowingTransformBlockUtils.CreateNonSwallowingTaskFunc(transform, nameof(TransformBlock<TInput, TOutput>));
            return new TransformBlock<TInput, TOutput>(nonSwallowingTransform);
        }

        public static TransformBlock<TInput, TOutput> Create<TInput, TOutput>(Func<TInput, TOutput> transform, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            Func<TInput, TOutput> nonSwallowingTransform = NonSwallowingTransformBlockUtils.CreateNonSwallowingFunc(transform, nameof(TransformBlock<TInput, TOutput>));
            return new TransformBlock<TInput, TOutput>(nonSwallowingTransform, dataflowBlockOptions);
        }

        public static TransformBlock<TInput, TOutput> Create<TInput, TOutput>(Func<TInput, Task<TOutput>> transform, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            Func<TInput, Task<TOutput>> nonSwallowingTransform = NonSwallowingTransformBlockUtils.CreateNonSwallowingTaskFunc(transform, nameof(TransformBlock<TInput, TOutput>));
            return new TransformBlock<TInput, TOutput>(nonSwallowingTransform, dataflowBlockOptions);
        }
    }
}
