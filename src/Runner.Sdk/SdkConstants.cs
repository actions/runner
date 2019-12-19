using System;

namespace GitHub.Runner.Sdk
{
    public class SdkConstants
    {
        public static class Variables
        {
            public static class Build
            {
                // Legacy "build" variables historically used by the runner
                // DO NOT add new variables here -- instead use either the Actions or Runner namespaces
                public const String BuildId = "build.buildId";
                public const String BuildNumber = "build.buildNumber";
                public const String ContainerId = "build.containerId";
            }
        }
    }
}
