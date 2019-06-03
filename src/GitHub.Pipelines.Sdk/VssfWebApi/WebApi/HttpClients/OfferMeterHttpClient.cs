using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Commerce.Client
{
    /// <summary>
    /// Class that represents methods communicating with the platform service via REST controller.
    /// </summary>
    [ResourceArea(CommerceResourceIds.AreaId)]
    [ClientCircuitBreakerSettings(timeoutSeconds: 30, failurePercentage: 80, MaxConcurrentRequests = 40)]
    [ClientCancellationTimeout(60)]
    public class OfferMeterHttpClient : VssHttpClientBase
    {
        #region Constructors

        public OfferMeterHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

        public OfferMeterHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public OfferMeterHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public OfferMeterHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public OfferMeterHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion

        /// <summary>
        /// Returns detailed facts about specified resource.
        /// </summary>
        /// <param name="resourceName">Unique name of the resource</param>
        /// <returns>Resource Information</returns>
        public virtual async Task<IOfferMeter> GetMeterFromGalleryId(string resourceName, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetOfferMeter"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>
                {
                    { "resourceNameResolveMethod", "GalleryId" }
                };

                return await SendAsync<OfferMeter>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.OfferMeterLocationId,
                    routeValues: new { resourceName = resourceName },
                    queryParameters: queryParameters,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.OfferMeterV1Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns detailed facts about specified resource ( meterName)
        /// </summary>
        /// <param name="resourceName">Unique name of the resource</param>
        /// <returns>Resource Information</returns>
        public virtual async Task<IOfferMeter> GetMeterFromMeterName(string resourceName, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetOfferMeter"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("resourceNameResolveMethod", "MeterName");

                return await SendAsync<OfferMeter>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.OfferMeterLocationId,
                    routeValues: new { resourceName = resourceName },
                    queryParameters: queryParameters,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.OfferMeterV1Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns detailed facts about all resources
        /// </summary>
        /// <returns>Enumerable of Resource Information</returns>
        public virtual async Task<List<OfferMeter>> GetMeters(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetOfferMeters"))
            {
                return await SendAsync<List<OfferMeter>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.OfferMeterLocationId,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.OfferMeterV1Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Create offer meter definition entry in the table
        /// <param name="offerMeter"></param>
        /// </summary>

        public virtual async Task CreateOfferMeterDefinition(IOfferMeter meterConfig, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "CreateOfferMeterDefinition"))
            {
                HttpContent content = new ObjectContent<IOfferMeter>(meterConfig, new VssJsonMediaTypeFormatter(true));
                var message = await CreateRequestMessageAsync(
                    method: HttpMethod.Post,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    locationId: CommerceResourceIds.OfferMeterLocationId,                    
                    userState: userState,
                    content: content,
                    cancellationToken: cancellationToken).ConfigureAwait(false);               

                await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public virtual async Task<PurchasableOfferMeter> GetPurchasableOfferMeter(string resourceName, string resourceNameResolveMethod, Guid? subscriptionId, bool includeMeterPricing, string offerCode = null, Guid? tenantId = null, Guid? objectId = null, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetPurchasableOfferMeter"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>
                {
                    { "resourceName", resourceName },
                    { "resourceNameResolveMethod", resourceNameResolveMethod },
                    { "subscriptionId", subscriptionId },
                    { "includeMeterPricing", includeMeterPricing },
                    { "offerCode", offerCode },
                    { "tenantId", tenantId },
                    { "objectId", objectId }
                };

                return await this.SendAsync<PurchasableOfferMeter>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.OfferMeterLocationId,
                    queryParameters: queryParameters,
                    routeValues: new { resourceName = resourceName, resourceNameResolveMethod = resourceNameResolveMethod, subscriptionId = subscriptionId, includeMeterPricing = includeMeterPricing, offerCode = offerCode, tenantId = tenantId, objectId = objectId },
                    userState: null,
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Overridable for test purposes
        /// </summary>
        [ExcludeFromCodeCoverage]
        public new virtual async Task<T> SendAsync<T>(HttpMethod method, Guid locationId, Object routeValues = null, ApiResourceVersion version = null, HttpContent content = null, IEnumerable<KeyValuePair<String, String>> queryParameters = null, Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.SendAsync<T>(method, locationId, routeValues, version, content, queryParameters, userState, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create an HTTP request message for the given location, replacing parameters in the location's route template
        /// with values in the supplied routeValues dictionary.
        /// </summary>
        /// <param name="method">HTTP verb to use</param>
        /// <param name="locationId">Id of the location to use</param>
        /// <param name="routeValues">Values to use to replace parameters in the location's route tempalte</param>
        /// <param name="version">Version to send in the request or null to use the VSS latest API version</param>
        /// <returns>HttpRequestMessage</returns>
        internal virtual async Task<HttpRequestMessage> CreateRequestMessageAsync(HttpMethod method, Guid locationId, Object routeValues = null, ApiResourceVersion version = null, HttpContent content = null, List<KeyValuePair<String, String>> queryParameters = null, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.CreateRequestMessageAsync(method, locationId, routeValues, version, content, queryParameters, userState, cancellationToken).ConfigureAwait(false);
        }

        [ExcludeFromCodeCoverage]
        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get { return s_translatedExceptions; }
        }

        protected static readonly Version previewApiVersion = new Version(2, 0);

        internal static readonly Dictionary<string, Type> s_translatedExceptions = new Dictionary<string, Type>
        {
            // 400 - Bad Request    
            {"InvalidResourceException", typeof(InvalidResourceException)},

            // 401 - Unauthorized
            {"CommerceSecurityException", typeof(CommerceSecurityException)},
        };
    }
}
