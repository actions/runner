using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Services.Graph.Client
{
    /// <summary>
    /// Storage key of a Graph entity
    /// </summary>
    [DataContract]
    public class GraphStorageKeyResult
    {
        [DataMember]
        public Guid Value { get; }

        /// <summary>
        /// This field contains zero or more interesting links about the graph storage key. 
        /// These links may be invoked to obtain additional relationships or more detailed 
        /// information about this graph storage key.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "_links")]
        public ReferenceLinks Links { get; private set; }

        [JsonConstructor]
        public GraphStorageKeyResult(Guid value, ReferenceLinks links)
        {
            Value = value;
            Links = links;
        }
    }
}
