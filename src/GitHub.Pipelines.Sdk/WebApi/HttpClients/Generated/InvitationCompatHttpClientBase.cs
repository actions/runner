using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Invitation
{
    [ResourceArea(InvitationResourceIds.AreaId)]
    public class InvitationCompatHttpClientBase : VssHttpClientBase
    {
        public InvitationCompatHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public InvitationCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public InvitationCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public InvitationCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public InvitationCompatHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Send Account Invitation to a user
        /// </summary>
        /// <param name="invitationData">optional Invitation Data</param>
        /// <param name="userId">IdentityId of the user</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never), Obsolete()]
        public async Task SendAccountInvitationAsync(
            InvitationData invitationData,
            Guid userId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("bc7ca053-e204-435b-a143-6240ba8a93bf");
            object routeValues = new { userId = userId };
            HttpContent content = new ObjectContent<InvitationData>(invitationData, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Send Account Invitation to a user
        /// </summary>
        /// <param name="invitationData">optional Invitation Data</param>
        /// <param name="userId">IdentityId of the user</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [EditorBrowsable(EditorBrowsableState.Never), Obsolete()]
        public async Task SendOrganizationInvitationAsync(
            InvitationData invitationData,
            Guid userId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("bc7ca053-e204-435b-a143-6240ba8a93bf");
            object routeValues = new { userId = userId };
            HttpContent content = new ObjectContent<InvitationData>(invitationData, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }
    }
}
