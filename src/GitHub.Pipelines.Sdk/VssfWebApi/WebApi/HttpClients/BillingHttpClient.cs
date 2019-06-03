using System;
using System.Collections.Generic;
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
    [ClientCircuitBreakerSettings(timeoutSeconds: 30, failurePercentage: 80, MaxConcurrentRequests = 55)]
    [ClientCancellationTimeout(60)]
    public class BillingHttpClient : VssHttpClientBase
    {
        #region Constructors

        public BillingHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

        public BillingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public BillingHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public BillingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public BillingHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion

        /// <summary>
        /// Returns detailed facts about the specified resource.
        /// </summary>
        /// <param name="offerMeterName">Unique name of the resource</param>
        /// <param name="renewalGroup">The renewal group.</param>
        /// <param name="nextBillingPeriod">Flag to indicate if offer quantity is for current or next month</param>
        /// <returns>Resource Information</returns>
        public virtual async Task<IOfferSubscription> GetResourceUsage(string offerMeterName, ResourceRenewalGroup renewalGroup, bool nextBillingPeriod,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetOfferSubscriptionForRenewalGroup"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>
                {
                    { "renewalGroup", renewalGroup },
                    { "nextBillingPeriod", nextBillingPeriod.ToString() },
                    { "galleryId", offerMeterName }
                };

                return await SendAsync<OfferSubscription>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    routeValues: new { resourceName = offerMeterName },
                    queryParameters: queryParameters,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns detailed facts about the specified resource.
        /// </summary>
        /// <param name="offerMeterName">Unique name of the resource</param>
        /// <param name="nextBillingPeriod">Flag to indicate if offer quantity is for current or next month</param>
        /// <returns>Resource Information</returns>
        public virtual async Task<IOfferSubscription> GetResourceUsage(string offerMeterName, bool nextBillingPeriod,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetOfferSubscription"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>
                {
                    { "nextBillingPeriod", nextBillingPeriod.ToString() },
                    { "galleryId", offerMeterName }
                };

                return await SendAsync<OfferSubscription>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    routeValues: new { resourceName = offerMeterName },
                    queryParameters: queryParameters,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns detailed facts for each resource.
        /// </summary>
        /// <param name="nextBillingPeriod">Flag to indicate if offer quantity is for current or next month</param>
        /// <returns>Resource Information</returns>
        public virtual async Task<IEnumerable<IOfferSubscription>> GetResourceUsage(bool nextBillingPeriod, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetOfferSubscriptions"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("nextBillingPeriod", nextBillingPeriod.ToString());

                return await SendAsync<IEnumerable<OfferSubscription>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    queryParameters: queryParameters,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns detailed facts about specified resource.
        /// </summary>
        /// <param name="validateAzuresubscription">Flag to validate associated azure subscriptions for usage</param>
        /// <param name="nextBillingPeriod">Flag to indicate if offer quantity is for current or next month</param>
        /// <returns>Resource Information</returns>
        public virtual async Task<IEnumerable<IOfferSubscription>> GetAllOfferSubscriptionsForUser(bool validateAzuresubscription = false, bool nextBillingPeriod = false, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetAllOfferSubscriptionsForUser"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("validateAzuresubscription", validateAzuresubscription.ToString());
                queryParameters.Add("nextBillingPeriod", nextBillingPeriod.ToString());

                return await SendAsync<IEnumerable<OfferSubscription>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    queryParameters: queryParameters,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns detailed facts about specified resource.
        /// </summary>
        /// <param name="azureSubscriptionId">Azure subscription Id</param>
        /// <param name="galleryItemId">Gallery item id</param>
        /// <param name="nextBillingPeriod">Flag to indicate if offer quantity is for current or next month</param>
        /// <returns>Resource Information</returns>
        public virtual async Task<IEnumerable<IOfferSubscription>> GetOfferSubscriptionsForGalleryItem(Guid azureSubscriptionId, string galleryItemId, bool nextBillingPeriod = false, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetOfferSubscriptionsForGalleryItem"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("galleryItemId", galleryItemId.ToString());
                queryParameters.Add("azureSubscriptionId", azureSubscriptionId.ToString());
                queryParameters.Add("nextBillingPeriod", nextBillingPeriod.ToString());

                return await SendAsync<IEnumerable<OfferSubscription>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    queryParameters: queryParameters,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets the maximum and included quantities for a resource.
        /// </summary>
        /// <param name="offerMeterName"></param>
        /// <param name="renewalGroup">The renewal group.</param>
        /// <param name="includedQuantity">Included resource quantity</param>
        /// <param name="maximumQuantity">Maximum resource quantity</param>
        public virtual async Task SetAccountQuantity(string offerMeterName, ResourceRenewalGroup renewalGroup, int? includedQuantity, int? maximumQuantity,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "SetAccountQuantity"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>
                {
                    { "offerMeterName", offerMeterName },
                    { "meterRenewalGroup", renewalGroup},
                    { "newIncludedQuantity", includedQuantity},
                    { "newMaximumQuantity", maximumQuantity}
                };

                var message = this.CreateRequestMessageAsync(
                    method: new HttpMethod("PATCH"),
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    queryParameters: queryParameters,
                    routeValues: new { offerMeterName = offerMeterName },
                    userState: userState,
                    cancellationToken: cancellationToken).SyncResult();

                await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Sets the maximum and included quantities for a resource
        /// </summary>
        /// <param name="offerMeterName"></param>
        /// <param name="includedQuantity">Included resource quantity</param>
        /// <param name="maximumQuantity">Maximum resource quantity</param>
        public virtual async Task SetAccountQuantity(string offerMeterName, int includedQuantity, int maximumQuantity, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "SetAccountQuantity"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>
                {
                    { "offerMeterName", offerMeterName },
                    { "newIncludedQuantity", includedQuantity},
                    { "newMaximumQuantity", maximumQuantity}
                };
                var message = this.CreateRequestMessageAsync(
                    method: new HttpMethod("PATCH"),
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    queryParameters: queryParameters,
                    routeValues: new { resourceName = offerMeterName },
                    userState: userState,
                    cancellationToken: cancellationToken).SyncResult();

                message.Content = null;

                await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offerMeterName"></param>
        /// <param name="paidBillingStatus"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task TogglePaidBilling(string offerMeterName, Boolean paidBillingStatus, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "UpdateOfferSubscription"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("offerMeterName", offerMeterName);

                // we have to read the old subscription resource in first to avoid changing things with the default values
                var subscriptionResource = (OfferSubscription)await this.GetResourceUsage(offerMeterName, true, cancellationToken: cancellationToken).ConfigureAwait(false);
                subscriptionResource.IsPaidBillingEnabled = paidBillingStatus;

                var message = await CreateRequestMessageAsync(
                    method: new HttpMethod("PATCH"),
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    queryParameters: queryParameters,
                    routeValues: new { resourceName = offerMeterName },
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                message.Content = new ObjectContent(subscriptionResource.GetType(), subscriptionResource, Formatter);

                await SendAsync(message, userState, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="offerMeterName"></param>
        /// <param name="azureSubscriptionId"></param>
        /// <param name="renewalGroup"></param>
        /// <param name="quantity"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="billingTarget"></param>
        /// <returns></returns>
        public virtual async Task CreateOfferSubscription(string offerMeterName, Guid azureSubscriptionId, ResourceRenewalGroup renewalGroup, Int32 quantity, Guid? billingTarget = null, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "CreateOfferSubscription"))
            {
                var requestContent = new OfferSubscription
                {
                    OfferMeter = new OfferMeter() { GalleryId = offerMeterName },
                    AzureSubscriptionId = azureSubscriptionId,
                    RenewalGroup = renewalGroup,
                    CommittedQuantity = quantity
                };

                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("billingTarget", billingTarget);

                var message = await CreateRequestMessageAsync(
                    method: HttpMethod.Post,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    routeValues: new { resourceName = offerMeterName },
                    queryParameters: queryParameters,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                message.Content = new ObjectContent(requestContent.GetType(), requestContent, this.Formatter);

                await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public virtual Task CancelOfferSubscription(string offerMeterName, Guid azureSubscriptionId, ResourceRenewalGroup renewalGroup, string cancelReason, Guid? billingTarget = null, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return CancelOfferSubscription(offerMeterName, azureSubscriptionId, renewalGroup, cancelReason, billingTarget, false, cancellationToken: cancellationToken);
        }

        public virtual async Task CancelOfferSubscription(string offerMeterName, Guid azureSubscriptionId, ResourceRenewalGroup renewalGroup, string cancelReason, Guid? billingTarget, bool immediate, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "CancelOfferSubscription"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("cancelReason", cancelReason);

                if (billingTarget.HasValue)
                {
                    queryParameters.Add("billingTarget", billingTarget);
                }
                queryParameters.Add("immediate", immediate);

                var offerSubscription = new OfferSubscription
                {
                    OfferMeter = new OfferMeter() { GalleryId = offerMeterName },
                    AzureSubscriptionId = azureSubscriptionId,
                    RenewalGroup = renewalGroup
                };
                var message = await CreateRequestMessageAsync(
                    method: new HttpMethod("PATCH"),
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                     queryParameters: queryParameters,
                    routeValues: new { resourceName = offerMeterName },
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                message.Content = new ObjectContent(offerSubscription.GetType(), offerSubscription, this.Formatter);

                await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Enables the trial or preview offer subscription.
        /// </summary>
        /// <param name="offerMeterName">Name of the offer meter.</param>
        /// <param name="renewalGroup">The renewal group.</param>
        /// <param name="userState">State of the user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public virtual async Task EnableTrialOrPreviewOfferSubscription(string offerMeterName, ResourceRenewalGroup renewalGroup, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "EnableTrialOrPreviewOfferSubscription"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("offerMeterName", offerMeterName);
                queryParameters.Add("renewalGroup", renewalGroup);

                var message = await CreateRequestMessageAsync(
                    method: HttpMethod.Post,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.BillingV1Resources),
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    queryParameters: queryParameters,
                    routeValues: new { resourceName = offerMeterName },
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Extends the trial for a given offer subscription
        /// </summary>
        /// <param name="offerMeterName"></param>
        /// <param name="renewalGroup"></param>
        /// <param name="endDate"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task EnableTrialOfferSubscriptionExtension(string offerMeterName, ResourceRenewalGroup renewalGroup, DateTime endDate, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "EnableTrialOfferSubscriptionExtension"))
            {
                var queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("offerMeterName", offerMeterName);
                queryParameters.Add("renewalGroup", renewalGroup);
                queryParameters.Add("endDate", endDate);

                var message = await CreateRequestMessageAsync(
                    method: new HttpMethod("PATCH"),
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    queryParameters: queryParameters,
                    routeValues: new { resourceName = offerMeterName },
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Decreases resource quantity.
        /// If <paramref name="shouldBeImmediate"/> is true, the update is immediate; otherwise, the update
        /// will happen on the next reset (i.e. billing period).
        /// 
        /// If called at the deployment level for bundle purchases, an <paramref name="azureSubscriptionId"/>
        /// must be passed to resolve the container of resouces associated with the subscription. Otherwise, if
        /// called the collection level, this is assumed to be the case for extensions (which are associated
        /// with a collection). Any other level results in an error.
        /// </summary>
        /// <param name="offerMeterName">The offer meter name.</param>
        /// <param name="renewalGroup">The renewal group.</param>
        /// <param name="quantity">The new quantity.</param>
        /// <param name="shouldBeImmediate">If false, updates only current quantity; otherwise, it also updates committed quantity.</param>
        /// <param name="azureSubscriptionId">Optional Azure subscription Id except at the deployment level.</param>
        public virtual async Task DecreaseResourceQuantity(string offerMeterName, ResourceRenewalGroup renewalGroup,
            int quantity, bool shouldBeImmediate, Guid? azureSubscriptionId,
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "DecreaseResourceQuantity"))
            {
                var queryParameters = new List<KeyValuePair<string, string>>();
                queryParameters.Add("offerMeterName", offerMeterName);
                queryParameters.Add("renewalGroup", renewalGroup);
                queryParameters.Add("quantity", quantity);
                queryParameters.Add("shouldBeImmediate", shouldBeImmediate);
                queryParameters.Add("azureSubscriptionId", azureSubscriptionId);

                var message = await CreateRequestMessageAsync(
                    method: new HttpMethod("PATCH"),
                    locationId: CommerceResourceIds.OfferSubscriptionResourceId,
                    queryParameters: queryParameters,
                    routeValues: new { resourceName = offerMeterName },
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await SendAsync(message, userState, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates a request to purchase an extensions/resource for a given quantity to subscription administrators who are also PCA's.
        /// </summary>
        /// <param name="request">Details of the purchase request including offer meter, quantity, and description</param>
        public virtual async Task CreatePurchaseRequest(PurchaseRequest request, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpContent content = new ObjectContent<PurchaseRequest>(request, new VssJsonMediaTypeFormatter(true));

            await SendAsync(
                HttpMethod.Put,
                CommerceResourceIds.PurchaseRequestLocationId,
                version: new ApiResourceVersion(apiVersion40, CommerceResourceVersions.PurchaseRequestV1Resources),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates a request with response from the approver
        /// </summary>
        /// <param name="request">Details of the purchase request including offer meter, quantity, and description</param>
        public virtual async Task UpdatePurchaseRequest(PurchaseRequest request, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpContent content = new ObjectContent<PurchaseRequest>(request, new VssJsonMediaTypeFormatter(true));

            await SendAsync(
                new HttpMethod("PATCH"),
                CommerceResourceIds.PurchaseRequestLocationId,
                version: new ApiResourceVersion(apiVersion40, CommerceResourceVersions.PurchaseRequestV1Resources),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false);
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
        protected static readonly Version apiVersion40 = new Version(4, 0);

        internal static readonly Dictionary<string, Type> s_translatedExceptions = new Dictionary<string, Type>
        {
            // 400 - Bad Request    
            {"InvalidResourceException", typeof(InvalidResourceException)},

            // 400 - Bad Request    
            {"InvalidOperationException", typeof(InvalidOperationException)},

            // 401 - Unauthorized
            {"CommerceSecurityException", typeof(CommerceSecurityException)},

            // 404 - Not found
            {"AccountNotFoundException", typeof(AccountNotFoundException)},

            // 413 - Request Entity Too Large
            {"AccountQuantityException", typeof(AccountQuantityException)},

             // User Not Admin Co-Admin of Subscription
            {"UserIsNotSubscriptionAdminException", typeof(UserIsNotSubscriptionAdminException)},

            // User Not Account Owner
            {"UserIsNotAccountOwnerException", typeof(UserIsNotAccountOwnerException)},

            // UnsupportedSubscriptionType
            {"UnsupportedSubscriptionTypeException", typeof(UnsupportedSubscriptionTypeException)},
        };
    }
}
