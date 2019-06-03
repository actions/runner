using System;
using GitHub.Services.Licensing;

namespace GitHub.Services.Compliance
{
    [Obsolete("This type is no longer used.")]
    public class AccountRightsValidation
    {
        public AccountRightsValidation()
        {
        }

        public AccountRightsValidation(ComplianceValidation validation, VisualStudioOnlineServiceLevel accountRights)
            : this(validation, accountRights, string.Empty)
        {
        }

        public AccountRightsValidation(ComplianceValidation validation, VisualStudioOnlineServiceLevel accountRights, string accountRightsReason)
            : this(validation, accountRights, accountRightsReason, default(AccountEntitlement))
        {
        }

        public AccountRightsValidation(
            ComplianceValidation validation, 
            VisualStudioOnlineServiceLevel accountRights, 
            string accountRightsReason, 
            AccountEntitlement accountEntitlement)
        {
            this.AccountRights = accountRights;
            this.ComplianceValidation = validation;
            this.AccountRightsReason = accountRightsReason;
            this.AccountEntitlement = accountEntitlement;
        }

        public AccountEntitlement AccountEntitlement { get; set; }

        public VisualStudioOnlineServiceLevel AccountRights { get; set; }

        public string AccountRightsReason { get; set; }

        public ComplianceValidation ComplianceValidation { get; set; }

        /// <summary>
        /// Flag to mark the state as unfit for long caching or persisting.
        /// </summary>
        public bool Volatile { get; set; }
    }
}
