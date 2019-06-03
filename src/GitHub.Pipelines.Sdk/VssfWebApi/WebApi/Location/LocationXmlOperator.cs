// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using GitHub.Services.Common;
using GitHub.Services.Location;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.WebApi.Location
{
    internal class LocationXmlOperator
    {
        /// <summary>
        /// This is to be used for reading in an xml file that contains service definitions that
        /// have to be loaded during install
        /// </summary>
        /// <param name="isClientCache">True if the parser is parsing xml from a client cache</param>
        public LocationXmlOperator(Boolean isClientCache)
        {
            m_isClientCache = isClientCache;
            m_accessMappingLocationServiceUrls = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Reads the service definitions from the provided document. 
        /// For a specification of what the xml should look like, see the 
        /// corresponding Write method.
        /// </summary>
        /// <param name="document">The document to read from.</param>
        /// <returns>A list of service definitions.</returns>
        public List<ServiceDefinition> ReadServices(XmlDocument document, Dictionary<String, AccessMapping> accessMappings)
        {
            List<ServiceDefinition> definitions = new List<ServiceDefinition>();

            XmlNodeList servicesNodeList = document.SelectNodes("//" + s_services);
            if (servicesNodeList == null)
            {
                return definitions;
            }

            foreach (XmlNode servicesNode in servicesNodeList)
            {
                // Get all of the service definition nodes
                foreach (XmlNode definitionNode in servicesNode.SelectNodes("./" + s_serviceDefinition))
                {
                    ServiceDefinition definition = new ServiceDefinition();

                    // Get the service type - it must exist
                    XmlNode serviceTypeNode = definitionNode.SelectSingleNode("./" + s_serviceType);
                    LocationXmlOperator.CheckXmlNodeNullOrEmpty(serviceTypeNode, s_serviceType, definitionNode);
                    definition.ServiceType = serviceTypeNode.InnerText;

                    // Get the identifier if it exists - it must exist if this is the client cache
                    XmlNode identifierNode = definitionNode.SelectSingleNode("./" + s_identifier);
                    if (m_isClientCache)
                    {
                        LocationXmlOperator.CheckXmlNodeNullOrEmpty(identifierNode, s_identifier, definitionNode);
                    }
                    definition.Identifier = (identifierNode != null) ? XmlConvert.ToGuid(identifierNode.InnerText) : Guid.Empty;

                    // Get the display name - it must exist
                    XmlNode displayNameNode = definitionNode.SelectSingleNode("./" + s_displayName);
                    LocationXmlOperator.CheckXmlNodeNullOrEmpty(displayNameNode, s_displayName, definitionNode);
                    definition.DisplayName = displayNameNode.InnerText;

                    // Get the description if it exists
                    XmlNode descriptionNode = definitionNode.SelectSingleNode("./" + s_description);
                    definition.Description = (descriptionNode != null) ? descriptionNode.InnerText : String.Empty;

                    // Get the relativePath and the relativeTo setting
                    XmlNode relativePathNode = definitionNode.SelectSingleNode("./" + s_relativePath);
                    LocationXmlOperator.CheckXmlNodeNull(relativePathNode, s_relativePath, definitionNode);
                    definition.RelativePath = relativePathNode.InnerText;

                    // Get the relativeTo setting
                    XmlAttribute relativeToAttribute = relativePathNode.Attributes[s_relativeTo];
                    CheckXmlAttributeNullOrEmpty(relativeToAttribute, s_relativeTo, relativePathNode);
                    RelativeToSetting setting;
                    if (!RelativeToEnumCache.GetRelativeToEnums().TryGetValue(relativeToAttribute.InnerText, out setting))
                    {
                        throw new ConfigFileException(relativeToAttribute.InnerText);
                    }
                    definition.RelativeToSetting = setting;

                    // If the relativeToSetting is FullyQualified and the path is empty, set it to null
                    // to make the framework happy.
                    if (definition.RelativeToSetting == RelativeToSetting.FullyQualified && definition.RelativePath == String.Empty)
                    {
                        definition.RelativePath = null;
                    }

                    XmlNode parentServiceTypeNode = definitionNode.SelectSingleNode("./" + s_parentServiceType);
                    definition.ParentServiceType = (parentServiceTypeNode != null) ? parentServiceTypeNode.InnerText : null;
                    
                    XmlNode parentIdentifierNode = definitionNode.SelectSingleNode("./" + s_parentIdentifier);
                    definition.ParentIdentifier = (parentIdentifierNode != null) ? XmlConvert.ToGuid(parentIdentifierNode.InnerText) : Guid.Empty;

                    // Get all of the location mappings
                    definition.LocationMappings = new List<LocationMapping>();
                    if (definition.RelativeToSetting == RelativeToSetting.FullyQualified)
                    {
                        XmlNodeList mappings = definitionNode.SelectNodes(".//" + s_locationMapping);

                        foreach (XmlNode mappingNode in mappings)
                        {
                            LocationMapping locationMapping = new LocationMapping();

                            // Get the accessMapping
                            XmlNode accessMappingNode = mappingNode.SelectSingleNode("./" + s_accessMapping);
                            LocationXmlOperator.CheckXmlNodeNullOrEmpty(accessMappingNode, s_accessMapping, mappingNode);
                            locationMapping.AccessMappingMoniker = accessMappingNode.InnerText;

                            // Only process the location code if this is the client cache and there better
                            // not be a location node if this isn't a client cache.
                            XmlNode locationNode = mappingNode.SelectSingleNode("./" + s_location);
                            if (m_isClientCache)
                            {
                                CheckXmlNodeNullOrEmpty(locationNode, s_location, mappingNode);
                            }

                            locationMapping.Location = (locationNode != null) ? locationNode.InnerText : null;

                            // We will let the caller build the proper location from the proper service definitions
                            // instead of doing it here.

                            definition.LocationMappings.Add(locationMapping);
                        }
                    }

                    // Get the resourceVersion
                    XmlNode resourceVersionNode = definitionNode.SelectSingleNode("./" + s_resourceVersion);
                    definition.ResourceVersion = (resourceVersionNode != null) ? XmlConvert.ToInt32(resourceVersionNode.InnerText) : 0;

                    // Get the minVersion
                    XmlNode minVersionNode = definitionNode.SelectSingleNode("./" + s_minVersion);
                    definition.MinVersionString = (minVersionNode != null) ? minVersionNode.InnerText : null;

                    // Get the maxVersion
                    XmlNode maxVersionNode = definitionNode.SelectSingleNode("./" + s_maxVersion);
                    definition.MaxVersionString = (maxVersionNode != null) ? maxVersionNode.InnerText : null;

                    // Get the releasedVersion
                    XmlNode releasedVersionNode = definitionNode.SelectSingleNode("./" + s_releasedVersion);
                    definition.ReleasedVersionString = (releasedVersionNode != null) ? releasedVersionNode.InnerText : null;

                    definitions.Add(definition);
                }
            }

            return definitions;
        }

        public List<String> ReadCachedMisses(XmlDocument document)
        {
            List<String> cachedMisses = new List<String>();

            XmlNodeList cachedMissesNodeList = document.SelectNodes("//" + s_cachedMisses);
            if (cachedMissesNodeList == null)
            {
                return cachedMisses;
            }

            foreach (XmlNode cachedMissesNode in cachedMissesNodeList)
            {
                // Get all of the service definition nodes
                foreach (XmlNode cachedMissNode in cachedMissesNode.SelectNodes("./" + s_cachedMiss))
                {
                    cachedMisses.Add(cachedMissNode.InnerText);
                }
            }

            return cachedMisses;
        }

        /// <summary>
        /// Reads the access mappings from the provided document. 
        /// For a specification of what the xml should look like, see the 
        /// corresponding Write method.
        /// </summary>
        /// <param name="document">The document to read from.</param>
        /// <returns>A list of access mappings.</returns>
        public List<AccessMapping> ReadAccessMappings(XmlDocument document)
        {
            List<AccessMapping> accessMappings = new List<AccessMapping>();

            XmlNodeList accessMappingNodeList = document.SelectNodes("//" + s_accessMappings);
            if (accessMappingNodeList == null)
            {
                return accessMappings;
            }

            foreach (XmlNode accessMappingsNode in accessMappingNodeList)
            {
                foreach (XmlNode accessMappingNode in accessMappingsNode.SelectNodes("./" + s_accessMapping))
                {
                    AccessMapping accessMapping = new AccessMapping();

                    // Get the moniker
                    XmlNode monikerNode = accessMappingNode.SelectSingleNode("./" + s_moniker);
                    CheckXmlNodeNullOrEmpty(monikerNode, s_moniker, accessMappingNode);
                    accessMapping.Moniker = monikerNode.InnerText;

                    // Get the enabled property
                    XmlNode accessPointNode = accessMappingNode.SelectSingleNode("./" + s_accessPoint);
                    CheckXmlNodeNullOrEmpty(accessPointNode, s_accessPoint, accessMappingNode);
                    accessMapping.AccessPoint = accessPointNode.InnerText;

                    // Get the displayName property
                    XmlNode displayNameNode = accessMappingNode.SelectSingleNode("./" + s_displayName);
                    accessMapping.DisplayName = (displayNameNode != null) ? displayNameNode.InnerText : null;

                    XmlNode virtualDirectoryNode = accessMappingNode.SelectSingleNode("./" + s_virtualDirectory);
                    accessMapping.VirtualDirectory = (virtualDirectoryNode != null) ? virtualDirectoryNode.InnerText : null;

                    // If this isn't the client cache, load the location service url
                    if (!m_isClientCache)
                    {
                        XmlNode locationServiceUrlNode = accessMappingNode.SelectSingleNode("./" + s_locationServiceUrl);
                        String locationServiceUrl = (locationServiceUrlNode != null) ? locationServiceUrlNode.InnerText : String.Empty;
                        m_accessMappingLocationServiceUrls[accessMapping.Moniker] = locationServiceUrl;
                    }

                    accessMappings.Add(accessMapping);
                }
            }

            return accessMappings;
        }

        /// <summary>
        /// Reads the last change id from the provided document. 
        /// For a specification of what the xml should look like, see the 
        /// corresponding Write method.
        /// </summary>
        /// <param name="document">The document to read from.</param>
        /// <returns>The last change id.</returns>
        public Int32 ReadLastChangeId(XmlDocument document)
        {
            XmlNode lastChangeIdNode = document.SelectSingleNode("//" + s_lastChangeId);
            return (lastChangeIdNode != null) ? XmlConvert.ToInt32(lastChangeIdNode.InnerText) : -1;
        }

        public DateTime ReadCacheExpirationDate(XmlDocument document)
        {
            XmlNode cacheExpirationDateNode = document.SelectSingleNode("//" + s_cacheExpirationDate);
            return (cacheExpirationDateNode != null) ? XmlConvert.ToDateTime(cacheExpirationDateNode.InnerText, XmlDateTimeSerializationMode.Utc) : DateTime.MinValue;
        }

        public String ReadDefaultAccessMappingMoniker(XmlDocument document)
        {
            XmlNode defaultAccessMappingMonikerNode = document.SelectSingleNode("//" + s_defaultAccessMappingMoniker);
            CheckXmlNodeNullOrEmpty(defaultAccessMappingMonikerNode, s_defaultAccessMappingMoniker, document);
            return defaultAccessMappingMonikerNode.InnerText;
        }

        public String ReadVirtualDirectory(XmlDocument document)
        {
            XmlNode virtualDirectoryNode = document.SelectSingleNode("//" + s_virtualDirectory);
            CheckXmlNodeNull(virtualDirectoryNode, s_virtualDirectory, document);
            return virtualDirectoryNode.InnerText;
        }

        /// <summary>
        /// Writes the lastChangeId to the provided document in the form
        /// <LastChangeId>value</LastChangeId>
        /// </summary>
        /// <param name="documentNode">The document to write to.</param>
        /// <param name="lastChangeId">The value to write.</param>
        public void WriteLastChangeId(XmlNode documentNode, Int32 lastChangeId)
        {
            XmlNode lastChangeIdNode = documentNode.OwnerDocument.CreateNode(XmlNodeType.Element, s_lastChangeId, null);
            documentNode.AppendChild(lastChangeIdNode);
            lastChangeIdNode.InnerText = XmlConvert.ToString(lastChangeId);
        }

        public void WriteCacheExpirationDate(XmlNode documentNode, DateTime cacheExpirationDate)
        {
            XmlNode cacheExpirationDateNode = documentNode.OwnerDocument.CreateNode(XmlNodeType.Element, s_cacheExpirationDate, null);
            documentNode.AppendChild(cacheExpirationDateNode);
            cacheExpirationDateNode.InnerText = XmlConvert.ToString(cacheExpirationDate, XmlDateTimeSerializationMode.Utc);
        }

        public void WriteDefaultAccessMappingMoniker(XmlNode documentNode, String defaultAccessMappingMoniker)
        {
            XmlNode defaultAccessMappingMonikerNode = documentNode.OwnerDocument.CreateNode(XmlNodeType.Element, s_defaultAccessMappingMoniker, null);
            documentNode.AppendChild(defaultAccessMappingMonikerNode);
            defaultAccessMappingMonikerNode.InnerText = defaultAccessMappingMoniker;
        }

        public void WriteVirtualDirectory(XmlNode documentNode, String virtualDirectory)
        {
            XmlNode virtualDirectoryNode = documentNode.OwnerDocument.CreateNode(XmlNodeType.Element, s_virtualDirectory, null);
            documentNode.AppendChild(virtualDirectoryNode);
            virtualDirectoryNode.InnerText = virtualDirectory;
        }

        /// <summary>
        /// Writes the access mapping information to the provided document in the form:
        /// <AccessMappings>
        ///     <AccessMapping>
        ///         <Moniker>value</Moniker>
        ///         <Enabled>value</Enabled>
        ///         <DisplayName>value</DisplayName>
        ///         <VirtualDirectory>value</VirtualDirectory>
        ///     </AccessMapping>
        /// </AccessMappings>        
        /// </summary>
        /// <param name="documentNode">The document to write to.</param>
        /// <param name="accessMappings">The values to write.</param>
        public void WriteAccessMappings(XmlNode documentNode, IEnumerable<AccessMapping> accessMappings)
        {
            XmlDocument document = documentNode.OwnerDocument;

            XmlNode accessMappingsNode = document.CreateNode(XmlNodeType.Element, s_accessMappings, null);
            documentNode.AppendChild(accessMappingsNode);

            foreach (AccessMapping accessMapping in accessMappings)
            {
                XmlNode accessMappingNode = document.CreateNode(XmlNodeType.Element, s_accessMapping, null);
                accessMappingsNode.AppendChild(accessMappingNode);

                XmlNode monikerNode = document.CreateNode(XmlNodeType.Element, s_moniker, null);
                accessMappingNode.AppendChild(monikerNode);
                monikerNode.InnerText = accessMapping.Moniker;

                XmlNode accessPointNode = document.CreateNode(XmlNodeType.Element, s_accessPoint, null);
                accessMappingNode.AppendChild(accessPointNode);
                accessPointNode.InnerText = accessMapping.AccessPoint;

                XmlNode displayNameNode = document.CreateNode(XmlNodeType.Element, s_displayName, null);
                accessMappingNode.AppendChild(displayNameNode);
                displayNameNode.InnerText = accessMapping.DisplayName;

                if (accessMapping.VirtualDirectory != null)
                {
                    XmlNode virtualDirectoryNode = document.CreateNode(XmlNodeType.Element, s_virtualDirectory, null);
                    accessMappingNode.AppendChild(virtualDirectoryNode);
                    virtualDirectoryNode.InnerText = accessMapping.VirtualDirectory;
                }
            }
        }

        /// <summary>
        /// Writes service definition information to the provided document in the form:
        /// <Services>
        ///     <ServiceDefinition>
        ///         <ServiceType>value</ServiceType>
        ///         <Identifier>value</Identifier>
        ///         <DisplayName>value</DisplayName>
        ///         <DefaultAccessMapping>value</DefaultAccessMapping>
        ///         <RelativePath relativeTo="value">value</RelativePath>
        ///         <LocationMappings>
        ///             <LocationMapping>
        ///                 <AccessMapping>value</AccessMapping>
        ///                 <Location>value</Location>
        ///             </LocationMapping>
        ///             .
        ///             .
        ///             .
        ///         </LocationMappings>
        ///     </ServiceDefinition>
        ///     .
        ///     .
        ///     .
        /// </Services>
        /// </summary>
        /// <param name="documentNode">The document to write to.</param>
        /// <param name="serviceDefinitions">The values to write</param>
        public void WriteServices(XmlNode documentNode, IEnumerable<ServiceDefinition> serviceDefinitions)
        {
            XmlDocument document = documentNode.OwnerDocument;

            XmlNode servicesNode = document.CreateNode(XmlNodeType.Element, s_services, null);
            documentNode.AppendChild(servicesNode);

            foreach (ServiceDefinition definition in serviceDefinitions)
            {
                XmlNode serviceDefinitionNode = document.CreateNode(XmlNodeType.Element, s_serviceDefinition, null);
                servicesNode.AppendChild(serviceDefinitionNode);

                XmlNode serviceTypeNode = document.CreateNode(XmlNodeType.Element, s_serviceType, null);
                serviceDefinitionNode.AppendChild(serviceTypeNode);
                serviceTypeNode.InnerText = definition.ServiceType;

                XmlNode identifierNode = document.CreateNode(XmlNodeType.Element, s_identifier, null);
                serviceDefinitionNode.AppendChild(identifierNode);
                identifierNode.InnerText = XmlConvert.ToString(definition.Identifier);

                if (definition.DisplayName != null)
                {
                    XmlNode displayNameNode = document.CreateNode(XmlNodeType.Element, s_displayName, null);
                    serviceDefinitionNode.AppendChild(displayNameNode);
                    displayNameNode.InnerText = definition.DisplayName;
                }

                if (definition.Description != null)
                {
                    XmlNode descriptionNode = document.CreateNode(XmlNodeType.Element, s_description, null);
                    serviceDefinitionNode.AppendChild(descriptionNode);
                    descriptionNode.InnerText = definition.Description;
                }

                XmlNode relativePathNode = document.CreateNode(XmlNodeType.Element, s_relativePath, null);
                serviceDefinitionNode.AppendChild(relativePathNode);
                relativePathNode.InnerText = definition.RelativePath;

                XmlUtility.AddXmlAttribute(relativePathNode, s_relativeTo, definition.RelativeToSetting.ToString());

                XmlNode parentServiceTypeNode = document.CreateNode(XmlNodeType.Element, s_parentServiceType, null);
                serviceDefinitionNode.AppendChild(parentServiceTypeNode);
                parentServiceTypeNode.InnerText = definition.ParentServiceType;

                XmlNode parentIdentifierNode = document.CreateNode(XmlNodeType.Element, s_parentIdentifier, null);
                serviceDefinitionNode.AppendChild(parentIdentifierNode);
                parentIdentifierNode.InnerText = XmlConvert.ToString(definition.ParentIdentifier);

                if (definition.RelativeToSetting == RelativeToSetting.FullyQualified)
                {
                    XmlNode locationMappingsNode = document.CreateNode(XmlNodeType.Element, s_locationMappings, null);
                    serviceDefinitionNode.AppendChild(locationMappingsNode);

                    foreach (LocationMapping mapping in definition.LocationMappings)
                    {
                        XmlNode locationMappingNode = document.CreateNode(XmlNodeType.Element, s_locationMapping, null);
                        locationMappingsNode.AppendChild(locationMappingNode);

                        XmlNode accessMappingNode = document.CreateNode(XmlNodeType.Element, s_accessMapping, null);
                        locationMappingNode.AppendChild(accessMappingNode);
                        accessMappingNode.InnerText = mapping.AccessMappingMoniker;

                        XmlNode locationNode = document.CreateNode(XmlNodeType.Element, s_location, null);
                        locationMappingNode.AppendChild(locationNode);
                        locationNode.InnerText = mapping.Location;
                    }
                }

                if (definition.ResourceVersion > 0)
                {
                    XmlNode resourceVersionNode = document.CreateNode(XmlNodeType.Element, s_resourceVersion, null);
                    serviceDefinitionNode.AppendChild(resourceVersionNode);
                    resourceVersionNode.InnerText = XmlConvert.ToString(definition.ResourceVersion);
                }

                if (definition.MinVersionString != null)
                {
                    XmlNode minVersionNode = document.CreateNode(XmlNodeType.Element, s_minVersion, null);
                    serviceDefinitionNode.AppendChild(minVersionNode);
                    minVersionNode.InnerText = definition.MinVersionString;
                }

                if (definition.MaxVersionString != null)
                {
                    XmlNode maxVersionNode = document.CreateNode(XmlNodeType.Element, s_maxVersion, null);
                    serviceDefinitionNode.AppendChild(maxVersionNode);
                    maxVersionNode.InnerText = definition.MaxVersionString;
                }

                if (definition.ReleasedVersionString != null)
                {
                    XmlNode releasedVersionNode = document.CreateNode(XmlNodeType.Element, s_releasedVersion, null);
                    serviceDefinitionNode.AppendChild(releasedVersionNode);
                    releasedVersionNode.InnerText = definition.ReleasedVersionString;
                }
            }
        }

        public void WriteCachedMisses(XmlNode documentNode, IEnumerable<String> cachedMisses)
        {
            XmlDocument document = documentNode.OwnerDocument;

            XmlNode cacheMissesNode = document.CreateNode(XmlNodeType.Element, s_cachedMisses, null);
            documentNode.AppendChild(cacheMissesNode);

            foreach (String cacheMiss in cachedMisses)
            {
                XmlNode cacheMissNode = document.CreateNode(XmlNodeType.Element, s_cachedMiss, null);
                cacheMissNode.InnerText = cacheMiss;
                cacheMissesNode.AppendChild(cacheMissNode);
            }
        }

        /// <summary>
        /// Gets the location service url for the access mapping moniker provided.
        /// This function should be used to retrieve location service urls for access
        /// zones that were loaded by this LocationXmlController instance.
        /// </summary>
        /// <param name="moniker">The access mapping moniker.</param>
        /// <returns>The location service url for this access mapping moniker.</returns>
        public String GetLocationServiceUrl(String moniker)
        {
            return m_accessMappingLocationServiceUrls[moniker];
        }

        /// <summary>
        /// Throws and exception if the node provided is null.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <param name="nodeName">The name of the node to check.</param>
        /// <param name="parent">The parent node of the node we are checking.</param>
        private static void CheckXmlNodeNull(XmlNode node, String nodeName, XmlNode parent)
        {
            if (node == null)
            {
                throw new ConfigFileException(CommonResources.XmlNodeMissing(nodeName, parent));
            }
        }

        /// <summary>
        /// Throws an exception if the xml node is null or empty.
        /// </summary>
        /// <param name="node">The node we are checking.</param>
        /// <param name="nodeName">The name of the node we are checking.</param>
        /// <param name="parent">The parent node of the node we are checking.</param>
        private static void CheckXmlNodeNullOrEmpty(XmlNode node, String nodeName, XmlNode parent)
        {
            CheckXmlNodeNull(node, nodeName, parent);

            if (node.InnerText.Equals(String.Empty))
            {
                throw new ConfigFileException(CommonResources.XmlNodeEmpty(nodeName, parent.Name));
            }
        }

        /// <summary>
        /// Throws exception if the attribute provided is null or empty
        /// </summary>
        /// <param name="attribute">The attribute we are checking.</param>
        /// <param name="attributeName">The name of the attribute we are checking.</param>
        /// <param name="element">The node that contains this attribute.</param>
        private static void CheckXmlAttributeNullOrEmpty(XmlAttribute attribute, String attributeName, XmlNode element)
        {
            if (attribute == null)
            {
                throw new ConfigFileException(CommonResources.XmlAttributeNull(attributeName, element.Name));
            }

            if (attribute.InnerText.Equals(String.Empty))
            {
                throw new ConfigFileException(CommonResources.XmlAttributeEmpty(attributeName, element.Name));
            }
        }

        /// <summary>
        /// Maps access mapping monikers to location service urls
        /// </summary>
        private Dictionary<String, String> m_accessMappingLocationServiceUrls;

        private Boolean m_isClientCache;

        private static readonly String s_lastChangeId = "LastChangeId";
        private static readonly String s_cacheExpirationDate = "CacheExpirationDate";
        private static readonly String s_defaultAccessMappingMoniker = "DefaultAccessMappingMoniker";
        private static readonly String s_virtualDirectory = "VirtualDirectory";

        private static readonly String s_services = "Services";
        private static readonly String s_cachedMisses = "CachedMisses";
        private static readonly String s_serviceDefinition = "ServiceDefinition";
        private static readonly String s_cachedMiss = "CachedMiss";
        private static readonly String s_serviceType = "ServiceType";
        private static readonly String s_identifier = "Identifier";
        private static readonly String s_displayName = "DisplayName";
        private static readonly String s_locationServiceUrl = "LocationServiceUrl";
        private static readonly String s_description = "Description";
        private static readonly String s_relativePath = "RelativePath";
        private static readonly String s_relativeTo = "relativeTo";
        private static readonly String s_parentServiceType = "ParentServiceType";
        private static readonly String s_parentIdentifier = "ParentIdentifier";
        private static readonly String s_locationMappings = "LocationMappings";
        private static readonly String s_locationMapping = "LocationMapping";
        private static readonly String s_location = "Location";
        private static readonly String s_resourceVersion = "ResourceVersion";
        private static readonly String s_minVersion = "MinVersion";
        private static readonly String s_maxVersion = "MaxVersion";
        private static readonly String s_releasedVersion = "ReleasedVersion";

        private static readonly String s_accessMappings = "AccessMappings";
        private static readonly String s_accessMapping = "AccessMapping";
        private static readonly String s_moniker = "Moniker";
        private static readonly String s_accessPoint = "AccessPoint";
    }

    internal static class RelativeToEnumCache
    {
        private static Dictionary<String, RelativeToSetting> s_relativeToEnums;

        static RelativeToEnumCache()
        {
            s_relativeToEnums = new Dictionary<String, RelativeToSetting>(StringComparer.OrdinalIgnoreCase);
            s_relativeToEnums["Context"] = RelativeToSetting.Context;
            s_relativeToEnums["FullyQualified"] = RelativeToSetting.FullyQualified;
            s_relativeToEnums["WebApplication"] = RelativeToSetting.WebApplication;
        }

        internal static Dictionary<String, RelativeToSetting> GetRelativeToEnums()
        {
            return s_relativeToEnums;
        }
    }
}
