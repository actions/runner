namespace Microsoft.VisualStudio.Services.Commerce
{
    public enum CsmSubscriptionState
    {
        /// <summary>
        /// The unknown state, invalid data.
        /// </summary>
        Unknown,

        /// <summary>
        /// The subscription registered, updated or enabled back from suspended state.
        /// </summary>
        Registered,

        /// <summary>
        /// The all resource types have been unregistered.
        /// </summary>
        Unregistered,

        /// <summary>
        /// The Subscription in suspended state.
        /// </summary>
        Suspended,

        /// <summary>
        /// The subscription was deleted.
        /// </summary>
        Deleted,

        /// <summary>
        /// The subscription will be suspended.
        /// </summary>
        Warned
    }

    public enum SubscriptionSource
    {
        Normal = 0,
        EnterpriseAgreement = 1,
        Internal = 2,
        Unknown = 3,
        FreeTier = 4
    }
}
