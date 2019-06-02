using System;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism for controlling validation behaviors.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BuildOptions
    {
        public static BuildOptions None { get; } = new BuildOptions();

        /// <summary>
        /// Gets or sets a value indicating whether or not a queue target without a queue should be considered an 
        /// error.
        /// </summary>
        public Boolean AllowEmptyQueueTarget
        {
            get;
            set;
        }

        /// <summary>
        /// Allow hyphens in names checked by the NameValidator. Used for yaml workflow schema
        /// </summary>
        public Boolean AllowHyphenNames
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to demand the latest agent version.
        /// </summary>
        public Boolean DemandLatestAgent
        {
            get;
            set;
        }

        /// <summary>
        /// If true, resource definitions are allowed to use expressions
        /// </summary>
        public Boolean EnableResourceExpressions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to resolve resource version.
        /// </summary>
        public Boolean ResolveResourceVersions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether input aliases defined in a task definition are honored.
        /// </summary>
        public Boolean ResolveTaskInputAliases
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the individual step demands should be rolled up into their
        /// parent phase's demands. Settings this value to true will result in Phase's demand sets being a superset
        /// of their children's demands.
        /// </summary>
        public Boolean RollupStepDemands
        {
            get;
            set;
        }

        /// <summary>
        /// If true, all expressions must be resolvable given a provided context. 
        /// This is normally going to be false for plan compile time and true for plan runtime.
        /// </summary>
        public Boolean ValidateExpressions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to validate resource existence and other constraints.
        /// </summary>
        public Boolean ValidateResources
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not step names provided by the caller should be validated for 
        /// correctness and uniqueness. Setting this value to false will automatically fix invalid step names and
        /// de-duplicate step names which may lead to unexpected behavior at runtime when binding output variables.
        /// </summary>
        public Boolean ValidateStepNames
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to run input validation defined by the task author.
        /// </summary>
        public Boolean ValidateTaskInputs
        {
            get;
            set;
        }
    }
}
