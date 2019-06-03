//-----------------------------------------------------------------------
// <copyright file="SubscriptionController.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Controller handling subscriptions related operations.
// </summary>
//-----------------------------------------------------------------------

namespace GitHub.Services.Commerce.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using GitHub.Services.Account;
    using GitHub.Services.Common;
    using GitHub.Services.WebApi;

    /// <summary>
    /// Class that represents methods communicating with the platform service via REST controller.
    /// </summary>
    [ResourceArea(CommerceResourceIds.AreaId)]
    [ClientCircuitBreakerSettings(timeoutSeconds: 100, failurePercentage: 80, MaxConcurrentRequests = 40)]
    public class SubscriptionHttpClient : VssHttpClientBase
    {
        #region Constructors

        public SubscriptionHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

        public SubscriptionHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public SubscriptionHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public SubscriptionHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public SubscriptionHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion

        /// <summary>
        /// Gets the accounts by subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<IAzureSubscription>> GetAzureSubscriptions(List<Guid> subscriptionIds, AccountProviderNamespace providerNamespaceId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!subscriptionIds?.Any() == true)
            {
                return new List<IAzureSubscription>();
            }

            using (new OperationScope(CommerceResourceIds.AreaName, "GetAzureSubscriptions"))
            {
                var queryParameters = new List<KeyValuePair<String, String>>
                {
                    { "providerNamespaceId", providerNamespaceId.ToString() }
                };
                foreach (var id in subscriptionIds)
                {
                    queryParameters.Add(new KeyValuePair<string, string>("ids", id.ToString()));
                }

                return await this.SendAsync<IEnumerable<AzureSubscription>>(
                        method: HttpMethod.Get,
                        locationId: CommerceResourceIds.SubscriptionLocationId,
                        queryParameters: queryParameters,
                        userState: null,
                        cancellationToken: cancellationToken
                        ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the accounts by subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<ISubscriptionAccount>> GetAccounts(Guid subscriptionId, AccountProviderNamespace providerNamespaceId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetAccountsBySubscription"))
            {
                var queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("providerNamespaceId", providerNamespaceId.ToString());

                return await this.SendAsync<IEnumerable<SubscriptionAccount>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.SubscriptionLocationId,
                    routeValues: new { subscriptionId = subscriptionId },
                    queryParameters: queryParameters,
                    userState: null,
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the accounts owned by identity.
        /// </summary>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <param name="memberId">The member identifier.</param>
        /// <param name="queryOnlyOwnerAccounts">if set to <c>true</c> [query for owners only].</param>
        /// <param name="inlcudeDisabledAccounts">if set to <c>true</c> [inlcude disabled accounts].</param>
        /// <returns></returns>
        public virtual async Task<IEnumerable<ISubscriptionAccount>> GetAccounts(AccountProviderNamespace providerNamespaceId, Guid memberId, bool queryOnlyOwnerAccounts, bool inlcudeDisabledAccounts = false, bool includeMSAAccounts = false, IEnumerable<Guid> serviceOwners = null, string galleryId = null, bool addUnlinkedSubscription = false, bool queryAccountsByUpn = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetAccountsOwnedByIdentity"))
            {
                var queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("providerNamespaceId", providerNamespaceId.ToString());
                queryParameters.Add("memberId", memberId.ToString());
                queryParameters.Add("queryOnlyOwnerAccounts", queryOnlyOwnerAccounts.ToString());
                queryParameters.Add("inlcudeDisabledAccounts", inlcudeDisabledAccounts.ToString());
                queryParameters.Add("includeMSAAccounts", includeMSAAccounts.ToString());
                queryParameters.Add("queryAccountsByUpn", queryAccountsByUpn.ToString());

                // add this querysting values only if galleryId is passed, without gallery id can't decide if subscriptions are eligible to purchase or not
                if(!string.IsNullOrEmpty(galleryId))
                {
                    queryParameters.Add("galleryId", galleryId);
                    queryParameters.Add("addUnlinkedSubscription", addUnlinkedSubscription.ToString());
                }
                if (serviceOwners != null)
                {
                    foreach (var serviceOwner in serviceOwners)
                    {
                        queryParameters.Add(new KeyValuePair<string, string>("serviceOwners", serviceOwner.ToString()));
                    }
                }

                return await this.SendAsync<IEnumerable<SubscriptionAccount>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.SubscriptionLocationId,
                    queryParameters: queryParameters,
                    userState: null,
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates the subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <param name="anniversaryDay">The anniversary day.</param>
        /// <param name="status">The status.</param>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public virtual Task CreateSubscription(Guid subscriptionId, AccountProviderNamespace providerNamespaceId, int anniversaryDay, SubscriptionStatus status, SubscriptionSource source, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();// This API is being deprecated and we do this for binary compat
        }

        /// <summary>
        /// Links the account.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="ownerId">The owner identifier.</param>
        /// <returns></returns>
        public virtual async Task LinkAccount(Guid subscriptionId, AccountProviderNamespace providerNamespaceId, Guid accountId, Guid ownerId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "LinkAccount"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("providerNamespaceId", providerNamespaceId.ToString());
                queryParameters.Add("accountId", accountId.ToString());
                queryParameters.Add("ownerId", ownerId.ToString());
                queryParameters.Add("hydrate", false.ToString());

                await this.SendAsync(
                    method: HttpMethod.Put,
                    locationId: CommerceResourceIds.SubscriptionLocationId,
                    routeValues: new { subscriptionId = subscriptionId },
                    queryParameters: queryParameters,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Links the account.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="ownerId">The owner identifier.</param>
        /// <param name="hydrate">Specifies whether or not to trigger hydration of the account into CSM for Ibiza</param>
        /// <returns></returns>
        public virtual async Task LinkAccount(Guid subscriptionId, AccountProviderNamespace providerNamespaceId, Guid accountId, Guid ownerId, bool hydrate, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "LinkAccount"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("providerNamespaceId", providerNamespaceId.ToString());
                queryParameters.Add("accountId", accountId.ToString());
                queryParameters.Add("ownerId", ownerId.ToString());
                queryParameters.Add("hydrate", hydrate.ToString());

                await this.SendAsync(
                    method: HttpMethod.Put,
                    locationId: CommerceResourceIds.SubscriptionLocationId,
                    routeValues: new { subscriptionId = subscriptionId },
                    queryParameters: queryParameters,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Unlinks an account from the subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="ownerId">The owner identifier.</param>
        /// <returns></returns>
        public virtual async Task UnlinkAccount(Guid subscriptionId, AccountProviderNamespace providerNamespaceId, Guid accountId, Guid ownerId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "UnlinkAccount"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("providerNamespaceId", providerNamespaceId.ToString());
                queryParameters.Add("accountId", accountId.ToString());
                queryParameters.Add("ownerId", ownerId.ToString());
                queryParameters.Add("hydrate", false.ToString());

                await this.SendAsync(
                    method: HttpMethod.Delete,
                    locationId: CommerceResourceIds.SubscriptionLocationId,
                    routeValues: new { subscriptionId = subscriptionId },
                    queryParameters: queryParameters,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Unlinks an account from the subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="ownerId">The owner identifier.</param>
        /// /// <param name="hydrate">Specifies whether or not to trigger dehydration of the account into CSM for Ibiza</param>
        /// <returns></returns>
        public virtual async Task UnlinkAccount(Guid subscriptionId, AccountProviderNamespace providerNamespaceId, Guid accountId, Guid ownerId, bool hydrate, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "UnlinkAccount"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("providerNamespaceId", providerNamespaceId.ToString());
                queryParameters.Add("accountId", accountId.ToString());
                queryParameters.Add("ownerId", ownerId.ToString());
                queryParameters.Add("hydrate", hydrate.ToString());

                await this.SendAsync(
                    method: HttpMethod.Delete,
                    locationId: CommerceResourceIds.SubscriptionLocationId,
                    routeValues: new { subscriptionId = subscriptionId },
                    queryParameters: queryParameters,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets subscription account for a given accountId
        /// </summary>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <param name="accountId">The account identifier.</param>
        /// <returns></returns>
        public virtual async Task<ISubscriptionAccount> GetSubscriptionAccount(AccountProviderNamespace providerNamespaceId, Guid accountId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetAccountSubscriptionId"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("providerNamespaceId", providerNamespaceId.ToString());
                queryParameters.Add("accountId", accountId.ToString());

                return await this.SendAsync<SubscriptionAccount>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.SubscriptionLocationId,
                    queryParameters: queryParameters,
                    userState: null,
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get list of azure subscription where user is admin- co-admin under tenant or valid azure subscriptions for purchase (passing accountId to get this information for AAD calls)
        /// </summary>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <param name="galleryItemId">Fully qualified gallery item id</param>
        /// <param name="accountId">The account identifier.</param>
        /// <returns>List of Subscriptions</returns>
        public virtual async Task<ISubscriptionAccount> GetAzureSubscriptionForPurchase(Guid subscriptionId, string galleryItemId, Guid accountId, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetAzureSubscriptionForPurchase"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("subscriptionid", subscriptionId);
                queryParameters.Add("galleryItemId", galleryItemId);
                queryParameters.Add("accountId", accountId);

                return await this.SendAsync<SubscriptionAccount>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.SubscriptionLocationId,
                    queryParameters: queryParameters,
                    routeValues: new { subscriptionId = subscriptionId, galleryItemId = galleryItemId , accountId = accountId },
                    userState: null,
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns azure subscriptions across tenants.
        /// </summary>
        /// <param name="requestContext">Deployment Request Context</param>
        /// <param name="subscriptionId">Azure subscription id</param>
        /// <param name="queryAcrossTenants">Query across multiple tenants if the logged in user is member of multiple tenants</param>
        /// <remarks>Query across tenants only works if the current user token can be used to retrieve token for other tenants, otherwise method only returns subscriptions for current logged in tenant</remarks>
        /// <returns>List of subscriptions</returns>
        public virtual async Task<IEnumerable<ISubscriptionAccount>> GetAzureSubscriptionForUser(Guid? subscriptionId = null, bool queryAcrossTenants = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetAzureSubscriptionForUser"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("subscriptionId", subscriptionId);
                queryParameters.Add("queryAcrossTenants", queryAcrossTenants.ToString());
                return await this.SendAsync<IEnumerable<SubscriptionAccount>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.SubscriptionLocationId,
                    queryParameters: queryParameters,
                    routeValues: new { subscriptionId = subscriptionId },
                    userState: null,
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Links the account.
        /// </summary>
        /// <param name="subscriptionId">The subscription identifier.</param>
        /// <param name="providerNamespaceId">The provider namespace identifier.</param>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="ownerId">The owner identifier.</param>
        /// <param name="hydrate">Specifies whether or not to trigger hydration of the account into CSM for Ibiza</param>
        /// <returns></returns>
        public virtual async Task ChangeSubscriptionAccount(Guid newSubscriptionId, AccountProviderNamespace providerNamespaceId, Guid accountId, bool hydrate, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "ChangeSubscriptionAccount"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("providerNamespaceId", providerNamespaceId.ToString());
                queryParameters.Add("accountId", accountId.ToString());
                queryParameters.Add("hydrate", hydrate.ToString());
                
                var message = this.CreateRequestMessageAsync(
                    method: new HttpMethod("PATCH"),
                    locationId: CommerceResourceIds.SubscriptionLocationId,
                    queryParameters: queryParameters,
                    routeValues: new { subscriptionId = newSubscriptionId },
                    userState: null,
                    cancellationToken: cancellationToken).SyncResult();

                message.Content = new ObjectContent(newSubscriptionId.GetType(), newSubscriptionId, Formatter);

                await SendAsync(message, null, cancellationToken: cancellationToken).ConfigureAwait(false);
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
        internal virtual async Task<HttpRequestMessage> CreateRequestMessageAsync(HttpMethod method, Guid locationId, Object routeValues = null, ApiResourceVersion version = null, HttpContent content = null, List<KeyValuePair<String, String>> queryParameters = null, Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.CreateRequestMessageAsync(method, locationId, routeValues, version, content, queryParameters, userState, cancellationToken).ConfigureAwait(false);
        }


        [ExcludeFromCodeCoverage]
        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get { return s_translatedExceptions; }
        }

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
