using System;
using System.ComponentModel;

namespace GitHub.Build.WebApi
{
    // moved to WebAccess/Build.Plugins
    [Obsolete]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class WellKnownDataProviderKeys
    {
        // Extensions
        public const String MyDefinitions = "TFS.Build.MyDefinitions";
        public const String AllDefinitions = "TFS.Build.AllDefinitions";
        public const String QueuedDefinitions = "TFS.Build.QueuedDefinitions";
        public const String AllBuilds = "TFS.Build.AllBuilds";
        public const String DefinitionSummary = "TFS.Build.DefinitionSummary";
        public const String DefinitionHistory = "TFS.Build.DefinitionHistory";
        public const String DefinitionDeletedHistory = "TFS.Build.DefinitionDeletedHistory";

        // Resources
        public const String Builds = "TFS.Build.Builds";
        public const String Changes = "TFS.Build.Changes";
        public const String Definitions = "TFS.Build.Definitions";
        public const String Folders = "TFS.Build.Folders";
        public const String Queues = "TFS.Build.Queues";

        // Resources grouped together
        public const String BuildHistory = "TFS.Build.BuildHistory";

        // Settings
        public const String NewCIWorkflowOptInState = "TFS.Build.NewCIWorkflowOptInState";
        public const String NewCIWorkflowPreviewFeatureState = "TFS.Build.NewCIWorkflowPreviewFeatureState";

        // Others
        public const String AllDefinitionIds = "TFS.Build.AllDefinitions.DefinitionIds";
        public const String BuildIds = "TFS.Build.Mine.BuildIds";
        public const String HasMyBuilds = "TFS.Build.Mine.HasMyBuilds";
        public const String MyFavoriteDefinitionIds = "TFS.Build.MyFavoriteDefinitionIds";
        public const String TeamFavoriteDefinitionIds = "TFS.Build.TeamFavoriteDefinitionIds";

        public const String BuildsContinuationToken = "TFS.Build.Builds.ContinuationToken";
        public const String DefinitionsContinuationToken = "TFS.Build.Definitions.ContinuationToken";
    }
}
