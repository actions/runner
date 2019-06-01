namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// The body of a POST request to check name availability.
    /// </summary>
    public class CheckNameAvailabilityParameter
    {
        /// <summary>
        /// The type of resource to check availability for.
        /// </summary>
        public string ResourceType { get; set; }
        /// <summary>
        /// The name of the resource to check availability for.
        /// </summary>
        public string ResourceName { get; set; }
    }
}
