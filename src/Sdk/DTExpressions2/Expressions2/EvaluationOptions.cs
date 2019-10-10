using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions2
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class EvaluationOptions
    {
        public EvaluationOptions()
        {
        }

        public EvaluationOptions(EvaluationOptions copy)
        {
            if (copy != null)
            {
                MaxMemory = copy.MaxMemory;
            }
        }

        public Int32 MaxMemory { get; set; }
    }
}
