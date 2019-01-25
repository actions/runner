using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.FeatureAvailability.WebApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Worker.CodeCoverage
{
    public static class FeatureFlagUtility
    {
        public static bool GetFeatureFlagState(FeatureAvailabilityHttpClient featureAvailabilityHttpClient, string FFName, IAsyncCommandContext context)
        {
            try
            {
                var featureFlag = featureAvailabilityHttpClient?.GetFeatureFlagByNameAsync(FFName).Result;
                if (featureFlag != null && featureFlag.EffectiveState.Equals("Off", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            catch
            {
                context.Debug(StringUtil.Format("Failed to get FF {0} Value. By default, publishing data to TCM.", FFName));
                return true;
            }
            return true;
        }
    }
}
