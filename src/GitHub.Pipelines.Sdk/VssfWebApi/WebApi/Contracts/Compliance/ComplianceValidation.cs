using System;

namespace Microsoft.VisualStudio.Services.Compliance
{
    public class ComplianceValidation
    {
        public ComplianceValidation()
        {
        }

        public ComplianceValidation(DateTime complianceValidated)
        {
            this.ComplianceValidated = new DateTimeOffset(complianceValidated);
            this.IsValidated = true;
        }

        public ComplianceValidation(DateTimeOffset complianceValidated)
        {
            this.ComplianceValidated = complianceValidated;
        }

        public ComplianceValidation(Uri redirectUrl)
        {
            this.RedirectUrl = redirectUrl;
        }

        public DateTimeOffset ComplianceValidated { get; set; }

        public bool IsValidated { get; set; }

        public Uri RedirectUrl { get; set; }
    }
}
