using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Identity;
using GitHub.Services.Location;
using GitHub.Services.Location.Client;
using GitHub.Services.WebApi.Utilities;

namespace GitHub.Services.WebApi.Location
{
    /// <summary>
    /// 
    /// </summary>
    public interface IVssServerDataProvider : ILocationDataProvider
    {
        /// <summary>
        /// 
        /// </summary>
        Boolean HasConnected { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Identity.Identity> GetAuthorizedIdentityAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Identity.Identity> GetAuthenticatedIdentityAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Performs all of the steps that are necessary for setting up a connection
        /// with a TeamFoundationServer.  Specify what information should be 
        /// returned in the connectOptions parameter.
        /// 
        /// Each time this call is made the username for the current user 
        /// will be returned as well as the client zone that this client is making 
        /// requests from.
        /// </summary>
        /// <param name="connectOptions">Specifies what information that should be 
        /// returned from the server.</param>
        Task ConnectAsync(ConnectOptions connectOptions, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Reset the connection state back to disconnect
        /// The client must reconnect
        /// </summary>
        Task DisconnectAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

    /// <summary>
    /// This class provides data about the server via the LocationService.
    /// </summary>
    internal class VssServerDataProvider : IVssServerDataProvider
    {
        public VssServerDataProvider(
            VssConnection connection,
            HttpMessageHandler pipeline,
            String fullyQualifiedUrl)
        {
            m_connection = connection;
            m_baseUri = connection.Uri;
            m_fullyQualifiedUrl = fullyQualifiedUrl;
            m_locationClient = new LocationHttpClient(m_baseUri, pipeline, false);

            // Try to get the guid for this server
            ServerMapData serverData = LocationServerMapCache.ReadServerData(m_fullyQualifiedUrl);
            m_locationDataCacheManager = new LocationCacheManager(serverData.ServerId, serverData.ServiceOwner, m_baseUri);
        }

        // Back-pointer to connection
        internal VssConnection Connection
        {
            get { return m_connection; }
        }

        /// <summary>
        /// Returns true if this object has successfully authenticated.
        /// </summary>
        public bool HasConnected
        {
            get
            {
                return m_connectionMade == true;
            }
        }

        /// <summary>
        /// Gets the authorized user.  This function will authenticate with the server if it has
        /// not done so already.  Like any other regular method, it throws VssUnauthorizedException 
        /// if the server is contacted and authentication fails.
        /// </summary>
        /// <returns>The authenticated user.</returns>
        public async Task<Identity.Identity> GetAuthorizedIdentityAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await EnsureConnectedAsync(ConnectOptions.None).ConfigureAwait(false);

            Debug.Assert(m_authorizedIdentity != null);
            return m_authorizedIdentity;
        }

        /// <summary>
        /// Gets the authenticated user.  This function will authenticate with the server if it has
        /// not done so already.  Like any other regular method, it throws VssUnauthorizedException 
        /// if the server is contacted and authentication fails.
        /// </summary>
        /// <returns>The authenticated user.</returns>
        public async Task<Identity.Identity> GetAuthenticatedIdentityAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await EnsureConnectedAsync(ConnectOptions.None).ConfigureAwait(false);

            Debug.Assert(m_authenticatedIdentity != null);
            return m_authenticatedIdentity;
        }

        /// <summary>
        /// 
        /// </summary>
        public Guid InstanceId
        {
            get
            {
                return GetInstanceIdAsync().SyncResult();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Guid InstanceType
        {
            get
            {
                return GetInstanceTypeAsync().SyncResult();
            }
        }

        /// <summary>
        /// The unique identifier for this server. This method will attempt to return
        /// a cached value, if possible.
        /// </summary>
        public async Task<Guid> GetInstanceIdAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!NeedToConnect(ConnectOptions.None))
            {
                // We've already made a Connect call and have the authoritative instance ID.
                return m_instanceId;
            }
            else
            {
                // Check the location server cache to see if we have the instance ID there.
                ServerMapData serverData = LocationServerMapCache.ReadServerData(m_fullyQualifiedUrl);
                Guid toReturn = serverData.ServerId;

                if (Guid.Empty != toReturn)
                {
                    // We do. Return it.
                    return toReturn;
                }

                // We do not. Make a Connect call and retrieve the instance ID.
                await EnsureConnectedAsync(ConnectOptions.None, cancellationToken).ConfigureAwait(false);
                return m_instanceId;
            }
        }

        /// <summary>
        /// The unique identifier for the service owner. This property will attempt to return
        /// a cached value, if possible.
        /// </summary>
        public async Task<Guid> GetInstanceTypeAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!NeedToConnect(ConnectOptions.None))
            {
                // We've already made a Connect call and have the authoritative service owner ID.
                return m_serviceOwner;
            }
            else
            {
                ServerMapData serverData = LocationServerMapCache.ReadServerData(m_fullyQualifiedUrl);
                Guid toReturn = serverData.ServiceOwner;

                if (Guid.Empty != toReturn)
                {
                    // We do. Return it.
                    return toReturn;
                }

                // We do not. Make a Connect call and retrieve the service owner ID.
                await EnsureConnectedAsync(ConnectOptions.None, cancellationToken).ConfigureAwait(false);
                return m_serviceOwner;
            }
        }

        public AccessMapping DefaultAccessMapping
        {
            get
            {
                return GetDefaultAccessMappingAsync().SyncResult();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<AccessMapping> GetDefaultAccessMappingAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            AccessMapping defaultAccessMapping = m_locationDataCacheManager.DefaultAccessMapping;

            // If defaultAccessMapping is null we may not have the cache information yet, go to the server to get the information.
            if (defaultAccessMapping == null)
            {
                await EnsureConnectedAsync(ConnectOptions.IncludeServices, cancellationToken).ConfigureAwait(false);
                defaultAccessMapping = m_locationDataCacheManager.DefaultAccessMapping;

                Debug.Assert(defaultAccessMapping != null, "defaultAccessMapping should never be null");
            }

            return defaultAccessMapping;
        }

        public AccessMapping ClientAccessMapping
        {
            get
            {
                return GetClientAccessMappingAsync().SyncResult();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<AccessMapping> GetClientAccessMappingAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            AccessMapping clientAccessMapping = m_locationDataCacheManager.ClientAccessMapping;

            // If definition is null we may not have the cache information yet, go to the server to get the information.
            if (clientAccessMapping == null)
            {
                await EnsureConnectedAsync(ConnectOptions.IncludeServices, cancellationToken).ConfigureAwait(false);
                clientAccessMapping = m_locationDataCacheManager.ClientAccessMapping;

                Debug.Assert(clientAccessMapping != null, "clientAccessMapping should never be null");
            }

            return clientAccessMapping;
        }

        public IEnumerable<AccessMapping> ConfiguredAccessMappings
        {
            get
            {
                return GetConfiguredAccessMappingsAsync().SyncResult();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AccessMapping>> GetConfiguredAccessMappingsAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            await EnsureConnectedAsync(ConnectOptions.IncludeServices, cancellationToken).ConfigureAwait(false);
            return m_locationDataCacheManager.AccessMappings;
        }

        public AccessMapping GetAccessMapping(String moniker)
        {
            return GetAccessMappingAsync(moniker).SyncResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="moniker"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<AccessMapping> GetAccessMappingAsync(
            String moniker,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(moniker, "moniker");

            await EnsureConnectedAsync(ConnectOptions.IncludeServices, cancellationToken).ConfigureAwait(false);
            return m_locationDataCacheManager.GetAccessMapping(moniker);
        }

        public String LocationForAccessMapping(String serviceType, Guid serviceIdentifier, AccessMapping accessMapping)
        {
            return LocationForAccessMappingAsync(serviceType, serviceIdentifier, accessMapping).SyncResult();
        }

        public async Task<String> LocationForAccessMappingAsync(
            String serviceType,
            Guid serviceIdentifier,
            AccessMapping accessMapping,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ServiceDefinition serviceDefinition = await FindServiceDefinitionAsync(serviceType, serviceIdentifier, cancellationToken).ConfigureAwait(false);

            if (serviceDefinition == null)
            {
                // This method is expected to return a location or fail so throw if we couldn't find
                // the service definition.
                throw new ServiceDefinitionDoesNotExistException(WebApiResources.ServiceDefinitionDoesNotExist(serviceType, serviceIdentifier));
            }

            return await LocationForAccessMappingAsync(serviceDefinition, accessMapping, cancellationToken).ConfigureAwait(false);
        }

        public String LocationForAccessMapping(
            ServiceDefinition serviceDefinition,
            AccessMapping accessMapping)
        {
            return LocationForAccessMappingAsync(serviceDefinition, accessMapping).SyncResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceDefinition"></param>
        /// <param name="accessMapping"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<String> LocationForAccessMappingAsync(
            ServiceDefinition serviceDefinition,
            AccessMapping accessMapping,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(serviceDefinition, "serviceDefinition");
            ArgumentUtility.CheckForNull(accessMapping, "accessMapping");

            // If this is FullyQualified then look through our location mappings
            if (serviceDefinition.RelativeToSetting == RelativeToSetting.FullyQualified)
            {
                LocationMapping locationMapping = serviceDefinition.GetLocationMapping(accessMapping);

                if (locationMapping != null)
                {
                    return Task.FromResult<String>(locationMapping.Location);
                }

                // We weren't able to find the location for the access mapping.  Return null.
                return Task.FromResult<String>(null);
            }
            else
            {
                // Make sure the AccessMapping has a valid AccessPoint.
                if (String.IsNullOrEmpty(accessMapping.AccessPoint))
                {
                    throw new InvalidAccessPointException(WebApiResources.InvalidAccessMappingLocationServiceUrl());
                }

                String webApplicationRelativeDirectory = m_locationDataCacheManager.WebApplicationRelativeDirectory;

                if (accessMapping.VirtualDirectory != null)
                {
                    webApplicationRelativeDirectory = accessMapping.VirtualDirectory;
                }

                Uri uri = new Uri(accessMapping.AccessPoint);

                String properRoot = String.Empty;
                switch (serviceDefinition.RelativeToSetting)
                {
                    case RelativeToSetting.Context:
                        properRoot = PathUtility.Combine(uri.AbsoluteUri, webApplicationRelativeDirectory);
                        break;
                    case RelativeToSetting.WebApplication:
                        properRoot = accessMapping.AccessPoint;
                        break;
                    default:
                        Debug.Assert(true, "Found an unknown RelativeToSetting");
                        break;
                }

                return Task.FromResult<String>(PathUtility.Combine(properRoot, serviceDefinition.RelativePath));
            }
        }

        public String LocationForCurrentConnection(
            String serviceType,
            Guid serviceIdentifier)
        {
            return LocationForCurrentConnectionAsync(serviceType, serviceIdentifier).SyncResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="serviceIdentifier"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<String> LocationForCurrentConnectionAsync(
            String serviceType,
            Guid serviceIdentifier,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (StringComparer.CurrentCultureIgnoreCase.Equals(serviceType, ServiceInterfaces.LocationService2) &&
                serviceIdentifier == LocationServiceConstants.SelfReferenceIdentifier)
            {
                // This is an edge case because the server may not have registered a self-reference pointer
                // or the server is legacy and doesn't send back service owner yet.
                return m_baseUri.AbsoluteUri;
            }

            ServiceDefinition serviceDefinition = await FindServiceDefinitionAsync(serviceType, serviceIdentifier, cancellationToken).ConfigureAwait(false);

            if (serviceDefinition == null)
            {
                // This method should not throw if a ServiceDefinition could not be found.
                return null;
            }

            return await LocationForCurrentConnectionAsync(serviceDefinition, cancellationToken).ConfigureAwait(false);
        }

        public String LocationForCurrentConnection(ServiceDefinition serviceDefinition)
        {
            return LocationForCurrentConnectionAsync(serviceDefinition).SyncResult();
        }

        public async Task<String> LocationForCurrentConnectionAsync(
            ServiceDefinition serviceDefinition,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            AccessMapping clientAccessMapping = await GetClientAccessMappingAsync(cancellationToken).ConfigureAwait(false);
            String location = await LocationForAccessMappingAsync(serviceDefinition, clientAccessMapping, cancellationToken).ConfigureAwait(false);

            if (location == null)
            {
                AccessMapping defaultAccessMapping = await GetDefaultAccessMappingAsync(cancellationToken).ConfigureAwait(false);
                location = await LocationForAccessMappingAsync(serviceDefinition, defaultAccessMapping, cancellationToken).ConfigureAwait(false);

                if (location == null)
                {
                    LocationMapping firstLocationMapping = serviceDefinition.LocationMappings.FirstOrDefault();

                    if (firstLocationMapping == null)
                    {
                        throw new InvalidServiceDefinitionException(WebApiResources.ServiceDefinitionWithNoLocations(serviceDefinition.ServiceType));
                    }

                    location = firstLocationMapping.Location;
                }
            }

            return location;
        }

        public IEnumerable<ServiceDefinition> FindServiceDefinitions(String serviceType)
        {
            return FindServiceDefinitionsAsync(serviceType).SyncResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ServiceDefinition>> FindServiceDefinitionsAsync(
            String serviceType,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Look in the cache
            IEnumerable<ServiceDefinition> definitions = null;

            if (m_locationDataCacheManager != null)
            {
                definitions = m_locationDataCacheManager.FindServices(serviceType);
            }

            // If definitions is null, we had a potential cache miss, go to the server to see if our cache is up-to-date
            if (definitions == null)
            {
                await CheckForServerUpdatesAsync(cancellationToken).ConfigureAwait(false);

                // Try again to see if we can find it now in case that something has updated.
                return m_locationDataCacheManager.FindServices(serviceType);
            }

            return definitions;
        }

        public ServiceDefinition FindServiceDefinition(String serviceType, Guid serviceIdentifier)
        {
            return FindServiceDefinitionAsync(serviceType, serviceIdentifier).SyncResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="serviceIdentifier"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ServiceDefinition> FindServiceDefinitionAsync(
            String serviceType,
            Guid serviceIdentifier,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(serviceType, "serviceType");

            int lastChangeId = m_locationDataCacheManager.GetLastChangeId();

            ServiceDefinition definition;
            if (m_locationDataCacheManager.TryFindService(serviceType, serviceIdentifier, out definition))
            {
                // If we hit a cache entry return it whether it is null or not.
                return definition;
            }

            // If we got here that means that we have a first-time cache miss, go to the server to see if our cache is up-to-date
            await CheckForServerUpdatesAsync(cancellationToken).ConfigureAwait(false);

            // Try again to see if we can find it now in case that something has updated.
            if (!m_locationDataCacheManager.TryFindService(serviceType, serviceIdentifier, out definition))
            {
                // If it is a LS2 then try to fault the definition in, otherwise add a cache miss
                if (String.Equals(serviceType, ServiceInterfaces.LocationService2, StringComparison.OrdinalIgnoreCase) &&
                    serviceIdentifier != LocationServiceConstants.RootIdentifier &&
                    serviceIdentifier != LocationServiceConstants.ApplicationIdentifier &&
                    await GetInstanceTypeAsync(cancellationToken).ConfigureAwait(false) == LocationServiceConstants.RootIdentifier)
                {
                    // Force SPS to fault in the definition
                    definition = await m_locationClient.GetServiceDefinitionAsync(serviceType, serviceIdentifier, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    m_locationDataCacheManager.AddCachedMiss(serviceType, serviceIdentifier, lastChangeId);
                    return null;
                }
            }

            return definition;
        }

        public ApiResourceLocationCollection GetResourceLocations()
        {
            return GetResourceLocationsAsync().SyncResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ApiResourceLocationCollection> GetResourceLocationsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (m_resourceLocations == null)
            {
                IEnumerable<ServiceDefinition> definitions = await FindServiceDefinitionsAsync(null).ConfigureAwait(false);

                if (definitions != null)
                {
                    IEnumerable<ServiceDefinition> resourceLocationDefinitions = definitions.Where(x => x.ResourceVersion > 0);

                    if (resourceLocationDefinitions.Any())
                    {
                        ApiResourceLocationCollection resourceLocations = new ApiResourceLocationCollection();

                        foreach (ServiceDefinition definition in resourceLocationDefinitions)
                        {
                            resourceLocations.AddResourceLocation(ApiResourceLocation.FromServiceDefinition(definition));
                        }

                        m_resourceLocations = resourceLocations;
                    }
                }
            }

            return m_resourceLocations;
        }

        /// <summary>
        /// Consults the server to see if any services from the filter array have 
        /// changed.  It updates the cache with the new values.
        /// </summary>
        /// <param name="cancellationToken"></param>
        private async Task CheckForServerUpdatesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Boolean checkedForUpdates = await EnsureConnectedAsync(ConnectOptions.IncludeServices, cancellationToken).ConfigureAwait(false);

            if (!checkedForUpdates)
            {
                Int32 lastChangeId = m_locationDataCacheManager.GetLastChangeId();

                // If the ServerDataProvider believes it is already connected (i.e. EnsureConnectedAsync returns false) but the location cache is in a bad state
                // we need to make another Connect call to get back to a good state.
                // This can happen if the disk cache is invalidated by another process (or another VssConnection object in the same process) writing to the shared file
                // this will invalidate the memory cache (via FileSystemWatcher), but we could then subsequently fail to reload the disk cache (IOException) for some reason.
                if (lastChangeId == -1)
                {
                    await ConnectAsync(ConnectOptions.IncludeServices, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// This function ensures that the connection data that is needed by the caller
        /// has been retrieved from the server.  This function does not use the 
        /// credentials provider if authentication fails.
        /// </summary>
        /// <param name="optionsNeeded">The options that designate the information the
        /// caller needs from the server.</param>
        private async Task<Boolean> EnsureConnectedAsync(
            ConnectOptions optionsNeeded,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (NeedToConnect(optionsNeeded))
            {
                // We only want one thread to make the server call, so we will lock this section.
                // It's **really** important that the locked contents (i.e. ConnectAsync) has no recursive path back into this code
                // otherwise we will deadlock.
                using (await m_connectionLock.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (NeedToConnect(optionsNeeded))
                    {
                        await ConnectAsync(optionsNeeded, cancellationToken).ConfigureAwait(false);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if we need to connect to the server.
        /// </summary>
        /// <param name="optionsNeeded"></param>
        /// <returns></returns>
        private Boolean NeedToConnect(ConnectOptions optionsNeeded)
        {
            // Make sure we refresh the information if the impersonated user has changed.
            if (m_locationDataCacheManager.CacheDataExpired)
            {
                m_connectionMade = false;
                m_validConnectionData = ConnectOptions.None;
            }

            return !m_connectionMade || ((optionsNeeded & m_validConnectionData) != optionsNeeded);
        }

        public async Task ConnectAsync(ConnectOptions connectOptions, CancellationToken cancellationToken = default(CancellationToken))
        {
            // We want to force ourselves to includes services if our location service cache has no access mappings.
            // This means that this is our first time connecting.
            if (!m_locationDataCacheManager.AccessMappings.Any())
            {
                connectOptions |= ConnectOptions.IncludeServices;
            }

            Int32 lastChangeId = m_locationDataCacheManager.GetLastChangeId();

            // If we have -1 then that means we have no disk cache yet or it means that we recently hit an exception trying to reload
            // the the cache from disk (see Exception catch block in EnsureDiskCacheLoaded).
            // Either way, we cannot make a call to the server with -1 and pass None.
            // If we do, the resulting payload (which would have ClientCacheFresh=false but include no ServiceDefinitions)
            // would leave the in-memory cache in an inconsistent state
            if (lastChangeId == -1)
            {
                connectOptions |= ConnectOptions.IncludeServices;
            }

            Boolean includeServices = (connectOptions & ConnectOptions.IncludeServices) == ConnectOptions.IncludeServices;

            // Perform the connection
            ConnectionData connectionData = await GetConnectionDataAsync(connectOptions, lastChangeId, cancellationToken).ConfigureAwait(false);
            LocationServiceData locationServiceData = connectionData.LocationServiceData;

            // If we were previously connected, make sure we cannot connect as a different user.
            if (m_authenticatedIdentity != null)
            {
                if (!IdentityDescriptorComparer.Instance.Equals(m_authenticatedIdentity.Descriptor, connectionData.AuthenticatedUser.Descriptor))
                {
                    throw new VssAuthenticationException(WebApiResources.CannotAuthenticateAsAnotherUser(m_authenticatedIdentity.DisplayName, connectionData.AuthenticatedUser.DisplayName));
                }
            }

            m_authenticatedIdentity = connectionData.AuthenticatedUser;
            m_authorizedIdentity = connectionData.AuthorizedUser;

            m_instanceId = connectionData.InstanceId;

            if (locationServiceData != null)
            {
                Guid serviceOwner = connectionData.LocationServiceData.ServiceOwner;

                if (Guid.Empty == serviceOwner)
                {
                    serviceOwner = ServiceInstanceTypes.TFSOnPremises;
                }

                m_serviceOwner = serviceOwner;
            }

            // Verify with our locationServerMap cache that we are storing the correct guid
            // for this server.  If we are, this is essentially a no-op.
            Boolean wroteMapping = LocationServerMapCache.EnsureServerMappingExists(m_fullyQualifiedUrl, m_instanceId, m_serviceOwner);

            if (wroteMapping)
            {
                if (includeServices &&
                    (connectionData.LocationServiceData.ServiceDefinitions == null ||
                    connectionData.LocationServiceData.ServiceDefinitions.Count == 0))
                {
                    // This is the rare, rare case where a new server exists at the same url
                    // that an old server used to (guids are different) and both servers had the same
                    // location service last change id.  In that case, Connect would not have
                    // brought down any services. To fix this we need to query the services back
                    // down with -1 as our last change id
                    ConnectionData updatedConnectionData = await GetConnectionDataAsync(ConnectOptions.IncludeServices, -1, cancellationToken).ConfigureAwait(false);
                    locationServiceData = updatedConnectionData.LocationServiceData;
                }

                m_locationDataCacheManager = new LocationCacheManager(m_instanceId, m_serviceOwner, m_baseUri);
            }

            // update the location service cache if we tried to retireve location service data
            m_locationDataCacheManager.WebApplicationRelativeDirectory = connectionData.WebApplicationRelativeDirectory;
            if (locationServiceData != null)
            {
                m_locationDataCacheManager.LoadServicesData(locationServiceData, includeServices);
            }

            // Set the connection data that we have retrieved
            m_validConnectionData |= connectOptions;

            m_connectionMade = true;
        }

        /// <summary>
        /// Reset the connected state of the provider
        /// </summary>
        public Task DisconnectAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            m_connectionMade = false;
            m_authenticatedIdentity = null;
            m_authorizedIdentity = null;
            return Task.FromResult<Object>(null);
        }

        /// <summary>
        /// Passed in on construction. The Uris for the server we are connecting to.
        /// </summary>
        private VssConnection m_connection;
        private Uri m_baseUri;
        private String m_fullyQualifiedUrl;

        /// <summary>
        /// These are the values we are responsible for determining
        /// </summary>
        private Identity.Identity m_authenticatedIdentity;
        private Identity.Identity m_authorizedIdentity;
        private Guid m_instanceId;
        private Guid m_serviceOwner;

        /// <summary>
        /// These handle talking to the web service and dealing with connection data
        /// </summary>
        private LocationHttpClient m_locationClient;
        private ConnectOptions m_validConnectionData;
        private Boolean m_connectionMade;

        /// <summary>
        /// This object manages the location data cache
        /// </summary>
        private LocationCacheManager m_locationDataCacheManager;

        /// <summary>
        /// Cache of the resource locations
        /// </summary>
        private ApiResourceLocationCollection m_resourceLocations;

        private readonly AsyncLock m_connectionLock = new AsyncLock();

        private async Task<ConnectionData> GetConnectionDataAsync(ConnectOptions connectOptions, int lastChangeId, CancellationToken cancellationToken)
        {
            int timeoutRetries = 1;

            while (true)
            {
                try
                {
                    return await m_locationClient.GetConnectionDataAsync(connectOptions, lastChangeId, cancellationToken).ConfigureAwait(false);
                }
                catch(TimeoutException) when (timeoutRetries-- > 0) { } // Catch TimeoutException when we have retries remaining; otherwise, let it go.
            }
        }
    }
}
