using System;

namespace GitHub.Services.TokenSigningKeyLifecycle
{
    /// <summary>
    /// Represents a container that store metadata about signing key
    /// </summary>
    public class TokenSigningKeyMetadata
    {
        public int KeyId { get; set; }
        /// <summary>
        /// Creation DatetimeOffset of the Key
        /// </summary>
        public DateTimeOffset ValidFrom { get; set; }
        /// <summary>
        /// DatetimeOffset indicating expiry of key for signing purposes.
        /// </summary>
        public DateTimeOffset SigningValidTo { get; set; }
        /// <summary>
        /// DatetimeOffset indicating expiry of key for validation purposes.
        /// </summary>
        public DateTimeOffset ValidationValidTo { get; set; }
    }
}
