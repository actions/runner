using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Information to check whether the OS is going to be deprecated soon
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class OSWarning
    {
        /// <summary>
        /// Gets or sets the file to check
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String FilePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the regular expression to match
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String RegularExpression
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the warning annotation message, if the regular expression matches the content of the file
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Warning
        {
            get;
            set;
        }
    }
}
