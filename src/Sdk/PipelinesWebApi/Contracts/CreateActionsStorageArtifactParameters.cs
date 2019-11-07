using System.Runtime.Serialization;

namespace GitHub.Actions.Pipelines.WebApi
{
    [DataContract]
    public class CreateActionsStorageArtifactParameters : CreateArtifactParameters
    {
        public CreateActionsStorageArtifactParameters()
            : base(ArtifactType.Actions_Storage)
        {
        }

        /// <summary>
        /// the id of the file container 
        /// </summary>
        [DataMember]
        public long ContainerId
        {
            get;
            set;
        }

        /// <summary>
        /// path of the artifact in the container
        /// </summary>
        [DataMember]
        public string Path
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
    }
}
