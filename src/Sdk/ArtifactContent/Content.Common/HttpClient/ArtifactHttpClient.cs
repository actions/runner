using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Content.Common.Tracing;
using GitHub.Services.WebApi;
using System.Net;
using static ServicePointExtensions;

namespace GitHub.Services.Content.Common
{
    public abstract class ArtifactHttpClient : VssHttpClientBase, IArtifactHttpClient
    {
        public ArtifactHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
            UpdateServicePointSettings();
        }

        public ArtifactHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
            UpdateServicePointSettings();
        }

        public ArtifactHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
            UpdateServicePointSettings();
        }

        public ArtifactHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
            UpdateServicePointSettings();
        }

        public ArtifactHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
            UpdateServicePointSettings();
        }

        public abstract Guid ResourceId { get; }
        
        public Task GetOptionsAsync(CancellationToken cancellationToken)
        {
            // Note that before the parameter name was specified, cancellationToken was being passed as 3rd parameter "[object routeValues = null]"
            return SendAsync(method: HttpMethod.Options, locationId: this.ResourceId, cancellationToken: cancellationToken);
        }

        protected IAppTraceSource maybeTracer;
        protected IAppTraceSource tracer => maybeTracer ?? NoopAppTraceSource.Instance;

        public void SetTracer(IAppTraceSource tracer)
        {
            if (this.maybeTracer != null)
            {
                throw new InvalidOperationException($"{nameof(SetTracer)} was already called earlier. In order to preserve thread safety it cannot be changed.");
            }

            this.maybeTracer = tracer;
        }
        protected override bool ShouldThrowError(HttpResponseMessage response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.Found:
                case HttpStatusCode.SeeOther:
                    return false;
                default:
                    return base.ShouldThrowError(response);
            }
        }

        protected virtual ServicePointConfig GetServicePointSettings()
        {
            return new ServicePointConfig
            {
                MaxConnectionsPerProcessor = 32,
                ConnectionLeaseTimeout = null, // BlobStore2HttpClient overrides to 3 minutes
                Expect100Continue = false,
                UseNagleAlgorithm = false,
                TcpKeepAlive = new ServicePointConfigKeepAlive
                {
                    KeepAliveTime = TimeSpan.FromSeconds(30), // SymbolHttpClient overrides to 60s
                    KeepAliveInterval = TimeSpan.FromSeconds(5)
                }
            };
        }

        /// <remarks>
        /// Previously called from constructors in BlobStore2HttpClient, DedupStoreHttpClient, ItemHttpClientBase, SymbolHttpClient
        /// </remarks>
        private void UpdateServicePointSettings()
        {
            if (this.BaseAddress != null)
            {
                var servicePoint = ServicePointManager.FindServicePoint(this.BaseAddress);
                var settings = GetServicePointSettings();
                servicePoint.UpdateServicePointSettings(settings);
            }
        }
    }
}
