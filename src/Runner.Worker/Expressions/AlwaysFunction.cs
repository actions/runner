using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;
using PipelineTemplateConstants = GitHub.DistributedTask.Pipelines.ObjectTemplating.PipelineTemplateConstants;

namespace GitHub.Runner.Worker.Expressions
{
    public sealed class AlwaysFunction : Function
    {
        protected override Object EvaluateCore(EvaluationContext context, out ResultMemory resultMemory)
        {
            resultMemory = null;
            return true;
        }
    }
}
