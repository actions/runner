using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Utilities;

namespace Sdk.WebApi.WebApi.RawClient
{
    public class RawConnection : IDisposable
    {
        public RawConnection(
            Uri baseUrl,
            VssOAuthCredential credentials,
            RawClientHttpRequestSettings settings)
            : this(baseUrl, new RawHttpMessageHandler(credentials, settings), null)
        {
        }

        public RawConnection(
            Uri baseUrl,
            RawHttpMessageHandler innerHandler,
            IEnumerable<DelegatingHandler> delegatingHandlers)
        {
            ArgumentUtility.CheckForNull(baseUrl, "baseUrl");
            ArgumentUtility.CheckForNull(innerHandler, "innerHandler");

            // Permit delegatingHandlers to be null
            m_delegatingHandlers = delegatingHandlers = delegatingHandlers ?? Enumerable.Empty<DelegatingHandler>();

            m_baseUrl = baseUrl;
            m_innerHandler = innerHandler;

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
        }

        /// <summary>
        ///
        /// </summary>
        public RawClientHttpRequestSettings Settings
        {
            get
            {
                return (RawClientHttpRequestSettings)m_innerHandler.Settings;
            }
        }

        public async Task<T> GetClientAsync<T>(CancellationToken cancellationToken = default(CancellationToken)) where T : RawHttpClientBase
        {
            CheckForDisposed();
            Type clientType = typeof(T);

            return (T)await GetClientServiceImplAsync(typeof(T), cancellationToken).ConfigureAwait(false);
        }

        private async Task<Object> GetClientServiceImplAsync(
            Type requestedType,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            CheckForDisposed();
            Object requestedObject = null;

            // Get the actual type to lookup or instantiate, which will either be requestedType itself
            // or an extensible type if one was registered
            Type managedType = GetExtensibleType(requestedType);

            if (!m_cachedTypes.TryGetValue(managedType, out requestedObject))
            {
                AsyncLock typeLock = m_loadingTypes.GetOrAdd(managedType, (t) => new AsyncLock());

                // This ensures only a single thread at a time will be performing the work to initialize this particular type
                // The other threads will go async awaiting the lock task. This is still an improvement over the old synchronous locking,
                // as this thread won't be blocked (like a Monitor.Enter), but can return a task to the caller so that the thread
                // can continue to be used to do useful work while the result is being worked on.
                // We are trusting that getInstanceAsync does not have any code paths that lead back here (for the same type), otherwise we can deadlock on ourselves.
                // The old code also extended the same trust which (if violated) would've resulted in a StackOverflowException,
                // but with async tasks it will lead to a deadlock.
                using (await typeLock.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (!m_cachedTypes.TryGetValue(managedType, out requestedObject))
                    {
                        requestedObject = (RawHttpClientBase)Activator.CreateInstance(managedType, m_baseUrl, m_pipeline, false /* disposeHandler */);
                        m_cachedTypes[managedType] = requestedObject;

                        AsyncLock removed;
                        m_loadingTypes.TryRemove(managedType, out removed);
                    }
                }
            }

            return requestedObject;
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

        private bool m_isDisposed = false;
        private object m_disposeLock = new object();
        private readonly ConcurrentDictionary<String, Type> m_extensibleServiceTypes = new ConcurrentDictionary<String, Type>();
        private readonly Uri m_baseUrl;
        private readonly HttpMessageHandler m_pipeline;
        private readonly IEnumerable<DelegatingHandler> m_delegatingHandlers;
        private readonly RawHttpMessageHandler m_innerHandler;
        private readonly ConcurrentDictionary<Type, AsyncLock> m_loadingTypes = new ConcurrentDictionary<Type, AsyncLock>();
        private readonly ConcurrentDictionary<Type, Object> m_cachedTypes = new ConcurrentDictionary<Type, Object>();
    }
}
