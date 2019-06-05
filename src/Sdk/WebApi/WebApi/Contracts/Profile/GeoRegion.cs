using GitHub.Services.WebApi;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    [DataContract]
    [ClientIncludeModel]
    public class GeoRegion
    {
        [DataMember]
        public string RegionCode { get; set; }
    }
}
