using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using System.Threading;

namespace GitHub.Services.Compliance.Client
{
    [ResourceArea(ComplianceResourceIds.AreaId)]
    [Obsolete("This type is no longer used.")]
    public class ComplianceHttpClient : VssHttpClientBase
    {
        #region Constructors
        
        public ComplianceHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

         public ComplianceHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public ComplianceHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public ComplianceHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public ComplianceHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion

        public virtual async Task<ComplianceConfiguration> GetComplianceConfiguration(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(ComplianceResourceIds.AreaName, "GetComplianceConfiguration"))
            {
                var complianceConfiguration = await SendAsync<ComplianceConfiguration>(
                    method: HttpMethod.Get,
                    locationId: ComplianceResourceIds.ConfigurationLocationId,
                    version: new ApiResourceVersion(apiVersion, ComplianceResourceVersions.ConfigurationResourceVersion),
                    userState: userState,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return complianceConfiguration;
            }
        }

        public virtual async Task<AccountRightsValidation> ValidateAccountRights(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(ComplianceResourceIds.AreaName, "ValidateAccountRights"))
            {
                var queryParams = new List<KeyValuePair<String, String>>();

                var accountRightsValidation = await SendAsync<AccountRightsValidation>(
                    method: HttpMethod.Get,
                    locationId: ComplianceResourceIds.AccountRightsLocationId,
                    version: new ApiResourceVersion(apiVersion, ComplianceResourceVersions.AccountRightsResourceVersion),
                    userState: userState,
                    queryParameters: queryParams,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return accountRightsValidation;
            }
        }

        public virtual async Task<ComplianceValidation> ValidateBusinessPolicy(object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(ComplianceResourceIds.AreaName, "ValidateBusinessPolicy"))
            {
                var queryParams = new List<KeyValuePair<String, String>>();
                
                var complianceValidation = await SendAsync<ComplianceValidation>(
                    method: HttpMethod.Get,
                    locationId: ComplianceResourceIds.ValidationLocationId,
                    version: new ApiResourceVersion(apiVersion, ComplianceResourceVersions.ValidationResourceVersion),
                    userState: userState,
                    queryParameters: queryParams,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return complianceValidation;
            }
        }

        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get { return s_translatedExceptions; }
        }

        private static readonly Dictionary<string, Type> s_translatedExceptions = new Dictionary<string, Type>
        {
            // None yet.
        };

        protected static readonly Version apiVersion = new Version(1, 0);
    }
}
