using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Contracts.Licensing;

namespace Microsoft.VisualStudio.Services.Licensing.Client
{
    [ResourceArea(LicensingResourceIds.AreaId)]
    public class LicensingHttpClient : LicensingCompatHttpClientBase
    {
        #region Constructors

        public LicensingHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

        public LicensingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
           : base(baseUrl, credentials, settings)
        {
        }

        public LicensingHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public LicensingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public LicensingHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion Constructors

        #region Public methods

        public virtual async Task<IEnumerable<IUsageRight>> GetUsageRightsAsync(
            string rightName = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetUsageRights"))
            {
                if (rightName != null)
                {
                    ArgumentUtility.CheckStringForInvalidCharacters(rightName, "rightName");
                }

                var rights = await SendAsync<IEnumerable<UsageRight>>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.UsageRightsLocationid,
                    routeValues: new { rightName = rightName },
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.UsageRightsResourcePreviewVersion),
                    cancellationToken: cancellationToken,
                    userState: userState
                ).ConfigureAwait(false);

                return rights?.Select(right => (IUsageRight)right);
            }
        }

        public virtual async Task<IEnumerable<IServiceRight>> GetServiceRightsAsync(
            string rightName = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetServiceRights"))
            {
                if (rightName != null)
                {
                    ArgumentUtility.CheckStringForInvalidCharacters(rightName, "rightName");
                }

                var rights = await SendAsync<IEnumerable<ServiceRight>>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.ServiceRightsLocationid,
                    routeValues: new { rightName = rightName },
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.ServiceRightsResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    userState: userState
                ).ConfigureAwait(false);

                if (rights == null)
                {
                    return Enumerable.Empty<IServiceRight>();
                }

                return rights.Select(right => (IServiceRight)right);
            }
        }

        public virtual async Task<ClientRightsContainer> GetClientRightsContainerAsync(
            ClientRightsQueryContext queryContext,
            ClientRightsTelemetryContext telemetryContext = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetClientRightsContainer"))
            {
                ValidateClientRightsQueryContext(queryContext);
                ValidateClientRightsTelemetryContext(telemetryContext);

                var queryParameters = new List<KeyValuePair<string, string>>
                {
                    { "productVersion", queryContext.ProductVersion }
                };
                if (queryContext.ProductEdition != null)
                {
                    queryParameters.Add("edition", queryContext.ProductEdition);
                }
                if (queryContext.ReleaseType != null)
                {
                    queryParameters.Add("relType", queryContext.ReleaseType);
                }
                if (queryContext.IncludeCertificate)
                {
                    queryParameters.Add("includeCertificate", "true");
                }
                if (queryContext.Canary != null)
                {
                    queryParameters.Add("canary", queryContext.Canary);
                }
                if (queryContext.MachineId != null)
                {
                    queryParameters.Add("machineId", queryContext.MachineId);
                }
                SerializeTelemetryContextAsOptionalQueryParameters(telemetryContext, queryParameters);

                var container = await SendAsync<ClientRightsContainer>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.ClientRightsLocationid,
                    routeValues: new { rightName = queryContext.ProductFamily },
                    queryParameters: queryParameters,
                    cancellationToken: cancellationToken,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.ClientRightsResourcePreviewVersion)
                ).ConfigureAwait(false);

                return container;
            }
        }

        public virtual async Task<byte[]> GetCertificateAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))

        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetCertificate"))
            {
                var response = await SendAsync(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.CertificateLocationid,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.CertificateResourcePreviewVersion),
                    cancellationToken: cancellationToken,
                    userState: userState
                ).ConfigureAwait(false);

                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        public virtual async Task<IEnumerable<MsdnEntitlement>> GetMsdnEntitlementsAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetMsdnEntitlements"))
            {
                var entitlements = await SendAsync<IEnumerable<MsdnEntitlement>>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.MsdnEntitlementsLocationId,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.MsdnResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    userState: userState
                ).ConfigureAwait(false);

                return entitlements;
            }
        }

        public virtual async Task<IDictionary<string, bool>> ComputeExtensionRightsAsync(
            IEnumerable<string> extensionIds,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "ComputeExtensionRights"))
            {
                var content = new ObjectContent<IEnumerable<string>>(extensionIds, new VssJsonMediaTypeFormatter(true));

                var extensionRights = await SendAsync<IDictionary<string, bool>>(
                    method: HttpMethod.Post,
                    locationId: LicensingResourceIds.ExtensionRightsLocationId,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.ExtensionRightsResourceRtmVersion),
                    content: content,
                    cancellationToken: cancellationToken,
                    userState: userState
                ).ConfigureAwait(false);

                return extensionRights;
            }
        }

        public virtual async Task<ExtensionRightsResult> GetExtensionRightsAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetExtensionRights"))
            {

                var extensionRights = await SendAsync<ExtensionRightsResult>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.ExtensionRightsLocationId,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.ExtensionRightsResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    userState: userState
                ).ConfigureAwait(false);

                return extensionRights;
            }
        }

        public virtual async Task<IEnumerable<AccountLicenseUsage>> GetAccountLicensesUsageAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetAccountLicensesUsage"))
            {
                var accountLicensesUsage = await SendAsync<IEnumerable<AccountLicenseUsage>>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.UsageLocationid,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.AccountUsageResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    userState: userState
                ).ConfigureAwait(false);

                return accountLicensesUsage ?? Enumerable.Empty<AccountLicenseUsage>();
            }
        }

        public virtual async Task<IEnumerable<AccountEntitlement>> GetAccountEntitlementsAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetAccountEntitlements"))
            {
                var response = await SendAsync<IEnumerable<AccountEntitlement>>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.EntitlementsLocationid,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    userState: userState
                ).ConfigureAwait(false);

                return response;
            }
        }

        public virtual async Task<IEnumerable<AccountEntitlement>> GetAccountEntitlementsAsync(
            int top,
            int skip = 0,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var queryParams = new List<KeyValuePair<string, string>>
            {
                { "top", top },
                { "skip", skip }
            };
            using (new OperationScope(LicensingResourceIds.AreaName, "GetAccountEntitlements"))
            {
                var response = await SendAsync<IEnumerable<AccountEntitlement>>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.EntitlementsLocationid,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    queryParameters: queryParams,
                    userState: userState
                ).ConfigureAwait(false);

                return response;
            }
        }

        public virtual async Task<IEnumerable<AccountEntitlement>> GetAccountEntitlementsAsync(
            IList<Guid> userIds,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetAccountEntitlements"))
            {
                ArgumentUtility.CheckEnumerableForNullOrEmpty(userIds, nameof(userIds));
                var content = new ObjectContent<IList<Guid>>(userIds, new VssJsonMediaTypeFormatter(true));

                return await SendAsync<IEnumerable<AccountEntitlement>>(method: HttpMethod.Post,
                    locationId: LicensingResourceIds.UserEntitlementsBatchLocationId,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementsBatchResourcePreviewVersion),
                    cancellationToken: cancellationToken,
                    routeValues: new { action = "GetUsersEntitlements" },
                    content: content,
                    userState: userState).ConfigureAwait(false);
            }
        }

        public virtual async Task<IList<AccountEntitlement>> ObtainAvailableAccountEntitlementsAsync(
            IList<Guid> userIds,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetAccountEntitlements"))
            {
                ArgumentUtility.CheckEnumerableForNullOrEmpty(userIds, nameof(userIds));
                var content = new ObjectContent<IList<Guid>>(userIds, new VssJsonMediaTypeFormatter(true));

                return await SendAsync<IList<AccountEntitlement>>(method: HttpMethod.Post,
                    locationId: LicensingResourceIds.UserEntitlementsBatchLocationId,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementsBatchResourcePreviewVersion),
                    cancellationToken: cancellationToken,
                    routeValues: new { action = "GetAvailableUsersEntitlements" },
                    content: content,
                    userState: userState).ConfigureAwait(false);
            }
        }

        public virtual async Task<AccountEntitlement> GetAccountEntitlementAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetAccountEntitlement"))
            {
                var response = await SendAsync<AccountEntitlement>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.CurrentUserEntitlementsLocationId,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    userState: userState
                ).ConfigureAwait(false);

                return response;
            }
        }

        public virtual Task<AccountEntitlement> GetAccountEntitlementAsync(
           Guid userId,
           object userState = null,
           CancellationToken cancellationToken = default(CancellationToken))
        {
            return GetAccountEntitlementAsync(
                userId: userId,
                queryParams: null,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        public virtual Task<AccountEntitlement> GetAccountEntitlementAsync(
            Guid userId,
            bool determineRights,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(userId, nameof(userId));
            var queryParams = new List<KeyValuePair<string, string>>
            {
                { "determineRights", determineRights }
            };
            return GetAccountEntitlementAsync(userId, queryParams, userState, cancellationToken);
        }

        public virtual Task<AccountEntitlement> GetAccountEntitlementAsync(
            Guid userId,
            bool determineRights,
            bool createIfNotExists,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(userId, nameof(userId));
            var queryParams = new List<KeyValuePair<string, string>>
            {
                { "determineRights", determineRights },
                { "createIfNotExists", createIfNotExists }
            };

            return GetAccountEntitlementAsync(userId, queryParams, userState, cancellationToken);
        }

        private async Task<AccountEntitlement> GetAccountEntitlementAsync(
            Guid userId,
            List<KeyValuePair<string, string>> queryParams,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetAccountEntitlement"))
            {
                var response = await SendAsync<AccountEntitlement>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.UserEntitlementsLocationId,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    routeValues: new { userId = userId },
                    userState: userState,
                    queryParameters: queryParams
                ).ConfigureAwait(false);

                return response;
            }
        }

        public virtual async Task<AccountEntitlement> AssignEntitlementAsync(
            Guid userId,
            License license,
            bool dontNotifyUser = false,
            LicensingOrigin origin = LicensingOrigin.None,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "AssignEntitlement"))
            {
                ArgumentUtility.CheckForEmptyGuid(userId, "userId");
                ArgumentUtility.CheckForNull(license, "license");

                var queryParams = new List<KeyValuePair<string, string>>
                {
                    { "dontNotifyUser", dontNotifyUser },
                    { "origin", origin }
                };

                var response = await SendAsync<AccountEntitlement>(
                    method: HttpMethod.Put,
                    locationId: LicensingResourceIds.UserEntitlementsLocationId,
                    routeValues: new { userId = userId },
                    content: CreateContentFor(new AccountEntitlementUpdateModel { License = license }),
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    userState: userState,
                    queryParameters: queryParams
                ).ConfigureAwait(false);

                return response;
            }
        }

        public virtual async Task<AccountEntitlement> AssignAvailableEntitlementAsync(
            Guid userId,
            bool dontNotifyUser = false,
            LicensingOrigin origin = LicensingOrigin.None,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "AssignEntitlement"))
            {
                ArgumentUtility.CheckForEmptyGuid(userId, "userId");

                var queryParams = new List<KeyValuePair<string, string>>
                {
                    { "dontNotifyUser", dontNotifyUser },
                    { "origin", origin }
                };

                var response = await SendAsync<AccountEntitlement>(
                    method: HttpMethod.Put,
                    locationId: LicensingResourceIds.UserEntitlementsLocationId,
                    routeValues: new { userId = userId },
                    content: CreateContentFor(new AccountEntitlementUpdateModel { License = License.Auto }),
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    userState: userState,
                    queryParameters: queryParams
                ).ConfigureAwait(false);

                return response;
            }
        }

        public virtual async Task DeleteEntitlementAsync(
            Guid userId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckForEmptyGuid(userId, "userId");

            var queryParams = new List<KeyValuePair<string, string>>();

            await SendAsync(
                method: HttpMethod.Delete,
                locationId: LicensingResourceIds.UserEntitlementsLocationId,
                routeValues: new { userId = userId },
                version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                cancellationToken: cancellationToken,
                userState: userState,
                queryParameters: queryParams
            ).ConfigureAwait(false);
        }

        public virtual async Task<bool> RegisterExtensionLicenseAsync(
            ExtensionLicenseData extensionLicenseData,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "ExtensionLicenseRegistration"))
            {
                var content = new ObjectContent<ExtensionLicenseData>(extensionLicenseData, new VssJsonMediaTypeFormatter(true));

                var result = await SendAsync<bool>(
                    method: HttpMethod.Post,
                    locationId: LicensingResourceIds.ExtensionLicenseLocationId,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.ExtensionLicenseResourceRtmVersion),
                    content: content,
                    cancellationToken: cancellationToken,
                    userState: userState
                ).ConfigureAwait(false);

                return result;
            }
        }

        public virtual async Task<ExtensionLicenseData> GetExtensionLicenseDataAsync(
            string extensionId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "ExtensionLicenseRegistration"))
            {
                var result = await SendAsync<ExtensionLicenseData>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.ExtensionLicenseLocationId,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.ExtensionLicenseResourceRtmVersion),
                    cancellationToken: cancellationToken,
                    routeValues: new { extensionId = extensionId },
                    userState: userState
                ).ConfigureAwait(false);

                return result;
            }
        }

        public virtual async Task TransferIdentityRightsAsync(
            IEnumerable<KeyValuePair<Guid, Guid>> userIdTransferMap,
            bool? validateOnly = null,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("8671b016-fa74-4c88-b693-83bbb88c2264");
            HttpContent content = new ObjectContent<IEnumerable<KeyValuePair<Guid, Guid>>>(userIdTransferMap, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (validateOnly != null)
            {
                queryParams.Add("validateOnly", validateOnly.Value.ToString());
            }

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.LicensingRightsResourceRtmVersion),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        #endregion Public methods

        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get { return s_translatedExceptions; }
        }

        private static readonly Dictionary<string, Type> s_translatedExceptions = new Dictionary<string, Type>
        {
            { "InvalidRightNameException", typeof(InvalidRightNameException) },
            { "InvalidClientVersionException", typeof(InvalidClientVersionException) },
            { "InvalidClientRightsQueryContextException", typeof(InvalidClientRightsQueryContextException) },
            { "InvalidLicensingOperation", typeof(InvalidLicensingOperation) },
        };

        protected static readonly Version previewApiVersion = new Version(1, 0);
    }
}