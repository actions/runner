using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.TokenSigningKeyLifecycle
{
    /// <summary>
    /// Represents a keynamespace publishing rules associated with creation and usage of signingkeys generated in the namespace.
    /// </summary>
    public class TokenSigningKeyNamespace
    {
        /// <summary>
        /// Unique namespace.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Min pool size of SigningKeys in namespace active for signing and validation
        /// </summary>
        public int SigningKeyBatchSize { get; set; }
        /// <summary>
        /// Signing lifetime of keys in days.
        /// </summary>
        public int SigningLifetimeInDays { get; set; }
        /// <summary>
        /// Validation lifetime of keys in days.
        /// </summary>
        public int ValidationLifetimeInDays { get; set; }
        /// <summary>
        /// List of SigningKeyIds valid for signing
        /// </summary>
        public IReadOnlyList<int> SigningKeyIds { get; set; }
        /// <summary>
        /// Dictionary of SigningKeyIds and associated key metadata valid for validation.
        /// SigningKeyIds will be a subset of ValidationKeys.
        /// </summary>
        public IReadOnlyDictionary<int, TokenSigningKeyMetadata> ValidationKeys { get; set; }
    }
}
