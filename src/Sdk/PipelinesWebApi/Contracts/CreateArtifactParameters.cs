using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.Actions.Pipelines.WebApi
{
    [DataContract]
    [KnownType(typeof(CreateActionsStorageArtifactParameters))]
    [JsonConverter(typeof(CreateArtifactParametersJsonConverter))]
    public class CreateArtifactParameters
    {
        protected CreateArtifactParameters(ArtifactType type)
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
    }
}
