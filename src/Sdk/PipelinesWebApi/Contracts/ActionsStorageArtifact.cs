using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Actions.Pipelines.WebApi
{
    [DataContract]
    public class ActionsStorageArtifact : Artifact
    {
        public ActionsStorageArtifact()
            : base(ArtifactType.Actions_Storage)
        {
        }

        /// <summary>
        /// File Container ID
        /// </summary>
        [DataMember]
        public long ContainerId 
        {
            get;
            set;
        }

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        [DataMember]
        public long Size
        {
            get;
            set;
        }

        /// <summary>
        /// Signed content url for downloading the artifact 
        /// </summary>
        [DataMember]
        public SignedUrl SignedContent
        {
            get;
            set;
        }
    }
}
