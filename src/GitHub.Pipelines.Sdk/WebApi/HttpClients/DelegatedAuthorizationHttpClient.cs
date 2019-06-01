using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Tokens;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Jwt;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization.Client
{
    [ResourceArea(TokenResourceIds.AreaId)]
    [Obsolete("This class has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient or Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.TokenHttpClient instead.")]
    public class DelegatedAuthorizationHttpClient : VssHttpClientBase
    {
        static DelegatedAuthorizationHttpClient()
        {
            s_translatedExceptions = new Dictionary<String, Type>();
            s_currentApiVersion = new ApiResourceVersion(2.0);
        }

        public DelegatedAuthorizationHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public DelegatedAuthorizationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public DelegatedAuthorizationHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public DelegatedAuthorizationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public DelegatedAuthorizationHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #region Operations on access token controller

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.GetAccessTokenAsync instead.")]
        public async Task<AccessToken> Exchange(string key, Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.Exchange(false, key, userState, cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.GetAccessTokenAsync instead.")]
        public virtual async Task<AccessToken> Exchange(bool isPublic, string key, Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenResourceIds.AreaName, "Exchange"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("isPublic", isPublic.ToString());

                return await SendAsync<AccessToken>(
                    HttpMethod.Get,
                    TokenResourceIds.AccessToken,
                    new { key = key },
                    queryParameters: queryParameters,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        #region Operations on session token controller

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.CreateSessionTokenAsync instead.")]
        public async Task<SessionToken> CreateSessionToken(
            Guid? clientId = null,
            Guid? userId = null,
            string displayName = null,
            DateTime? validTo = null,
            string scope = null,
            IList<Guid> targetAccounts = null,
            SessionTokenType tokenType = SessionTokenType.SelfDescribing,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.CreateSessionToken(null, null, false, clientId, userId, displayName, validTo, scope, targetAccounts, tokenType, userState, cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.CreateSessionTokenAsync instead.")]
        public async Task<SessionToken> CreateSessionToken(
            bool isPublic,
            Guid? clientId = null,
            Guid? userId = null,
            string displayName = null,
            DateTime? validTo = null,
            string scope = null,
            IList<Guid> targetAccounts = null,
            SessionTokenType tokenType = SessionTokenType.SelfDescribing,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.CreateSessionToken(null, null, isPublic, clientId, userId, displayName, validTo, scope, targetAccounts, tokenType, userState, cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.CreateSessionTokenAsync instead.")]
        public async Task<SessionToken> CreateSessionToken(
            string source,
            bool isPublic,
            Guid? clientId = null,
            Guid? userId = null,
            string displayName = null,
            DateTime? validTo = null,
            string scope = null,
            IList<Guid> targetAccounts = null,
            SessionTokenType tokenType = SessionTokenType.SelfDescribing,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.CreateSessionToken(null, source, isPublic, clientId, userId, displayName, validTo, scope, targetAccounts, tokenType, userState, cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.CreateSessionTokenAsync instead.")]
        public async Task<SessionToken> CreateSessionToken(
            string publicData,
            string source,
            bool isPublic,
            Guid? clientId = null,
            Guid? userId = null,
            string displayName = null,
            DateTime? validTo = null,
            string scope = null,
            IList<Guid> targetAccounts = null,
            SessionTokenType tokenType = SessionTokenType.SelfDescribing,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenResourceIds.AreaName, "CreateSessionToken"))
            {
                SessionToken sessionToken = new SessionToken();

                if (userId.HasValue)
                {
                    sessionToken.UserId = userId.Value;
                }

                if (validTo.HasValue)
                {
                    sessionToken.ValidTo = validTo.Value;
                }

                if (clientId.HasValue)
                {
                    sessionToken.ClientId = clientId.Value;
                }

                sessionToken.DisplayName = displayName;
                sessionToken.Scope = scope;
                sessionToken.TargetAccounts = targetAccounts;
                sessionToken.Source = source;
                sessionToken.PublicData = publicData;

                HttpContent content = new ObjectContent<SessionToken>(sessionToken, base.Formatter);

                var query = new List<KeyValuePair<String, String>>();

                if (tokenType != SessionTokenType.SelfDescribing)
                {
                    query.Add(QueryParameters.TokenType, tokenType.ToString());
                }

                query.Add("isPublic", isPublic.ToString());

                return await SendAsync<SessionToken>(
                    HttpMethod.Post,
                    TokenResourceIds.SessionToken,
                    version: s_currentApiVersion,
                    content: content,
                    queryParameters: query,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.CreateSessionTokenAsync instead.")]
        public async Task<SessionToken> UpdateSessionToken(
            Guid authorizationId,
            string displayName = null,
            string scope = null,
            DateTime? validTo = null,
            IList<Guid> targetAccounts = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.UpdateSessionToken(false, authorizationId, displayName, scope, validTo, targetAccounts, userState, cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.CreateSessionTokenAsync instead.")]
        public async Task<SessionToken> UpdateSessionToken(
            bool isPublic,
            Guid authorizationId,
            string displayName = null,
            string scope = null,
            DateTime? validTo = null,
            IList<Guid> targetAccounts = null,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenResourceIds.AreaName, "UpdateSessionToken"))
            {
                SessionToken sessionToken = new SessionToken();

                sessionToken.AuthorizationId = authorizationId;
                if (validTo.HasValue)
                {
                    sessionToken.ValidTo = validTo.Value;
                }

                sessionToken.DisplayName = displayName;
                sessionToken.Scope = scope;
                sessionToken.TargetAccounts = targetAccounts;

                HttpContent content = new ObjectContent<SessionToken>(sessionToken, base.Formatter);
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("isPublic", isPublic.ToString());

                return await SendAsync<SessionToken>(
                    HttpMethod.Post,
                    TokenResourceIds.SessionToken,
                    version: s_currentApiVersion,
                    content: content,
                    queryParameters: queryParameters,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.GetSessionTokensAsync instead.")]
        public async Task<List<SessionToken>> ListSessionTokens(
            Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.ListSessionTokens(false, false, userState, cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.GetSessionTokensAsync instead.")]
        public async Task<List<SessionToken>> ListSessionTokens(
            bool isPublic,
            Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.ListSessionTokens(false, isPublic, userState, cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.GetSessionTokensAsync instead.")]
        public async Task<List<SessionToken>> ListSessionTokens(
            bool includePublicData,
            bool isPublic,
            Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenResourceIds.AreaName, "ListSessionTokens"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("isPublic", isPublic.ToString());
                queryParameters.Add("includePublicData", includePublicData.ToString());

                return await SendAsync<List<SessionToken>>(
                    HttpMethod.Get,
                    TokenResourceIds.SessionToken,
                    queryParameters: queryParameters,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.GetSessionTokenAsync instead.")]
        public async Task<SessionToken> GetSessionToken(
            Guid authorizationId,
            Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.GetSessionToken(false, authorizationId, userState, cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.GetSessionTokenAsync instead.")]
        public async Task<SessionToken> GetSessionToken(
            bool isPublic,
            Guid authorizationId,
            Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenResourceIds.AreaName, "GetSessionToken"))
            {
                var query = new List<KeyValuePair<String, String>>();
                query.Add("authorizationId", authorizationId.ToString());
                query.Add("isPublic", isPublic.ToString());

                return await SendAsync<SessionToken>(
                    HttpMethod.Get,
                    TokenResourceIds.SessionToken,
                    queryParameters: query,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.RemovePublicKeyAsync instead.")]
        public async Task RemovePublicKey(
            string publicKey,
            Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenResourceIds.AreaName, "RemovePublicKey"))
            {
                HttpContent content = new ObjectContent<SshPublicKey>(new SshPublicKey { Value = publicKey }, base.Formatter);

                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("remove", "true");

                await SendAsync(
                    HttpMethod.Post,
                    TokenResourceIds.SessionToken,
                    queryParameters: queryParameters,
                    content: content,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.RemovePublicKeyAsync instead.")]
        public async Task RevokeSessionToken(
            Guid authorizationId,
            Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.RevokeSessionToken(false, authorizationId, userState, cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.RevokeSessionTokenAsync instead.")]
        public async Task RevokeSessionToken(
            bool isPublic,
            Guid authorizationId,
            Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenResourceIds.AreaName, "RevokeSessionToken"))
            {
                var query = new List<KeyValuePair<String, String>>();
                query.Add("authorizationId", authorizationId.ToString());
                query.Add("isPublic", isPublic.ToString());

                await SendAsync(
                    HttpMethod.Delete,
                    TokenResourceIds.SessionToken,
                    queryParameters: query,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        #region Operations on authorizations controller

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.InitiateAuthorizationAsync instead.")]
        public async Task<AuthorizationDescription> InitiateAuthorization(
            Guid userId,
            ResponseType responseType,
            Guid clientId,
            Uri redirectUri,
            string scopes,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, nameof(InitiateAuthorization)))
            {
                var query = new List<KeyValuePair<String, String>>();
                query.Add("responseType", responseType.ToString());
                query.Add("clientId", clientId.ToString());
                query.Add("redirectUri", redirectUri.ToString());
                query.Add("scopes", scopes);

                return await SendAsync<AuthorizationDescription>(
                    HttpMethod.Get,
                    DelegatedAuthResourceIds.Authorization,
                    new { userId = userId },
                    queryParameters: query,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.InitiateAuthorizationAsync instead.")]
        public async Task<AuthorizationDecision> Authorize(
            Guid userId,
            ResponseType responseType,
            Guid clientId,
            Uri redirectUri,
            string scopes,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, nameof(Authorize)))
            {
                var query = new List<KeyValuePair<String, String>>();
                query.Add("responseType", responseType.ToString());
                query.Add("clientId", clientId.ToString());
                query.Add("redirectUri", redirectUri.ToString());
                query.Add("scopes", scopes);

                return await SendAsync<AuthorizationDecision>(
                    HttpMethod.Post,
                    DelegatedAuthResourceIds.Authorization,
                    new { userId = userId },
                    queryParameters: query,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.RevokeAuthorizationAsync instead.")]
        public async Task RevokeAuthorization(
            Guid userId,
            Guid authorizationId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, nameof(RevokeAuthorization)))
            {
                var query = new List<KeyValuePair<String, String>>();
                query.Add("authorizationId", authorizationId.ToString());

                await SendAsync(
                    HttpMethod.Post,
                    DelegatedAuthResourceIds.Authorization,
                    new { userId = userId },
                    queryParameters: query,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.GetAuthorizationsAsync instead.")]
        public async Task<IEnumerable<AuthorizationDetails>> GetAuthorizations(
            Guid userId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, nameof(GetAuthorizations)))
            {
                return await SendAsync<IEnumerable<AuthorizationDetails>>(
                    HttpMethod.Get,
                    DelegatedAuthResourceIds.Authorization,
                    new { userId = userId },
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.IssueAppSessionTokenAsync instead.")]
        public async Task<AppSessionTokenResult> IssueAppSessionToken(
           Guid clientId,
           Guid? userId,
           Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenResourceIds.AreaName, nameof(IssueAppSessionToken)))
            {
                var query = new List<KeyValuePair<String, String>>();
                query.Add("clientId", clientId.ToString());
                if (userId.HasValue)
                {
                    query.Add("userId", userId.ToString());
                }
                return await SendAsync<AppSessionTokenResult>(
                    HttpMethod.Post,
                    TokenResourceIds.AppSessionToken,
                    version: s_currentApiVersion,
                    queryParameters: query,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #region Operations on HostAuthorization controller

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.AuthorizeHostAsync instead.")]
        public async Task<HostAuthorizationDecision> AuthorizeHost(
        Guid clientId,
        Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, nameof(AuthorizeHost)))
            {
                var query = new List<KeyValuePair<String, String>>();
                query.Add("clientId", clientId.ToString());

                return await SendAsync<HostAuthorizationDecision>(
                    HttpMethod.Post,
                    DelegatedAuthResourceIds.HostAuthorizeId,
                    version: s_currentApiVersion,
                    queryParameters: query,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.RevokeHostAuthorizationAsync instead.")]
        public async Task<HttpResponseMessage> RevokeHostAuthorization(
            Guid clientId,
            Guid? hostId,
            Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, nameof(RevokeHostAuthorization)))
            {
                var query = new List<KeyValuePair<String, String>>();
                query.Add("clientId", clientId.ToString());
                query.Add("hostId", hostId.ToString());

                return await SendAsync<HttpResponseMessage>(
                    HttpMethod.Delete,
                    DelegatedAuthResourceIds.HostAuthorizeId,
                    version: s_currentApiVersion,
                    queryParameters: query,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.GetHostAuthorizationsAsync instead.")]
        public async Task<IList<HostAuthorization>> GetHostAuthorizations(Guid hostId,
            Object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, nameof(GetHostAuthorizations)))
            {
                var query = new List<KeyValuePair<String, String>>();
                query.Add("hostId", hostId.ToString());

                return await SendAsync<IList<HostAuthorization>>(
                    HttpMethod.Get,
                    DelegatedAuthResourceIds.HostAuthorizeId,
                    version: s_currentApiVersion,
                    queryParameters: query,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.SessionTokenHttpClient.ExchangeAppTokenAsync instead.")]
        public async Task<AccessTokenResult> ExchangeAppToken(
            string appToken,
            string clientSecret,
            Object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(TokenResourceIds.AreaName, nameof(ExchangeAppToken)))
            {
                var appTokenSecretPair = new AppTokenSecretPair
                {
                    AppToken = appToken,
                    ClientSecret = clientSecret
                };

                HttpContent content = new ObjectContent<AppTokenSecretPair>(appTokenSecretPair, base.Formatter);

                return await SendAsync<AccessTokenResult>(
                    HttpMethod.Post,
                    TokenResourceIds.AppTokenPair,
                    version: s_currentApiVersion,
                    content: content,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #region - Registration

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.CreateRegistrationAsync instead.")]
        public async Task<Registration> CreateRegistration(
            Registration registration,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await CreateRegistration(registration, includeSecret: false, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.CreateRegistrationAsync instead.")]
        public async Task<Registration> CreateRegistration(
            Registration registration,
            bool includeSecret,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, "CreateRegistration"))
            {
                HttpContent content = new ObjectContent<Registration>(registration, base.Formatter);

                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add(nameof(includeSecret), includeSecret.ToString());

                return await SendAsync<Registration>(
                    HttpMethod.Put,
                    DelegatedAuthResourceIds.Registration,
                    version: s_currentApiVersion,
                    content: content,
                    userState: userState,
                    queryParameters: queryParameters,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.UpdateRegistrationAsync instead.")]
        public async Task<Registration> UpdateRegistration(
            Registration registration,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await UpdateRegistration(registration, includeSecret: false, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.UpdateRegistrationAsync instead.")]
        public async Task<Registration> UpdateRegistration(
            Registration registration,
            bool includeSecret,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, "UpdateRegistration"))
            {
                HttpContent content = new ObjectContent<Registration>(registration, base.Formatter);

                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add(nameof(includeSecret), includeSecret.ToString());

                return await SendAsync<Registration>(
                    HttpMethod.Post,
                    DelegatedAuthResourceIds.Registration,
                    version: s_currentApiVersion,
                    content: content,
                    userState: userState,
                    queryParameters: queryParameters,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.DeleteRegistrationAsync instead.")]
        public async Task Delete(
            Guid registrationId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, "Delete"))
            {
                await SendAsync(
                    HttpMethod.Delete,
                    DelegatedAuthResourceIds.Registration,
                    new { registrationId = registrationId },
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.GetRegistrationAsync instead.")]
        public async Task<Registration> GetRegistration(
            Guid registrationId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetRegistration(registrationId, includeSecret: false, userState: userState, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.GetRegistrationAsync instead.")]
        public async Task<Registration> GetRegistration(
            Guid registrationId,
            bool includeSecret,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, "GetRegistration"))
            {
                var query = new List<KeyValuePair<String, String>>();
                query.Add(nameof(registrationId), registrationId.ToString());
                query.Add(nameof(includeSecret), includeSecret.ToString());

                return await SendAsync<Registration>(
                    HttpMethod.Get,
                    DelegatedAuthResourceIds.Registration,
                    queryParameters: query,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.GetSecretAsync instead.")]
        public async Task<JsonWebToken> GetSecret(
            Guid registrationId,
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, "GetSecret"))
            {
                return await SendAsync<JsonWebToken>(
                    HttpMethod.Get,
                    DelegatedAuthResourceIds.RegistrationSecret,
                    new { registrationId = registrationId },
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        [Obsolete("This methos has been deprecated. Please use Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi.DelegatedAuthorizationHttpClient.GetRegistrationsAsync instead.")]
        public async Task<IList<Registration>> ListRegistrations(
            object userState = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(DelegatedAuthResourceIds.AreaName, nameof(ListRegistrations)))
            {
                return await SendAsync<IList<Registration>>(
                    HttpMethod.Get,
                    DelegatedAuthResourceIds.Registration,
                    version: s_currentApiVersion,
                    userState: userState,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion

        protected override IDictionary<String, Type> TranslatedExceptions
        {
            get
            {
                return s_translatedExceptions;
            }
        }

        private static Dictionary<String, Type> s_translatedExceptions;
        private static readonly ApiResourceVersion s_currentApiVersion;
    }
}
