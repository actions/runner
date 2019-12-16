using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Location.Client
{
    [ClientCircuitBreakerSettings(timeoutSeconds: 15, failurePercentage: 80)]
    [ClientCancellationTimeout(timeoutSeconds: 30)]
    public class LocationHttpClient : VssHttpClientBase
    {
        static LocationHttpClient()
        {
            s_translatedExceptions = new Dictionary<String, Type>();
            s_translatedExceptions.Add("ServiceDefinitionDoesNotExistException", typeof(ServiceDefinitionDoesNotExistException));
            s_translatedExceptions.Add("InvalidAccessPointException", typeof(InvalidAccessPointException));
            s_translatedExceptions.Add("InvalidServiceDefinitionException", typeof(InvalidServiceDefinitionException));
            s_translatedExceptions.Add("ParentDefinitionNotFoundException", typeof(ParentDefinitionNotFoundException));
            s_translatedExceptions.Add("CannotChangeParentDefinitionException", typeof(CannotChangeParentDefinitionException));
            s_translatedExceptions.Add("ActionDeniedBySubscriberException", typeof(ActionDeniedBySubscriberException));
        }

        public LocationHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public LocationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public LocationHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public LocationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public LocationHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public async Task<ConnectionData> GetConnectionDataAsync(ConnectOptions connectOptions, Int64 lastChangeId, CancellationToken cancellationToken = default(CancellationToken), Object userState = null)
        {
            using (new OperationScope(LocationResourceIds.LocationServiceArea, "GetConnectionData"))
            {
                var uri = new Uri(PathUtility.Combine(BaseAddress.GetLeftPart(UriPartial.Path), connectSubUrl));
                var uriBuilder = new UriBuilder(uri) { Query = BaseAddress.Query };

                var query = new List<KeyValuePair<String, String>>
                {
                    new KeyValuePair<String, String>("connectOptions", ((Int32)connectOptions).ToString(CultureInfo.InvariantCulture)),
                    new KeyValuePair<String, String>("lastChangeId", ((Int32)lastChangeId).ToString(CultureInfo.InvariantCulture)),
                    new KeyValuePair<String, String>("lastChangeId64", lastChangeId.ToString(CultureInfo.InvariantCulture))
                };

                uri = uriBuilder.Uri.AppendQuery(query);

                var message = new HttpRequestMessage(HttpMethod.Get, uri.ToString());
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                return await SendAsync<ConnectionData>(message, userState, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<ServiceDefinition> GetServiceDefinitionAsync(String serviceType, Guid identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetServiceDefinitionAsync(serviceType, identifier, allowFaultIn: true, previewFaultIn: false, cancellationToken: cancellationToken);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task<ServiceDefinition> GetServiceDefinitionAsync(String serviceType, Guid identifier, Boolean allowFaultIn, Boolean previewFaultIn, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LocationResourceIds.LocationServiceArea, "GetServiceDefinitions"))
            {
                List<KeyValuePair<String, String>> query = new List<KeyValuePair<String, String>>();

                if (!allowFaultIn)
                {
                    query.Add("allowFaultIn", Boolean.FalseString);
                }

                if (previewFaultIn)
                {
                    if (!allowFaultIn)
                    {
                        throw new InvalidOperationException("Cannot preview a service definition fault in if we do not allow the fault in.");
                    }

                    query.Add("previewFaultIn", Boolean.TrueString);
                }

                return await SendAsync<ServiceDefinition>(HttpMethod.Get, LocationResourceIds.ServiceDefinitions, new { serviceType = serviceType, identifier = identifier }, s_currentApiVersion, queryParameters: query, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Exceptions for location errors
        /// </summary>
        protected override IDictionary<String, Type> TranslatedExceptions
        {
            get
            {
                return s_translatedExceptions;
            }
        }

        private static Dictionary<String, Type> s_translatedExceptions;
        private const String connectSubUrl = "_apis/connectionData";
        protected static readonly ApiResourceVersion s_currentApiVersion = new ApiResourceVersion(1.0);
    }
}
