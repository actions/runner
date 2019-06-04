using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common.Internal;
using GitHub.Services.Location;

namespace GitHub.Services.WebApi.Location
{
    /// <summary>
    /// 
    /// </summary>
    internal class LocationService : ILocationService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public virtual void Initialize(
            VssConnection connection)
        {
            m_connection = connection;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <returns></returns>
        public ILocationDataProvider GetLocationData(
            Guid locationAreaIdentifier)
        {
            return GetLocationDataAsync(locationAreaIdentifier).SyncResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ILocationDataProvider> GetLocationDataAsync(
            Guid locationAreaIdentifier,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (locationAreaIdentifier == Guid.Empty ||
                locationAreaIdentifier == LocationServiceConstants.SelfReferenceIdentifier)
            {
                return LocalDataProvider;
            }
            else
            {
                // These methods might make a server call but generally it will be accessing cached data
                Guid instanceId = await LocalDataProvider.GetInstanceIdAsync(cancellationToken).ConfigureAwait(false);
                Guid instanceType = await LocalDataProvider.GetInstanceTypeAsync(cancellationToken).ConfigureAwait(false);

                if (locationAreaIdentifier == instanceId ||
                    locationAreaIdentifier == instanceType ||
                    instanceType == ServiceInstanceTypes.TFSOnPremises)
                {
                    // Never do location traversal for OnPrem
                    return LocalDataProvider;
                }
                else
                {
                    return await ResolveLocationDataAsync(locationAreaIdentifier, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <param name="currentProvider"></param>
        /// <returns></returns>
        private async Task<ILocationDataProvider> ResolveLocationDataAsync(
            Guid locationAreaIdentifier,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ILocationDataProvider locationData = null;
            ProviderCache providerLookup = m_providerLookup;

            if (providerLookup == null)
            {
                providerLookup = new ProviderCache();

                // Create and seed the cache with the local url
                String location = await LocalDataProvider.LocationForCurrentConnectionAsync(
                    ServiceInterfaces.LocationService2,
                    LocationServiceConstants.SelfReferenceIdentifier,
                    cancellationToken).ConfigureAwait(false);

                if (location != null)
                {
                    providerLookup.GetOrAdd(location, LocalDataProvider);
                }

                ProviderCache actualProvider = Interlocked.CompareExchange(ref m_providerLookup, providerLookup, null);

                // Did we lose the race? Pick the winner
                if (actualProvider != null)
                {
                    providerLookup = actualProvider;
                }
            }            

            if (!providerLookup.TryGetValue(locationAreaIdentifier, out locationData))
            {
                // First, check our current provider (see if a direct pointer is registered)
                String location = await LocalDataProvider.LocationForCurrentConnectionAsync(
                    ServiceInterfaces.LocationService2,
                    locationAreaIdentifier,
                    cancellationToken).ConfigureAwait(false);

                // Next, check and see if we have a root pointer
                if (location == null &&
                    locationAreaIdentifier != LocationServiceConstants.ApplicationIdentifier &&
                    locationAreaIdentifier != LocationServiceConstants.RootIdentifier) // Don't infinitely recurse
                {
                    ILocationDataProvider rootProvider = await ResolveLocationDataAsync(
                        LocationServiceConstants.RootIdentifier,
                        cancellationToken).ConfigureAwait(false);

                    if (rootProvider != null &&
                        !Object.ReferenceEquals(rootProvider, LocalDataProvider))
                    {
                        location = await rootProvider.LocationForCurrentConnectionAsync(
                            ServiceInterfaces.LocationService2,
                            locationAreaIdentifier,
                            cancellationToken).ConfigureAwait(false);
                    }
                }

                if (location != null)
                {
                    // The caller could be asking for a serviceIdentifier which resolves to a URL
                    // for which we already have a cached provider.
                    // This is typical when serviceIdentifier is a ResourceArea guid.
                    if (!providerLookup.TryGetValue(location, out locationData))
                    {
                        locationData = await CreateDataProviderAsync(location, cancellationToken).ConfigureAwait(false);
                        locationData = providerLookup.GetOrAdd(location, locationData);
                    }

                    providerLookup[locationAreaIdentifier] = locationData;
                }
            }

            return locationData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <returns></returns>
        public String GetLocationServiceUrl(
            Guid locationAreaIdentifier)
        {
            return GetLocationServiceUrlAsync(locationAreaIdentifier, null).SyncResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <param name="accessMappingMoniker"></param>
        /// <returns></returns>
        public String GetLocationServiceUrl(
            Guid locationAreaIdentifier,
            String accessMappingMoniker = null)
        {
            return GetLocationServiceUrlAsync(locationAreaIdentifier, accessMappingMoniker).SyncResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationAreaIdentifier"></param>
        /// <param name="accessMappingMoniker"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<String> GetLocationServiceUrlAsync(
            Guid locationAreaIdentifier,
            String accessMappingMoniker = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ILocationDataProvider locationData = await GetLocationDataAsync(locationAreaIdentifier, cancellationToken).ConfigureAwait(false);

            if (locationData == null)
            {
                return null;
            }

            AccessMapping accessMapping = await locationData.GetAccessMappingAsync(accessMappingMoniker ?? AccessMappingConstants.PublicAccessMappingMoniker).ConfigureAwait(false);

            if (accessMapping == null)
            {
                accessMapping = await locationData.GetClientAccessMappingAsync().ConfigureAwait(false);
            }

            return await locationData.LocationForAccessMappingAsync(
                ServiceInterfaces.LocationService2,
                LocationServiceConstants.SelfReferenceIdentifier,
                accessMapping,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="location"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task<ILocationDataProvider> CreateDataProviderAsync(
            String location,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            VssClientHttpRequestSettings locationServiceRequestSettings = VssClientHttpRequestSettings.Default.Clone();
            locationServiceRequestSettings.SendTimeout = TimeSpan.FromSeconds(30); // If not set here, the send timeout will use the default of 100 seconds, which is too long.
            VssConnection connection = new VssConnection(new Uri(location), m_connection.Credentials, locationServiceRequestSettings);
            IVssServerDataProvider dataProvider = connection.ServerDataProvider;

            // If this provider is connected, then we should make sure the remote provider
            // is also up-to-date
            if (m_connection.ServerDataProvider.HasConnected)
            {
                await dataProvider.ConnectAsync(ConnectOptions.None, cancellationToken).ConfigureAwait(false);
            }

            return dataProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual ILocationDataProvider LocalDataProvider
        {
            get
            {
                return m_connection.ServerDataProvider;
            }
        }

        private VssConnection m_connection;
        private ProviderCache m_providerLookup;

        private class ProviderCache
        {
            public Boolean TryGetValue(Guid locationAreaIdentfier, out ILocationDataProvider provider)
            {
                return m_guidCache.TryGetValue(locationAreaIdentfier, out provider);
            }

            public Boolean TryGetValue(String locationUrl, out ILocationDataProvider provider)
            {
                return m_urlCache.TryGetValue(NormalizeUrl(locationUrl), out provider);
            }

            public ILocationDataProvider GetOrAdd(String locationUrl, ILocationDataProvider provider)
            {
                return m_urlCache.GetOrAdd(NormalizeUrl(locationUrl), provider);
            }

            public ILocationDataProvider this[Guid locationAreaIdentifier]
            {
                get { return m_guidCache[locationAreaIdentifier]; }
                set { m_guidCache[locationAreaIdentifier] = value; }
            }

            private static String NormalizeUrl(String locationUrl)
            {
                return UriUtility.AppendSlashToPathIfNeeded(locationUrl);
            }

            private ConcurrentDictionary<Guid, ILocationDataProvider> m_guidCache = new ConcurrentDictionary<Guid, ILocationDataProvider>();
            private ConcurrentDictionary<String, ILocationDataProvider> m_urlCache = new ConcurrentDictionary<String, ILocationDataProvider>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
