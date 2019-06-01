using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Licensing
{
    /// <summary>
    /// Model for updating an AccountEntitlement for a user, used for the Web API
    /// </summary>
    [JsonObject]
    public class AccountEntitlementUpdateModel
    {
        /// <summary>
        /// Gets or sets the license for the entitlement
        /// </summary>
        public License License { get; set; }
    }
}
