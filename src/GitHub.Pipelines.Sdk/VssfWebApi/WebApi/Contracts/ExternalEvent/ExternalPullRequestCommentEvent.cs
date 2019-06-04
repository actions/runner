using System;
using System.Runtime.Serialization;

namespace GitHub.Services.ExternalEvent
{
    [DataContract]
    public class ExternalPullRequestCommentEvent : ExternalGitComment, IExternalGitEvent
    {
        /// <summary>
        /// Owner of the repository where the comment on the PR was made
        /// </summary>
        [DataMember]
        public String RepositoryOwner;

        /// <summary>
        /// Pull request number in the repository where the comment originates from
        /// </summary>
        [DataMember]
        public String PullRequestNumber;

        /// <summary>
        /// Association of the individual who created the comment
        /// </summary>
        [DataMember]
        public String AuthorAssociation;

        /// <summary>
        /// A TFS project ID
        /// </summary>
        [DataMember]
        public String ProjectId;

        /// <summary>
        /// Describes the command that was used when making the comments. Corresponds to some action that we should respond to
        /// </summary>
        [DataMember]
        public ExternalCommentEventCommand Command;

        /// <summary>
        /// The pull request that the comment was made on
        /// </summary>
        [DataMember]
        public ExternalGitPullRequest PullRequest;

        [DataMember]
        public string PipelineEventId { get; set; }
    }

    public class ExternalCommentEventCommand
    {
        /// <summary>
        /// The keyword that is used to define the command
        /// </summary>
        [DataMember]
        public String CommandKeyword;

        /// <summary>
        /// Remaining comment body after the command keyword
        /// </summary>
        [DataMember]
        public String RemainingParameters;

    }
}
