using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.ExternalEvent
{
    [DataContract]
    public class ExternalGitPush : IExternalGitEvent
    {
        /// <summary>
        /// Current resource version.
        /// </summary>
        [IgnoreDataMember]
        public static ApiResourceVersion CurrentVersion = new ApiResourceVersion(new Version(1, 0), 1);

        /// <summary>
        /// Identifer of the push on the external system. External-system specific.
        /// </summary>
        [DataMember]
        public String Id;

        /// <summary>
        /// Full name of the ref that was pushed to. For example: refs/heads/master.
        /// </summary>
        [DataMember(Name = "ref")]
        public String GitRef;

        /// <summary>
        /// SHA of the branch prior to the push.
        /// </summary>
        [DataMember]
        public String BeforeSha;

        /// <summary>
        /// SHA of the branch after the push.
        /// </summary>
        [DataMember]
        public String AfterSha;

        /// <summary>
        /// Commits pushed.
        /// </summary>
        [DataMember]
        public IList<ExternalGitCommit> Commits;

        /// <summary>
        /// Git repository of this push.
        /// </summary>
        [DataMember]
        public ExternalGitRepo Repo;

        /// <summary>
        /// User that pushed.
        /// </summary>
        [DataMember]
        public ExternalGitUser PushedBy;

        /// <summary>
        /// A TFS project ID -- TODO find a better way of sending this info to build
        /// Also used for identifying the Azure DevOps project associated with a GitHub check rerun event.
        /// </summary>
        [DataMember]
        public String ProjectId;

        /// <summary>
        /// Property bag.  Subscription publisher inputs are copied here.
        /// </summary>
        [DataMember]
        public IDictionary<String, String> Properties;

        /// <summary>
        /// Describes the definition that should be built when a GitHub check rerun is requested if we can't find the build by build id.
        /// </summary>
        [DataMember]
        public String DefinitionToBuild;

        /// <summary>
        /// Describes the build that should be retried when a GitHub check rerun is requested.
        /// </summary>
        [DataMember]
        public String BuildToRetry;

        [DataMember]
        public String PipelineEventId { get; set; }
    }

    public static class ExternalGitPushConstants
    {
        public const string NoCiMessage = "***NO_CI***";
        /// Currently this list is being checked in the SimulateCodePushAction class to catch CI events using OAuth or PAT connections and in the
        ///   GitHubSourceProvider to catch CI events coming from the GitHub app. If other source providers are moved to using an app connection, work 
        ///   will need to be done to ensure that commits using these keywords are skipped.
        public static IReadOnlyList<string> SkipCICheckInKeywords => new List<string>
        {
            "***no_ci***",
            "[skip ci]",
            "[ci skip]",
            "skip-checks: true",
            "skip-checks:true",
            "[skip azp]",
            "[azp skip]",
            "[skip azpipelines]",
            "[azpipelines skip]",
            "[skip azurepipelines]",
            "[azurepipelines skip]"
        };
        public const string HasNoCiPropertyId = "HasNoCi";
        public const string HasCommitsPropertyId = "HasCommits";
    }
}
