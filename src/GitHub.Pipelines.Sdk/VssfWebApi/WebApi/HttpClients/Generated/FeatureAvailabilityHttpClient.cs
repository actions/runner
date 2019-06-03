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
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.FeatureAvailability.WebApi
{
    public class FeatureAvailabilityHttpClient : VssHttpClientBase
    {
        public FeatureAvailabilityHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public FeatureAvailabilityHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public FeatureAvailabilityHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public FeatureAvailabilityHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public FeatureAvailabilityHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Retrieve a listing of all feature flags and their current states
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<List<FeatureFlag>> GetAllFeatureFlagsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3e2b80f8-9e6f-441e-8393-005610692d9c");

            return SendAsync<List<FeatureFlag>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieve a listing of all feature flags and their current states for a user
        /// </summary>
        /// <param name="userEmail">The email of the user to check</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<List<FeatureFlag>> GetAllFeatureFlagsAsync(
            string userEmail,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3e2b80f8-9e6f-441e-8393-005610692d9c");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("userEmail", userEmail);

            return SendAsync<List<FeatureFlag>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieve information on a single feature flag and its current states
        /// </summary>
        /// <param name="name">The name of the feature to retrieve</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<FeatureFlag> GetFeatureFlagByNameAsync(
            string name,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3e2b80f8-9e6f-441e-8393-005610692d9c");
            object routeValues = new { name = name };

            return SendAsync<FeatureFlag>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieve information on a single feature flag and its current states
        /// </summary>
        /// <param name="name">The name of the feature to retrieve</param>
        /// <param name="checkFeatureExists">Check if feature exists</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<FeatureFlag> GetFeatureFlagByNameAsync(
            string name,
            bool checkFeatureExists,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3e2b80f8-9e6f-441e-8393-005610692d9c");
            object routeValues = new { name = name };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("checkFeatureExists", checkFeatureExists.ToString());

            return SendAsync<FeatureFlag>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieve information on a single feature flag and its current states for a user
        /// </summary>
        /// <param name="name">The name of the feature to retrieve</param>
        /// <param name="userEmail">The email of the user to check</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<FeatureFlag> GetFeatureFlagByNameAndUserEmailAsync(
            string name,
            string userEmail,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3e2b80f8-9e6f-441e-8393-005610692d9c");
            object routeValues = new { name = name };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("userEmail", userEmail);

            return SendAsync<FeatureFlag>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieve information on a single feature flag and its current states for a user
        /// </summary>
        /// <param name="name">The name of the feature to retrieve</param>
        /// <param name="userEmail">The email of the user to check</param>
        /// <param name="checkFeatureExists">Check if feature exists</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<FeatureFlag> GetFeatureFlagByNameAndUserEmailAsync(
            string name,
            string userEmail,
            bool checkFeatureExists,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3e2b80f8-9e6f-441e-8393-005610692d9c");
            object routeValues = new { name = name };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("userEmail", userEmail);
            queryParams.Add("checkFeatureExists", checkFeatureExists.ToString());

            return SendAsync<FeatureFlag>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieve information on a single feature flag and its current states for a user
        /// </summary>
        /// <param name="name">The name of the feature to retrieve</param>
        /// <param name="userId">The id of the user to check</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<FeatureFlag> GetFeatureFlagByNameAndUserIdAsync(
            string name,
            Guid userId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3e2b80f8-9e6f-441e-8393-005610692d9c");
            object routeValues = new { name = name };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("userId", userId.ToString());

            return SendAsync<FeatureFlag>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Retrieve information on a single feature flag and its current states for a user
        /// </summary>
        /// <param name="name">The name of the feature to retrieve</param>
        /// <param name="userId">The id of the user to check</param>
        /// <param name="checkFeatureExists">Check if feature exists</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<FeatureFlag> GetFeatureFlagByNameAndUserIdAsync(
            string name,
            Guid userId,
            bool checkFeatureExists,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3e2b80f8-9e6f-441e-8393-005610692d9c");
            object routeValues = new { name = name };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("userId", userId.ToString());
            queryParams.Add("checkFeatureExists", checkFeatureExists.ToString());

            return SendAsync<FeatureFlag>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Change the state of an individual feature flag
        /// </summary>
        /// <param name="state">State that should be set</param>
        /// <param name="name">The name of the feature to change</param>
        /// <param name="checkFeatureExists">Checks if the feature exists before setting the state</param>
        /// <param name="setAtApplicationLevelAlso">If true and currently at collection level, set the feature state at application also</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<FeatureFlag> UpdateFeatureFlagAsync(
            FeatureFlagPatch state,
            string name,
            bool? checkFeatureExists = null,
            bool? setAtApplicationLevelAlso = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("3e2b80f8-9e6f-441e-8393-005610692d9c");
            object routeValues = new { name = name };
            HttpContent content = new ObjectContent<FeatureFlagPatch>(state, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (checkFeatureExists != null)
            {
                queryParams.Add("checkFeatureExists", checkFeatureExists.Value.ToString());
            }
            if (setAtApplicationLevelAlso != null)
            {
                queryParams.Add("setAtApplicationLevelAlso", setAtApplicationLevelAlso.Value.ToString());
            }

            return SendAsync<FeatureFlag>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Change the state of an individual feature flag for a name
        /// </summary>
        /// <param name="state">State that should be set</param>
        /// <param name="name">The name of the feature to change</param>
        /// <param name="userEmail"></param>
        /// <param name="checkFeatureExists">Checks if the feature exists before setting the state</param>
        /// <param name="setAtApplicationLevelAlso"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public Task<FeatureFlag> UpdateFeatureFlagAsync(
            FeatureFlagPatch state,
            string name,
            string userEmail,
            bool? checkFeatureExists = null,
            bool? setAtApplicationLevelAlso = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("3e2b80f8-9e6f-441e-8393-005610692d9c");
            object routeValues = new { name = name };
            HttpContent content = new ObjectContent<FeatureFlagPatch>(state, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("userEmail", userEmail);
            if (checkFeatureExists != null)
            {
                queryParams.Add("checkFeatureExists", checkFeatureExists.Value.ToString());
            }
            if (setAtApplicationLevelAlso != null)
            {
                queryParams.Add("setAtApplicationLevelAlso", setAtApplicationLevelAlso.Value.ToString());
            }

            return SendAsync<FeatureFlag>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }
    }
}
