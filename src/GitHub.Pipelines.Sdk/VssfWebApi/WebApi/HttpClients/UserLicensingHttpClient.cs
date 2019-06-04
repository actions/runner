using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Licensing;
using GitHub.Services.WebApi;

namespace GitHub.Services.UserLicensing.Client
{
    [ResourceArea(UserLicensingResourceIds.AreaId)]
    public class UserLicensingHttpClient : UserLicensingHttpClientBase
    {
        public UserLicensingHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public UserLicensingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public UserLicensingHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public UserLicensingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public UserLicensingHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public new virtual async Task<Stream> GetCertificateAsync(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(UserLicensingResourceIds.AreaName, "GetCertificate"))
            {
                return await base.GetCertificateAsync(userState, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<List<MsdnEntitlement>> GetMsdnEntitlementsAsync(
            string descriptor,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(UserLicensingResourceIds.AreaName, "GetMsdnEntitlements"))
            {
                return await base.GetEntitlementsAsync(descriptor, userState, cancellationToken).ConfigureAwait(false);
            }
        }

        public virtual async Task<ClientRightsContainer> GetClientRightsContainerAsync(
            string descriptor,
            ClientRightsQueryContext queryContext,
            ClientRightsTelemetryContext telemetryContext = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
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

                ClientRightsContainer container =  await base.GetClientRightsAsync(
                    descriptor: descriptor, 
                    rightName: queryContext.ProductFamily,
                    productVersion: queryContext.ProductVersion,
                    edition: queryContext.ProductEdition,
                    relType: queryContext.ReleaseType,
                    includeCertificate: queryContext.IncludeCertificate,
                    canary: queryContext.Canary,
                    machineId: queryContext.MachineId,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return container;
            }
        }

        public Task SetVisualStudioTrialInfoAsync(
            SubjectDescriptor descriptor,
            int majorVersion,
            int productFamilyId,
            int productEditionId,
            DateTime expirationDate,
            DateTime createdDate,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.SetVisualStudioTrialInfoAsync(
                descriptor: descriptor, 
                majorVersion: majorVersion, 
                productFamilyId: productFamilyId, 
                productEditionId: productEditionId, 
                expirationDate: expirationDate, 
                createdDate: createdDate, 
                userState: userState, 
                cancellationToken: cancellationToken);
        }

        public Task<long> GetVisualStudioTrialExpirationAsync(
            SubjectDescriptor descriptor,
            string machineId,
            int majorVersion,
            int productFamilyId,
            int productEditionId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetVisualStudioTrialExpirationAsync(
                descriptor: descriptor,
                machineId: machineId,
                majorVersion: majorVersion,
                productFamilyId: productFamilyId,
                productEditionId: productEditionId,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        private static void ValidateClientRightsQueryContext(ClientRightsQueryContext queryContext)
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

        private static void ValidateClientRightsTelemetryContext(ClientRightsTelemetryContext telemetryContext)
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

        private static void SerializeTelemetryContextAsOptionalQueryParameters(ClientRightsTelemetryContext telemetryContext, IList<KeyValuePair<String, String>> queryParameters)
        {
            if (telemetryContext == null
                || telemetryContext.Attributes == null
                || telemetryContext.Attributes.Count < 1)
            {
                return;
            }

            foreach (var kv in telemetryContext.Attributes)
            {
                queryParameters.Add(new KeyValuePair<String, String>(QueryParameters.TelemetryPrefix + kv.Key, kv.Value ?? string.Empty));
            }
        }
    }
}
