namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// Plan data for an extension resource.
    /// </summary>
    public class ExtensionResourcePlan
    {
        /// <summary>
        /// Name of the plan.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Name of the extension publisher.
        /// </summary>
        public string publisher { get; set; }
        /// <summary>
        /// Product name.
        /// </summary>
        public string product { get; set; }
        /// <summary>
        /// Optional: the promotion code associated with the plan.
        /// </summary>
        public string promotionCode { get; set; }
        /// <summary>
        /// A string that uniquely identifies the plan version.
        /// </summary>
        public string version { get; set; }

        public override string ToString()
        {
            return $"Publisher: {publisher}, Product: {product}, Name: {name}";
        }
    }
}
