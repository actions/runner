using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.WebApi;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides constant values for constructs used in the pipeline APIs.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class PipelineConstants
    {
        /// <summary>
        /// The minimum agent version when performing an advanced checkout. This demand
        /// is required when multiple checkout steps are used, when the checkout step
        /// is not the first step, or when any repository is checked out other than self
        /// or none.
        /// </summary>
        public static readonly String AdvancedCheckoutMinAgentVersion = "2.137.0";

        public static readonly String AgentVersionDemandName = "Runner.Version";

        public static readonly String AgentName = "Agent.Name";

        /// <summary>
        /// The default job cancel timeout in minutes.
        /// </summary>
        public static readonly Int32 DefaultJobCancelTimeoutInMinutes = 5;

        /// <summary>
        /// The default job name. This job name is used when a phase does not leverage multipliers
        /// or slicing and only has one implicit job.
        /// </summary>
        public static readonly String DefaultJobName = "__default";

        /// <summary>
        /// The default job display name. For when the user doesn't specify names for anything.
        /// </summary>
        public static readonly String DefaultJobDisplayName = "Job";

        /// <summary>
        /// The default job timeout in minutes.
        /// </summary>
        public static readonly Int32 DefaultJobTimeoutInMinutes = 360;

        /// <summary>
        /// The max length for a node within a pipeline - e.g. a stage name or a job name.
        /// </summary>
        public static readonly Int32 MaxNodeNameLength = 100;

        /// <summary>
        /// The repository alias to use for dont-sync-sources.
        /// </summary>
        public static readonly String NoneAlias = "none";

        /// <summary>
        /// Alias for the self repository.
        /// </summary>
        public static readonly String SelfAlias = "self";

        /// <summary>
        /// Alias for the repository coming from designer build definition.
        /// </summary>
        public static readonly String DesignerRepo = "__designer_repo";

        /// <summary>
        /// Error code during graph validation.
        /// </summary>
        internal const String DependencyNotFound = nameof(DependencyNotFound);

        /// <summary>
        /// Error code during graph validation.
        /// </summary>
        internal const String GraphContainsCycle = nameof(GraphContainsCycle);

        /// <summary>
        /// Error code during graph validation.
        /// </summary>
        internal const String NameInvalid = nameof(NameInvalid);

        /// <summary>
        /// Error code during graph validation.
        /// </summary>
        internal const String NameNotUnique = nameof(NameNotUnique);

        /// <summary>
        /// Error code during graph validation.
        /// </summary>
        internal const String StartingPointNotFound = nameof(StartingPointNotFound);

        internal const String CheckpointNodeInstanceNameClaimKey = "nodeInstanceName";
        internal const String CheckpointIdClaimKey = "checkpointId";

        public static class CheckoutTaskInputs
        {
            public static readonly String Repository = "repository";
            public static readonly String Ref = "ref";
            public static readonly String Version = "version";
            public static readonly String Token = "token";
            public static readonly String Clean = "clean";
            public static readonly String Submodules = "submodules";
            public static readonly String Lfs = "lfs";
            public static readonly String FetchDepth = "fetchDepth";
            public static readonly String PersistCredentials = "persistCredentials";
            public static readonly String Path = "path";
            public static readonly String WorkspaceRepo = "workspaceRepo";

            public static class SubmodulesOptions
            {
                public static readonly String Recursive = "recursive";
                public static readonly String True = "true";
            }
        }

        public static class WorkspaceCleanOptions
        {
            public static readonly String Outputs = "outputs";
            public static readonly String Resources = "resources";
            public static readonly String All = "all";
        }

        public static class EnvironmentVariables
        {
            public static readonly String EnvironmentId = "Environment.Id";
            public static readonly String EnvironmentName = "Environment.Name";
            public static readonly String EnvironmentResourceId = "Environment.ResourceId";
            public static readonly String EnvironmentResourceName = "Environment.ResourceName";
        }

        public static class ScriptStepInputs
        {
            public static readonly String Script = "script";
            public static readonly String WorkingDirectory = "workingDirectory";
            public static readonly String Shell = "shell";
        }
    }
}
