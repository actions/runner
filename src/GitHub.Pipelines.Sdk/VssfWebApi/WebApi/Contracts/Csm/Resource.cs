using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Commerce
{
    /// <summary>
    /// A generic Azure Resource Manager resource.
    /// </summary>
    public class Resource : IEquatable<Resource>
    {
        public Resource(string id, string name, string type)
        {
            this.id = id;
            this.name = name;
            this.type = type;
        }

        /// <summary>
        /// Unique identifier of the resource.
        /// </summary>
        public string id { get; protected set; }

        /// <summary>
        /// Resource name.
        /// </summary>
        public string name { get; protected set; }

        /// <summary>
        /// Resource type.
        /// </summary>
        public string type { get; protected set; }

        /// <summary>
        /// Resource location.
        /// </summary>
        public string location { get; set; }

        /// <summary>
        /// Resource tags.
        /// </summary>
        public Dictionary<string, string> tags { get; set; } = new Dictionary<string, string>();

        bool IEquatable<Resource>.Equals(Resource other)
        {
            return id == other.id
                && name == other.name
                && type == other.type
                && location == other.location
                && tags.Except(other.tags).Count() == 0
                && other.tags.Except(tags).Count() == 0;
        }
    }
}
