using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Security
{
    /// <summary>
    /// Class for describing the details of a TeamFoundationSecurityNamespace.
    /// </summary>
    [DataContract]
    public sealed class SecurityNamespaceDescription
    {
        public SecurityNamespaceDescription()
        {
        }

        /// <summary>
        /// Creates a SecurityNamespaceDescription which can be used to create a 
        /// SecurityNamespace
        /// </summary>
        /// <param name="namespaceId">
        ///     The id that uniquely identifies the SecurityNamespace.
        /// </param>
        /// <param name="name">
        ///     The non-localized name for the SecurityNamespace that will be used for things
        ///     like the command-line.
        /// </param>
        /// <param name="displayName">
        ///     The localized display name for the SecurityNamespace.
        /// </param>
        /// <param name="dataspaceCategory">
        ///     This is the dataspace category that describes where the security information
        ///     for this SecurityNamespace should be stored.
        /// </param>
        /// <param name="separatorValue">
        ///     If the security tokens this namespace will be operating on need to be split 
        ///     on certain characters to determine its elements that character should be 
        ///     specified here. If not, this value must be the null character.
        /// </param>
        /// <param name="elementLength">
        ///     If the security tokens this namespace will be operating on need to be split 
        ///     on certain character lengths to determine its elements, that length should be
        ///     specified here. If not, this value must be -1.</param>
        /// <param name="structure">
        ///     The structure that this SecurityNamespace will use to organize its tokens.
        ///     If this namespace is hierarchical, either the separatorValue or the 
        ///     elementLength parameter must have a non-default value.
        /// </param>
        /// <param name="writePermission">
        ///     The permission bits needed by a user in order to modify security data in this
        ///     SecurityNamespace.
        /// </param>
        /// <param name="readPermission">
        ///     The permission bits needed by a user in order to read security data in this
        ///     SecurityNamespace.
        /// </param>
        /// <param name="actions">
        ///     The list of actions that this SecurityNamespace is responsible for securing.
        /// </param>
        /// <param name="extensionType">
        ///     The full type of the class that implements ISecurityNamespaceExtension that
        ///     will act as a security namespace extension for this security namespace. Null
        ///     or String.Empty if this security namespace will not have a security namespace
        ///     extension.
        /// </param>
        /// <param name="isRemotable">
        ///     If true, the security namespace is remotable, allowing another service to proxy
        ///     the namespace.
        /// </param>
        /// <param name="useTokenTranslator">
        ///     If true, the security service will expect an ISecurityDataspaceTokenTranslator plugin to
        ///     exist for this namespace
        /// </param>
        /// <param name="systemBitMask">
        ///     The bits reserved by system store 
        /// </param>
        public SecurityNamespaceDescription(
           Guid namespaceId,
           String name,
           String displayName,
           String dataspaceCategory,
           Char separatorValue,
           Int32 elementLength,
           Int32 structure,
           Int32 writePermission,
           Int32 readPermission,
           List<ActionDefinition> actions,
           String extensionType,
           Boolean isRemotable,
           Boolean useTokenTranslator,
           Int32 systemBitMask) 
            : this(namespaceId, name, displayName, dataspaceCategory, separatorValue, elementLength, structure, writePermission, readPermission, actions, extensionType, isRemotable, useTokenTranslator)
        {
            SystemBitMask = systemBitMask;
        }

        public SecurityNamespaceDescription(
            Guid namespaceId,
            String name,
            String displayName,
            String dataspaceCategory,
            Char separatorValue,
            Int32 elementLength,
            Int32 structure,
            Int32 writePermission,
            Int32 readPermission,
            List<ActionDefinition> actions,
            String extensionType,
            Boolean isRemotable,
            Boolean useTokenTranslator)
        {
            NamespaceId = namespaceId;
            Name = name;
            DisplayName = displayName;
            SeparatorValue = separatorValue;
            ElementLength = elementLength;
            StructureValue = structure;
            ReadPermission = readPermission;
            WritePermission = writePermission;
            DataspaceCategory = dataspaceCategory;
            ExtensionType = extensionType;
            Actions = actions;
            IsRemotable = isRemotable;
            UseTokenTranslator = useTokenTranslator;
        }

        /// <summary>
        /// The unique identifier for this namespace.
        /// </summary>
        [DataMember]
        public Guid NamespaceId { get; set; }

        /// <summary>
        /// This non-localized for this namespace.
        /// </summary>
        [DataMember]
        public String Name { get; set; }

        /// <summary>
        /// This localized name for this namespace.
        /// </summary>
        [DataMember]
        public String DisplayName { get; set; }

        /// <summary>
        /// If the security tokens this namespace will be operating on need to be split 
        /// on certain characters to determine its elements that character should be 
        /// specified here. If not, this value will be the null character.
        /// </summary>
        [DataMember]
        public Char SeparatorValue { get; set; }

        /// <summary>
        /// If the security tokens this namespace will be operating on need to be split 
        /// on certain character lengths to determine its elements, that length should be
        /// specified here. If not, this value will be -1.
        /// </summary>
        [DataMember]
        public Int32 ElementLength { get; set; }

        /// <summary>
        /// The permission bits needed by a user in order to modify security data on the
        /// Security Namespace.
        /// </summary>
        [DataMember]
        public Int32 WritePermission { get; set; }

        /// <summary>
        /// The permission bits needed by a user in order to read security data on the
        /// Security Namespace.
        /// </summary>
        [DataMember]
        public Int32 ReadPermission { get; set; }

        /// <summary>
        /// This is the dataspace category that describes where the security information
        /// for this SecurityNamespace should be stored.
        /// </summary>
        [DataMember]
        public String DataspaceCategory { get; set; }

        /// <summary>
        /// The list of actions that this Security Namespace is responsible for securing.
        /// </summary>
        [DataMember]
        public List<ActionDefinition> Actions { get; set; }

        /// <summary>
        /// Used to send information about the structure of the security namespace over the web service.
        /// </summary>
        [DataMember]
        public Int32 StructureValue { get; set; }

        /// <summary>
        /// This is the type of the extension that should be loaded from the plugins
        /// directory for extending this security namespace.
        /// </summary>
        [DataMember]
        public String ExtensionType { get; set; }

        /// <summary>
        /// If true, the security namespace is remotable, allowing another service to proxy
        /// the namespace.
        /// </summary>
        [DataMember]
        public bool IsRemotable { get; set; }

        /// <summary>
        /// If true, the security service will expect an ISecurityDataspaceTokenTranslator plugin to
        /// exist for this namespace
        /// </summary>
        [DataMember]
        public bool UseTokenTranslator { get; set; }

        /// <summary>
        /// The bits reserved by system store
        /// </summary>
        [DataMember]
        public Int32 SystemBitMask { get; set; }
    }
}
