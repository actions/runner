namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// The response to an account resource list GET request.
    /// </summary>
    public class AccountResourceListResult
    {
        /// <summary>
        /// Array of resource details.
        /// </summary>
        public AccountResource[] Value { get; set; }
    }
}
