using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GitHub.Services.Content.Common
{
    // Dataflow blocks swallow all operation canceled exceptions linked to the cancellation token passed
    // https://stackoverflow.com/questions/37219648/operationcanceledexception-thrown-in-tpl-dataflow-block-gets-swallowed
    // We designed our system with the intention of having dataflow blocks by default throwing on all exceptions
    // https://github.com/dotnet/corefx/blob/master/src/System.Threading.Tasks.Dataflow/src/Blocks/ActionBlock.cs#L133-L150
    public static class NonSwallowingTransformManyBlock
    {
        public static TransformManyBlock<TInput, TOutput> Create<TInput, TOutput>(Func<TInput, IEnumerable<TOutput>> transform)
        {
            Func<TInput, IEnumerable<TOutput>> nonSwallowingTransform = NonSwallowingTransformBlockUtils.CreateNonSwallowingFunc(transform, nameof(TransformManyBlock<TInput, TOutput>));
            return new TransformManyBlock<TInput, TOutput>(nonSwallowingTransform);
        }

        public static TransformManyBlock<TInput, TOutput> Create<TInput, TOutput>(Func<TInput, Task<IEnumerable<TOutput>>> transform)
        {
            Func<TInput, Task<IEnumerable<TOutput>>> nonSwallowingTransform = NonSwallowingTransformBlockUtils.CreateNonSwallowingTaskFunc(transform, nameof(TransformManyBlock<TInput, TOutput>));
            return new TransformManyBlock<TInput, TOutput>(nonSwallowingTransform);
        }

        public static TransformManyBlock<TInput, TOutput> Create<TInput, TOutput>(Func<TInput, IEnumerable<TOutput>> transform, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            Func<TInput, IEnumerable<TOutput>> nonSwallowingTransform = NonSwallowingTransformBlockUtils.CreateNonSwallowingFunc(transform, nameof(TransformManyBlock<TInput, TOutput>));
            return new TransformManyBlock<TInput, TOutput>(nonSwallowingTransform, dataflowBlockOptions);
        }

        public static TransformManyBlock<TInput, TOutput> Create<TInput, TOutput>(Func<TInput, Task<IEnumerable<TOutput>>> transform, ExecutionDataflowBlockOptions dataflowBlockOptions)
        {
            Func<TInput, Task<IEnumerable<TOutput>>> nonSwallowingTransform = NonSwallowingTransformBlockUtils.CreateNonSwallowingTaskFunc(transform, nameof(TransformManyBlock<TInput, TOutput>));
            return new TransformManyBlock<TInput, TOutput>(nonSwallowingTransform, dataflowBlockOptions);
        }
    }
}
