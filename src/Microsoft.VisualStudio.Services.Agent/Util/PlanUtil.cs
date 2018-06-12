using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.OAuth;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Services.Agent.Util
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