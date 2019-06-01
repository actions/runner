using System;

namespace Microsoft.VisualStudio.Services.UserLicensing
{
    public static class UserLicensingResourceIds
    {
        public const string AreaName = "UserLicensing";
        public const string AreaId = "5B508ADE-4C35-4913-A78E-6312FF28F84E";

        public const String CertificateResourceName = "Certificate";
        public static readonly Guid CertificateResourceLocationId = new Guid("0F7E6AA1-8D3F-428B-B6D2-5E52D08C343A");

        public const string ClientRightsResourceName = "ClientRights";
        public static readonly Guid ClientRightsResourceLocationid = new Guid("2CC58BFD-3B77-4DC1-B0B3-74B0775D41CB");

        public const string MsdnResourceName = "MsdnEntitlements";
        public const string MsdnEntitlementsResourceLocationIdAsString = "58DDE369-BEC9-4F13-93DE-E8DFA381293C";
        public static readonly Guid MsdnEntitlementsResourceLocationId = new Guid(MsdnEntitlementsResourceLocationIdAsString);

        // This is a temporary REST end point to migrate data from SPS to User service. Please do not make use of it.
        public const string VsTrialLicenseResourceName = "VsTrialLicense";
        public static readonly Guid VsTrialLicenseResourceLocationId = new Guid("2083F3EC-0E90-4267-8122-394A68664A6E");
    }
}
