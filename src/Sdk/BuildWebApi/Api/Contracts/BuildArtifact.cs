using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents an artifact produced by a build.
    /// </summary>
    [DataContract]
    public class BuildArtifact : BaseSecuredObject
    {
        public BuildArtifact()
        {
        }

        internal BuildArtifact(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The artifact ID.
        /// </summary>
        [DataMember]
        public Int32 Id
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the artifact.
        /// </summary>
        [DataMember]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The artifact source, which will be the ID of the job that produced this artifact.
        /// </summary>
        [DataMember]
        public String Source
        {
            get;
            set;
        }

        /// <summary>
        /// The actual resource.
        /// </summary>
        [DataMember]
        public ArtifactResource Resource
        {
            get;
            set;
        }
    }
}
