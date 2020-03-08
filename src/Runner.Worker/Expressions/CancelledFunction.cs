using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;
using PipelineTemplateConstants = GitHub.DistributedTask.Pipelines.ObjectTemplating.PipelineTemplateConstants;

namespace GitHub.Runner.Worker.Expressions
{
    public sealed class CancelledFunction : Function
    {
        protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
        {
            resultMemory = null;
            var templateContext = evaluationContext.State as TemplateContext;
            ArgUtil.NotNull(templateContext, nameof(templateContext));
            var executionContext = templateContext.State[nameof(IExecutionContext)] as IExecutionContext;
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ActionResult jobStatus = executionContext.JobContext.Status ?? ActionResult.Success;
            return jobStatus == ActionResult.Cancelled;
        }
    }
}
