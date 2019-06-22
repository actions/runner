using System;

namespace GitHub.Build.WebApi
{
    [Obsolete("No longer used.")]
    public static class WellKnownBuildOptions
    {
        public static readonly Guid CreateDrop = Guid.Parse("{E8B30F6F-039D-4D34-969C-449BBE9C3B9E}");
        public static readonly Guid CopyToStagingFolder = Guid.Parse("{82F9A3E8-3930-482E-AC62-AE3276F284D5}");
    }
}
