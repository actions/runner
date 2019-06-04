using GitHub.Services.Common;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Notification.Client
{
    [ResourceArea(PersistedNotificationResourceIds.AreaId)]
    public class PersistedNotificationHttpClient : VssHttpClientBase
    {
        #region Constructors

        public PersistedNotificationHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

        public PersistedNotificationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public PersistedNotificationHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public PersistedNotificationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public PersistedNotificationHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion

        public virtual async Task SaveNotificationsAsync(IEnumerable<Notification> notifications, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(PersistedNotificationResourceIds.AreaName, "SaveNotificationsAsync"))
            {
                ArgumentUtility.CheckForNull(notifications, "notifications");

                var content = new ObjectContent<VssJsonCollectionWrapper<IEnumerable<Notification>>>(
                    new VssJsonCollectionWrapper<IEnumerable<Notification>>(notifications),
                    base.Formatter);

                await SendAsync(
                    method: HttpMethod.Post,
                    locationId: PersistedNotificationResourceIds.NotificationsId,
                    version: new ApiResourceVersion(previewApiVersion, PersistedNotificationResourceVersions.NotificationsResourcePreviewVersion),
                    content: content,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public virtual async Task<IList<Notification>> GetRecipientNotificationsAsync(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(PersistedNotificationResourceIds.AreaName, "GetRecipientNotificationsAsync"))
            {
                return await SendAsync<IList<Notification>>(
                    method: HttpMethod.Get,
                    locationId: PersistedNotificationResourceIds.NotificationsId,
                    version: new ApiResourceVersion(previewApiVersion, PersistedNotificationResourceVersions.NotificationsResourcePreviewVersion),
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public virtual async Task<RecipientMetadata> GetRecipientMetadataAsync(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(PersistedNotificationResourceIds.AreaName, "GetRecipientMetadataAsync"))
            {
                return await SendAsync<RecipientMetadata>(
                    method: HttpMethod.Get,
                    locationId: PersistedNotificationResourceIds.RecipientMetadataId,
                    version: new ApiResourceVersion(previewApiVersion, PersistedNotificationResourceVersions.RecipientMetadataPreviewVersion),
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<RecipientMetadata> UpdateRecipientMetadataAsync(RecipientMetadata metadata, Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(PersistedNotificationResourceIds.AreaName, "UpdateRecipientMetadataAsync"))
            {
                HttpContent content = new ObjectContent<RecipientMetadata>(metadata, base.Formatter);

                return SendAsync<RecipientMetadata>(
                    method: new HttpMethod("PATCH"), 
                    locationId:PersistedNotificationResourceIds.RecipientMetadataId,
                    version: new ApiResourceVersion(previewApiVersion, PersistedNotificationResourceVersions.RecipientMetadataPreviewVersion),
                    content: content,
                    userState: userState,
                    cancellationToken: cancellationToken);
            }
        }

        protected static readonly Version previewApiVersion = new Version(1, 0);
    }
}
