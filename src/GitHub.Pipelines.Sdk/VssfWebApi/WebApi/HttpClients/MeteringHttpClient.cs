using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Account;
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
    public class MeteringHttpClient : VssHttpClientBase
    {
        #region Constructors

        public MeteringHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

        public MeteringHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public MeteringHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public MeteringHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public MeteringHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion

        /// <summary>
        /// Returns detailed facts about specified resource.
        /// </summary>
        /// <param name="resourceName">Unique name of the resource</param>
        /// <returns>Resource Information</returns>
        public virtual async Task<ISubscriptionResource> GetResourceStatus(ResourceName? resourceName, bool nextBillingPeriod, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetResourceStatus"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("nextBillingPeriod", nextBillingPeriod.ToString());

                return await SendAsync<SubscriptionResource>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.MeterLocationid,
                    routeValues: new { resourceName = resourceName.ToString() },
                    queryParameters: queryParameters,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.MeterV2Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns detailed facts about specified resource.
        /// </summary>
        /// <returns>Resource Information</returns>
        public virtual async Task<IEnumerable<ISubscriptionResource>> GetResourceStatus(bool nextBillingPeriod, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetResourceStatus"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("nextBillingPeriod", nextBillingPeriod.ToString());

                return await SendAsync<IEnumerable<SubscriptionResource>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.MeterLocationid,
                    queryParameters: queryParameters,
                    userState: userState,
    	            version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.MeterV2Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets the maximum and included quantities for a resource
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <param name="includedQuantity">Included resource quantity</param>
        /// <param name="maximumQuantity">Maximum resource quantity</param>
        public virtual async Task SetAccountQuantity(ResourceName name, int includedQuantity, int maximumQuantity, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "UpdateMeter"))
            {
                // we have to read the old subscription resource in first to avoid changing things with the default values
                SubscriptionResource subscriptionResource = (SubscriptionResource)await this.GetResourceStatus(name, true, null, cancellationToken).ConfigureAwait(false);
                subscriptionResource.IncludedQuantity = includedQuantity;
                subscriptionResource.MaximumQuantity = maximumQuantity;

                var message = this.CreateRequestMessageAsync(
                    method: new HttpMethod("PATCH"),
                    locationId: CommerceResourceIds.MeterLocationid,
                    routeValues: new { resourceName = name.ToString() },
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.MeterV2Resources),
                    cancellationToken: cancellationToken).SyncResult();

                message.Content = new ObjectContent(subscriptionResource.GetType(), subscriptionResource, Formatter);

                await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="paidBillingStatus"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task TogglePaidBilling(ResourceName name, Boolean paidBillingStatus, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "UpdateMeter"))
            {
                // we have to read the old subscription resource in first to avoid changing things with the default values
                SubscriptionResource subscriptionResource = (SubscriptionResource)await this.GetResourceStatus(name, true, cancellationToken: cancellationToken).ConfigureAwait(false);
                subscriptionResource.IsPaidBillingEnabled = paidBillingStatus;

                var message = await CreateRequestMessageAsync(
                    method: new HttpMethod("PATCH"),
                    locationId: CommerceResourceIds.MeterLocationid,
                    routeValues: new { resourceName = name.ToString() },
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.MeterV2Resources),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                message.Content = new ObjectContent(subscriptionResource.GetType(), subscriptionResource, Formatter);

                await SendAsync(message, userState, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sends usage information to billing pipeline. 
        /// Note that this method requires the request context to
        /// be at the application level with user identity.
        /// </summary>
        /// <param name="resourceName">Unique name of the resource</param>
        /// <param name="quantity">Quantity used in the billing event</param>
        /// <param name="eventId">Unique human readable identifier for the billing event</param>
        /// <param name="billingEventDateTime">Timestamp at which the event started</param>
        public virtual async Task ReportUsage(Guid eventUserId, ResourceName resourceName, int quantity, string eventId, DateTime billingEventDateTime, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "ReportUsage"))
            {
                var requestContent = new
                {
                    BillableDate = billingEventDateTime,
                    EventId = eventId,
                    AssociatedUser = eventUserId,
                    Quantity = quantity,
                    ResouceName = resourceName // TODO: Typo in property name, probably not used since we set resourceName in the route values
                };

                var message = await CreateRequestMessageAsync(
                    method: HttpMethod.Post,
                    locationId: CommerceResourceIds.UsageEventLocationid,
                    routeValues: new { resourceName = resourceName.ToString() },
                    cancellationToken: cancellationToken,
                    userState: userState).ConfigureAwait(false);

                message.Content = new ObjectContent(requestContent.GetType(), requestContent, Formatter);

                await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns the aggregate resource usage over the specified time range
        /// </summary>
        /// <param name="startTime">Start of the the time range to retrieve, inclusive</param>
        /// <param name="endTime">End of the time range to retrieve, exclusive</param>
        /// <param name="timeSpan">Interval of the time to retrieve, should be in a multiple of hour or day</param>
        /// <param name="resource">Resource name</param>
        /// <returns>An enumerable of aggregated resource data, one for each timespan</returns>
        public virtual async Task<IEnumerable<IUsageEventAggregate>> GetUsage(DateTime startTime, DateTime endTime, TimeSpan timeSpan, ResourceName resource, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetUsage"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("startTime", startTime.ToString());
                queryParameters.Add("endTime", endTime.ToString());
                queryParameters.Add("timeSpan", timeSpan.ToString());

                return await SendAsync<IEnumerable<UsageEventAggregate>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.UsageEventLocationid,
                    routeValues: new { resourceName = resource.ToString() },
                    queryParameters: queryParameters,
                    userState: userState,
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

            // 404 - Not found
            {"AccountNotFoundException", typeof(AccountNotFoundException)},

            // 413 - Request Entity Too Large
            {"AccountQuantityException", typeof(AccountQuantityException)},
        };
    }
}
