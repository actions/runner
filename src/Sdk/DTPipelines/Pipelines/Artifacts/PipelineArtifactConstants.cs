using System;

using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines.Artifacts
{
    public static class PipelineArtifactConstants
    {
        internal static class CommonArtifactTaskInputValues
        {
            internal const String DefaultDownloadPath = "$(Pipeline.Workspace)";
            internal const String DefaultDownloadPattern = "**";
        }

        public static class PipelineArtifactTaskInputs
        {
            public const String ArtifactName = "artifactName";

            public const String BuildType = "buildType";

            public const String BuildId = "buildId";

            public const String BuildVersionToDownload = "buildVersionToDownload";

            public const String Definition = "definition";

            public const String DownloadType = "downloadType";

            public const String DownloadPath = "downloadPath";

            public const String FileSharePath = "fileSharePath";

            public const String ItemPattern = "itemPattern";

            public const String Project = "project";
        }

        public static class PipelineArtifactTaskInputValues
        {
            public const String DownloadTypeSingle = "single";
            public const String SpecificBuildType = "specific";
            public const String CurrentBuildType = "current";
            public const String AutomaticMode = "automatic";
            public const String ManualMode = "manual";
        }

        internal static class YamlConstants
        {
            internal const String Connection = "connection";
            internal const String Current = "current";
            internal const String None = "none";
        }

        public static class ArtifactTypes
        {
            public const string AzurePipelineArtifactType = "Pipeline";
        }

        public static class DownloadTaskInputs
        {
            public const String Alias = "alias";
            public const String Artifact = "artifact";
            public const String Mode = "mode";
            public const String Path = "path";
            public const String Patterns = "patterns";
        }

        public static class TraceConstants
        {
            public const String Area = "PipelineArtifacts";
            public const String DownloadPipelineArtifactFeature = "DownloadPipelineArtifact";
        }

        public static readonly TaskDefinition DownloadTask = new TaskDefinition
        {
            Id = new Guid("30f35852-3f7e-4c0c-9a88-e127b4f97211"),
            Name = "Download",
            FriendlyName = "Download Artifact",
            Author = "Microsoft",
            RunsOn = { TaskRunsOnConstants.RunsOnAgent },
            Version = new TaskVersion("1.0.0"),
            Description = "Downloads pipeline type artifacts.",
            HelpMarkDown = "[More Information](https://github.com)",
            Inputs = {
                new TaskInputDefinition()
                {
                    Name =  DownloadTaskInputs.Artifact,
                    Required = true,
                    InputType = TaskInputType.String
                },
                new TaskInputDefinition()
                {
                    Name = DownloadTaskInputs.Patterns,
                    Required = false,
                    DefaultValue = "**",
                    InputType = TaskInputType.String
                },
                new TaskInputDefinition()
                {
                    Name = DownloadTaskInputs.Path,
                    Required = false,
                    InputType = TaskInputType.String
                },
                new TaskInputDefinition()
                {
                    Name=DownloadTaskInputs.Alias,
                    Required = false,
                    InputType = TaskInputType.String
                }
            },
        };
    }
}
