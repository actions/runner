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
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\genclient.json
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
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Jwt;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization.WebApi
{
    [ResourceArea(Tokens.DelegatedAuthResourceIds.AreaId)]
    public class DelegatedAuthorizationHttpClient : VssHttpClientBase
    {
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
        public Task<AuthorizationDecision> AuthorizeAsync(
            Guid userId,
            ResponseType responseType,
            Guid clientId,
            Uri redirectUri,
            string scopes,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("efbf6e0c-1150-43fd-b869-7e2b04fc0d09");
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
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<AuthorizationDecision> AuthorizeAsync(
            ResponseType responseType,
            Guid clientId,
            Uri redirectUri,
            string scopes,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("efbf6e0c-1150-43fd-b869-7e2b04fc0d09");

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
            Guid locationId = new Guid("efbf6e0c-1150-43fd-b869-7e2b04fc0d09");
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
            Guid locationId = new Guid("efbf6e0c-1150-43fd-b869-7e2b04fc0d09");
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
            Guid locationId = new Guid("efbf6e0c-1150-43fd-b869-7e2b04fc0d09");

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
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("efbf6e0c-1150-43fd-b869-7e2b04fc0d09");
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
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<HostAuthorizationDecision> AuthorizeHostAsync(
            Guid clientId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("7372fdd9-238c-467c-b0f2-995f4bfe0d94");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("clientId", clientId.ToString());

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
            Guid locationId = new Guid("7372fdd9-238c-467c-b0f2-995f4bfe0d94");

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
            Guid locationId = new Guid("7372fdd9-238c-467c-b0f2-995f4bfe0d94");

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
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("909cd090-3005-480d-a1b4-220b76cb0afe");
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
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("909cd090-3005-480d-a1b4-220b76cb0afe");
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
            Guid locationId = new Guid("909cd090-3005-480d-a1b4-220b76cb0afe");
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
            Guid locationId = new Guid("909cd090-3005-480d-a1b4-220b76cb0afe");
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
            Guid locationId = new Guid("909cd090-3005-480d-a1b4-220b76cb0afe");
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
            Guid locationId = new Guid("909cd090-3005-480d-a1b4-220b76cb0afe");

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
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("909cd090-3005-480d-a1b4-220b76cb0afe");
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
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("909cd090-3005-480d-a1b4-220b76cb0afe");
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
            Guid locationId = new Guid("f37e5023-dfbe-490e-9e40-7b7fb6b67887");
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
