// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using GitHub.Services.Common;
using GitHub.Services.Location;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.WebApi.Location
{
    /// <summary>
    /// This class is responsible for managing both the in-memory and disk cache
    /// for the location service.
    /// </summary>
    internal class LocationCacheManager
    {
        /// <summary>
        /// Creates a new cache manager for the serverGuid passed in.
        /// </summary>
        /// <param name="serverGuid"></param>
        public LocationCacheManager(Guid serverGuid, Guid serviceOwner, Uri connectionBaseUrl)
        {
            m_cacheAvailable = (serverGuid.Equals(Guid.Empty)) ? false : true;
            
            m_lastChangeId = -1;
            m_cacheExpirationDate = DateTime.MinValue;

            if (serviceOwner == Guid.Empty)
            {
                // For a legacy server (which didn't return serviceOwner in the connectionData), let's not try to break anything
                // just use the old path.
                // We should be able to remove this case eventually.
                m_cacheFilePath = Path.Combine(Path.Combine(VssClientSettings.ClientCacheDirectory, serverGuid.ToString()),
                                               s_cacheFileName);
            }
            else
            {
                m_cacheFilePath = Path.Combine(Path.Combine(Path.Combine(VssClientSettings.ClientCacheDirectory, serverGuid.ToString()), serviceOwner.ToString()),
                                               s_cacheFileName);
            }

            m_cacheLocallyFresh = false;
            m_accessMappings = new Dictionary<String, AccessMapping>(VssStringComparer.AccessMappingMoniker);
            m_services = new Dictionary<String, Dictionary<Guid, ServiceDefinition>>(VssStringComparer.ServiceType);
            m_cachedMisses = new HashSet<String>(VssStringComparer.ServiceType);

            m_connectionBaseUrl = connectionBaseUrl;
            m_locationXmlOperator = new LocationXmlOperator(true);
        }

        /// <summary>
        /// True if there is a cache on disk available for this server
        /// </summary>
        public Boolean LocalCacheAvailable
        {
            get
            {
                EnsureDiskCacheLoaded();

                return m_cacheAvailable;
            }
        }

        /// <summary>
        /// Whether or not the cached data has expired (and should be refreshed)
        /// </summary>
        internal Boolean CacheDataExpired
        {
            get
            {
                // A) Cache is available (i.e. we're not relying solely on the memory cache because the disk cache file was unavailable)
                // and B) The memory cache is correct with the disk cache (necessary to enforce expiration)
                // and C) It is after the expiration time
                return m_cacheAvailable && m_cacheLocallyFresh && DateTime.UtcNow >= m_cacheExpirationDate;
            }
        }

        public AccessMapping ClientAccessMapping
        {
            get
            {
                m_accessLock.EnterReadLock();

                try
                {
                    return !CacheDataExpired ? m_clientAccessMapping : null;
                }
                finally
                {
                    m_accessLock.ExitReadLock();
                }
            }
        }

        public AccessMapping DefaultAccessMapping
        {
            get
            {
                m_accessLock.EnterReadLock();

                try
                {
                    return !CacheDataExpired ? m_defaultAccessMapping : null;
                }
                finally
                {
                    m_accessLock.ExitReadLock();
                }
            }
        }

        public String WebApplicationRelativeDirectory
        {
            get
            {
                return m_webApplicationRelativeDirectory;
            }
            set
            {
                m_webApplicationRelativeDirectory = String.IsNullOrEmpty(value) ? m_webApplicationRelativeDirectory : value;
            }
        }

        public void ClearIfCacheNotFresh(Int32 serverLastChangeId)
        {
            if (serverLastChangeId != m_lastChangeId)
            {
                m_accessLock.EnterWriteLock();

                try
                {
                    if (serverLastChangeId != m_lastChangeId)
                    {
                        m_accessMappings.Clear();
                        m_services.Clear();
                        m_cachedMisses.Clear();
                        m_lastChangeId = -1;
                        m_cacheExpirationDate = DateTime.MinValue;
                    }
                }
                finally
                {
                    m_accessLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Removes services from both the in-memory cache and the disk cache.
        /// </summary>
        /// <param name="serviceDefinitions">The service definitions to remove.</param>
        /// <param name="lastChangeId">The lastChangeId the server returned when
        /// it performed this operation.</param>
        public void RemoveServices(IEnumerable<ServiceDefinition> serviceDefinitions, Int32 lastChangeId)
        {
            EnsureDiskCacheLoaded();

            m_accessLock.EnterWriteLock();

            try
            {
                foreach (ServiceDefinition serviceDefinition in serviceDefinitions)
                {
                    Dictionary<Guid, ServiceDefinition> definitions = null;
                    if (!m_services.TryGetValue(serviceDefinition.ServiceType, out definitions))
                    {
                        continue;
                    }

                    // If the entry is removed and there are no more definitions of this type, remove that
                    // entry from the services structure
                    if (definitions.Remove(serviceDefinition.Identifier) && definitions.Count == 0)
                    {
                        m_services.Remove(serviceDefinition.ServiceType);
                    }
                }

                SetLastChangeId(lastChangeId, false);
                Debug.Assert(m_lastChangeId == -1 || m_services.Count > 0);
                WriteCacheToDisk();
            }
            finally
            {
                m_accessLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns the access mapping for the provided moniker.
        /// </summary>
        /// <param name="moniker">The moniker of the access mapping to 
        /// return.</param>
        /// <returns>The access mapping for the provided moniker or null
        /// if an access mapping for the moniker doesn't exist..</returns>
        public AccessMapping GetAccessMapping(String moniker)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(moniker, "moniker");
            EnsureDiskCacheLoaded();
            m_accessLock.EnterReadLock();

            try
            {
                if (CacheDataExpired)
                {
                    return null;
                }

                AccessMapping accessMapping;
                m_accessMappings.TryGetValue(moniker, out accessMapping);

                return accessMapping;
            }
            finally
            {
                m_accessLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the service definition for the service with the 
        /// provided service type and identifier.  Null will be returned
        /// if there is no entry in the cache for this service.
        /// </summary>
        /// <param name="serviceType">The service type we are looking for.</param>
        /// <param name="serviceIdentifier">The identifier for the specific
        /// service instance we are looking for.</param>
        /// <returns>The service definition for the service with the 
        /// provided service type and identifier.  Null will be returned
        /// if there is no entry in the cache for this service.</returns>
        public Boolean TryFindService(String serviceType, Guid serviceIdentifier, out ServiceDefinition serviceDefinition)
        {
            EnsureDiskCacheLoaded();
            m_accessLock.EnterReadLock();

            try
            {
                Dictionary<Guid, ServiceDefinition> services = null;
                serviceDefinition = null;

                if (CacheDataExpired)
                {
                    return false;
                }

                if (m_services.TryGetValue(serviceType, out services))
                {
                    if (services.TryGetValue(serviceIdentifier, out serviceDefinition))
                    {
                        return true;
                    }
                }

                // Look in our cachedMisses to see if we can find it there.
                if (m_cachedMisses.Contains(BuildCacheMissString(serviceType, serviceIdentifier)))
                {
                    // We found an entry in cached misses so return true.
                    return true;
                }

                return false;
            }
            finally
            {
                m_accessLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Finds all services with the provided service type.
        /// </summary>
        /// <param name="serviceType">The service type we are looking for.</param>
        /// <returns>All of the service definitions with the serviceType that
        /// are in the cache or null if none are in the cache.</returns>
        public IEnumerable<ServiceDefinition> FindServices(String serviceType)
        {
            EnsureDiskCacheLoaded();
            m_accessLock.EnterReadLock();

            try
            {

                Debug.Assert(m_lastChangeId == -1 || m_services.Count > 0);

                if (CacheDataExpired)
                {
                    return null;
                }

                // We either have all of the services or none. If we have none then return null.
                if (m_services.Count == 0)
                {
                    return null;
                }

                // If service type is null, return all services as long as we know
                // that we have all of the services
                IEnumerable<Dictionary<Guid, ServiceDefinition>> dictionaries;
                if (String.IsNullOrEmpty(serviceType))
                {
                    dictionaries = m_services.Values;
                }
                else
                {
                    Dictionary<Guid, ServiceDefinition> services = null;
                    if (!m_services.TryGetValue(serviceType, out services))
                    {
                        return null;
                    }

                    dictionaries = new Dictionary<Guid, ServiceDefinition>[] { services };
                }

                // Make a copy of all of the service definitions to pass back.
                List<ServiceDefinition> serviceDefinitions = new List<ServiceDefinition>();
                foreach (Dictionary<Guid, ServiceDefinition> dict in dictionaries)
                {
                    foreach (ServiceDefinition definition in dict.Values)
                    {
                        serviceDefinitions.Add(definition.Clone());
                    }
                }

                return serviceDefinitions;
            }
            finally
            {
                m_accessLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Loads the service data into the in-memory cache and writes the values to disk.
        /// </summary>
        /// <param name="locationServiceData">The data to write to the cache.</param>
        /// <returns>Copies of the service definitions created by this load</returns>
        public void LoadServicesData(LocationServiceData locationServiceData, Boolean allServicesIncluded)
        {
            m_accessLock.EnterWriteLock();

            try
            {
                // If the server is telling us our client cache isn't fresh and we agree
                // with it, clear the storage.  The reason we check to see if we agree with it
                // is that because of the way we cache based on filters, we may sometimes 
                // tell the server that our last change id is -1 because we don't have a given
                // filter cached.  In this case, the server will tell us our cache is out of
                // date even though it isn't.
                if (!locationServiceData.ClientCacheFresh && locationServiceData.LastChangeId != m_lastChangeId)
                {
                    m_accessMappings = new Dictionary<String, AccessMapping>(VssStringComparer.AccessMappingMoniker);
                    m_services = new Dictionary<String, Dictionary<Guid, ServiceDefinition>>(VssStringComparer.ServiceType);
                    m_cachedMisses = new HashSet<String>(VssStringComparer.ServiceType);
                    m_lastChangeId = -1;
                    m_cacheExpirationDate = DateTime.MinValue;
                }
                else
                {
                    EnsureDiskCacheLoadedHelper();
                }

                // We have to update the lastChangeId outside of the above if check because there
                // are cases such as a register service where we cause the lastChangeId to be incremented
                // and our cache isn't out of date.
                SetLastChangeId(locationServiceData.LastChangeId, allServicesIncluded);

                // Use the client value if provided (this lets clients override the server)
                // otherwise just use the server specified TTL.
                Int32 clientCacheTimeToLive = (ClientCacheTimeToLive != null) ? ClientCacheTimeToLive.Value : locationServiceData.ClientCacheTimeToLive;
                m_cacheExpirationDate = DateTime.UtcNow.AddSeconds(clientCacheTimeToLive);

                ICollection<AccessMapping> accessMappings = locationServiceData.AccessMappings;
                if (accessMappings != null && accessMappings.Count > 0)
                {
                    // Get all of the access mappings 
                    foreach (AccessMapping accessMapping in accessMappings)
                    {
                        // We can't remove this compat code from the client library yet since
                        // we still support newer clients talking to older TFS servers
                        // which might not send VirtualDirectory
                        // Older server means earlier than TFS 2015 CU2
                        if (accessMapping.VirtualDirectory == null &&
                            !String.IsNullOrEmpty(WebApplicationRelativeDirectory))
                        {
                            String absoluteUriTrimmed = accessMapping.AccessPoint.TrimEnd('/');
                            String relativeDirectoryTrimmed = WebApplicationRelativeDirectory.TrimEnd('/');
                            
                            if (VssStringComparer.ServerUrl.EndsWith(absoluteUriTrimmed, relativeDirectoryTrimmed))
                            {
                                accessMapping.AccessPoint = absoluteUriTrimmed.Substring(0, absoluteUriTrimmed.Length - relativeDirectoryTrimmed.Length);
                            }
                        }

                        // if we can find it, update the values so the objects that reference this
                        // access mapping are updated as well
                        AccessMapping existingAccessMapping;
                        if (m_accessMappings.TryGetValue(accessMapping.Moniker, out existingAccessMapping))
                        {
                            existingAccessMapping.DisplayName = accessMapping.DisplayName;
                            existingAccessMapping.AccessPoint = accessMapping.AccessPoint;
                            existingAccessMapping.VirtualDirectory = accessMapping.VirtualDirectory;
                        }
                        else
                        {
                            // We didn't find it, so just set it
                            existingAccessMapping = accessMapping;
                            m_accessMappings[accessMapping.Moniker] = accessMapping;
                        }
                    }

                    DetermineClientAndDefaultZones(locationServiceData.DefaultAccessMappingMoniker);
                }

                if (locationServiceData.ServiceDefinitions != null)
                {
                    // Get all of the services
                    foreach (ServiceDefinition definition in locationServiceData.ServiceDefinitions)
                    {
                        Dictionary<Guid, ServiceDefinition> definitions = null;
                        if (!m_services.TryGetValue(definition.ServiceType, out definitions))
                        {
                            definitions = new Dictionary<Guid, ServiceDefinition>();
                            m_services[definition.ServiceType] = definitions;
                        }

                        definitions[definition.Identifier] = definition;
                    }
                }

                // Even if the cache file wasn't previously available, let's give ourselves another opportunity to update the cache.
                m_cacheAvailable = true;
                WriteCacheToDisk();
            }
            finally
            {
                Debug.Assert(m_lastChangeId == -1 || m_services.Count > 0);
                
                m_accessLock.ExitWriteLock();
            }
        }

        private void DetermineClientAndDefaultZones(String defaultAccessMappingMoniker)
        {
            Debug.Assert(m_accessLock.IsWriteLockHeld);

            m_defaultAccessMapping = null;
            m_clientAccessMapping = null;

            // For comparisons below we MUST use .ToString() here instead of .AbsoluteUri.  .AbsoluteUri will return the path
            // portion of the query string as encoded if it contains characters that are unicode, .ToString()
            // will not return them encoded.  We must not have them encoded so that the comparison below works
            // correctly.  Also, we do not need to worry about the downfalls of using ToString() instead of AbsoluteUri
            // here because any urls that are generated with the generated access point will be placed back into a
            // Uri object before they are used in a web request.
            String relativeDirectoryTrimmed = (WebApplicationRelativeDirectory != null) ? WebApplicationRelativeDirectory.TrimEnd('/') : String.Empty;

            foreach (AccessMapping accessMapping in m_accessMappings.Values)
            {
                if (VssStringComparer.ServerUrl.StartsWith(m_connectionBaseUrl.ToString(), accessMapping.AccessPoint.TrimEnd('/')) &&
                    (accessMapping.VirtualDirectory == null ||
                    VssStringComparer.UrlPath.Equals(accessMapping.VirtualDirectory, relativeDirectoryTrimmed)))
                {
                    m_clientAccessMapping = accessMapping;
                }
            }

            m_defaultAccessMapping = m_accessMappings[defaultAccessMappingMoniker];

            if (m_clientAccessMapping == null)
            {
                String accessPoint = m_connectionBaseUrl.ToString().TrimEnd('/');
                String virtualDirectory = String.Empty;

                if (!String.IsNullOrEmpty(WebApplicationRelativeDirectory))
                {
                    if (VssStringComparer.ServerUrl.EndsWith(accessPoint, relativeDirectoryTrimmed))
                    {
                        accessPoint = accessPoint.Substring(0, accessPoint.Length - relativeDirectoryTrimmed.Length);
                        virtualDirectory = relativeDirectoryTrimmed;
                    }
                }

                // Looks like we are in an unregistered zone, make up our own.
                m_clientAccessMapping = new AccessMapping()
                {
                    Moniker = accessPoint,
                    DisplayName = accessPoint,
                    AccessPoint = accessPoint,
                    VirtualDirectory = virtualDirectory
                };
            }
        }

        /// <summary>
        /// Returns the AccessMappings that this location service cache knows about.
        /// Note that each time this property is accessed, the list is copied and
        /// returned.  
        /// </summary>
        public IEnumerable<AccessMapping> AccessMappings
        {
            get
            {
                EnsureDiskCacheLoaded();
                m_accessLock.EnterReadLock();

                try
                {
                    // return a copy to prevent race conditions
                    List<AccessMapping> accessMappings = new List<AccessMapping>();

                    if (!CacheDataExpired)
                    {
                        foreach (AccessMapping accessMapping in m_accessMappings.Values)
                        {
                            accessMappings.Add(accessMapping);
                        }
                    }

                    return accessMappings;
                }
                finally
                {
                    m_accessLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Removes the access mapping with the provided access mapping moniker
        /// and all of the location mapping entries that have this access
        /// zone.
        /// </summary>
        /// <param name="moniker">The moniker of the access mapping to remove.
        /// </param>
        public void RemoveAccessMapping(String moniker)
        {
            EnsureDiskCacheLoaded();

            m_accessLock.EnterWriteLock();

            try
            {
                // Remove it from the access mappings
                m_accessMappings.Remove(moniker);

                // Remove each instance from the service definitions
                foreach (Dictionary<Guid, ServiceDefinition> serviceGroup in m_services.Values)
                {
                    foreach (ServiceDefinition definition in serviceGroup.Values)
                    {
                        // We know that it is illegal to delete an access mapping that is the default access mapping of 
                        // a service definition so we don't have to update any of those values.

                        // Remove the mapping that has the removed access mapping
                        for (int i = 0; i < definition.LocationMappings.Count; i++)
                        {
                            // If this one needs to be removed, swap it with the end and update the end counter
                            if (VssStringComparer.AccessMappingMoniker.Equals(moniker, definition.LocationMappings[i].AccessMappingMoniker))
                            {
                                definition.LocationMappings.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }

                WriteCacheToDisk();
            }
            finally
            {
                m_accessLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds a cached miss to the location service data, if the last change ID presented
        /// matches the current value.
        /// </summary>
        public void AddCachedMiss(String serviceType, Guid serviceIdentifier, int missedLastChangeId)
        {
            if (missedLastChangeId < 0)
            {
                return;
            }

            EnsureDiskCacheLoaded();
            m_accessLock.EnterWriteLock();

            try
            {
                if (missedLastChangeId == m_lastChangeId &&
                    m_cachedMisses.Add(BuildCacheMissString(serviceType, serviceIdentifier)))
                {
                    WriteCacheToDisk();
                }
            }
            finally
            {
                m_accessLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Returns the id of the last change that this cache is aware of.
        /// </summary>
        public Int32 GetLastChangeId()
        {
            EnsureDiskCacheLoaded();
            m_accessLock.EnterReadLock();

            try
            {
                return m_lastChangeId;
            }
            finally
            {
                m_accessLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns the time of the cache expiration.
        /// </summary>
        /// <returns></returns>
        internal DateTime GetCacheExpirationDate()
        {
            EnsureDiskCacheLoaded();

            m_accessLock.EnterReadLock();

            try
            {
                return m_cacheExpirationDate;
            }
            finally
            {
                m_accessLock.ExitReadLock();
            }
        }

        private void SetLastChangeId(Int32 lastChangeId, Boolean allServicesUpdated)
        {
            Debug.Assert(m_accessLock.IsWriteLockHeld);

            if (m_lastChangeId != -1 || allServicesUpdated)
            {
                // We only update our last change id if the last change id was valid before
                // and this is an incremental update or this data includes all services.
                m_lastChangeId = lastChangeId;
            }
        }

        private static String BuildCacheMissString(String serviceType, Guid serviceIdentifier)
        {
            return String.Concat(serviceType, "_", serviceIdentifier.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        internal void EnsureDiskCacheLoaded()
        {
            if (m_cacheLocallyFresh || !m_cacheAvailable)
            {
                return;
            }

            m_accessLock.EnterWriteLock();

            try
            {
                EnsureDiskCacheLoadedHelper();
            }
            finally
            {
                m_accessLock.ExitWriteLock();
            }
        }

        private void EnsureDiskCacheLoadedHelper()
        {
            Debug.Assert(m_accessLock.IsWriteLockHeld);

            FileStream unusedFile = null;

            try
            {
                if (m_cacheLocallyFresh || !m_cacheAvailable)
                {
                    return;
                }

                // actually load the cache from disk
                // Open the file, allowing concurrent reads.
                // Do not create the file if it does not exist.
                XmlDocument document = XmlUtility.OpenXmlFile(out unusedFile, m_cacheFilePath, FileShare.Read, saveFile: false);

                if (document != null)
                {
                    m_accessMappings = new Dictionary<String, AccessMapping>(VssStringComparer.AccessMappingMoniker);
                    m_services = new Dictionary<String, Dictionary<Guid, ServiceDefinition>>(VssStringComparer.ServiceType);
                    m_cachedMisses = new HashSet<String>(VssStringComparer.ServiceType);

                    // There is an existing cache, load it
                    m_lastChangeId = m_locationXmlOperator.ReadLastChangeId(document);
                    m_cacheExpirationDate = m_locationXmlOperator.ReadCacheExpirationDate(document);
                    String defaultAccessMappingMoniker = m_locationXmlOperator.ReadDefaultAccessMappingMoniker(document);
                    m_webApplicationRelativeDirectory = m_locationXmlOperator.ReadVirtualDirectory(document);

                    // Read and organize the access mappings
                    List<AccessMapping> accessMappings = m_locationXmlOperator.ReadAccessMappings(document);
                    foreach (AccessMapping accessMapping in accessMappings)
                    {
                        m_accessMappings[accessMapping.Moniker] = accessMapping;
                    }

                    if (accessMappings.Count > 0)
                    {
                        DetermineClientAndDefaultZones(defaultAccessMappingMoniker);
                    }
                    else
                    {
                        m_cacheAvailable = false;
                        m_lastChangeId = -1;
                        return;
                    }

                    // Read and organize the service definitions
                    List<ServiceDefinition> serviceDefinitions = m_locationXmlOperator.ReadServices(document, m_accessMappings);
                    foreach (ServiceDefinition definition in serviceDefinitions)
                    {
                        Dictionary<Guid, ServiceDefinition> serviceTypeSet;
                        if (!m_services.TryGetValue(definition.ServiceType, out serviceTypeSet))
                        {
                            serviceTypeSet = new Dictionary<Guid, ServiceDefinition>();
                            m_services.Add(definition.ServiceType, serviceTypeSet);
                        }

                        serviceTypeSet[definition.Identifier] = definition;
                    }

                    List<String> cachedMisses = m_locationXmlOperator.ReadCachedMisses(document);
                    foreach (String cachedMiss in cachedMisses)
                    {
                        m_cachedMisses.Add(cachedMiss);
                    }
                }

                // Hook up the file system watcher if we haven't already
                if (m_fileSystemWatcher == null)
                {
                    m_fileSystemWatcher = new FileSystemWatcher(Path.GetDirectoryName(m_cacheFilePath), s_cacheFileName);
                    m_fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
                    m_fileSystemWatcher.Changed += new FileSystemEventHandler(m_fileSystemWatcher_Changed);
                }

                Debug.Assert(m_lastChangeId == -1 || m_services.Count > 0);
            }
            catch (Exception)
            {
                // It looks as though we don't have access to the cache file.  Eat
                // this exception and mark the cache as unavailable so we don't
                // repeatedly try to access it
                m_cacheAvailable = false;
                m_lastChangeId = -1;
            }
            finally
            {
                m_cacheLocallyFresh = true;

                if (unusedFile != null)
                {
                    unusedFile.Dispose();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            m_cacheLocallyFresh = false;
        }

        /// <summary>
        /// Writes the cache to disk.  Callers of this function should have a writer
        /// lock.
        /// </summary>
        private void WriteCacheToDisk()
        {
            Debug.Assert(m_accessLock.IsWriteLockHeld);

            if (!m_cacheAvailable)
            {
                return;
            }

            try
            {
                Debug.Assert(m_lastChangeId == -1 || m_services.Count > 0);
                // Get an exclusive lock on the file
                using (FileStream file = XmlUtility.OpenFile(m_cacheFilePath, FileShare.None, true))
                {
                    XmlDocument document = new XmlDocument();

                    XmlNode documentNode = document.CreateNode(XmlNodeType.Element, s_docStartElement, null);
                    document.AppendChild(documentNode);

                    m_locationXmlOperator.WriteLastChangeId(documentNode, m_lastChangeId);
                    m_locationXmlOperator.WriteCacheExpirationDate(documentNode, m_cacheExpirationDate);
                    m_locationXmlOperator.WriteDefaultAccessMappingMoniker(documentNode, m_defaultAccessMapping.Moniker);
                    m_locationXmlOperator.WriteVirtualDirectory(documentNode, m_webApplicationRelativeDirectory);
                    m_locationXmlOperator.WriteAccessMappings(documentNode, m_accessMappings.Values);

                    // Build up a list of the service definitions for writing
                    List<ServiceDefinition> serviceDefinitions = new List<ServiceDefinition>();
                    foreach (Dictionary<Guid, ServiceDefinition> serviceTypeSet in m_services.Values)
                    {
                        serviceDefinitions.AddRange(serviceTypeSet.Values);
                    }

                    m_locationXmlOperator.WriteServices(documentNode, serviceDefinitions);
                    m_locationXmlOperator.WriteCachedMisses(documentNode, m_cachedMisses);

                    // Reset the file stream.
                    file.SetLength(0);
                    file.Position = 0;

                    // Save the file.
                    document.Save(file);
                }
            }
            catch (Exception)
            {
                // It looks as though we don't have access to the cache file.  Eat
                // this exception and mark the cache as unavailable so we don't
                // repeatedly try to access it
                m_cacheAvailable = false;
            }
        }

        /// <summary>
        /// This setting controls the amount of time before the cache expires
        /// </summary>
        internal Int32? ClientCacheTimeToLive
        {
            get;
            set;
        }

        /// <summary>
        /// This is the set of services available from this service location
        /// service.
        /// </summary>
        private Dictionary<String, Dictionary<Guid, ServiceDefinition>> m_services;

        /// <summary>
        /// This is the set of services that have been queried since our last update
        /// from the server that we know don't exist.
        /// </summary>
        private HashSet<String> m_cachedMisses;

        /// <summary>
        /// Keeps track of all access mappings that have been given to us by the server.
        /// The key is their identifier.
        /// </summary>
        private Dictionary<String, AccessMapping> m_accessMappings;

        /// <summary>
        /// Keeps track of the lastChangeId for the last change that was put in this cache.
        /// </summary>
        private Int32 m_lastChangeId;

        /// <summary>
        /// The time after which the local cache data is invalid. This is used to prematurely expire the client cache
        /// even if we don't know (yet) whether or not the server changed. By expiring the client cache we
        /// can ensure that clients will be forced to check for server updates periodically rather than relying on the
        /// client cache indefinitely in the degenerate case where no client tools ever explicitly call Connect() (such as tf.exe)
        /// </summary>
        private DateTime m_cacheExpirationDate;

        /// <summary>
        /// This is used to protect the services in-memory store.
        /// </summary>
        private ReaderWriterLockSlim m_accessLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private String m_webApplicationRelativeDirectory;

        /// <summary>
        /// Only let one process write to a cache at a time.
        /// </summary>
        private static Object s_cacheMutex = new Object();

        /// <summary>
        /// This object is used to keep track of whether or not our cache is fresh
        /// with respect to what we have on disk.
        /// </summary>
        private Boolean m_cacheLocallyFresh;

        /// <summary>
        /// This is true if we do not have access to the cache file
        /// </summary>
        private Boolean m_cacheAvailable;

        /// <summary>
        /// This is used to watch for others changing our cache so we can respond to 
        /// those changes
        /// </summary>
        private FileSystemWatcher m_fileSystemWatcher;

        private Uri m_connectionBaseUrl;

        /// <summary>
        /// The two calculated access mappings that this manager caches.
        /// </summary>
        private AccessMapping m_clientAccessMapping;
        private AccessMapping m_defaultAccessMapping;

        /// <summary>
        /// persistent cache file name values
        /// </summary>
        private static readonly String s_cacheFileName = "LocationServiceData.config";
        private String m_cacheFilePath;

        private LocationXmlOperator m_locationXmlOperator;

        /// <summary>
        /// xml document related constants
        /// </summary>           
        private const String s_docStartElement = "LocationServiceConfiguration";
    }
}
