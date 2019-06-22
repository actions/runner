using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [GenerateAllConstants]
    public static class DefinitionMetrics
    {
        // historic metrics
        public const String SuccessfulBuilds = "SuccessfulBuilds";
        public const String FailedBuilds = "FailedBuilds";
        public const String PartiallySuccessfulBuilds = "PartiallySuccessfulBuilds";
        public const String CanceledBuilds = "CanceledBuilds";
        public const String TotalBuilds = "TotalBuilds";

        // current metrics - scopeddate null
        public const String CurrentBuildsInQueue = "CurrentBuildsInQueue";
        public const String CurrentBuildsInProgress = "CurrentBuildsInProgress";
    }

    [Obsolete("Use DefinitionMetrics instead.")]
    public static class WellKnownDefinitionMetrics
    {
        // historic metrics
        public const String SuccessfulBuilds = DefinitionMetrics.SuccessfulBuilds;
        public const String FailedBuilds = DefinitionMetrics.FailedBuilds;
        public const String PartiallySuccessfulBuilds = DefinitionMetrics.PartiallySuccessfulBuilds;
        public const String CanceledBuilds = DefinitionMetrics.CanceledBuilds;
        public const String TotalBuilds = DefinitionMetrics.TotalBuilds;

        // current metrics - scopeddate null
        public const String CurrentBuildsInQueue = DefinitionMetrics.CurrentBuildsInQueue;
        public const String CurrentBuildsInProgress = DefinitionMetrics.CurrentBuildsInProgress;
    }
}
