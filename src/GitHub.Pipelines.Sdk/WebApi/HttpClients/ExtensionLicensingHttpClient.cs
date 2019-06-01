using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Licensing.Client
{
    [ResourceArea(LicensingResourceIds.AreaId)]
    public class ExtensionLicensingHttpClient : VssHttpClientBase
    {
        #region Constructors

        public ExtensionLicensingHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

        public ExtensionLicensingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public ExtensionLicensingHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public ExtensionLicensingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public ExtensionLicensingHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion Constructors

        #region Public Methods

        public virtual async Task TransferExtensionsForIdentitiesAsync(IList<IdentityMapping> identityMapping, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(identityMapping, nameof(identityMapping));
            using (new OperationScope(LicensingResourceIds.AreaName, "TransferExtensionsForIdentities"))
            {
                var content = new ObjectContent<IList<IdentityMapping>>(
                   identityMapping, new VssJsonMediaTypeFormatter(true));

                await SendAsync(method: HttpMethod.Post,
                    locationId: LicensingResourceIds.TransferIdentitiesExtensionsLocationId,
                    version: new ApiResourceVersion(currentApiVersion,
                        LicensingResourceVersions.ExtensionEntitlementsResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    content: content,
                    userState: userState).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Assigns the access to the given extension for a given list of users
        /// </summary>
        /// <param name="requestContext">The application level request context.</param>
        /// <param name="extensionId">The extension id to assign the access to.</param>
        /// <param name="userIds">The list of users that their access to the extension should be assigned/param>
        public virtual async Task<ICollection<ExtensionOperationResult>> AssignExtensionToUsersAsync(string extensionId, IList<Guid> userIds, bool isAutoAssignment = false, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(extensionId, nameof(extensionId));
            ArgumentUtility.CheckForNull(userIds, nameof(userIds));

            using (new OperationScope(LicensingResourceIds.AreaName, "AssignExtensionToUsers"))
            {
                var content = new ObjectContent<ExtensionAssignment>(new ExtensionAssignment()
                {
                    ExtensionGalleryId = extensionId,
                    UserIds = userIds,
                    IsAutoAssignment = isAutoAssignment
                }, new VssJsonMediaTypeFormatter(true));

                return await SendAsync<ICollection<ExtensionOperationResult>>(method: HttpMethod.Put,
                    locationId: LicensingResourceIds.UserExtensionEntitlementsLocationId,
                    version: new ApiResourceVersion(currentApiVersion, LicensingResourceVersions.ExtensionEntitlementsResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    content: content,
                    userState: userState).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns extensions that are currently assigned to the user in the account
        /// </summary>
        /// <param name="userId">The user's identity id.</param>
        public virtual async Task<IDictionary<string, LicensingSource>> GetExtensionsAssignedToUserAsync(Guid userId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(userId, nameof(userId));

            using (new OperationScope(LicensingResourceIds.AreaName, "GetExtensionsAssignedToUser"))
            {
                return await SendAsync<IDictionary<string, LicensingSource>>(method: HttpMethod.Get,
                locationId: LicensingResourceIds.UserExtensionEntitlementsLocationId,
                version: new ApiResourceVersion(currentApiVersion, LicensingResourceVersions.ExtensionEntitlementsResourceRtmVersion),
                routeValues: new { userId = userId },
                cancellationToken: cancellationToken,
                userState: userState).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns extensions that are currently assigned to the users in the account
        /// </summary>
        /// <param name="userId">The user's identity id.</param>
        public virtual async Task<IDictionary<Guid, IList<ExtensionSource>>> BulkGetExtensionsAssignedToUsersAsync(IList<Guid> userIds, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(userIds, nameof(userIds));

            using (new OperationScope(LicensingResourceIds.AreaName, "GetExtensionsAssignedToUsers"))
            {
                var content = new ObjectContent<IList<Guid>>(userIds, new VssJsonMediaTypeFormatter(true));

                return await SendAsync<IDictionary<Guid, IList<ExtensionSource>>>(method: HttpMethod.Put,
                    locationId: LicensingResourceIds.UsersBatchExtensionEntitlementsLocationId,
                    version: new ApiResourceVersion(currentApiVersion, LicensingResourceVersions.ExtensionEntitlementsBatch2ResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    content: content,
                    userState: userState).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// This method has become deprecated
        /// </summary>
        /// <param name="userIds"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IDictionary<Guid, IList<string>>> GetExtensionsAssignedToUsersBatchAsync(IList<Guid> userIds, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForNull(userIds, nameof(userIds));

            using (new OperationScope(LicensingResourceIds.AreaName, "GetExtensionsAssignedToUsers"))
            {
                var content = new ObjectContent<IList<Guid>>(userIds, new VssJsonMediaTypeFormatter(true));

                return await SendAsync<IDictionary<Guid, IList<string>>>(method: HttpMethod.Put,
                    locationId: LicensingResourceIds.UsersBatchExtensionEntitlementsLocationId,
                    version: new ApiResourceVersion(currentApiVersion, LicensingResourceVersions.ExtensionEntitlementsBatchResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    content: content,
                    userState: userState).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns a dictionary of userIds to lists of extensionIds cooresponding
        /// extensions that are assigned to said users in the current account
        /// </summary>
        /// <param name="extensionId">The extension to check the status of the users for.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IDictionary<Guid, ExtensionAssignmentDetails>> GetExtensionStatusForUsersAsync(string extensionId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(extensionId, nameof(extensionId));

            using (new OperationScope(LicensingResourceIds.AreaName, "GetExtensionStatusForUsers"))
            {
                return await SendAsync<IDictionary<Guid, ExtensionAssignmentDetails>>(method: HttpMethod.Get,
                    locationId: LicensingResourceIds.ExtensionEntitlementsLocationId,
                    version: new ApiResourceVersion(currentApiVersion, LicensingResourceVersions.ExtensionEntitlementsResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    routeValues: new { extensionId = extensionId },
                    userState: userState).ConfigureAwait(false);
            }
        }

        public virtual async Task<IEnumerable<AccountLicenseExtensionUsage>> GetExtensionLicenseUsageAsync(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetExtensionsAssignedToUser"))
            {
                return await SendAsync<IEnumerable<AccountLicenseExtensionUsage>>(method: HttpMethod.Get,
                locationId: LicensingResourceIds.ExtensionsAssignedToAccountLocationId,
                version: new ApiResourceVersion(currentApiVersion, LicensingResourceVersions.ExtensionEntitlementsResourceRtmVersion),
                cancellationToken: cancellationToken,
                userState: userState).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Revokes the access to the given extension for a given list of users
        /// </summary>
        /// <param name="extensionId">The extension id to revoke the access of the user from.</param>
        /// <param name="userIds">The list of users that their access to the extension should be revoked/param>
        public virtual async Task<ICollection<ExtensionOperationResult>> UnassignExtensionFromUsersAsync(string extensionId, IList<Guid> userIds, LicensingSource source, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(extensionId, nameof(extensionId));
            ArgumentUtility.CheckForNull(userIds, nameof(userIds));
            ArgumentUtility.CheckForDefinedEnum(source, nameof(source));

            using (new OperationScope(LicensingResourceIds.AreaName, "UnassignExtensionFromUsers"))
            {
                var content = new ObjectContent<ExtensionAssignment>(new ExtensionAssignment()
                {
                    ExtensionGalleryId = extensionId,
                    UserIds = userIds,
                    LicensingSource = source
                }, new VssJsonMediaTypeFormatter(true));

                return await SendAsync<ICollection<ExtensionOperationResult>>(method: HttpMethod.Delete,
                    locationId: LicensingResourceIds.UserExtensionEntitlementsLocationId,
                    version: new ApiResourceVersion(currentApiVersion, LicensingResourceVersions.ExtensionEntitlementsResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    content: content,
                    userState: userState).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Assigns the access to the given extension for all eligible users in the account
        /// that do not already have access to the extension though bundle or account assignment
        /// </summary>
        /// <param name="extensionId">The extension id to assign the access to.</param>
        public virtual async Task<ICollection<ExtensionOperationResult>> AssignExtensionToAllEligibleUsersAsync(string extensionId, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(extensionId, nameof(extensionId));

            using (new OperationScope(LicensingResourceIds.AreaName, "AssignExtensionToAllEligibleUsers"))
            {
                return await SendAsync<ICollection<ExtensionOperationResult>>(method: HttpMethod.Put,
                    locationId: LicensingResourceIds.ExtensionEntitlementsLocationId,
                    version: new ApiResourceVersion(currentApiVersion, LicensingResourceVersions.ExtensionEntitlementsResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    routeValues: new { extensionId = extensionId },
                    userState: userState).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns users that are currently eligible to assign the extension to.
        /// the list is filtered based on the value of ExtensionFilterOptions
        /// </summary>
        /// <param name="requestContext">The application level request context.</param>
        /// <param name="extensionId">The extension to check the eligibility of the users for.</param>
        /// <param name="options">The options to filter the list.</param>
        public virtual async Task<IList<Guid>> GetEligibleUsersForExtensionAsync(string extensionId, ExtensionFilterOptions options = ExtensionFilterOptions.None, object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(extensionId, nameof(extensionId));

            using (new OperationScope(LicensingResourceIds.AreaName, "GetEligibleUsersForExtension"))
            {
                var queryParameters = new List<KeyValuePair<String, String>>
                {
                    { "options", options }
                };
                return await SendAsync<IList<Guid>>(method: HttpMethod.Get,
                    locationId: LicensingResourceIds.ExtensionEntitlementsLocationId,
                    version: new ApiResourceVersion(currentApiVersion, LicensingResourceVersions.ExtensionEntitlementsResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    queryParameters: queryParameters,
                    routeValues: new { extensionId = extensionId },
                    userState: userState).ConfigureAwait(false);
            }
        }

        #endregion Public Methods

        protected static readonly Version currentApiVersion = new Version(3, 1);
    }
}