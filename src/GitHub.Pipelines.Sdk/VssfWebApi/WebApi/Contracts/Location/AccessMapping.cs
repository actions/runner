using GitHub.Services.Common;
using GitHub.Services.WebApi;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;

namespace GitHub.Services.Location
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class AccessMapping : ISecuredObject
    {
        public AccessMapping() { }

        public AccessMapping(String moniker, String displayName, String accessPoint, Guid serviceOwner = new Guid())
            :this (moniker, displayName, accessPoint, serviceOwner, null)
        {
        }

        public AccessMapping(String moniker, String displayName, String accessPoint, Guid serviceOwner, String virtualDirectory)
        {
            DisplayName = displayName;
            Moniker = moniker;
            AccessPoint = accessPoint;
            ServiceOwner = serviceOwner;
            VirtualDirectory = virtualDirectory;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Moniker
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String AccessPoint
        {
            get;
            set;
        }

        /// <summary>
        /// The service which owns this access mapping e.g. TFS, ELS, etc.
        /// </summary>
        [DataMember]
        public Guid ServiceOwner
        {
            get;
            set;
        }

        /// <summary>
        /// Part of the access mapping which applies context after the access point
        /// of the server.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String VirtualDirectory
        {
            get;
            set;
        }

        public AccessMapping Clone()
        {
            return new AccessMapping(Moniker, DisplayName, AccessPoint, ServiceOwner, VirtualDirectory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal static AccessMapping FromXml(IServiceProvider serviceProvider, XmlReader reader)
        {
            AccessMapping obj = new AccessMapping();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            Boolean empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "AccessPoint":
                            obj.AccessPoint = reader.Value;
                            break;
                        case "DisplayName":
                            obj.DisplayName = reader.Value;
                            break;
                        case "Moniker":
                            obj.Moniker = reader.Value;
                            break;
                        default:
                            // Allow attributes such as xsi:type to fall through
                            break;
                    }
                }
            }

            // Process the fields in Xml elements
            reader.Read();
            if (!empty)
            {
                while (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        default:
                            // Make sure that we ignore XML node trees we do not understand
                            reader.ReadOuterXml();
                            break;
                    }
                }
                reader.ReadEndElement();
            }
            return obj;
        }

        #region ISecuredObject
        Guid ISecuredObject.NamespaceId => LocationSecurityConstants.NamespaceId;

        int ISecuredObject.RequiredPermissions => LocationSecurityConstants.Read;

        string ISecuredObject.GetToken() => LocationSecurityConstants.NamespaceRootToken;
        #endregion
    }
}
