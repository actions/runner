using System;

namespace GitHub.Runner.Common
{
    public enum RunMode
    {
        Normal, // Keep "Normal" first (default value).
        Local,
    }

    public enum WellKnownDirectory
    {
        Bin,
        Diag,
        Externals,
        Root,
        Actions,
        Temp,
        Tools,
        Update,
        Work,
    }

    public enum WellKnownConfigFile
    {
        Runner,
        Credentials,
        RSACredentials,
        Service,
        CredentialStore,
        Certificates,
        Proxy,
        ProxyCredentials,
        ProxyBypass,
        Options,
    }

    public static class Constants
    {
        /// <summary>Path environment variable name.</summary>
#if OS_WINDOWS
        public static readonly string PathVariable = "Path";
#else
        public static readonly string PathVariable = "PATH";
#endif

        public static string ProcessLookupId = "GITHUB_PROCESS_LOOKUP_ID";
        public static string PluginTracePrefix = "##[plugin.trace]";
        public static readonly int RunnerDownloadRetryMaxAttempts = 3;

        // This enum is embedded within the Constants class to make it easier to reference and avoid
        // ambiguous type reference with System.Runtime.InteropServices.OSPlatform and System.Runtime.InteropServices.Architecture
        public enum OSPlatform
        {
            OSX,
            Linux,
            Windows
        }

        public enum Architecture
        {
            X86,
            X64,
            Arm,
            Arm64
        }

        public static class Runner
        {
#if OS_LINUX
            public static readonly OSPlatform Platform = OSPlatform.Linux;
#elif OS_OSX
            public static readonly OSPlatform Platform = OSPlatform.OSX;
#elif OS_WINDOWS
            public static readonly OSPlatform Platform = OSPlatform.Windows;
#endif

#if X86
            public static readonly Architecture PlatformArchitecture = Architecture.X86;
#elif X64
            public static readonly Architecture PlatformArchitecture = Architecture.X64;
#elif ARM
            public static readonly Architecture PlatformArchitecture = Architecture.Arm;
#elif ARM64            
            public static readonly Architecture PlatformArchitecture = Architecture.Arm64;
#endif

            public static readonly TimeSpan ExitOnUnloadTimeout = TimeSpan.FromSeconds(30);

            public static class CommandLine
            {
                //if you are adding a new arg, please make sure you update the
                //validArgs array as well present in the CommandSettings.cs
                public static class Args
                {
                    public static readonly string Agent = "agent";
                    public static readonly string Auth = "auth";
                    public static readonly string CollectionName = "collectionname";
                    public static readonly string DeploymentGroupName = "deploymentgroupname";
                    public static readonly string DeploymentPoolName = "deploymentpoolname";
                    public static readonly string DeploymentGroupTags = "deploymentgrouptags";
                    public static readonly string MachineGroupName = "machinegroupname";
                    public static readonly string MachineGroupTags = "machinegrouptags";
                    public static readonly string Matrix = "matrix";
                    public static readonly string MonitorSocketAddress = "monitorsocketaddress";
                    public static readonly string NotificationPipeName = "notificationpipename";
                    public static readonly string NotificationSocketAddress = "notificationsocketaddress";
                    public static readonly string Pool = "pool";
                    public static readonly string ProjectName = "projectname";
                    public static readonly string ProxyUrl = "proxyurl";
                    public static readonly string ProxyUserName = "proxyusername";
                    public static readonly string SslCACert = "sslcacert";
                    public static readonly string SslClientCert = "sslclientcert";
                    public static readonly string SslClientCertKey = "sslclientcertkey";
                    public static readonly string SslClientCertArchive = "sslclientcertarchive";
                    public static readonly string SslClientCertPassword = "sslclientcertpassword";
                    public static readonly string StartupType = "startuptype";
                    public static readonly string Url = "url";
                    public static readonly string UserName = "username";
                    public static readonly string WindowsLogonAccount = "windowslogonaccount";
                    public static readonly string Work = "work";
                    public static readonly string Yml = "yml";

                    // Secret args. Must be added to the "Secrets" getter as well.
                    public static readonly string Password = "password";
                    public static readonly string ProxyPassword = "proxypassword";
                    public static readonly string Token = "token";
                    public static readonly string WindowsLogonPassword = "windowslogonpassword";
                    public static string[] Secrets => new[]
                    {
                        Password,
                        ProxyPassword,
                        SslClientCertPassword,
                        Token,
                        WindowsLogonPassword,
                    };
                }

                public static class Commands
                {
                    public static readonly string Configure = "configure";
                    public static readonly string LocalRun = "localRun";
                    public static readonly string Remove = "remove";
                    public static readonly string Run = "run";
                    public static readonly string Warmup = "warmup";
                }

                //if you are adding a new flag, please make sure you update the
                //validFlags array as well present in the CommandSettings.cs
                public static class Flags
                {
                    public static readonly string AcceptTeeEula = "acceptteeeula";
                    public static readonly string AddDeploymentGroupTags = "adddeploymentgrouptags";
                    public static readonly string AddMachineGroupTags = "addmachinegrouptags";
                    public static readonly string Commit = "commit";
                    public static readonly string DeploymentGroup = "deploymentgroup";
                    public static readonly string DeploymentPool = "deploymentpool";
                    public static readonly string OverwriteAutoLogon = "overwriteautologon";
                    public static readonly string GitUseSChannel = "gituseschannel";
                    public static readonly string Help = "help";
                    public static readonly string MachineGroup = "machinegroup";
                    public static readonly string Replace = "replace";
                    public static readonly string NoRestart = "norestart";
                    public static readonly string LaunchBrowser = "launchbrowser";
                    public static readonly string Once = "once";
                    public static readonly string RunAsAutoLogon = "runasautologon";
                    public static readonly string RunAsService = "runasservice";
                    public static readonly string SslSkipCertValidation = "sslskipcertvalidation";
                    public static readonly string Unattended = "unattended";
                    public static readonly string Version = "version";
                    public static readonly string WhatIf = "whatif";
                }
            }

            public static class ReturnCode
            {
                public const int Success = 0;
                public const int TerminatedError = 1;
                public const int RetryableError = 2;
                public const int RunnerUpdating = 3;
                public const int RunOnceRunnerUpdating = 4;
            }
        }

        public static class Pipeline
        {
            public static class Path
            {
                public static readonly string PipelineMappingDirectory = "_PipelineMapping";
                public static readonly string TrackingConfigFile = "PipelineFolder.json";
            }
        }

        public static class Configuration
        {
            public static readonly string AAD = "AAD";
            public static readonly string PAT = "PAT";
            public static readonly string Alternate = "ALT";
            public static readonly string Negotiate = "Negotiate";
            public static readonly string Integrated = "Integrated";
            public static readonly string OAuth = "OAuth";
            public static readonly string ServiceIdentity = "ServiceIdentity";
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
            public static readonly string ActionManifestFile = "action.yml";
            public static readonly string ActionsDirectory = "_actions";
            public static readonly string UpdateDirectory = "_update";
            public static readonly string WorkDirectory = "_work";
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
                public static readonly string AcceptTeeEula = "agent.acceptteeeula";
                public static readonly string AllowAllEndpoints = "agent.allowAllEndpoints"; // remove after sprint 120 or so.
                public static readonly string AllowAllSecureFiles = "agent.allowAllSecureFiles"; // remove after sprint 121 or so.
                public static readonly string BuildDirectory = "agent.builddirectory";
                public static readonly string ContainerId = "agent.containerid";
                public static readonly string ContainerNetwork = "agent.containernetwork";
                public static readonly string Diagnostic = "agent.diagnostic";
                public static readonly string HomeDirectory = "agent.homedirectory";
                public static readonly string Id = "agent.id";
                public static readonly string GitUseSChannel = "agent.gituseschannel";
                public static readonly string JobName = "agent.jobname";
                public static readonly string JobStatus = "agent.jobstatus";
                public static readonly string MachineName = "agent.machinename";
                public static readonly string Name = "agent.name";
                public static readonly string OS = "agent.os";
                public static readonly string OSArchitecture = "agent.osarchitecture";
                public static readonly string OSVersion = "agent.osversion";
                public static readonly string ProxyUrl = "agent.proxyurl";
                public static readonly string ProxyUsername = "agent.proxyusername";
                public static readonly string ProxyPassword = "agent.proxypassword";
                public static readonly string ProxyBypassList = "agent.proxybypasslist";
                public static readonly string RetainDefaultEncoding = "agent.retainDefaultEncoding";
                public static readonly string RootDirectory = "agent.RootDirectory";
                public static readonly string RunMode = "agent.runmode";
                public static readonly string ServerOMDirectory = "agent.ServerOMDirectory";
                public static readonly string ServicePortPrefix = "agent.services";
                public static readonly string SslCAInfo = "agent.cainfo";
                public static readonly string SslClientCert = "agent.clientcert";
                public static readonly string SslClientCertKey = "agent.clientcertkey";
                public static readonly string SslClientCertArchive = "agent.clientcertarchive";
                public static readonly string SslClientCertPassword = "agent.clientcertpassword";
                public static readonly string SslSkipCertValidation = "agent.skipcertvalidation";
                public static readonly string TempDirectory = "agent.TempDirectory";
                public static readonly string ToolsDirectory = "agent.ToolsDirectory";
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
                public static readonly string Number = "build.buildNumber";
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
                public static readonly string PhaseDisplayName = "system.phaseDisplayName";
                public static readonly string PreferGitFromPath = "system.prefergitfrompath";
                public static readonly string PullRequestTargetBranchName = "system.pullrequest.targetbranch";
                public static readonly string SelfManageGitCreds = "system.selfmanagegitcreds";
                public static readonly string ServerType = "system.servertype";
                public static readonly string TFServerUrl = "system.TeamFoundationServerUri"; // back compat variable, do not document
                public static readonly string TeamProject = "system.teamproject";
                public static readonly string TeamProjectId = "system.teamProjectId";
                public static readonly string WorkFolder = "system.workfolder";
            }
        }
    }
}
