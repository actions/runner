namespace Microsoft.VisualStudio.Services.Licensing
{
    public class AccountLicenseUsage
    {
        public AccountLicenseUsage() 
            : this(new AccountUserLicense(LicensingSource.Account, (int) AccountLicenseType.None), 0, 0) { }

        public AccountLicenseUsage(AccountUserLicense license, int provisionedCount, int usedCount)
            : this(license, provisionedCount, usedCount, 0, provisionedCount) { }

        public AccountLicenseUsage(AccountUserLicense license, int provisionedCount, int usedCount, int disabledCount, int pendingProvisionedCount)
        {
            License = license;
            ProvisionedCount = provisionedCount;
            UsedCount = usedCount;
            DisabledCount = disabledCount;
            PendingProvisionedCount = pendingProvisionedCount;
        }

        public virtual AccountUserLicense License { get; set; }

        /// <summary>
        /// Amount that has been purchased
        /// </summary>
        public int ProvisionedCount { get; set; }

        /// <summary>
        /// Amount currently being used.
        /// </summary>
        public int UsedCount { get; set; }

        /// <summary>
        /// Amount that is disabled (Usually from licenses that were provisioned, 
        /// but became invalid due to loss of subscription in a new billing cycle)
        /// </summary>
        public int DisabledCount { get; set; }

        /// <summary>
        /// Amount that will be purchased in the next billing cycle
        /// </summary>
        public int PendingProvisionedCount { get; set; }
    }
}
