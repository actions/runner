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
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\organization.genclient.json
 */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Patch.Json;

namespace GitHub.Services.Organization.Client
{
    [ResourceArea(OrganizationResourceIds.AreaId)]
    public class OrganizationHttpClient : OrganizationCompatHttpClientBase
    {
        public OrganizationHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public OrganizationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public OrganizationHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public OrganizationHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public OrganizationHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="collectionId"></param>
        /// <param name="patchDocument"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<bool> UpdateCollectionPropertiesAsync(
            Guid collectionId,
            JsonPatchDocument patchDocument,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("a0f9c508-a3c4-456b-a812-3fb0c4743521");
            object routeValues = new { collectionId = collectionId };
            HttpContent content = new ObjectContent<JsonPatchDocument>(patchDocument, new VssJsonMediaTypeFormatter(true), "application/json-patch+json");

            return SendAsync<bool>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Collection> CreateCollectionAsync(
            Collection resource,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("668b5607-0db2-49bb-83f8-5f46f1094250");
            HttpContent content = new ObjectContent<Collection>(resource, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Collection>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="collectionId"></param>
        /// <param name="gracePeriodToRestoreInHours"></param>
        /// <param name="violatedTerms"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<bool> DeleteCollectionAsync(
            Guid collectionId,
            int? gracePeriodToRestoreInHours = null,
            bool? violatedTerms = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("668b5607-0db2-49bb-83f8-5f46f1094250");
            object routeValues = new { collectionId = collectionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (gracePeriodToRestoreInHours != null)
            {
                queryParams.Add("gracePeriodToRestoreInHours", gracePeriodToRestoreInHours.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (violatedTerms != null)
            {
                queryParams.Add("violatedTerms", violatedTerms.Value.ToString());
            }

            return SendAsync<bool>(
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
        /// <param name="collectionId"></param>
        /// <param name="propertyNames"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Collection> GetCollectionAsync(
            string collectionId,
            IEnumerable<string> propertyNames = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("668b5607-0db2-49bb-83f8-5f46f1094250");
            object routeValues = new { collectionId = collectionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (propertyNames != null && propertyNames.Any())
            {
                queryParams.Add("propertyNames", string.Join(",", propertyNames));
            }

            return SendAsync<Collection>(
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
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Collection>> GetCollectionsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("668b5607-0db2-49bb-83f8-5f46f1094250");

            return SendAsync<List<Collection>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="searchKind"></param>
        /// <param name="searchValue"></param>
        /// <param name="includeDeletedCollections"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Collection>> GetCollectionsAsync(
            CollectionSearchKind searchKind,
            string searchValue,
            bool? includeDeletedCollections = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("668b5607-0db2-49bb-83f8-5f46f1094250");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("searchKind", searchKind.ToString());
            queryParams.Add("searchValue", searchValue);
            if (includeDeletedCollections != null)
            {
                queryParams.Add("includeDeletedCollections", includeDeletedCollections.Value.ToString());
            }

            return SendAsync<List<Collection>>(
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
        /// <param name="collectionId"></param>
        /// <param name="collectionName"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<bool> RestoreCollectionAsync(
            Guid collectionId,
            string collectionName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("668b5607-0db2-49bb-83f8-5f46f1094250");
            object routeValues = new { collectionId = collectionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("collectionName", collectionName);

            return SendAsync<bool>(
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
        /// <param name="patchDocument"></param>
        /// <param name="collectionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Collection> UpdateCollectionAsync(
            JsonPatchDocument patchDocument,
            string collectionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("668b5607-0db2-49bb-83f8-5f46f1094250");
            object routeValues = new { collectionId = collectionId };
            HttpContent content = new ObjectContent<JsonPatchDocument>(patchDocument, new VssJsonMediaTypeFormatter(true), "application/json-patch+json");

            return SendAsync<Collection>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="organizationId"></param>
        /// <param name="logo"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<bool> UpdateOrganizationLogoAsync(
            Guid organizationId,
            Logo logo,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("a9eeec19-85b4-40ae-8a52-b4f697260ac4");
            object routeValues = new { organizationId = organizationId };
            HttpContent content = new ObjectContent<Logo>(logo, new VssJsonMediaTypeFormatter(true));

            return SendAsync<bool>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="organizationId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<OrganizationMigrationBlob> ExportOrganizationMigrationBlobAsync(
            Guid organizationId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("93f69239-28ba-497e-b4d4-33e51e6303c3");
            object routeValues = new { organizationId = organizationId };

            return SendAsync<OrganizationMigrationBlob>(
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
        /// <param name="migrationBlob"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task ImportOrganizationMigrationBlobAsync(
            OrganizationMigrationBlob migrationBlob,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("93f69239-28ba-497e-b4d4-33e51e6303c3");
            HttpContent content = new ObjectContent<OrganizationMigrationBlob>(migrationBlob, new VssJsonMediaTypeFormatter(true));

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="organizationId"></param>
        /// <param name="patchDocument"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<bool> UpdateOrganizationPropertiesAsync(
            Guid organizationId,
            JsonPatchDocument patchDocument,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("103707c6-236d-4434-a0a2-9031fbb65fa6");
            object routeValues = new { organizationId = organizationId };
            HttpContent content = new ObjectContent<JsonPatchDocument>(patchDocument, new VssJsonMediaTypeFormatter(true), "application/json-patch+json");

            return SendAsync<bool>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Organization> CreateOrganizationAsync(
            Organization resource,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("95f49097-6cdc-4afe-a039-48b4d4c4cbf7");
            HttpContent content = new ObjectContent<Organization>(resource, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Organization>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="organizationId"></param>
        /// <param name="propertyNames"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Organization> GetOrganizationAsync(
            string organizationId,
            IEnumerable<string> propertyNames = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("95f49097-6cdc-4afe-a039-48b4d4c4cbf7");
            object routeValues = new { organizationId = organizationId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (propertyNames != null && propertyNames.Any())
            {
                queryParams.Add("propertyNames", string.Join(",", propertyNames));
            }

            return SendAsync<Organization>(
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
        /// <param name="searchKind"></param>
        /// <param name="searchValue"></param>
        /// <param name="isActivated"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Organization>> GetOrganizationsAsync(
            OrganizationSearchKind searchKind,
            string searchValue,
            bool? isActivated = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("95f49097-6cdc-4afe-a039-48b4d4c4cbf7");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("searchKind", searchKind.ToString());
            queryParams.Add("searchValue", searchValue);
            if (isActivated != null)
            {
                queryParams.Add("isActivated", isActivated.Value.ToString());
            }

            return SendAsync<List<Organization>>(
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
        /// <param name="patchDocument"></param>
        /// <param name="organizationId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Organization> UpdateOrganizationAsync(
            JsonPatchDocument patchDocument,
            string organizationId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("95f49097-6cdc-4afe-a039-48b4d4c4cbf7");
            object routeValues = new { organizationId = organizationId };
            HttpContent content = new ObjectContent<JsonPatchDocument>(patchDocument, new VssJsonMediaTypeFormatter(true), "application/json-patch+json");

            return SendAsync<Organization>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="includeRegionsWithNoAvailableHosts"></param>
        /// <param name="impersonatedUser"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Region>> GetRegionsAsync(
            bool? includeRegionsWithNoAvailableHosts = null,
            string impersonatedUser = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6f84936f-1801-46f6-94fa-1817545d366d");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (includeRegionsWithNoAvailableHosts != null)
            {
                queryParams.Add("includeRegionsWithNoAvailableHosts", includeRegionsWithNoAvailableHosts.Value.ToString());
            }
            if (impersonatedUser != null)
            {
                queryParams.Add("impersonatedUser", impersonatedUser);
            }

            return SendAsync<List<Region>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
