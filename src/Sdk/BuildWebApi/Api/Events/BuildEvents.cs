using System;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [GenerateAllConstants]
    public static class BuildEvents
    {
        public const String ArtifactAdded = "artifactAdded";
        public const String BuildUpdated = "buildUpdated";
        public const String ChangesCalculated = "changesCalculated";
        public const String ConsoleLinesReceived = "consoleLinesReceived";
        public const String StagesUpdated = "stagesUpdated";
        public const String TagsAdded = "tagsAdded";
        public const String TimelineRecordsUpdated = "timelineRecordsUpdated";
    }
}
