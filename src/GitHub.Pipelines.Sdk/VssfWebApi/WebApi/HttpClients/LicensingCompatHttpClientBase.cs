using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Licensing.Client
{
    /// <summary>
    /// This class contains deprecated overloads to maintain binary compatibility
    /// </summary>
    /// <remarks>
    /// See: https://vsowiki.com/index.php?title=Rest_Client_Generation#Toolsets.5CAPICompatCheck.5CAPICompatCheck.targets.28x.2Cy.29:_error
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class LicensingCompatHttpClientBase : VssHttpClientBase
    {
        protected LicensingCompatHttpClientBase(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
        {
        }

        protected LicensingCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings) : base(baseUrl, credentials, settings)
        {
        }

        protected LicensingCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers) : base(baseUrl, credentials, handlers)
        {
        }

        protected LicensingCompatHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler) : base(baseUrl, pipeline, disposeHandler)
        {
        }

        protected LicensingCompatHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers) : base(baseUrl, credentials, settings, handlers)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<IEnumerable<IUsageRight>> GetUsageRightsAsync(
            string rightName,
            object userState)
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
                    cancellationToken: default(CancellationToken),
                    userState: userState
                ).ConfigureAwait(false);

                return rights?.Select(right => (IUsageRight)right);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<IEnumerable<IServiceRight>> GetServiceRightsAsync(
            string rightName,
            object userState)
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
                    cancellationToken: default(CancellationToken),
                    userState: userState
                ).ConfigureAwait(false);

                if (rights == null)
                {
                    return Enumerable.Empty<IServiceRight>();
                }

                return rights.Select(right => (IServiceRight)right);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<byte[]> GetCertificateAsync(
            object userState)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetCertificate"))
            {
                var response = await SendAsync(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.CertificateLocationid,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.CertificateResourcePreviewVersion),
                    cancellationToken: default(CancellationToken),
                    userState: userState
                ).ConfigureAwait(false);

                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<ClientRightsContainer> GetClientRightsContainerAsync(
            ClientRightsQueryContext queryContext,
            ClientRightsTelemetryContext telemetryContext,
            Object userState)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetClientRightsContainer"))
            {
                ValidateClientRightsQueryContext(queryContext);
                ValidateClientRightsTelemetryContext(telemetryContext);

                var queryParameters = new List<KeyValuePair<String, String>>
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
                    cancellationToken: default(CancellationToken),
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.ClientRightsResourcePreviewVersion)
                ).ConfigureAwait(false);

                return container;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<IEnumerable<AccountLicenseUsage>> GetAccountLicensesUsageAsync(
            object userState)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetAccountLicensesUsage"))
            {
                var accountLicensesUsage = await SendAsync<IEnumerable<AccountLicenseUsage>>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.UsageLocationid,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.AccountUsageResourceRtmVersion),
                    cancellationToken: default(CancellationToken),
                    userState: userState
                ).ConfigureAwait(false);

                return accountLicensesUsage ?? Enumerable.Empty<AccountLicenseUsage>();
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<IEnumerable<AccountEntitlement>> GetAccountEntitlementsAsync(
            object userState)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetAccountEntitlements"))
            {
                var response = await SendAsync<IEnumerable<AccountEntitlement>>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.EntitlementsLocationid,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                    cancellationToken: default(CancellationToken),
                    userState: userState
                ).ConfigureAwait(false);

                return response;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<AccountEntitlement> GetAccountEntitlementAsync(
            object userState)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "GetAccountEntitlement"))
            {
                var response = await SendAsync<AccountEntitlement>(
                    method: HttpMethod.Get,
                    locationId: LicensingResourceIds.CurrentUserEntitlementsLocationId,
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                    cancellationToken: default(CancellationToken),
                    userState: userState
                ).ConfigureAwait(false);

                return response;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<AccountEntitlement> AssignEntitlementAsync(
            Guid userId,
            License license,
            object userState)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "AssignEntitlement"))
            {
                ArgumentUtility.CheckForEmptyGuid(userId, "userId");
                ArgumentUtility.CheckForNull(license, "license");

                var queryParams = new List<KeyValuePair<string, string>> { { "dontNotifyUser", false } };

                var response = await SendAsync<AccountEntitlement>(
                    method: HttpMethod.Put,
                    locationId: LicensingResourceIds.UserEntitlementsLocationId,
                    routeValues: new { userId = userId },
                    content: CreateContentFor(new AccountEntitlementUpdateModel { License = license }),
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                    cancellationToken: default(CancellationToken),
                    userState: userState,
                    queryParameters: queryParams
                ).ConfigureAwait(false);

                return response;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<AccountEntitlement> AssignEntitlementAsync(
            Guid userId,
            License license,
            object userState,
            CancellationToken cancellationToken)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "AssignEntitlement"))
            {
                ArgumentUtility.CheckForEmptyGuid(userId, "userId");
                ArgumentUtility.CheckForNull(license, "license");

                var queryParams = new List<KeyValuePair<string, string>> { { "dontNotifyUser", false } };

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

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<AccountEntitlement> AssignAvailableEntitlementAsync(
            Guid userId,
            object userState)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "AssignEntitlement"))
            {
                ArgumentUtility.CheckForEmptyGuid(userId, "userId");

                var queryParams = new List<KeyValuePair<string, string>> { { "dontNotifyUser", false } };

                var response = await SendAsync<AccountEntitlement>(
                    method: HttpMethod.Put,
                    locationId: LicensingResourceIds.UserEntitlementsLocationId,
                    routeValues: new { userId = userId },
                    content: CreateContentFor(new AccountEntitlementUpdateModel { License = License.Auto }),
                    version: new ApiResourceVersion(previewApiVersion, LicensingResourceVersions.EntitlementResourceRtmVersion),
                    cancellationToken: default(CancellationToken),
                    userState: userState,
                    queryParameters: queryParams
                ).ConfigureAwait(false);

                return response;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<AccountEntitlement> AssignEntitlementAsync(
            Guid userId,
            License license,
            bool dontNotifyUser,
            object userState,
            CancellationToken cancellationToken)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "AssignEntitlement"))
            {
                ArgumentUtility.CheckForEmptyGuid(userId, "userId");
                ArgumentUtility.CheckForNull(license, "license");

                var queryParams = new List<KeyValuePair<string, string>> { { "dontNotifyUser", dontNotifyUser } };

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

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<AccountEntitlement> AssignAvailableEntitlementAsync(
            Guid userId,
            object userState,
            CancellationToken cancellationToken)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "AssignEntitlement"))
            {
                ArgumentUtility.CheckForEmptyGuid(userId, "userId");

                var queryParams = new List<KeyValuePair<string, string>> { { "dontNotifyUser", false } };

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

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete]
        public virtual async Task<AccountEntitlement> AssignAvailableEntitlementAsync(
            Guid userId,
            bool dontNotifyUser,
            object userState,
            CancellationToken cancellationToken)
        {
            using (new OperationScope(LicensingResourceIds.AreaName, "AssignEntitlement"))
            {
                ArgumentUtility.CheckForEmptyGuid(userId, "userId");

                var queryParams = new List<KeyValuePair<string, string>> { { "dontNotifyUser", dontNotifyUser } };

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

        #region Private helpers

        /// <summary>
        /// Creates an <see cref="ObjectContent{T}"/> for the specified value using the current <see cref="VssHttpClientBase.Formatter"/>
        /// </summary>
        /// <typeparam name="T">The type for the value</typeparam>
        /// <param name="value">The value</param>
        /// <returns>An <see cref="ObjectContent{T}"/> for the provided value</returns>
        protected ObjectContent<T> CreateContentFor<T>(T value)
        {
            return new ObjectContent<T>(value, this.Formatter);
        }

        protected static void ValidateClientRightsQueryContext(ClientRightsQueryContext queryContext)
        {
            ArgumentUtility.CheckForNull(queryContext, "queryContext");

            // Required parameters
            ArgumentUtility.CheckStringForNullOrEmpty(queryContext.ProductFamily, "queryContext.ProductFamily");
            ArgumentUtility.CheckStringForInvalidCharacters(queryContext.ProductFamily, "queryContext.ProductFamily");
            ArgumentUtility.CheckStringForNullOrEmpty(queryContext.ProductVersion, "queryContext.ProductVersion");
            ArgumentUtility.CheckStringForInvalidCharacters(queryContext.ProductVersion, "queryContext.ProductVersion");

            // Optional parameters
            if (queryContext.ProductEdition != null)
            {
                ArgumentUtility.CheckStringForInvalidCharacters(queryContext.ProductEdition, "queryContext.ProductEdition");
            }
            if (queryContext.ReleaseType != null)
            {
                ArgumentUtility.CheckStringForInvalidCharacters(queryContext.ReleaseType, "queryContext.ReleaseType");
            }
            if (queryContext.Canary != null)
            {
                ArgumentUtility.CheckStringForInvalidCharacters(queryContext.Canary, "queryContext.Canary");
            }
            if (queryContext.MachineId != null)
            {
                ArgumentUtility.CheckStringForInvalidCharacters(queryContext.MachineId, "queryContext.MachineId");
            }
        }

        protected static void ValidateClientRightsTelemetryContext(ClientRightsTelemetryContext telemetryContext)
        {
            // Optional telemetry
            if (telemetryContext == null
                || telemetryContext.Attributes == null
                || telemetryContext.Attributes.Count < 1)
            {
                return;
            }

            foreach (var kv in telemetryContext.Attributes)
            {
                ArgumentUtility.CheckStringForInvalidCharacters(kv.Key, "Key");
                if (string.IsNullOrEmpty(kv.Value))
                {
                    ArgumentUtility.CheckStringForInvalidCharacters(kv.Value, "Value");
                }
            }
        }

        protected static void SerializeTelemetryContextAsOptionalQueryParameters(ClientRightsTelemetryContext telemetryContext, IList<KeyValuePair<string, string>> queryParameters)
        {
            if (telemetryContext == null
                || telemetryContext.Attributes == null
                || telemetryContext.Attributes.Count < 1)
            {
                return;
            }

            foreach (var kv in telemetryContext.Attributes)
            {
                queryParameters.Add(new KeyValuePair<string, string>(QueryParameters.TelemetryPrefix + kv.Key, kv.Value ?? string.Empty));
            }
        }

        #endregion Private helpers

        private static readonly Version previewApiVersion = new Version(1, 0);
    }
}
