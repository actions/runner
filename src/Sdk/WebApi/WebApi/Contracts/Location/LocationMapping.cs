using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using GitHub.Services.WebApi;
using GitHub.Services.Common;

namespace GitHub.Services.Location
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class LocationMapping : ISecuredObject
    {
        public LocationMapping() { }

        public LocationMapping(String accessMappingMoniker, String location)
        {
            AccessMappingMoniker = accessMappingMoniker;
            Location = location;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [XmlAttribute("accessMappingMoniker")] // needed for servicing serialization
        public String AccessMappingMoniker
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        [XmlAttribute("location")] // needed for servicing serialization
        public String Location
        {
            get;
            set;
        }

        public override string ToString() => Location;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal static LocationMapping FromXml(IServiceProvider serviceProvider, XmlReader reader)
        {
            LocationMapping obj = new LocationMapping();
            Debug.Assert(reader.NodeType == XmlNodeType.Element, "Expected a node.");

            Boolean empty = reader.IsEmptyElement;

            // Process the xml attributes
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    switch (reader.Name)
                    {
                        case "accessMappingMoniker":
                            obj.AccessMappingMoniker = reader.Value;
                            break;
                        case "location":
                            obj.Location = reader.Value;
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

        string ISecuredObject.GetToken()
        {
            return LocationSecurityConstants.ServiceDefinitionsToken;
        }
        #endregion

        public LocationMapping Clone()
            => new LocationMapping(AccessMappingMoniker, Location);
    }
}
