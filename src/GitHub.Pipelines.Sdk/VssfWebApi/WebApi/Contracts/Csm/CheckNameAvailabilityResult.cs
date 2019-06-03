namespace GitHub.Services.Commerce
{
    /// <summary>
    /// The response to a name availability request.
    /// </summary>
    public class CheckNameAvailabilityResult
    {
        /// <summary>
        /// The value which indicates whether the provided name is available.
        /// </summary>
        public bool NameAvailable { get; set; }
        /// <summary>
        /// The message describing the detailed reason.
        /// </summary>
        public string Message { get; set; }
    }
}
