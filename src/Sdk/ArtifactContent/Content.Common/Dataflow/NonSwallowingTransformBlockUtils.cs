using System;
using System.Threading.Tasks;

namespace GitHub.Services.Content.Common
{
    internal static class NonSwallowingTransformBlockUtils
    {
        internal static Func<TInput, TTransformOutput> CreateNonSwallowingFunc<TInput, TTransformOutput>(Func<TInput, TTransformOutput> transform, string nameofBlock)
        {
            Func<TInput, TTransformOutput> nonSwallowingTransform = (input) =>
            {
                try
                {
                    return transform(input);
                }
                catch (OperationCanceledException oce)
                {
                    throw CreateTimeoutException(oce, nameofBlock);
                }
            };
            return nonSwallowingTransform;
        }

        internal static Func<TInput, Task<TTransformOutput>> CreateNonSwallowingTaskFunc<TInput, TTransformOutput>(Func<TInput, Task<TTransformOutput>> transform, string nameofBlock)
        {
            Func<TInput, Task<TTransformOutput>> nonSwallowingTransform = async (input) =>
            {
                try
                {
                    return await transform(input);
                }
                catch (OperationCanceledException oce)
                {
                    throw CreateTimeoutException(oce, nameofBlock);
                }
            };
            return nonSwallowingTransform;
        }

        // if the exception thrown is changing, make sure AsyncEnumerator disposal's catch on the same exception does not throw
        internal static TimeoutException CreateTimeoutException(OperationCanceledException oce, string nameofBlock)
        {
            return new TimeoutException($"Timed out while running {nameofBlock}.", oce);
        }
    }
}
