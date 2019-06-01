using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Graph.Client
{
    [DataContract]
    public class GraphCachePolicies
    {
        /// <summary>
        /// Size of the cache
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int CacheSize { get; set; }
    }
}
