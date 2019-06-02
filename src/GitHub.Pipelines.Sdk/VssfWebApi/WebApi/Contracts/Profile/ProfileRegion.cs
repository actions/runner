using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Profile
{
    /// <summary>
    /// Country/region information
    /// </summary>
    [DataContract]
    public class ProfileRegion
    {
        /// <summary>
        /// Localized country/region name
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// The two-letter code defined in ISO 3166 for the country/region.
        /// </summary>
        [DataMember]
        public string Code { get; set; }
    }
}
