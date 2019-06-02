using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Location.Client
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

        public async Task UpdateServiceDefinitionsAsync(IEnumerable<ServiceDefinition> definitions, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LocationResourceIds.LocationServiceArea, "UpdateServiceDefinitions"))
            {
                ArgumentUtility.CheckEnumerableForNullOrEmpty(definitions, "definitions");

                HttpContent content = new ObjectContent<VssJsonCollectionWrapper<IEnumerable<ServiceDefinition>>>(new VssJsonCollectionWrapper<IEnumerable<ServiceDefinition>>(definitions), base.Formatter);
                await SendAsync(new HttpMethod("PATCH"), LocationResourceIds.ServiceDefinitions, null, s_currentApiVersion, content, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<HttpResponseMessage> DeleteServiceDefinitionAsync(String serviceType, Guid identifier, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LocationResourceIds.LocationServiceArea, "DeleteServiceDefinitions"))
            {
                return await SendAsync<HttpResponseMessage>(HttpMethod.Delete, LocationResourceIds.ServiceDefinitions, new { serviceType = serviceType, identifier = identifier }, s_currentApiVersion, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ServiceDefinition>> GetServiceDefinitionsAsync()
        {
            using (new OperationScope(LocationResourceIds.LocationServiceArea, "GetServiceDefinitions"))
            {
                return await SendAsync<IEnumerable<ServiceDefinition>>(HttpMethod.Get, LocationResourceIds.ServiceDefinitions, null, s_currentApiVersion).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ServiceDefinition>> GetServiceDefinitionsAsync(String serviceType)
        {
            using (new OperationScope(LocationResourceIds.LocationServiceArea, "GetServiceDefinitions"))
            {
                return await SendAsync<IEnumerable<ServiceDefinition>>(HttpMethod.Get, LocationResourceIds.ServiceDefinitions, new { serviceType = serviceType }, s_currentApiVersion).ConfigureAwait(false);
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

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async Task<HttpResponseMessage> FlushSpsServiceDefinitionAsync(Guid hostId, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Used when migrating an SPS host to update all registered service definitions across other VSO instances.
            using (new OperationScope(LocationResourceIds.LocationServiceArea, "FlushSpsServiceDefinition"))
            {
                return await SendAsync(HttpMethod.Put, LocationResourceIds.SpsServiceDefinition, new { hostId = hostId }, s_currentApiVersion, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ResourceAreaInfo>> GetResourceAreasAsync()
        {
            using (new OperationScope(LocationResourceIds.LocationServiceArea, "GetResourceAreas"))
            {
                return await SendAsync<IEnumerable<ResourceAreaInfo>>(HttpMethod.Get, LocationResourceIds.ResourceAreas, null, new ApiResourceVersion("3.2-preview.1")).ConfigureAwait(false);
            }
        }

        public async Task<ResourceAreaInfo> GetResourceAreaAsync(Guid areaId)
        {
            using (new OperationScope(LocationResourceIds.LocationServiceArea, "GetResourceAreas"))
            {
                return await SendAsync<ResourceAreaInfo>(HttpMethod.Get, LocationResourceIds.ResourceAreas, new { areaId = areaId } , new ApiResourceVersion("3.2-preview.1")).ConfigureAwait(false);
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
