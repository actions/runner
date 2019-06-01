using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Licensing;

namespace Microsoft.VisualStudio.Services.GroupLicensingRule
{
    /// <summary>
    /// Represents an GroupLicensingRuleUpdate Model
    /// </summary>
    [DataContract]
    public class GroupLicensingRuleUpdate
    {
        /// <summary>
        /// SubjectDescriptor for the rule
        /// </summary>
        [DataMember]
        public SubjectDescriptor SubjectDescriptor { get; set; }

        /// <summary>
        /// New License
        /// </summary>
        [DataMember]
        public License License { get; set; }

        /// <summary>
        /// Extensions to Add
        /// </summary>
        [DataMember]
        public IEnumerable<string> ExtensionsToAdd { get; set; }

        /// <summary>
        /// Extensions to Remove
        /// </summary>
        [DataMember]
        public IEnumerable<string> ExtensionsToRemove { get; set; }
    }
}
