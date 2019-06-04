using System;
using System.Collections.Generic;

namespace GitHub.Services.Commerce
{
    /// <summary>
    /// The body of an extension resource PUT request.
    /// </summary>
    public class ExtensionResourceRequest
    {
        public ExtensionResourceRequest()
        {
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The Azure region of the Visual Studio account associated with this request (i.e 'southcentralus'.)
        /// </summary>
        public string Location { get; set; }
        /// <summary>
        /// A dictionary of user-defined tags to be stored with the extension resource.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }
        /// <summary>
        /// A dictionary of extended properties. This property is currently unused.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
        /// <summary>
        /// Extended information about the plan being purchased for this extension resource.
        /// </summary>
        public ExtensionResourcePlan Plan { get; set; }
    }
}
