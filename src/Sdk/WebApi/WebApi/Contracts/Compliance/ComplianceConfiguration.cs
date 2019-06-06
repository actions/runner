using System;

namespace GitHub.Services.Compliance
{
    [Obsolete("This type is no longer used.")]
    public class ComplianceConfiguration
    {
        public ComplianceConfiguration()
        {
        }

        public ComplianceConfiguration(DateTime complianceInvalidated, TimeSpan complianceGracePeriod, TimeSpan complianceStateRevalidationInterval, string complianceStateRepositoryStrategy)
        {
            this.ComplianceGracePeriod = complianceGracePeriod;
            this.ComplianceInvalidated = new DateTimeOffset(complianceInvalidated);
            this.ComplianceStateRepositoryStrategy = complianceStateRepositoryStrategy;
            this.ComplianceStateRevalidationInterval = complianceStateRevalidationInterval;
        }

        public TimeSpan ComplianceGracePeriod { get; set; }

        public DateTimeOffset ComplianceInvalidated { get; set; }

        public string ComplianceStateRepositoryStrategy { get; set; }

        public TimeSpan ComplianceStateRevalidationInterval { get; set; }
    }
}
