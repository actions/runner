using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents system-wide build settings.
    /// </summary>
    [DataContract]
    public class BuildSettings : BaseSecuredObject
    {
        public BuildSettings()
            : this(null)
        {
        }

        public BuildSettings(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The default retention policy.
        /// </summary>
        [DataMember]
        public RetentionPolicy DefaultRetentionPolicy
        {
            get;
            set;
        }

        /// <summary>
        /// The maximum retention policy.
        /// </summary>
        [DataMember]
        public RetentionPolicy MaximumRetentionPolicy
        {
            get;
            set;
        }

        /// <summary>
        /// The number of days to keep records of deleted builds.
        /// </summary>
        [DataMember]
        public Int32 DaysToKeepDeletedBuildsBeforeDestroy
        {
            get;
            set;
        }
    }
}
