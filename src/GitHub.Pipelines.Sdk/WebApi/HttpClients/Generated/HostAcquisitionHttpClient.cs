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
 *   vssf\client\webapi\httpclients\clientgeneratorconfigs\hostacquisition.genclient.json
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

namespace Microsoft.VisualStudio.Services.HostAcquisition.Client
{
    [ResourceArea(HostAcquisitionResourceIds.AreaId)]
    public class HostAcquisitionHttpClient : VssHttpClientBase
    {
        public HostAcquisitionHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public HostAcquisitionHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public HostAcquisitionHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public HostAcquisitionHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public HostAcquisitionHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Creates a new collection of the given name in the given region
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="collectionName"></param>
        /// <param name="preferredRegion"></param>
        /// <param name="ownerDescriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Organization.Client.Collection> CreateCollectionAsync(
            IDictionary<string, string> properties,
            string collectionName,
            string preferredRegion,
            string ownerDescriptor = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("2bbead06-ca34-4dd7-9fe2-148735723a0a");
            HttpContent content = new ObjectContent<IDictionary<string, string>>(properties, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("collectionName", collectionName);
            queryParams.Add("preferredRegion", preferredRegion);
            if (ownerDescriptor != null)
            {
                queryParams.Add("ownerDescriptor", ownerDescriptor);
            }

            return SendAsync<Organization.Client.Collection>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 3),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Get all collection Ids that are backed by the input tenant.
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Guid>> GetCollectionsByTenantIdAsync(
            Guid tenantId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2bbead06-ca34-4dd7-9fe2-148735723a0a");

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("tenantId", tenantId.ToString());

            return SendAsync<List<Guid>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 3),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get information about a list of collection ids under a tenant
        /// </summary>
        /// <param name="collectionIds"></param>
        /// <param name="tenantId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BatchCollectionInfo>> GetCollectionsInfoAsync(
            IEnumerable<Guid> collectionIds,
            Guid tenantId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("2bbead06-ca34-4dd7-9fe2-148735723a0a");
            HttpContent content = new ObjectContent<IEnumerable<Guid>>(collectionIds, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("tenantId", tenantId.ToString());

            return SendAsync<List<BatchCollectionInfo>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 3),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="name"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<NameAvailability> GetNameAvailabilityAsync(
            string name,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("01a4cda4-66d1-4f35-918a-212111edc9a4");
            object routeValues = new { name = name };

            return SendAsync<NameAvailability>(
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
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Region>> GetRegionsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("776ef918-0dad-4eb1-a614-04988ca3a072");

            return SendAsync<List<Region>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.1, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
