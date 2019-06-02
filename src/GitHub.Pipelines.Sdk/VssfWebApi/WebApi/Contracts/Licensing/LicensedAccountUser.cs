using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Licensing
{
    public class LicensedAccountUser
    {
        public LicensedAccountUser(Guid accountId, Guid userId)
        {
            AccountId = accountId;
            UserId = userId;
            AvailableLicenses = new List<AccountUserLicense>();
        }

        public LicensedAccountUser(Guid accountId, Guid userId, List<AccountUserLicense> availableRights)
        {
            AccountId = accountId;
            UserId = userId;
            AvailableLicenses = availableRights;
        }

        public Guid AccountId { get; set; }
        public Guid UserId { get; set; }
        public List<AccountUserLicense> AvailableLicenses { get; set; }
    }
}
