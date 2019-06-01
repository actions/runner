using Microsoft.VisualStudio.Services.Account;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Commerce.Client
{
    /// <summary>
    /// Class that represents methods communicating with the platform service via REST controller.
    /// </summary>
    [ResourceArea(CommerceResourceIds.AreaId)]
    [ClientCircuitBreakerSettings(timeoutSeconds: 15, failurePercentage: 80, MaxConcurrentRequests = 30)]
    [ClientCancellationTimeout(30)]
    public class OfferMeterPriceHttpClient : VssHttpClientBase
    {
        #region Constructors

        public OfferMeterPriceHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

        public OfferMeterPriceHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public OfferMeterPriceHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public OfferMeterPriceHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public OfferMeterPriceHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }
        #endregion

        /// <summary>
        /// Return IOfferMeterPrice for each region available
        /// </summary>
        /// <returns>Enumerable IOfferMeterPrice for each region available</returns>
        public virtual async Task<IList<OfferMeterPrice>> GetOfferMeterPrice(string galleryId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetOfferMeterPrice"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("galleryId", galleryId);

                return await SendAsync<IList<OfferMeterPrice>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.OfferMeterPriceLocationId,
                    queryParameters: queryParameters,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.OfferMeterPriceV1Resources),
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

        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get { return s_translatedExceptions; }
        }

        protected static readonly Version previewApiVersion = new Version(2, 0);
        /// <summary>
        /// The translated exceptions
        /// </summary>
        internal static readonly Dictionary<string, Type> s_translatedExceptions = new Dictionary<string, Type>
        {
            // 400 - Bad Request    
            {"InvalidResourceException", typeof(InvalidResourceException)},

            // 401 - Unauthorized
            {"CommerceSecurityException", typeof(CommerceSecurityException)},

            // 404 - Not found
            {"AccountNotFoundException", typeof(AccountNotFoundException)},
        };
    }
}
