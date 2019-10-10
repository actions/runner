using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a build log.
    /// </summary>
    [DataContract]
    public class BuildLog : BuildLogReference
    {
        public BuildLog()
        {
        }

        public BuildLog(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The number of lines in the log.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int64 LineCount
        {
            get;
            set;
        }

        /// <summary>
        /// The date and time the log was created.
        /// </summary>
        [DataMember]
        public DateTime? CreatedOn
        {
            get;
            set;
        }

        /// <summary>
        /// The date and time the log was last changed.
        /// </summary>
        [DataMember]
        public DateTime? LastChangedOn
        {
            get;
            set;
        }
    }
}
