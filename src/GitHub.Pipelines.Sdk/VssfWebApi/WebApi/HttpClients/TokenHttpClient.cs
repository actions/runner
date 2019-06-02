using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Tokens;

namespace Microsoft.VisualStudio.Services.WebApi.HttpClients
{
    [ResourceArea(TokenResourceIds.AreaId)]
    public class TokenHttpClient : VssHttpClientBase
    {
        private static ApiResourceVersion DefaultApiResourceVersion;
        private static Dictionary<string, Type> TranslatedExceptionsMap;

        static TokenHttpClient()
        {
            DefaultApiResourceVersion = new ApiResourceVersion(1.0);

            TranslatedExceptionsMap = new Dictionary<string, Type>();
        }

        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get { return TranslatedExceptionsMap; }
        }

        public TokenHttpClient(
            Uri baseUrl,
            VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TokenHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TokenHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TokenHttpClient(
            Uri baseUrl,
            VssCredentials credentials,
            VssHttpRequestSettings settings,
            params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TokenHttpClient(
            Uri baseUrl,
            HttpMessageHandler pipeline,
            bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        public virtual Task<string> GetAadUserAccessToken(
            string resource,
            string tenantId,
            IdentityDescriptor identityDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(resource, "resource");
            ArgumentUtility.CheckStringForNullOrEmpty(tenantId, "tenantId");
            ArgumentUtility.CheckForNull(identityDescriptor, "identityDescriptor");

            var queryParams = new List<KeyValuePair<String, String>>();
            queryParams.Add("resource", resource);
            queryParams.Add("tenantId", tenantId);
            queryParams.Add("identity", identityDescriptor.IdentityType + ";" + identityDescriptor.Identifier);

            using (new OperationScope(TokenResourceIds.AreaName, "GetAadUserAccessToken"))
            {
                return GetAsync<string>(
                    TokenResourceIds.AadUserToken,
                    version: DefaultApiResourceVersion,
                    queryParameters: queryParams,
                    userState: userState,
                    cancellationToken: cancellationToken);
            }
        }

        public virtual Task<string> GetAadAppAccessToken(
            string resource,
            string tenantId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(resource, "resource");
            ArgumentUtility.CheckStringForNullOrEmpty(tenantId, "tenantId");

            var queryParams = new List<KeyValuePair<String, String>>();
            queryParams.Add("resource", resource);
            queryParams.Add("tenantId", tenantId);

            using (new OperationScope(TokenResourceIds.AreaName, "GetAadAccessToken"))
            {
                return GetAsync<string>(
                    TokenResourceIds.AadAppToken,
                    version: DefaultApiResourceVersion,
                    queryParameters: queryParams,
                    userState: userState,
                    cancellationToken: cancellationToken);
            }
        }

        public virtual Task<string> UpdateRefreshTokenOnBehalfOf(
            string accessToken,
            string resource,
            string tenantId,
            IdentityDescriptor identityDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(accessToken, "accessToken");
            ArgumentUtility.CheckStringForNullOrEmpty(resource, "resource");
            ArgumentUtility.CheckStringForNullOrEmpty(tenantId, "tenantId");
            ArgumentUtility.CheckForNull(identityDescriptor, "identityDescriptor");

            var queryParams = new List<KeyValuePair<String, String>>();
            queryParams.Add("resource", resource);
            queryParams.Add("tenantId", tenantId);
            queryParams.Add("identity", identityDescriptor.IdentityType + ";" + identityDescriptor.Identifier);

            using (new OperationScope(TokenResourceIds.AreaName, "UpdateRefreshTokenOnBehalfOf"))
            {
                return PostAsync<dynamic,string>(
                    value: new { accessToken = accessToken },
                    locationId: TokenResourceIds.AadUserToken,
                    version: DefaultApiResourceVersion,
                    queryParameters: queryParams,
                    userState: userState,
                    cancellationToken: cancellationToken
                    );
            }            
        }

        public virtual Task<string> GetUserAccessTokenFromAuthCode(
            string authCode,
            string resource,
            string tenantId,
            string replyToUri,
            IdentityDescriptor identityDescriptor,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentUtility.CheckStringForNullOrEmpty(authCode, "authCode");
            ArgumentUtility.CheckStringForNullOrEmpty(resource, "resource");
            ArgumentUtility.CheckStringForNullOrEmpty(tenantId, "tenantId");
            ArgumentUtility.CheckStringForNullOrEmpty(replyToUri, "replyToUri");
            ArgumentUtility.CheckForNull(identityDescriptor, "identityDescriptor");

            var queryParams = new List<KeyValuePair<String, String>>();
            queryParams.Add("resource", resource);
            queryParams.Add("tenantId", tenantId);
            queryParams.Add("identity", identityDescriptor.IdentityType + ";" + identityDescriptor.Identifier);
            queryParams.Add("replyToUri", replyToUri);

            using (new OperationScope(TokenResourceIds.AreaName, "GetUserAccessTokenFromAuthCode"))
            {
                return PostAsync<dynamic, string>(
                    value: authCode,
                    locationId: TokenResourceIds.AadUserToken,
                    version: DefaultApiResourceVersion,
                    queryParameters: queryParams,
                    userState: userState,
                    cancellationToken: cancellationToken
                    );
            }
        }
    }
}
