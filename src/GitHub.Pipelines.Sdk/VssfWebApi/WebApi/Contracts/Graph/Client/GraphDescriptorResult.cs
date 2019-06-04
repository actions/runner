using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Services.Graph.Client
{
    /// <summary>
    /// Subject descriptor of a Graph entity
    /// </summary>
    [DataContract]
    public class GraphDescriptorResult
    {
        [DataMember]
        public SubjectDescriptor Value { get; }

        /// <summary>
        /// This field contains zero or more interesting links about the graph descriptor. These links may be invoked to obtain additional
        /// relationships or more detailed information about this graph descriptor.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "_links")]
        public ReferenceLinks Links { get; private set; }

        [JsonConstructor]
        public GraphDescriptorResult(SubjectDescriptor value, ReferenceLinks links)
        {
            Value = value;
            Links = links;
        }
    }
}
