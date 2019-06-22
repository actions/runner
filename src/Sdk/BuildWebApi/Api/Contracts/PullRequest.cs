using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a pull request object.  These are retrieved from Source Providers.
    /// </summary>
    [DataContract]
    public class PullRequest: BaseSecuredObject
    {
        public PullRequest()
        {
            this.Links = new ReferenceLinks();
        }

        internal PullRequest(
            ISecuredObject securedObject)
            : base(securedObject)
        {
            this.Links = new ReferenceLinks();
        }

        /// <summary>
        ///  The name of the provider this pull request is associated with.
        /// </summary>
        [DataMember]
        public String ProviderName { get; set; }

        /// <summary>
        /// Unique identifier for the pull request
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Id { get; set; }

        /// <summary>
        /// Title of the pull request.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Title { get; set; }

        /// <summary>
        /// Description for the pull request.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description { get; set; }

        /// <summary>
        /// Current state of the pull request, e.g. open, merged, closed, conflicts, etc.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String CurrentState { get; set; }

        /// <summary>
        /// Author of the pull request.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef Author { get; set; }

        /// <summary>
        /// Owner of the source repository of this pull request
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String SourceRepositoryOwner { get; set; }

        /// <summary>
        /// Source branch ref of this pull request
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String SourceBranchRef { get; set; }

        /// <summary>
        /// Owner of the target repository of this pull request
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String TargetRepositoryOwner { get; set; }

        /// <summary>
        /// Target branch ref of this pull request
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String TargetBranchRef { get; set; }

        /// <summary>
        /// The links to other objects related to this object.
        /// </summary>
        [DataMember(Name = "_links", EmitDefaultValue = false)]
        public ReferenceLinks Links { get; set; }
    }
}
