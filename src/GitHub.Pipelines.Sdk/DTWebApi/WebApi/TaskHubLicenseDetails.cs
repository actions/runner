using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TaskHubLicenseDetails
    {
        public static readonly Int32 DefaultFreeLicenseCount = 0;

        [DataMember(Name = "FreeLicenseCount")]
        public Int32 FreePrivateLicenseCount;

        [DataMember]
        public Int32 FreeHostedLicenseCount;

        [DataMember]
        public Int32 EnterpriseUsersCount;

        /// <summary>
        /// Self-hosted licenses purchased from VSTS directly.
        /// </summary>
        [DataMember(Name = "PurchasedLicenseCount")]
        public Int32 PurchasedPrivateLicenseCount;

        /// <summary>
        /// Microsoft-hosted licenses purchased from VSTS directly.
        /// </summary>
        [DataMember]
        public Int32 PurchasedHostedLicenseCount;

        [DataMember]
        public Boolean HostedLicensesArePremium;

        /// <summary>
        /// Microsoft-hosted licenses purchased from secondary marketplaces.
        /// </summary>
        public List<MarketplacePurchasedLicense> MarketplacePurchasedHostedLicenses
        {
            get
            {
                if (m_marketplacePurchasedHostedLicenses == null)
                {
                    m_marketplacePurchasedHostedLicenses = new List<MarketplacePurchasedLicense>();
                }
                return m_marketplacePurchasedHostedLicenses;
            }
        }

        [DataMember]
        public Int32 TotalLicenseCount;

        [DataMember]
        public Boolean HasLicenseCountEverUpdated;

        [DataMember]
        public Int32 MsdnUsersCount;

        [DataMember]
        public Int32 HostedAgentMinutesFreeCount;

        [DataMember]
        public Int32 HostedAgentMinutesUsedCount;

        [DataMember]
        public Boolean FailedToReachAllProviders;

        [DataMember]
        public Int32 TotalPrivateLicenseCount;

        [DataMember]
        public Int32 TotalHostedLicenseCount;

        [DataMember(Name = "MarketplacePurchasedHostedLicenses", EmitDefaultValue = false)]
        private List<MarketplacePurchasedLicense> m_marketplacePurchasedHostedLicenses;
    }
}
