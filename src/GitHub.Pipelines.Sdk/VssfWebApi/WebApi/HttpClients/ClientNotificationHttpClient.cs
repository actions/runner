using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.ClientNotification.Client
{
    [ResourceArea(ClientNotificationResourceIds.AreaId)]
    [ClientCircuitBreakerSettings(timeoutSeconds: 10, failurePercentage: 80, MaxConcurrentRequests = 40)]
    [ClientCancellationTimeout(timeoutSeconds: 30)]
    public partial class ClientNotificationHttpClient : ClientNotificationHttpClientBase
    {
        protected ClientNotificationHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public ClientNotificationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public ClientNotificationHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public ClientNotificationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public ClientNotificationHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never), Obsolete("Use GetSubscriptionAsync instead.")]
        public virtual Task<ClientNotificationSubscription> RegisterNotificationAsync(
            ClientNotificationHttpContext context,
            CancellationToken cancellationToken = default,
            string id = ProfileRestApiConstants.Me,
            object userState = null)
        {
            return GetSubscriptionAsync(userState, cancellationToken);
        }
        
        public override Task<ClientNotificationSubscription> GetSubscriptionAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e037c69c-5ad1-4b26-b340-51c18035516f");
            
            // The id route parameter of the Subscriptions API was previously non-optional, so we still have to send it until the server is upgraded.
            object routeValues = new { id = "me" };

            return SendAsync<ClientNotificationSubscription>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("5.0-preview.2"),
                userState: userState,
                cancellationToken: cancellationToken,
                routeValues: routeValues);
        }

        protected override IDictionary<string, Type> TranslatedExceptions { get; } = new Dictionary<string, Type>()
        {
            {"NotAuthorizedException", typeof(NotAuthorizedException)},
        };
    }
}
