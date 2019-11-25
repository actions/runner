using System.Runtime.Serialization;
using GitHub.Actions.Pipelines.WebApi.Contracts;
using Newtonsoft.Json;

namespace GitHub.Actions.Pipelines.WebApi
{
    [DataContract]
    [KnownType(typeof(ActionsStorageArtifact))]
    [JsonConverter(typeof(ArtifactJsonConverter))]
    public class Artifact
    {
        public Artifact(ArtifactType type)
        {
            Type = type;
        }

        /// <summary>
        /// The type of the artifact.
        /// </summary>
        [DataMember]
        public ArtifactType Type
        {
            get;
        }

        /// <summary>
        /// The name of the artifact.
        /// </summary>
        [DataMember]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Self-referential url
        /// </summary>
        [DataMember]
        public string Url
        {
            get;
            set;
        }
     }
}
