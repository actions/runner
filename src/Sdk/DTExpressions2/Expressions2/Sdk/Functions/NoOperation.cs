using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    /// <summary>
    /// Used for building expression parse trees.
    /// </summary>
    internal sealed class NoOperation : Function
    {
        protected override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            return null;
        }
    }
}
