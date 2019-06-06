using System;
using System.Collections.Generic;

namespace GitHub.Services.Licensing
{
    public class AccountUserLicenseComparer : IComparer<AccountUserLicense>
    {
        public int Compare(AccountUserLicense x, AccountUserLicense y)
        {
            Func<AccountUserLicense, License> convertAccountUserLicense = (userLicense) => userLicense.Source == LicensingSource.Account 
                ? AccountLicense.GetLicense((AccountLicenseType)userLicense.License) 
                : MsdnLicense.GetLicense((MsdnLicenseType)userLicense.License);
            return LicenseComparer.Instance.Compare(convertAccountUserLicense(x), convertAccountUserLicense(y));
        }

        public static AccountUserLicenseComparer Instance { get; } = new AccountUserLicenseComparer();
    }
}
