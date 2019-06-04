using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.ExternalEvent
{
    [DataContract]
    public class ExternalGitPullRequest : IExternalArtifact, IExternalGitEvent, IAdditionalProperties
    {
        /// <summary>
        /// Current resource version.
        /// </summary>
        [IgnoreDataMember]
        public static ApiResourceVersion CurrentVersion = new ApiResourceVersion(new Version(1, 0), 1);

        /// <summary>
        /// Identifer of the push or pull request on the external system. External-system specific.
        /// </summary>
        [DataMember]
        public String Id;

        /// <summary>
        /// Pull request number in a repository on the external system.
        /// </summary>
        [DataMember]
        public String Number;

        /// <summary>
        /// Pull request title.
        /// </summary>
        [DataMember]
        public String Title;

        /// <summary>
        /// The description of the pull request
        /// </summary>
        [DataMember]
        public String Description;

        /// <summary>
        /// The url for the pull request.
        /// </summary>
        [DataMember]
        public String Url;

        /// <summary>
        /// The web url for the pull request.
        /// </summary>
        [DataMember]
        public String WebUrl;

        /// <summary>
        /// Full name of the pull request merge branch. For example: refs/pull/5/merge
        /// </summary>
        [DataMember]
        public String MergeRef;

        /// <summary>
        /// Full name of the target branch. For example: refs/heads/master
        /// </summary>
        [DataMember]
        public String TargetRef;

        /// <summary>
        /// SHA of the targer branch.
        /// </summary>
        [DataMember]
        public String TargetSha;

        /// <summary>
        /// Full name of the source branch. For example: refs/heads/features/myfeature
        /// </summary>
        [DataMember]
        public String SourceRef;

        /// <summary>
        /// SHA of the source branch.
        /// </summary>
        [DataMember]
        public String SourceSha;

        /// <summary>
        /// Indicates whether the pull request is coming from a fork.
        /// </summary>
        [DataMember]
        public Boolean IsFork;

        /// <summary>
        /// Indicates whether the pull request is "closed" (merged or abandoned) or "open".
        /// </summary>
        [DataMember]
        public String State;

        /// <summary>
        /// Indicates whether the pull request is mergable, i.e.
        /// null  - the merge job has not finished yet creating the merge commit;
        /// false - the pull request has conflicts and the merge commit cannot be created;
        /// true  - the merge commit has been successfully created and the MergeCommitSha field contains a correct value.
        /// </summary>
        [DataMember]
        public Boolean? IsMergeable;

        /// <summary>
        /// SHA of the merge commit.
        /// </summary>
        [DataMember]
        public String MergeCommitSha;

        /// <summary>
        /// The time and date when the pull request was merged
        /// </summary>
        [DataMember]
        public String MergedAt;

        /// <summary>
        /// The time and date when the PR was updated
        /// 
        /// </summary>
        [DataMember]
        public String UpdatedAt;

        /// <summary>
        /// The time and date when the PR was created
        /// 
        /// </summary>
        [DataMember]
        public String CreatedAt;

        /// <summary>
        /// The time and date when the PR was closed
        /// 
        /// </summary>
        [DataMember]
        public String ClosedAt;

        /// <summary>
        /// Git repository of this push.
        /// </summary>
        [DataMember]
        public ExternalGitRepo Repo;

        /// <summary>
        /// A TFS project ID -- TODO find a better way of sending this info to build
        /// Also used for identifying the Azure DevOps project associated with a GitHub check rerun event.
        /// </summary>
        [DataMember]
        public String ProjectId;

        /// <summary>
        /// The sender who sent the pull request
        /// 
        /// GitHub Note: The webhook payload includes both sender and pusher objects. 
        /// Sender and pusher are the same user who initiated the push event, but the 
        /// sender object contains more detail.
        /// </summary>
        [DataMember]
        public ExternalGitUser Sender;

        /// <summary>
        /// Property bag.  Subscription publisher inputs are copied here.
        /// </summary>
        [DataMember]
        public IDictionary<String, String> Properties;

        /// <summary>
        /// Describes if the external git pull request originates from a comment event
        /// </summary>
        [DataMember]
        public Boolean IsFromComment;

        /// <summary>
        /// Describes the definition that should be built if the pull request originates from a comment event. If all definitions should be built, it will be "all". If IsFromComment is false, this value won't be checked and can be set to null
        /// Also used to specify the definition to build when a GitHub check rerun is requested if we can't find the build by build id.
        /// </summary>
        [DataMember]
        public String DefinitionToBuild;

        /// <summary>
        /// Describes the build that should be retried when a GitHub check rerun is requested.
        /// </summary>
        [DataMember]
        public String BuildToRetry;

        /// <summary>
        /// The last commit made to the head ref of the PR
        /// </summary>
        [DataMember]
        public ExternalGitCommit LastHeadRefCommit;

        /// <summary>
        /// The commits that make up the pull request
        /// </summary>
        [DataMember]
        public IEnumerable<ExternalGitCommit> Commits;

        [DataMember]
        public String PipelineEventId { get; set; }

        /// <summary>
        /// The assignees that are on the pull request
        /// </summary>
        [DataMember]
        public ICollection<ExternalGitUser> Assignees;

        /// <summary>
        /// Bucket for storing external data source related properties
        /// </summary>
        public IDictionary<string, object> AdditionalProperties { get; set; }

        /// <summary>
        /// Association between the individual who created the PR and the target repo
        /// Examples: COLLABORATOR, OWNER, CONTRIBUTOR, MEMBER, FIRST_TIMER, FIRST_TIME_CONTRIBUTOR, NONE
        /// </summary>
        [DataMember]
        public String AuthorAssociation;

        /// <summary>
        /// Indicates if the author of the PR has write access to the target repository
        /// </summary>
        [DataMember]
        public Boolean DoesAuthorHaveWriteAccess;
    }

    [DataContract]
    public class ExternalGitRepo : IAdditionalProperties
    {
        /// <summary>
        /// Identifer of the repo on the external system.
        /// </summary>
        [DataMember]
        public String Id;

        /// <summary>
        /// Name of the repo.
        /// </summary>
        [DataMember]
        public String Name;

        /// <summary>
        /// Clone URL of the repo. 
        /// </summary>
        [DataMember]
        public String Url;

        /// <summary>
        /// Browser-viewable URL of the repo.
        /// </summary>
        [DataMember]
        public String WebUrl;

        /// <summary>
        /// Is this repo private.
        /// </summary>
        [DataMember]
        public Boolean IsPrivate;

        /// <summary>
        /// The default branch of the repo.
        /// </summary>
        [DataMember]
        public string DefaultBranch { get; set; }

        /// <summary>
        /// Bucket for storing external data source related properties
        /// </summary>
        [DataMember]
        public IDictionary<string, object> AdditionalProperties { get; set; }
    }

    public class ExternalGitCommit : IExternalArtifact, IAdditionalProperties
    {
        /// <summary>
        /// Identifer of the commit.
        /// </summary>
        [DataMember]
        public String Sha;

        /// <summary>
        /// User-supplied commit message.
        /// </summary>
        [DataMember]
        public String Message;

        /// <summary>
        /// The date the commit was created
        /// </summary>
        [DataMember]
        public DateTime CommitedDate;

        /// <summary>
        /// The date the commit was pushed
        /// </summary>
        [DataMember]
        public DateTime? PushedDate;

        /// <summary>
        /// User that authored the commit.
        /// </summary>
        [DataMember]
        public ExternalGitUser Author;

        /// <summary>
        /// Git repository of this commit.
        /// </summary>
        [DataMember]
        public ExternalGitRepo Repo;

        /// <summary>
        /// Browser-viewable URL of the commit.
        /// </summary>
        [DataMember]
        public String WebUrl;

        /// <summary>
        /// Bucket for storing external data source related properties
        /// </summary>
        [DataMember]
        public IDictionary<string, object> AdditionalProperties { get; set; }
    }
}
