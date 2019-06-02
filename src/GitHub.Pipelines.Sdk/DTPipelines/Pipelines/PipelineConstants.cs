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

        public static readonly String AgentVersionDemandName = "Agent.Version";

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
        public static readonly Int32 DefaultJobTimeoutInMinutes = 60;

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
            public static readonly String Clean = "clean";
            public static readonly String Submodules = "submodules";
            public static readonly String Lfs = "lfs";
            public static readonly String FetchDepth = "fetchDepth";
            public static readonly String PersistCredentials = "persistCredentials";
            public static readonly String Path = "path";

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

        public static readonly TaskDefinition CheckoutTask = new TaskDefinition
        {
            Id = new Guid("6d15af64-176c-496d-b583-fd2ae21d4df4"),
            Name = "Checkout",
            FriendlyName = "Get sources",
            Author = "Microsoft",
            RunsOn = { TaskRunsOnConstants.RunsOnAgent },
            Version = new TaskVersion("1.0.0"),
            Description = "Get sources from a repository. Supports Git, TfsVC, and SVN repositories.",
            HelpMarkDown = "[More Information](https://go.microsoft.com/fwlink/?LinkId=798199)",
            Inputs = {
                new TaskInputDefinition()
                {
                    Name =  CheckoutTaskInputs.Repository,
                    Required = true,
                    InputType = TaskInputType.Repository
                },
                new TaskInputDefinition()
                {
                    Name = CheckoutTaskInputs.Clean,
                    Required = false,
                    DefaultValue = Boolean.TrueString,
                    InputType = TaskInputType.Boolean
                },
                // Git
                new TaskInputDefinition()
                {
                    Name = CheckoutTaskInputs.Submodules, // True or Recursive
                    Required = false,
                    InputType = TaskInputType.String
                },
                new TaskInputDefinition()
                {
                    Name = CheckoutTaskInputs.Lfs, // Checkout lfs object
                    Required = false,
                    DefaultValue = Boolean.FalseString,
                    InputType = TaskInputType.Boolean
                },
                new TaskInputDefinition()
                {
                    Name = CheckoutTaskInputs.FetchDepth, // Enable shallow fetch
                    Required = false,
                    InputType = TaskInputType.String
                },
                new TaskInputDefinition()
                {
                    Name = CheckoutTaskInputs.PersistCredentials, // Allow script git
                    Required = false,
                    DefaultValue = Boolean.FalseString,
                    InputType = TaskInputType.Boolean
                },
            },
            Execution =
            {
                {
                    "agentPlugin",
                    JObject.FromObject(new Dictionary<String, String>(){ { "target", "Agent.Plugins.Repository.CheckoutTask, Agent.Plugins"} })
                }
            },
            PostJobExecution =
            {
                {
                    "agentPlugin",
                    JObject.FromObject(new Dictionary<String, String>(){ { "target", "Agent.Plugins.Repository.CleanupTask, Agent.Plugins"} })
                }
            }
        };

        public static class ScriptStepInputs
        {
            public static readonly String Script = "script";
            public static readonly String WorkingDirectory = "workingDirectory";
        }

        public static class AgentPlugins
        {
            public static readonly String Checkout = "checkout";
        }

        public static Boolean IsCheckoutTask(this Step step)
        {
            if (step is TaskStep task &&
                task.Reference.Id == PipelineConstants.CheckoutTask.Id &&
                task.Reference.Version == PipelineConstants.CheckoutTask.Version)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Boolean IsCheckoutAction(this Step step)
        {
            if (step is ActionStep action &&
                action.Reference is PluginReference agentPlugin &&
                agentPlugin.Plugin == PipelineConstants.AgentPlugins.Checkout)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static class ScheduleType
        {
            public static readonly String Cron = "Cron";
        }
    }
}
