using System;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Util
{
    public static class PlanUtil
    {
        public static PlanFeatures GetFeatures(TaskOrchestrationPlanReference plan)
        {
            ArgUtil.NotNull(plan, nameof(plan));
            PlanFeatures features = PlanFeatures.None;
            if (plan.Version >= 8)
            {
                features |= PlanFeatures.JobCompletedPlanEvent;
            }

            return features;
        }
    }

    [Flags]
    public enum PlanFeatures
    {
        None = 0,
        JobCompletedPlanEvent = 1,
    }
}
