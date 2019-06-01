using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.MarketingPreferences
{
    [DataContract]
    public class MarketingPreferences
    {
        [DataMember(EmitDefaultValue = false)]
        public bool VisualStudio { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool VisualStudioSubscriptions { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool AzureDevOps { get; set; }
    }
}
