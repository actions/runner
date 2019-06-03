using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Commerce.Client
{
    public class CsmHttpClient : CsmHttpClientBase
    {
        public CsmHttpClient(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
        {
        }

        public CsmHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings) : base(baseUrl, credentials, settings)
        {
        }

        public CsmHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers) : base(baseUrl, credentials, handlers)
        {
        }

        public CsmHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers) : base(baseUrl, credentials, settings, handlers)
        {
        }

        public CsmHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler) : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Creates or updates a Azure DevOps Services account resource.
        /// </summary>
        /// <param name="requestData">The request data.</param>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroupName">Name of the resource group.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<AccountResource> Accounts_CreateAsync(
            AccountResourceRequest requestData,
            Guid subscriptionId,
            string resourceGroupName,
            string resourceName,
            Identity.Identity requestor,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.Accounts_CreateAsync(
                requestData,
                subscriptionId,
                resourceGroupName,
                resourceName,
                requestor.GetProperty<string>(IdentityAttributeTags.AccountName, string.Empty),
                requestor.GetProperty<string>(IdentityAttributeTags.Domain, string.Empty),
                userState,
                cancellationToken
            );
        }

        /// <summary>
        /// [Preview API] Creates or updates a Azure DevOps Services account resource.
        /// </summary>
        /// <param name="requestData">The request data.</param>
        /// <param name="subscriptionId">The Azure subscription identifier.</param>
        /// <param name="resourceGroupName">Name of the resource group.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<AccountResource> Accounts_CreateAsync(
            AccountResourceRequest requestData,
            Guid subscriptionId,
            string resourceGroupName,
            string resourceName,
            string principalName,
            string domain,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("5745408e-6e9e-49c7-92bf-62932c8df69d");
            object routeValues = new { subscriptionId = subscriptionId, resourceGroupName = resourceGroupName, resourceName = resourceName };
            HttpContent content = new ObjectContent<AccountResourceRequest>(requestData, new VssJsonMediaTypeFormatter(true));

            content.Headers.Add("x-ms-client-principal-name", principalName);
            content.Headers.Add("x-ms-client-tenant-id", domain);

            return SendAsync<AccountResource>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("4.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Task<AccountResource> Accounts_CreateOrUpdateAsync(AccountResourceRequest requestData, Guid subscriptionId, string resourceGroupName, string resourceName, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException($"Only the overload of {nameof(Accounts_CreateAsync)} that accepts a requestor identity is supported.");
        }

        public AccountResource Accounts_Get(Guid subscriptionId, string resourceGroupName, string resourceName, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return base.Accounts_GetAsync(subscriptionId, resourceGroupName, resourceName, userState, cancellationToken).SyncResult();
            }
            catch (VssServiceResponseException e)
            {
                if (e.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw;
            }
        }
    }
}
