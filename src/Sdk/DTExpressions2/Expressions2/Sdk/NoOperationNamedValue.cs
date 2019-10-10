using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    internal sealed class NoOperationNamedValue : NamedValue
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
