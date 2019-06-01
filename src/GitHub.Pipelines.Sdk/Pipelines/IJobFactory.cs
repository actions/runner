using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    internal interface IJobFactory
    {
        String Name { get; }

        Job CreateJob(
            JobExecutionContext context,
            ExpressionValue<String> container,
            IDictionary<String, ExpressionValue<String>> sidecarContainers,
            Boolean continueOnError,
            Int32 timeoutInMinutes,
            Int32 cancelTimeoutInMinutes,
            String displayName = null);
    }
}
