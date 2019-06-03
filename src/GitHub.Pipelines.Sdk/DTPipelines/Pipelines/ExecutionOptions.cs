using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism for controlling runtime behaviors.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ExecutionOptions
    {
        public ExecutionOptions()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to remove secrets from job message.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean RestrictSecrets
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating what scope the system jwt token will have.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String SystemTokenScope
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating what's the max number jobs we allow after expansion.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32? MaxJobExpansion
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating the max parallelism slots available to overwrite MaxConcurrency of test job slicing 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32? MaxParallelism
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating if we should allow expressions to define secured resources. 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean EnableResourceExpressions
        {
            get;
            set;
        }

        /// <summary>
        /// Driven by FF: DistributedTask.LegalNodeNames
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean EnforceLegalNodeNames
        {
            get;
            set;
        }

        /// <summary>
        /// Allows hyphens in yaml names
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean AllowHyphenNames
        {
            get;
            set;
        }
    }
}
