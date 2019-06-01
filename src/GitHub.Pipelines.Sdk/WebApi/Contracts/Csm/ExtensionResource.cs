using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// The response to an extension resource GET request.
    /// </summary>
    public class ExtensionResource : Resource
    {
        public ExtensionResource(string id, string name, string type) : base(id, name, type)
        {
        }

        /// <summary>
        /// Resource properties.
        /// </summary>
        public Dictionary<string, string> properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The extension plan that was purchased.
        /// </summary>
        public ExtensionResourcePlan plan { get; set; }
    }
}
