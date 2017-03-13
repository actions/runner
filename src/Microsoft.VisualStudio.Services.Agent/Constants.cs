using System;

namespace Microsoft.VisualStudio.Services.Agent
{
    public enum WellKnownDirectory
    {
        Bin,
        Diag,
        Externals,
        LegacyPSHost,
        Root,
        ServerOM,
        Tee,
        Tasks,
        Update,
        Work,
    }

    public static class Constants
    {
        /// <summary>Path environment varible name.</summary>
#if OS_WINDOWS
        public static readonly string PathVariable = "Path";
#else
        public static readonly string PathVariable = "PATH";
#endif
        public static string SecretMask = "********";
        public static string TFBuild = "TF_BUILD";

        // This enum is embedded within the Constants class to make it easier to reference and avoid
        // ambiguous type reference with System.Runtime.InteropServices.OSPlatform.
        public enum OSPlatform
        {
            OSX,
            Linux,
            Windows
        }

        public static class Agent
        {
            public static readonly string Version = "2.114.0";

#if OS_LINUX
            public static readonly OSPlatform Platform = OSPlatform.Linux;
#elif OS_OSX
            public static readonly OSPlatform Platform = OSPlatform.OSX;
#elif OS_WINDOWS
            public static readonly OSPlatform Platform = OSPlatform.Windows;
#endif
            public static readonly TimeSpan ExitOnUnloadTimeout = TimeSpan.FromSeconds(30);

            public static class CommandLine
            {
                public static class Args
                {
                    public static readonly string Agent = "agent";
                    public static readonly string Auth = "auth";
                    public static readonly string NotificationPipeName = "notificationpipename";
                    public static readonly string NotificationSocketAddress = "notificationsocketaddress";
                    public static readonly string Pool = "pool";
                    public static readonly string Url = "url";
                    public static readonly string UserName = "username";
                    public static readonly string WindowsLogonAccount = "windowslogonaccount";
                    public static readonly string Work = "work";
                    public static readonly string MachineGroupName = "machinegroupname";
                    public static readonly string ProjectName = "projectname";
                    public static readonly string CollectionName = "collectionname";
                    public static readonly string MachineGroupTags = "machinegrouptags";

                    // Secret args. Must be added to the "Secrets" getter as well.
                    public static readonly string Password = "password";
                    public static readonly string Token = "token";
                    public static readonly string WindowsLogonPassword = "windowslogonpassword";
                    public static string[] Secrets => new[]
                    {
                        Password,
                        Token,
                        WindowsLogonPassword,
                    };
                }

                public static class Commands
                {
                    public static readonly string Configure = "configure";
                    public static readonly string Run = "run";
                    public static readonly string Unconfigure = "remove";
                }

                public static class Flags
                {
                    public static readonly string AcceptTeeEula = "acceptteeeula";
                    public static readonly string AddMachineGroupTags = "addmachinegrouptags";
                    public static readonly string Commit = "commit";
                    public static readonly string Help = "help";
                    public static readonly string Replace = "replace";
                    public static readonly string RunAsService = "runasservice";
                    public static readonly string Unattended = "unattended";
                    public static readonly string Version = "version";
                    public static readonly string MachineGroup = "machinegroup";
                    public static readonly string DeploymentGroup = "deploymentgroup";
                }
            }

            public static class ReturnCode
            {
                public const int Success = 0;
                public const int TerminatedError = 1;
                public const int RetryableError = 2;
                public const int AgentUpdating = 3;
            }

            public static class AgentConfigurationProvider
            {
                public static readonly string BuildReleasesAgentConfiguration = "BuildReleasesAgentConfiguration";
                public static readonly string DeploymentAgentConfiguration = "DeploymentAgentConfiguration";
            }
        }

        public static class Build
        {
            public static readonly string NoCICheckInComment = "***NO_CI***";

            public static class Path
            {
                public static readonly string ArtifactsDirectory = "a";
                public static readonly string BinariesDirectory = "b";
                public static readonly string GarbageCollectionDirectory = "GC";
                public static readonly string LegacyArtifactsDirectory = "artifacts";
                public static readonly string LegacyStagingDirectory = "staging";
                public static readonly string SourceRootMappingDirectory = "SourceRootMapping";
                public static readonly string SourcesDirectory = "s";
                public static readonly string TestResultsDirectory = "TestResults";
                public static readonly string TopLevelTrackingConfigFile = "Mappings.json";
                public static readonly string TrackingConfigFile = "SourceFolder.json";
            }
        }

        public static class Configuration
        {
            public static readonly string PAT = "PAT";
            public static readonly string Alternate = "ALT";
            public static readonly string Negotiate = "Negotiate";
            public static readonly string Integrated = "Integrated";
            public static readonly string OAuth = "OAuth";
            public static readonly string ServiceIdentity = "ServiceIdentity";
        }

        public static class EndpointData
        {
            public static readonly string SourcesDirectory = "SourcesDirectory";
            public static readonly string SourceVersion = "SourceVersion";
            public static readonly string SourceBranch = "SourceBranch";
            public static readonly string SourceTfvcShelveset = "SourceTfvcShelveset";
            public static readonly string GatedShelvesetName = "GatedShelvesetName";
            public static readonly string GatedRunCI = "GatedRunCI";
        }

        public static class Expressions
        {
            public static readonly string Always = "always";
            public static readonly string Canceled = "canceled";
            public static readonly string Failed = "failed";
            public static readonly string Succeeded = "succeeded";
            public static readonly string SucceededOrFailed = "succeededOrFailed";
            public static readonly string Variables = "variables";
        }

        public static class Path
        {
            public static readonly string BinDirectory = "bin";
            public static readonly string DiagDirectory = "_diag";
            public static readonly string ExternalsDirectory = "externals";
            public static readonly string LegacyPSHostDirectory = "vstshost";
            public static readonly string ServerOMDirectory = "vstsom";
            public static readonly string TempDirectory = "_temp";
            public static readonly string TeeDirectory = "tee";
            public static readonly string ToolDirectory = "_tool";
            public static readonly string TaskJsonFile = "task.json";
            public static readonly string TasksDirectory = "_tasks";
            public static readonly string UpdateDirectory = "_update";
            public static readonly string WorkDirectory = "_work";
        }

        public static class Release
        {
            public static readonly string Map = "Map";

            public static class Path
            {
                public static readonly string ArtifactsDirectory = "a";
                public static readonly string DefinitionMapping = "DefinitionMapping.json";
                public static readonly string ReleaseDirectoryPrefix = "r";
                public static readonly string RootMappingDirectory = "ReleaseRootMapping";
            }
        }

        // Related to definition variables.
        public static class Variables
        {
            public static readonly string MacroPrefix = "$(";
            public static readonly string MacroSuffix = ")";

            public static class Agent
            {
                //
                // Keep alphabetical
                //
                public static readonly string BuildDirectory = "agent.builddirectory";
                public static readonly string HomeDirectory = "agent.homedirectory";
                public static readonly string Id = "agent.id";
                public static readonly string JobName = "agent.jobname";
                public static readonly string JobStatus = "agent.jobstatus";
                public static readonly string MachineName = "agent.machinename";
                public static readonly string Name = "agent.name";
                public static readonly string OS = "agent.os";
                public static readonly string OSVersion = "agent.osversion";
                public static readonly string ProxyUrl = "agent.proxyurl";
                public static readonly string ProxyUsername = "agent.proxyusername";
                public static readonly string ProxyPassword = "agent.proxypassword";
                public static readonly string RootDirectory = "agent.RootDirectory";
                public static readonly string ServerOMDirectory = "agent.ServerOMDirectory";
                public static readonly string ToolCache = "agent.ToolCache";
                public static readonly string Version = "agent.version";
                public static readonly string WorkFolder = "agent.workfolder";
                public static readonly string WorkingDirectory = "agent.WorkingDirectory";
            }

            public static class Build
            {
                //
                // Keep alphabetical
                //
                public static readonly string ArtifactStagingDirectory = "build.artifactstagingdirectory";
                public static readonly string BinariesDirectory = "build.binariesdirectory";
                public static readonly string Clean = "build.clean";
                public static readonly string DefinitionName = "build.definitionname";
                public static readonly string GatedRunCI = "build.gated.runci";
                public static readonly string GatedShelvesetName = "build.gated.shelvesetname";
                public static readonly string RepoClean = "build.repository.clean";
                public static readonly string RepoGitSubmoduleCheckout = "build.repository.git.submodulecheckout";
                public static readonly string RepoId = "build.repository.id";
                public static readonly string RepoLocalPath = "build.repository.localpath";
                public static readonly string RepoName = "build.Repository.name";
                public static readonly string RepoProvider = "build.repository.provider";
                public static readonly string RepoTfvcWorkspace = "build.repository.tfvc.workspace";
                public static readonly string RepoUri = "build.repository.uri";
                public static readonly string SourceBranch = "build.sourcebranch";
                public static readonly string SourceTfvcShelveset = "build.sourcetfvcshelveset";
                public static readonly string SourceVersion = "build.sourceversion";
                public static readonly string SourcesDirectory = "build.sourcesdirectory";
                public static readonly string StagingDirectory = "build.stagingdirectory";
                public static readonly string SyncSources = "build.syncSources";
            }

            public static class Common
            {
                public static readonly string TestResultsDirectory = "common.testresultsdirectory";
            }

            public static class Features
            {
                //
                // Keep alphabetical
                //
                public static readonly string BuildDirectoryClean = "agent.clean.buildDirectory";
                public static readonly string GitLfsSupport = "agent.source.git.lfs";
                public static readonly string GitShallowDepth = "agent.source.git.shallowFetchDepth";
                public static readonly string SkipSyncSource = "agent.source.skip";
            }

            public static class Release
            {
                //
                // Keep alphabetical
                //
                public static readonly string AgentReleaseDirectory = "agent.releaseDirectory";
                public static readonly string ArtifactsDirectory = "system.artifactsDirectory";
                public static readonly string AttemptNumber = "release.attemptNumber";
                public static readonly string ReleaseDefinitionName = "release.definitionName";
                public static readonly string ReleaseEnvironmentName = "release.environmentName";
                public static readonly string ReleaseEnvironmentUri = "release.environmentUri";
                public static readonly string ReleaseDefinitionId = "release.definitionId";
                public static readonly string ReleaseDescription = "release.releaseDescription";
                public static readonly string ReleaseId = "release.releaseId";
                public static readonly string ReleaseName = "release.releaseName";
                public static readonly string ReleaseRequestedForId = "release.requestedForId";
                public static readonly string ReleaseUri = "release.releaseUri";
                public static readonly string ReleaseWebUrl = "release.releaseWebUrl";
                public static readonly string RequestorId = "release.requestedFor";
                public static readonly string SkipArtifactsDownload = "release.skipartifactsDownload";
            }

            public static class System
            {
                //
                // Keep alphabetical
                //
                public static readonly string AccessToken = "system.accessToken";
                public static readonly string ArtifactsDirectory = "system.artifactsdirectory";
                public static readonly string CollectionId = "system.collectionid";
                public static readonly string Culture = "system.culture";
                public static readonly string Debug = "system.debug";
                public static readonly string DefaultWorkingDirectory = "system.defaultworkingdirectory";
                public static readonly string DefinitionId = "system.definitionid";
                public static readonly string EnableAccessToken = "system.enableAccessToken";
                public static readonly string HostType = "system.hosttype";
                public static readonly string PreferGitFromPath = "system.prefergitfrompath";
                public static readonly string SelfManageGitCreds = "system.selfmanagegitcreds";
                public static readonly string TFServerUrl = "system.TeamFoundationServerUri"; // back compat variable, do not document
                public static readonly string TeamProject = "system.teamproject";
                public static readonly string TeamProjectId = "system.teamProjectId";
                public static readonly string WorkFolder = "system.workfolder";
            }

            public static class Task
            {
                public static readonly string DisplayName = "task.displayname";
            }
        }
    }
}
