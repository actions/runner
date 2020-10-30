using System;

namespace GitHub.Runner.Common
{
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
        MigratedCredentials,
        RSACredentials,
        Service,
        CredentialStore,
        Certificates,
        Options,
        SetupInfo,
    }

    public static class Constants
    {
        /// <summary>Path environment variable name.</summary>
#if OS_WINDOWS
        public static readonly string PathVariable = "Path";
#else
        public static readonly string PathVariable = "PATH";
#endif

        public static string ProcessTrackingId = "RUNNER_TRACKING_ID";
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
                    public static readonly string Auth = "auth";
                    public static readonly string Labels = "labels";
                    public static readonly string MonitorSocketAddress = "monitorsocketaddress";
                    public static readonly string Name = "name";
                    public static readonly string RunnerGroup = "runnergroup";
                    public static readonly string StartupType = "startuptype";
                    public static readonly string Url = "url";
                    public static readonly string UserName = "username";
                    public static readonly string WindowsLogonAccount = "windowslogonaccount";
                    public static readonly string Work = "work";

                    // Secret args. Must be added to the "Secrets" getter as well.
                    public static readonly string Token = "token";
                    public static readonly string WindowsLogonPassword = "windowslogonpassword";
                    public static string[] Secrets => new[]
                    {
                        Token,
                        WindowsLogonPassword,
                    };
                }

                public static class Commands
                {
                    public static readonly string Configure = "configure";
                    public static readonly string Remove = "remove";
                    public static readonly string Run = "run";
                    public static readonly string Warmup = "warmup";
                }

                //if you are adding a new flag, please make sure you update the
                //validFlags array as well present in the CommandSettings.cs
                public static class Flags
                {
                    public static readonly string Commit = "commit";
                    public static readonly string Help = "help";
                    public static readonly string Replace = "replace";
                    public static readonly string Once = "once";
                    public static readonly string RunAsService = "runasservice";
                    public static readonly string Unattended = "unattended";
                    public static readonly string Version = "version";
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

            public static readonly string InternalTelemetryIssueDataKey = "_internal_telemetry";
            public static readonly string WorkerCrash = "WORKER_CRASH";
            public static readonly string UnsupportedCommand = "UNSUPPORTED_COMMAND";
            public static readonly string UnsupportedCommandMessageDisabled = "The `{0}` command is disabled. Please upgrade to using Environment Files or opt into unsecure command execution by setting the `ACTIONS_ALLOW_UNSECURE_COMMANDS` environment variable to `true`. For more information see: https://github.blog/changelog/2020-10-01-github-actions-deprecating-set-env-and-add-path-commands/";
        }

        public static class RunnerEvent
        {
            public static readonly string Register = "register";
            public static readonly string Remove = "remove";
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
            public static readonly string OAuthAccessToken = "OAuthAccessToken";
            public static readonly string OAuth = "OAuth";
        }

        public static class Expressions
        {
            public static readonly string Always = "always";
            public static readonly string Cancelled = "cancelled";
            public static readonly string Failure = "failure";
            public static readonly string Success = "success";
        }

        public static class Path
        {
            public static readonly string ActionsDirectory = "_actions";
            public static readonly string ActionManifestYmlFile = "action.yml";
            public static readonly string ActionManifestYamlFile = "action.yaml";
            public static readonly string BinDirectory = "bin";
            public static readonly string DiagDirectory = "_diag";
            public static readonly string ExternalsDirectory = "externals";
            public static readonly string RunnerDiagnosticLogPrefix = "Runner_";
            public static readonly string TempDirectory = "_temp";
            public static readonly string ToolDirectory = "_tool";
            public static readonly string UpdateDirectory = "_update";
            public static readonly string WorkDirectory = "_work";
            public static readonly string WorkerDiagnosticLogPrefix = "Worker_";
        }

        // Related to definition variables.
        public static class Variables
        {
            public static readonly string MacroPrefix = "$(";
            public static readonly string MacroSuffix = ")";

            public static class Actions
            {
                //
                // Keep alphabetical
                //
                public static readonly string AllowUnsupportedCommands = "ACTIONS_ALLOW_UNSECURE_COMMANDS";
                public static readonly string RunnerDebug = "ACTIONS_RUNNER_DEBUG";
                public static readonly string StepDebug = "ACTIONS_STEP_DEBUG";
            }

            public static class Agent
            {
                public static readonly string ToolsDirectory = "agent.ToolsDirectory";
            }

            public static class System
            {
                //
                // Keep alphabetical
                //
                public static readonly string AccessToken = "system.accessToken";
                public static readonly string Culture = "system.culture";
                public static readonly string PhaseDisplayName = "system.phaseDisplayName";
            }
        }
    }
}
