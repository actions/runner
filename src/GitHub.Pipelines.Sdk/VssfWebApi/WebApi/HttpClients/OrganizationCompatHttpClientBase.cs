using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Organization.Client
{
    /// <summary>
    /// This class contains deprecated overloads to maintain binary compatibility
    /// </summary>
    /// <remarks>
    /// See: https://vsowiki.com/index.php?title=Rest_Client_Generation#Toolsets.5CAPICompatCheck.5CAPICompatCheck.targets.28x.2Cy.29:_error
    /// </remarks>
    public abstract class OrganizationCompatHttpClientBase : VssHttpClientBase
    {
        public OrganizationCompatHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public OrganizationCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public OrganizationCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public OrganizationCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public OrganizationCompatHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="collectionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual Task<bool> DeleteCollectionAsync(
            Guid collectionId,
            object userState,
            CancellationToken cancellationToken)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("668b5607-0db2-49bb-83f8-5f46f1094250");
            object routeValues = new { collectionId = collectionId };

            return SendAsync<bool>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.0-preview.1"),
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
