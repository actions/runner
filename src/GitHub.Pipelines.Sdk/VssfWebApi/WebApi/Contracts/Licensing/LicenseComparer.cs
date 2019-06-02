using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Licensing
{
    public class LicenseComparer : IComparer<License>
    {
        public int Compare(License x, License y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            // both licenses have to have a source of Account or Msdn to compare weights
            if((x.Source == LicensingSource.Account || x.Source == LicensingSource.Msdn)
                && (y.Source == LicensingSource.Account || y.Source == LicensingSource.Msdn))
            {
                var thisLicenseWeight = GetWeight(x);
                var otherLicenseWeight = GetWeight(y);

                return thisLicenseWeight.CompareTo(otherLicenseWeight);
            }
            
            // Not a known source. Just do a license value compare.
            return x.GetLicenseAsInt32().CompareTo(y.GetLicenseAsInt32());
        }

        public int GetWeight(License license)
        {
            if (license == License.None)
            {
                return 0;
            }
            else if (license == AccountLicense.Stakeholder)
            {
                return 1;
            }
            else if (license == AccountLicense.Express)
            {
                return 2;
            }
            else if (license == AccountLicense.Professional)
            {
                return 3;
            }
            else if (license == MsdnLicense.Eligible)
            {
                return 4;
            }
            else if (license == MsdnLicense.Professional)
            {
                return 5;
            }
            else if (license == AccountLicense.Advanced)
            {
                return 6;
            }
            else if (license == MsdnLicense.TestProfessional)
            {
                return 7;
            }
            else if (license == MsdnLicense.Platforms)
            {
                return 8;
            }
            else if (license == MsdnLicense.Premium)
            {
                return 9;
            }
            else if (license == MsdnLicense.Ultimate)
            {
                return 10;
            }
            else if (license == MsdnLicense.Enterprise)
            {
                return 11;
            }
            else if (license == AccountLicense.EarlyAdopter)
            {
                return 12;
            }

            return 0; // Unexpected license
        }

        public static LicenseComparer Instance { get; } = new LicenseComparer();
    }
}