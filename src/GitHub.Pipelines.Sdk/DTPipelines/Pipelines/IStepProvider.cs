using System;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    public interface IStepProvider
    {
        IList<TaskStep> GetPreSteps(IPipelineContext context, IReadOnlyList<JobStep> steps);
        IList<TaskStep> GetPostSteps(IPipelineContext context, IReadOnlyList<JobStep> steps);

        /// <summary>
        /// Given a JobStep (eg., download step) it will translate into corresndponding task steps
        /// </summary>
        /// <param name="context"></param>
        /// <param name="step">Input step to be resolved</param>
        /// <param name="resolvedSteps">Resolved output steps</param>
        /// <returns>true if this is resolved, false otherwise. Passing a powershell step to ResolveStep would return false</returns>
        Boolean ResolveStep(IPipelineContext context, JobStep step, out IList<TaskStep> resolvedSteps);
    }
}
