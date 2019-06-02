using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Profile
{
    /// <summary>
    /// Container of country/region information
    /// </summary>
    [DataContract]
    [ClientIncludeModel]
    public class ProfileRegions
    {
        /// <summary>
        /// List of country/regions
        /// </summary>
        [DataMember]
        public IList<ProfileRegion> Regions;

        /// <summary>
        /// List of country/region code with contact consent requirement type of notice
        /// </summary>
        [DataMember]
        public IList<string> NoticeContactConsentRequirementRegions { get; set; }

        /// <summary>
        /// List of country/region code with contact consent requirement type of opt-out
        /// </summary>
        [DataMember]
        public IList<string> OptOutContactConsentRequirementRegions { get; set; }
    }
}
