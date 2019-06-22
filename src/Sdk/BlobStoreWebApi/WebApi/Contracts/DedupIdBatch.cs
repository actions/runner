using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.BlobStore.WebApi
{
    /// <summary>
    /// An array of node items. Each node item contains a node ID.
    /// </summary>
    [DataContract]
    public class DedupIdBatch
    {
        [DataMember(EmitDefaultValue = false, Name = "dedupIds")]
        public ISet<string> DedupIds { get; set; }
    }
}
