using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Security
{
    [DataContract]
    public sealed class ActionDefinition
    {
        public ActionDefinition()
        {
        }

        /// <summary>
        /// Creates an ActionDefinition for a SecurityNamespaceDescription.  This overload
        /// should only be used for reading from the database.
        /// </summary>
        /// <param name="namespaceId">The namespace that this action belongs to.</param>
        /// <param name="bit">The bit that this action maps to.</param>
        /// <param name="name">The non-localized name for this action.</param>
        /// <param name="displayName">The localized display name for this action.</param>
        public ActionDefinition(
            Guid namespaceId, 
            Int32 bit, 
            String name, 
            String displayName)
        {
            Bit = bit;
            Name = name;
            DisplayName = displayName;
            NamespaceId = namespaceId;
        }

        /// <summary>
        /// The bit mask integer for this action. Must be a power of 2.
        /// </summary>
        [DataMember]
        public Int32 Bit { get; set; }

        /// <summary>
        /// The non-localized name for this action.
        /// </summary>
        [DataMember]
        public String Name { get; set; }

        /// <summary>
        /// The localized display name for this action.
        /// </summary>
        [DataMember]
        public String DisplayName { get; set; }

        /// <summary>
        /// The namespace that this action belongs to.  This will only be used for reading from the database.
        /// </summary>
        [DataMember]
        public Guid NamespaceId { get; set; }
    }
}
