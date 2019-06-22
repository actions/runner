using System;
using System.ComponentModel;
using GitHub.Services.Common;

namespace GitHub.Build.WebApi
{
    [Obsolete("Use ArtifactResourceTypes instead.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class WellKnownArtifactResourceTypes
    {
        public const String FilePath = ArtifactResourceTypes.FilePath;
        public const String SymbolStore = ArtifactResourceTypes.SymbolStore;
        public const String VersionControl = ArtifactResourceTypes.VersionControl;
        public const String Container = ArtifactResourceTypes.Container;
        public const String GitRef = ArtifactResourceTypes.GitRef;
        public const String TfvcLabel = ArtifactResourceTypes.TfvcLabel;
        public const String SymbolRequest = ArtifactResourceTypes.SymbolRequest;
    }

    [GenerateAllConstants]
    public static class ArtifactResourceTypes
    {
        /// <summary>
        /// UNC or local folder path
        /// E.g. \\vscsstor\CIDrops\CloudU.Gated\140317.115955 or file://vscsstor/CIDrops/CloudU.Gated/140317.115955
        /// </summary>
        public const String FilePath = "FilePath";

        /// <summary>
        /// Symbol store UNC path
        /// E.g. \\symbolstore
        /// </summary>
        public const String SymbolStore = "SymbolStore";

        /// <summary>
        /// TF VC server folder path
        /// E.g. $/Dev1/Drops/CloudU.Gated/140317.115955
        /// </summary>
        public const String VersionControl = "VersionControl";

        /// <summary>
        /// Build container reference
        /// E.g. #/2121/drop
        /// </summary>
        public const String Container = "Container";

        /// <summary>
        /// Git ref
        /// E.g. refs/tags/MyCIDefinition.Buildable
        /// </summary>
        public const String GitRef = "GitRef";

        /// <summary>
        /// TFVC label
        /// </summary>
        public const String TfvcLabel = "TfvcLabel";

        /// <summary>
        /// Symbol store URL 
        /// E.g. https://mseng.artifacts.visualstudio.com/...
        /// </summary>
        public const String SymbolRequest = "SymbolRequest";

        /// <summary>
        /// Dedup Drop (old name fo PipelineArtifact)
        /// E.g. drop1
        /// </summary>
        public const String Drop = "Drop";

        /// <summary>
        /// Dedup'ed pipeline artifact
        /// E.g. artifact1
        /// </summary>
        public const String PipelineArtifact = "PipelineArtifact";
    }
}
