using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Licensing
{
    /// <summary>
    /// Model for assigning an extension to users, used for the Web API
    /// </summary>
    [JsonObject]
    public class ExtensionAssignment
    {
        /// <summary>
        /// Gets or sets the extension ID to assign.
        /// </summary>
        public string ExtensionGalleryId { get; set; }

        /// <summary>
        /// Gets or sets the user IDs to assign the extension to.
        /// </summary>
        public IList<Guid> UserIds { get; set; }

        /// <summary>
        /// Gets or sets the licensing source.
        /// </summary>
        public LicensingSource LicensingSource { get; set; }

        /// <summary>
        /// Set to true if this a auto assignment scenario.
        /// </summary>
        public bool IsAutoAssignment { get; set; }
    }
}
