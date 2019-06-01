using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Commerce
{
    public class CsmSubscriptionResource : Resource
    {
        public CsmSubscriptionResource(string id, string name, string type) : base(id, name, type)
        {
        }

        /// <summary>
        /// Resource properties.
        /// </summary>
        public Dictionary<string, string> properties { get; set; }
    }
}
