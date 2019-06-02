using Microsoft.VisualStudio.Services.WebApi;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Profile
{
    [DataContract]
    [ClientIncludeModel]
    public class GeoRegion
    {
        [DataMember]
        public string RegionCode { get; set; }
    }
}
