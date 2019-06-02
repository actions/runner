using System;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Artifacts
{
    public static class ArtifactConstants
    {
        internal static class ArtifactType
        {
            internal const String Build = nameof(Build);
            internal const String Container = nameof(Container);
            internal const String Package = nameof(Package);
            internal const String SourceControl = nameof(SourceControl);
        }
    }
}