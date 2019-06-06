namespace GitHub.Services.Commerce
{
    /// <summary>
    /// The response to an extension resource list GET request.
    /// </summary>
    public class ExtensionResourceListResult
    {
        /// <summary>
        /// Array of extension resource details.
        /// </summary>
        public ExtensionResource[] Value { get; set; }
    }
}
