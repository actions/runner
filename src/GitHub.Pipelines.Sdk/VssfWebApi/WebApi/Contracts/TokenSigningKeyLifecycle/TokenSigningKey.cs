namespace GitHub.Services.TokenSigningKeyLifecycle
{
    /// <summary>
    /// Represents a SigningKey object containing Key Id, pre-registered namespace of key and secretKeyData.
    /// </summary>
    public class TokenSigningKey
    {
        public int KeyId { get; set; }
        /// <summary>
        ///Registered key namespace
        /// </summary>
        public string SigningKeyNamespace { get; set; }
        /// <summary>
        /// Signingkey secret from store
        /// </summary>
        public string KeyData { get; set; }
    }
}
