using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Location;
using GitHub.Services.WebApi.Internal;
using GitHub.Services.WebApi.Location;
using GitHub.Services.WebApi.Utilities;

namespace GitHub.Services.WebApi
{
    public class VssConnection : IDisposable
    {
        public VssConnection(
            Uri baseUrl,
            VssCredentials credentials)
            : this(baseUrl, credentials, VssClientHttpRequestSettings.Default.Clone())
        {
        }

        public VssConnection(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : this(baseUrl, new VssHttpMessageHandler(credentials, settings), null)
        {
        }

        public VssConnection(
            Uri baseUrl,
            VssHttpMessageHandler innerHandler,
            IEnumerable<DelegatingHandler> delegatingHandlers)
            : this(baseUrl, innerHandler, delegatingHandlers, true)
        {
        }

        private VssConnection(
            Uri baseUrl,
            VssHttpMessageHandler innerHandler,
            IEnumerable<DelegatingHandler> delegatingHandlers,
            Boolean allowUnattributedClients)
        {
            ArgumentUtility.CheckForNull(baseUrl, "baseUrl");
            ArgumentUtility.CheckForNull(innerHandler, "innerHandler");

            // Permit delegatingHandlers to be null
            m_delegatingHandlers = delegatingHandlers = delegatingHandlers ?? Enumerable.Empty<DelegatingHandler>();

            m_baseUrl = baseUrl;
            m_innerHandler = innerHandler;
            m_allowUnattributedClients = allowUnattributedClients;

            // Do we need to add a retry handler to the pipeline? If so, it needs to come last.
            if (this.Settings.MaxRetryRequest > 0)
            {
                delegatingHandlers = delegatingHandlers.Concat(new DelegatingHandler[] { new VssHttpRetryMessageHandler(this.Settings.MaxRetryRequest) });
            }

            // Create and persist the pipeline.
            if (delegatingHandlers.Any())
            {
                m_pipeline = HttpClientFactory.CreatePipeline(m_innerHandler, delegatingHandlers);
            }
            else
            {
                m_pipeline = m_innerHandler;
            }

            m_serverDataProvider = new VssServerDataProvider(this, m_pipeline, m_baseUrl.AbsoluteUri);

            if (innerHandler.Credentials != null)
            {
                // store base url on credentials, as it is required when creating a token storage key.
                if (innerHandler.Credentials.Federated != null)
                {
                    innerHandler.Credentials.Federated.TokenStorageUrl = baseUrl;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Task ConnectAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return ConnectAsync(VssConnectMode.Automatic, null, cancellationToken);
        }

        public Task ConnectAsync(
            VssConnectMode connectMode,
            IDictionary<String, String> parameters,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckForDisposed();
            // Set the connectMode on the credential's FederatedPrompt
            if (Credentials.Federated != null && Credentials.Federated.Prompt != null)
            {
                if (parameters != null)
                {
                    // Create a copy of the parameters if any were supplied.
                    parameters = new Dictionary<String, String>(parameters);
                }
                else
                {
                    parameters = new Dictionary<String, String>();
                }

                IVssCredentialPrompt promptToSetParametersOn;

                // prompt can be a VssCredentialPrompts with VssFederatedCredentialPrompt inside it
                IVssCredentialPrompts credentialPrompts = Credentials.Federated.Prompt as IVssCredentialPrompts;
                if (credentialPrompts != null && credentialPrompts.FederatedPrompt != null)
                {
                    // IVssCredentialPrompts contains an inner federatedPrompt, then set the paramaters on the inner one
                    promptToSetParametersOn = credentialPrompts.FederatedPrompt;
                }
                else
                {
                    promptToSetParametersOn = Credentials.Federated.Prompt;
                }

                parameters[VssConnectionParameterKeys.VssConnectionMode] = connectMode.ToString();
                promptToSetParametersOn.Parameters = parameters;
            }

            return ServerDataProvider.ConnectAsync(ConnectOptions.None, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (HasAuthenticated)
                {
                    m_innerHandler.Credentials.SignOut(Uri, null, null);
                }
            }
            finally
            {
                ServerDataProvider.DisconnectAsync().SyncResult();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetService<T>() where T : IVssClientService
        {
            return (T)GetClientServiceImplAsync(typeof(T), Guid.Empty, GetServiceInstanceAsync).SyncResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> GetServiceAsync<T>(CancellationToken cancellationToken = default(CancellationToken)) where T : IVssClientService
        {
            return (T)await GetClientServiceImplAsync(typeof(T), Guid.Empty, GetServiceInstanceAsync, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves an HTTP client of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of client to retrieve</typeparam>
        /// <returns>The client of the specified type</returns>
        public T GetClient<T>() where T : VssHttpClientBase
        {
            return GetClientAsync<T>().SyncResult();
        }

        /// <summary>
        /// Retrieves an HTTP client of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of client to retrieve</typeparam>
        /// <returns>The client of the specified type</returns>
        public async Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default(CancellationToken)) where T : VssHttpClientBase
        {
            CheckForDisposed();
            Type clientType = typeof(T);
            Guid serviceIdentifier = GetServiceIdentifier(clientType);

            if (serviceIdentifier == Guid.Empty && !m_allowUnattributedClients)
            {
                throw new CannotGetUnattributedClientException(clientType);
            }

            return (T)await GetClientServiceImplAsync(typeof(T), serviceIdentifier, GetClientInstanceAsync, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestedType"></param>
        /// <param name="getInstanceAsync"></param>
        /// <returns></returns>
        private async Task<Object> GetClientServiceImplAsync(
            Type requestedType,
            Guid serviceIdentifier,
            Func<Type, Guid, CancellationToken, Task<Object>> getInstanceAsync,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckForDisposed();
            Object requestedObject = null;

            // Get the actual type to lookup or instantiate, which will either be requestedType itself
            // or an extensible type if one was registered
            Type managedType = GetExtensibleType(requestedType);

            ClientCacheKey cacheKey = new ClientCacheKey(managedType, serviceIdentifier);

            // First check if we have this type already constructed
            if (!m_cachedTypes.TryGetValue(cacheKey, out requestedObject))
            {
                AsyncLock typeLock = m_loadingTypes.GetOrAdd(cacheKey, (t) => new AsyncLock());

                // This ensures only a single thread at a time will be performing the work to initialize this particular type
                // The other threads will go async awaiting the lock task. This is still an improvement over the old synchronous locking,
                // as this thread won't be blocked (like a Monitor.Enter), but can return a task to the caller so that the thread
                // can continue to be used to do useful work while the result is being worked on.
                // We are trusting that getInstanceAsync does not have any code paths that lead back here (for the same type), otherwise we can deadlock on ourselves.
                // The old code also extended the same trust which (if violated) would've resulted in a StackOverflowException,
                // but with async tasks it will lead to a deadlock.
                using (await typeLock.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!m_cachedTypes.TryGetValue(cacheKey, out requestedObject))
                    {
                        requestedObject = await getInstanceAsync(managedType, serviceIdentifier, cancellationToken).ConfigureAwait(false);
                        m_cachedTypes[cacheKey] = requestedObject;

                        AsyncLock removed;
                        m_loadingTypes.TryRemove(cacheKey, out removed);
                    }
                }
            }

            return requestedObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="managedType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task<Object> GetClientInstanceAsync(
            Type managedType,
            Guid serviceIdentifier,
            CancellationToken cancellationToken)
        {
            return GetClientInstanceAsync(managedType, serviceIdentifier, cancellationToken, null, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="managedType"></param>
        /// <returns></returns>
        private async Task<Object> GetClientInstanceAsync(
            Type managedType,
            Guid serviceIdentifier,
            CancellationToken cancellationToken,
            VssHttpRequestSettings settings,
            DelegatingHandler[] handlers)
        {
            CheckForDisposed();
            ILocationService locationService = await GetServiceAsync<ILocationService>(cancellationToken).ConfigureAwait(false);
            ILocationDataProvider locationData = await locationService.GetLocationDataAsync(serviceIdentifier, cancellationToken).ConfigureAwait(false);

            if (locationData == null)
            {
                throw new VssServiceException(WebApiResources.ServerDataProviderNotFound(serviceIdentifier));
            }

            String serviceLocationString = await locationData.LocationForCurrentConnectionAsync(
                ServiceInterfaces.LocationService2,
                LocationServiceConstants.SelfReferenceIdentifier,
                cancellationToken).ConfigureAwait(false);

            // This won't ever be null because of compat code in ServerDataProvider
            Uri clientBaseUri = new Uri(serviceLocationString);

            VssHttpClientBase toReturn = null;

            if (settings != null)
            {
                toReturn = (VssHttpClientBase)Activator.CreateInstance(managedType, clientBaseUri, Credentials, settings, handlers);
            }
            else
            {
                toReturn = (VssHttpClientBase)Activator.CreateInstance(managedType, clientBaseUri, m_pipeline, false /* disposeHandler */);
            }

            ApiResourceLocationCollection resourceLocations = await locationData.GetResourceLocationsAsync(cancellationToken).ConfigureAwait(false);
            toReturn.SetResourceLocations(resourceLocations);

            return toReturn;
        }

        /// <summary>
        /// Gets the service and fallback identifiers from the [ResourceArea] attribute of the specified type
        /// </summary>
        private Guid GetServiceIdentifier(
            Type requestedType)
        {
            ResourceAreaAttribute[] attributes = (ResourceAreaAttribute[])requestedType.GetTypeInfo().GetCustomAttributes<ResourceAreaAttribute>(true);

            if (attributes.Length > 0)
            {
                return attributes[0].AreaId;
            }
            else
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="managedType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Task<Object> GetServiceInstanceAsync(
            Type managedType,
            Guid serviceIdentifier,
            CancellationToken cancellationToken)
        {
            CheckForDisposed();
            IVssClientService clientService;

            try
            {
                // Create our instance of the managed service object.
                clientService = (IVssClientService)Activator.CreateInstance(managedType);
            }
            catch (MissingMemberException ex)
            {
                throw new ArgumentException(WebApiResources.GetServiceArgumentError(managedType), ex);
            }


            //  We successfully created an object, initialize him and finally set the
            //  return value.
            clientService.Initialize(this);

            return Task.FromResult<Object>(clientService);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="managedType"></param>
        /// <returns></returns>
        private Type GetExtensibleType(Type managedType)
        {
            if (managedType.GetTypeInfo().IsAbstract || managedType.GetTypeInfo().IsInterface)
            {
                Type extensibleType = null;

                // We can add extensible type registration for the client later (app.config? windows registry?). For now it is based solely on the attribute
                if (!m_extensibleServiceTypes.TryGetValue(managedType.Name, out extensibleType))
                {
                    VssClientServiceImplementationAttribute[] attributes = (VssClientServiceImplementationAttribute[])managedType.GetTypeInfo().GetCustomAttributes<VssClientServiceImplementationAttribute>(true);
                    if (attributes.Length > 0)
                    {
                        if (attributes[0].Type != null)
                        {
                            extensibleType = attributes[0].Type;
                            m_extensibleServiceTypes[managedType.Name] = extensibleType;
                        }
                        else if (!String.IsNullOrEmpty(attributes[0].TypeName))
                        {
                            extensibleType = Type.GetType(attributes[0].TypeName);

                            if (extensibleType != null)
                            {
                                m_extensibleServiceTypes[managedType.Name] = extensibleType;
                            }
                            else
                            {
                                Debug.Assert(false, "VssConnection: Could not load type from type name: " + attributes[0].TypeName);
                            }
                        }
                    }
                }

                if (extensibleType == null)
                {
                    throw new ExtensibleServiceTypeNotRegisteredException(managedType);
                }

                if (!managedType.GetTypeInfo().IsAssignableFrom(extensibleType.GetTypeInfo()))
                {
                    throw new ExtensibleServiceTypeNotValidException(managedType, extensibleType);
                }

                return extensibleType;
            }
            else
            {
                return managedType;
            }
        }

        /// <summary>
        /// Used for Testing Only
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="type"></param>
        internal void RegisterExtensibleType(
            String typeName,
            Type type)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(typeName, "typeName");
            ArgumentUtility.CheckForNull(type, "type");

            m_extensibleServiceTypes[typeName] = type;
        }

        private bool m_isDisposed = false;
        private object m_disposeLock = new object();

        public void Dispose()
        {
            if (!m_isDisposed)
            {
                lock (m_disposeLock)
                {
                    if (!m_isDisposed)
                    {
                        m_isDisposed = true;
                        foreach (var cachedType in m_cachedTypes.Values.Where(v => v is IDisposable).Select(v => v as IDisposable))
                        {
                            cachedType.Dispose();
                        }
                        m_cachedTypes.Clear();
                        Disconnect();
                        if (m_parentConnection != null)
                        {
                            m_parentConnection.Dispose();
                            m_parentConnection = null;
                        }
                    }
                }
            }
        }

        private void CheckForDisposed()
        {
            if (m_isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Uri Uri
        {
            get
            {
                return m_baseUrl;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public VssHttpMessageHandler InnerHandler
        {
            get
            {
                return m_innerHandler;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<DelegatingHandler> DelegatingHandlers
        {
            get
            {
                return m_delegatingHandlers;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public VssCredentials Credentials
        {
            get
            {
                return m_innerHandler.Credentials;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public VssClientHttpRequestSettings Settings
        {
            get
            {
                return (VssClientHttpRequestSettings)m_innerHandler.Settings;
            }
        }

        /// <summary>
        /// The Guid that identifies the server associated with the <c>VssConnection</c>.
        /// </summary>
        public Guid ServerId
        {
            get
            {
                return ServerDataProvider.GetInstanceIdAsync().SyncResult();
            }
        }

        /// <summary>
        /// The Guid that identifies the type of server associated with the <c>VssConnection</c>
        /// </summary>
        public Guid ServerType
        {
            get
            {
                return ServerDataProvider.GetInstanceTypeAsync().SyncResult();
            }
        }

        /// <summary>
        /// The Id of the identity who the calls to the server are being made for.
        /// </summary>
        public Identity.Identity AuthorizedIdentity
        {
            get
            {
                return ServerDataProvider.GetAuthorizedIdentityAsync().SyncResult();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Identity.Identity AuthenticatedIdentity
        {
            get
            {
                return ServerDataProvider.GetAuthenticatedIdentityAsync().SyncResult();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Boolean HasAuthenticated
        {
            get
            {
                return ServerDataProvider.HasConnected;
            }
        }

        /// <summary>
        /// The connection to the parent host for this VSS connection. If this connection is to a collection host,
        /// then this property will return a connection to the account/tenant host.
        /// The property will return null if a parent cannot be located for the current connection.
        /// </summary>
        public VssConnection ParentConnection
        {
            get
            {
                CheckForDisposed();
                if (m_parentConnection == null)
                {
                    lock (m_parentConnectionLock)
                    {
                        ILocationService locationService = GetService<ILocationService>();
                        ILocationDataProvider locationData = locationService.GetLocationData(Guid.Empty);

                        String applicationLocation = locationData.LocationForCurrentConnection(
                            ServiceInterfaces.LocationService2,
                            LocationServiceConstants.ApplicationIdentifier);

                        if (String.IsNullOrEmpty(applicationLocation))
                        {
                            throw new VssServiceException(WebApiResources.ServerDataProviderNotFound(LocationServiceConstants.ApplicationIdentifier));
                        }

                        m_parentConnection = new VssConnection(
                            new Uri(applicationLocation),
                            new VssHttpMessageHandler(Credentials, VssClientHttpRequestSettings.Default.Clone()),
                            null,
                            allowUnattributedClients: false);
                    }
                }

                return m_parentConnection;
            }
        }

        /// <summary>
        /// Used for testing. Do not use for product code.
        /// </summary>
        internal IVssServerDataProvider ServerDataProvider
        {
            get
            {
                return m_serverDataProvider;
            }
            set
            {
                // Used for testing
                m_serverDataProvider = value;
            }
        }

        private IVssServerDataProvider m_serverDataProvider;
        private VssConnection m_parentConnection;
        private Object m_parentConnectionLock = new Object();

        private readonly Uri m_baseUrl;
        private readonly HttpMessageHandler m_pipeline;
        private readonly VssHttpMessageHandler m_innerHandler;
        private readonly IEnumerable<DelegatingHandler> m_delegatingHandlers;
        private readonly Boolean m_allowUnattributedClients;

        private readonly ConcurrentDictionary<ClientCacheKey, AsyncLock> m_loadingTypes = new ConcurrentDictionary<ClientCacheKey, AsyncLock>(ClientCacheKey.Comparer);
        private readonly ConcurrentDictionary<ClientCacheKey, Object> m_cachedTypes = new ConcurrentDictionary<ClientCacheKey, Object>(ClientCacheKey.Comparer);
        private readonly ConcurrentDictionary<String, Type> m_extensibleServiceTypes = new ConcurrentDictionary<String, Type>();

        private struct ClientCacheKey
        {
            public ClientCacheKey(Type type, Guid serviceIdentifier)
            {
                this.Type = type;
                this.ServiceIdentifier = serviceIdentifier;
            }

            public readonly Type Type;
            public readonly Guid ServiceIdentifier;

            public static readonly IEqualityComparer<ClientCacheKey> Comparer = new ClientCacheKeyComparer();

            private class ClientCacheKeyComparer : IEqualityComparer<ClientCacheKey>
            {
                public bool Equals(ClientCacheKey x, ClientCacheKey y)
                {
                    return x.Type.Equals(y.Type) &&
                           x.ServiceIdentifier.Equals(y.ServiceIdentifier);
                }

                public int GetHashCode(ClientCacheKey obj)
                {
                    return obj.Type.GetHashCode() ^ obj.ServiceIdentifier.GetHashCode();
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IVssClientService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        void Initialize(VssConnection connection);
    }

    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "FxCop can't tell that we have an accessor.")]
    public sealed class VssClientServiceImplementationAttribute : Attribute
    {
        public VssClientServiceImplementationAttribute(Type type)
        {
            this.Type = type;
        }

        public VssClientServiceImplementationAttribute(String typeName)
        {
            this.TypeName = typeName;
        }

        public Type Type
        {
            get;
            set;
        }

        public String TypeName
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [ExceptionMapping("0.0", "3.0", "ExtensibleServiceTypeNotRegisteredException", "GitHub.Services.Client.ExtensibleServiceTypeNotRegisteredException, GitHub.Services.Client, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ExtensibleServiceTypeNotRegisteredException : VssException
    {
        public ExtensibleServiceTypeNotRegisteredException(Type managedType)
            : base(WebApiResources.ExtensibleServiceTypeNotRegistered(managedType.Name))
        {
        }

        public ExtensibleServiceTypeNotRegisteredException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [ExceptionMapping("0.0", "3.0", "ExtensibleServiceTypeNotValidException", "GitHub.Services.Client.ExtensibleServiceTypeNotValidException, GitHub.Services.Client, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ExtensibleServiceTypeNotValidException : VssException
    {
        public ExtensibleServiceTypeNotValidException(Type managedType, Type extensibleType)
            : base(WebApiResources.ExtensibleServiceTypeNotValid(managedType.Name, extensibleType.Name))
        {
        }

        public ExtensibleServiceTypeNotValidException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class CannotGetUnattributedClientException : VssException
    {
        public CannotGetUnattributedClientException(Type clientType)
            : base(WebApiResources.CannotGetUnattributedClient(clientType.Name))
        {
        }

        public CannotGetUnattributedClientException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
