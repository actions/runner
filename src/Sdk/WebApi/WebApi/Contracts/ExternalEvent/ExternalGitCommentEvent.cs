using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.ExternalEvent
{
    [DataContract]
    public class ExternalGitComment : IExternalArtifact, IAdditionalProperties
    {
        /// <summary>
        /// Current resource version.
        /// </summary>
        [IgnoreDataMember]
        public static readonly ApiResourceVersion CurrentVersion = new ApiResourceVersion(new Version(1, 0), 1);

        /// <summary>
        /// Body of the comment that was made on the commit
        /// </summary>
        [DataMember]
        public string CommentBody { get; set; }

        /// <summary>
        /// Timestamp of when the comment was last updated. We need to check to make sure that the comment happened after the most recent update to a PR
        /// </summary>
        [DataMember]
        public string UpdatedAt { get; set; }

        /// <summary>
        /// Id of the entire comment.
        /// </summary>
        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// Property bag.  Subscription publisher inputs are copied here.
        /// </summary>
        [DataMember]
        public IDictionary<string, string> Properties { get; set; }

        /// <summary>
        /// User who made the comment and is responsible for kicking off the build process
        /// </summary>
        [DataMember]
        public ExternalGitUser CommentedBy { get; set; }

        /// <summary>
        /// Git repository of this push.
        /// </summary>
        [DataMember]
        public ExternalGitRepo Repo { get; set; }

        /// <summary>
        /// Bucket for storing external data source related properties
        /// </summary>
        [DataMember]
        public IDictionary<string, object> AdditionalProperties { get; set; }
    }
}
