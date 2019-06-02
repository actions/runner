namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// Operation types for account resource PUT request.
    /// </summary>
    public enum AccountResourceRequestOperationType
    {
        /// <summary>
        /// The operation is unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// A new account will be created.
        /// </summary>
        Create,

        /// <summary>
        /// An existing account will be updated.
        /// </summary>
        Update,

        /// <summary>
        /// Links an existing account to an Azure subscription.
        /// </summary>
        Link
    }
}
