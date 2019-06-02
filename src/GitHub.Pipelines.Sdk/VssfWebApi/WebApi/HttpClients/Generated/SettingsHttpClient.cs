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

namespace Microsoft.VisualStudio.Services.Settings.WebApi
{
    public class SettingsHttpClient : VssHttpClientBase
    {
        public SettingsHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public SettingsHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public SettingsHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public SettingsHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public SettingsHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Get all setting entries for the given user/all-users scope
        /// </summary>
        /// <param name="userScope">User-Scope at which to get the value. Should be "me" for the current user or "host" for all users.</param>
        /// <param name="key">Optional key under which to filter all the entries</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Dictionary<string, Object>> GetEntriesAsync(
            string userScope,
            string key = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("cd006711-163d-4cd4-a597-b05bad2556ff");
            object routeValues = new { userScope = userScope, key = key };

            return SendAsync<Dictionary<string, Object>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Remove the entry or entries under the specified path
        /// </summary>
        /// <param name="userScope">User-Scope at which to remove the value. Should be "me" for the current user or "host" for all users.</param>
        /// <param name="key">Root key of the entry or entries to remove</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task RemoveEntriesAsync(
            string userScope,
            string key,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("cd006711-163d-4cd4-a597-b05bad2556ff");
            object routeValues = new { userScope = userScope, key = key };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Set the specified setting entry values for the given user/all-users scope
        /// </summary>
        /// <param name="entries">The entries to set</param>
        /// <param name="userScope">User-Scope at which to set the values. Should be "me" for the current user or "host" for all users.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task SetEntriesAsync(
            IDictionary<string, Object> entries,
            string userScope,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("cd006711-163d-4cd4-a597-b05bad2556ff");
            object routeValues = new { userScope = userScope };
            HttpContent content = new ObjectContent<IDictionary<string, Object>>(entries, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Get all setting entries for the given named scope
        /// </summary>
        /// <param name="userScope">User-Scope at which to get the value. Should be "me" for the current user or "host" for all users.</param>
        /// <param name="scopeName">Scope at which to get the setting for (e.g. "project" or "team")</param>
        /// <param name="scopeValue">Value of the scope (e.g. the project or team id)</param>
        /// <param name="key">Optional key under which to filter all the entries</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<Dictionary<string, Object>> GetEntriesForScopeAsync(
            string userScope,
            string scopeName,
            string scopeValue,
            string key = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("4cbaafaf-e8af-4570-98d1-79ee99c56327");
            object routeValues = new { userScope = userScope, scopeName = scopeName, scopeValue = scopeValue, key = key };

            return SendAsync<Dictionary<string, Object>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Remove the entry or entries under the specified path
        /// </summary>
        /// <param name="userScope">User-Scope at which to remove the value. Should be "me" for the current user or "host" for all users.</param>
        /// <param name="scopeName">Scope at which to get the setting for (e.g. "project" or "team")</param>
        /// <param name="scopeValue">Value of the scope (e.g. the project or team id)</param>
        /// <param name="key">Root key of the entry or entries to remove</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task RemoveEntriesForScopeAsync(
            string userScope,
            string scopeName,
            string scopeValue,
            string key,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("4cbaafaf-e8af-4570-98d1-79ee99c56327");
            object routeValues = new { userScope = userScope, scopeName = scopeName, scopeValue = scopeValue, key = key };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Set the specified entries for the given named scope
        /// </summary>
        /// <param name="entries">The entries to set</param>
        /// <param name="userScope">User-Scope at which to set the values. Should be "me" for the current user or "host" for all users.</param>
        /// <param name="scopeName">Scope at which to set the settings on (e.g. "project" or "team")</param>
        /// <param name="scopeValue">Value of the scope (e.g. the project or team id)</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public async Task SetEntriesForScopeAsync(
            IDictionary<string, Object> entries,
            string userScope,
            string scopeName,
            string scopeValue,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("4cbaafaf-e8af-4570-98d1-79ee99c56327");
            object routeValues = new { userScope = userScope, scopeName = scopeName, scopeValue = scopeValue };
            HttpContent content = new ObjectContent<IDictionary<string, Object>>(entries, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }
    }
}
