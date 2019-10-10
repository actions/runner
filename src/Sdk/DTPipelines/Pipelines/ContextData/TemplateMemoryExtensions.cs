using System;
using System.Collections.Generic;
using GitHub.DistributedTask.ObjectTemplating;

namespace GitHub.DistributedTask.Pipelines.ContextData
{
    internal static class TemplateMemoryExtensions
    {
        internal static void AddBytes(
            this TemplateMemory memory,
            PipelineContextData value,
            Boolean traverse)
        {
            var bytes = CalculateBytes(memory, value, traverse);
            memory.AddBytes(bytes);
        }

        internal static Int32 CalculateBytes(
            this TemplateMemory memory,
            PipelineContextData value,
            Boolean traverse)
        {
            var enumerable = traverse ? value.Traverse() : new[] { value } as IEnumerable<PipelineContextData>;
            var result = 0;
            foreach (var item in enumerable)
            {
                // This measurement doesn't have to be perfect
                // https://codeblog.jonskeet.uk/2011/04/05/of-memory-and-strings/
                switch (item?.Type)
                {
                    case PipelineContextDataType.String:
                        var str = item.AssertString("string").Value;
                        checked
                        {
                            result += TemplateMemory.MinObjectSize + TemplateMemory.StringBaseOverhead + ((str?.Length ?? 0) * sizeof(Char));
                        }
                        break;

                    case PipelineContextDataType.Array:
                    case PipelineContextDataType.Dictionary:
                    case PipelineContextDataType.Boolean:
                    case PipelineContextDataType.Number:
                        // Min object size is good enough. Allows for base + a few fields.
                        checked
                        {
                            result += TemplateMemory.MinObjectSize;
                        }
                        break;

                    case null:
                        checked
                        {
                            result += IntPtr.Size;
                        }
                        break;

                    default:
                        throw new NotSupportedException($"Unexpected pipeline context data type '{item.Type}'");
                }
            }

            return result;
        }
    }
}
