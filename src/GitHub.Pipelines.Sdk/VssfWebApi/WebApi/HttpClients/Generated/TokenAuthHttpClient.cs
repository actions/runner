/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 *
 * ---------------------------------------------------------
 * Generated file, DO NOT EDIT
 * ---------------------------------------------------------
 *
 * See following wiki page for instructions on how to regenerate:
 *   https://aka.ms/azure-devops-client-generation
 *
 * Configuration file:
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\tokenauth.genclient.json
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.DelegatedAuthorization;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Jwt;

namespace GitHub.Services.Tokens.WebApi
{
    [ResourceArea(TokenAuthResourceIds.AreaId)]
    public class TokenAuthHttpClient : VssHttpClientBase
    {
        public TokenAuthHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public TokenAuthHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public TokenAuthHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public TokenAuthHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public TokenAuthHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="responseType"></param>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="scopes"></param>
        /// <param name="authorizationId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AuthorizationDecision> AuthorizeAsync(
            Guid userId,
            ResponseType responseType,
            Guid clientId,
            Uri redirectUri,
            string scopes,
            Guid? authorizationId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("7d7ddc0d-60bd-4978-a0b5-295cb099a400");
            object routeValues = new { userId = userId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("responseType", responseType.ToString());
            queryParams.Add("clientId", clientId.ToString());
            string redirectUriAsString = null;
            if (redirectUri != null)
            {
                redirectUriAsString = redirectUri.ToString();
            }
            queryParams.Add("redirectUri", redirectUriAsString);
            queryParams.Add("scopes", scopes);
            if (authorizationId != null)
            {
                queryParams.Add("authorizationId", authorizationId.Value.ToString());
            }

            return SendAsync<AuthorizationDecision>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="responseType"></param>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="scopes"></param>
        /// <param name="authorizationId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AuthorizationDecision> AuthorizeAsync(
            ResponseType responseType,
            Guid clientId,
            Uri redirectUri,
            string scopes,
            Guid? authorizationId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("7d7ddc0d-60bd-4978-a0b5-295cb099a400");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("responseType", responseType.ToString());
            queryParams.Add("clientId", clientId.ToString());
            string redirectUriAsString = null;
            if (redirectUri != null)
            {
                redirectUriAsString = redirectUri.ToString();
            }
            queryParams.Add("redirectUri", redirectUriAsString);
            queryParams.Add("scopes", scopes);
            if (authorizationId != null)
            {
                queryParams.Add("authorizationId", authorizationId.Value.ToString());
            }

            return SendAsync<AuthorizationDecision>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<List<AuthorizationDetails>> GetAuthorizationsAsync(
            Guid? userId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7d7ddc0d-60bd-4978-a0b5-295cb099a400");
            object routeValues = new { userId = userId };

            return SendAsync<List<AuthorizationDetails>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="responseType"></param>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="scopes"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AuthorizationDescription> InitiateAuthorizationAsync(
            Guid userId,
            ResponseType responseType,
            Guid clientId,
            Uri redirectUri,
            string scopes,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7d7ddc0d-60bd-4978-a0b5-295cb099a400");
            object routeValues = new { userId = userId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("responseType", responseType.ToString());
            queryParams.Add("clientId", clientId.ToString());
            string redirectUriAsString = null;
            if (redirectUri != null)
            {
                redirectUriAsString = redirectUri.ToString();
            }
            queryParams.Add("redirectUri", redirectUriAsString);
            queryParams.Add("scopes", scopes);

            return SendAsync<AuthorizationDescription>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="responseType"></param>
        /// <param name="clientId"></param>
        /// <param name="redirectUri"></param>
        /// <param name="scopes"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AuthorizationDescription> InitiateAuthorizationAsync(
            ResponseType responseType,
            Guid clientId,
            Uri redirectUri,
            string scopes,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7d7ddc0d-60bd-4978-a0b5-295cb099a400");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("responseType", responseType.ToString());
            queryParams.Add("clientId", clientId.ToString());
            string redirectUriAsString = null;
            if (redirectUri != null)
            {
                redirectUriAsString = redirectUri.ToString();
            }
            queryParams.Add("redirectUri", redirectUriAsString);
            queryParams.Add("scopes", scopes);

            return SendAsync<AuthorizationDescription>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="authorizationId"></param>
        /// <param name="userId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task RevokeAuthorizationAsync(
            Guid authorizationId,
            Guid? userId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("7d7ddc0d-60bd-4978-a0b5-295cb099a400");
            object routeValues = new { userId = userId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("authorizationId", authorizationId.ToString());

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="hostId"></param>
        /// <param name="newId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<HostAuthorizationDecision> AuthorizeHostAsync(
            Guid clientId,
            Guid hostId,
            Guid? newId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("817d2b46-1507-4efe-be2b-adccf17ffd3b");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("clientId", clientId.ToString());
            queryParams.Add("hostId", hostId.ToString());
            if (newId != null)
            {
                queryParams.Add("newId", newId.Value.ToString());
            }

            return SendAsync<HostAuthorizationDecision>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="hostId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<List<HostAuthorization>> GetHostAuthorizationsAsync(
            Guid hostId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("817d2b46-1507-4efe-be2b-adccf17ffd3b");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("hostId", hostId.ToString());

            return SendAsync<List<HostAuthorization>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="hostId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task RevokeHostAuthorizationAsync(
            Guid clientId,
            Guid? hostId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("817d2b46-1507-4efe-be2b-adccf17ffd3b");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("clientId", clientId.ToString());
            if (hostId != null)
            {
                queryParams.Add("hostId", hostId.Value.ToString());
            }

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Registration> CreateRegistrationAsync(
            Registration registration,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("522ad1a0-389d-4c6f-90da-b145fd2d3ad8");
            HttpContent content = new ObjectContent<Registration>(registration, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Registration>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="includeSecret"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Registration> CreateRegistrationAsync(
            Registration registration,
            bool includeSecret,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("522ad1a0-389d-4c6f-90da-b145fd2d3ad8");
            HttpContent content = new ObjectContent<Registration>(registration, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("includeSecret", includeSecret.ToString());

            return SendAsync<Registration>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="registrationId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task DeleteRegistrationAsync(
            Guid registrationId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("522ad1a0-389d-4c6f-90da-b145fd2d3ad8");
            object routeValues = new { registrationId = registrationId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="registrationId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Registration> GetRegistrationAsync(
            Guid registrationId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("522ad1a0-389d-4c6f-90da-b145fd2d3ad8");
            object routeValues = new { registrationId = registrationId };

            return SendAsync<Registration>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="registrationId"></param>
        /// <param name="includeSecret"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Registration> GetRegistrationAsync(
            Guid registrationId,
            bool includeSecret,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("522ad1a0-389d-4c6f-90da-b145fd2d3ad8");
            object routeValues = new { registrationId = registrationId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("includeSecret", includeSecret.ToString());

            return SendAsync<Registration>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<List<Registration>> GetRegistrationsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("522ad1a0-389d-4c6f-90da-b145fd2d3ad8");

            return SendAsync<List<Registration>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Registration> UpdateRegistrationAsync(
            Registration registration,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("522ad1a0-389d-4c6f-90da-b145fd2d3ad8");
            HttpContent content = new ObjectContent<Registration>(registration, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Registration>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="includeSecret"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Registration> UpdateRegistrationAsync(
            Registration registration,
            bool includeSecret,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("522ad1a0-389d-4c6f-90da-b145fd2d3ad8");
            HttpContent content = new ObjectContent<Registration>(registration, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("includeSecret", includeSecret.ToString());

            return SendAsync<Registration>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="registrationId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<JsonWebToken> GetSecretAsync(
            Guid registrationId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("74896548-9cdd-4315-8aeb-9ecd88fceb21");
            object routeValues = new { registrationId = registrationId };

            return SendAsync<JsonWebToken>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
