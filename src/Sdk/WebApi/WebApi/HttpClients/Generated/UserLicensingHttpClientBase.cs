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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.Licensing;
using GitHub.Services.WebApi;

namespace GitHub.Services.UserLicensing.Client
{
    [ResourceArea(UserLicensingResourceIds.AreaId)]
    public abstract class UserLicensingHttpClientBase : VssHttpClientBase
    {
        public UserLicensingHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public UserLicensingHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public UserLicensingHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public UserLicensingHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public UserLicensingHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task<Stream> GetCertificateAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0f7e6aa1-8d3f-428b-b6d2-5e52d08c343a");
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                version: new ApiResourceVersion("5.1-preview.1"),
                mediaType: "application/octet-stream",
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                response = await SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, userState, cancellationToken).ConfigureAwait(false);
            }
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentEncoding.Contains("gzip", StringComparer.OrdinalIgnoreCase))
            {
                Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return new GZipStream(responseStream, CompressionMode.Decompress);
            }
            else
            {
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="descriptor"></param>
        /// <param name="rightName"></param>
        /// <param name="productVersion"></param>
        /// <param name="edition"></param>
        /// <param name="relType"></param>
        /// <param name="includeCertificate"></param>
        /// <param name="canary"></param>
        /// <param name="machineId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<ClientRightsContainer> GetClientRightsAsync(
            string descriptor,
            string rightName = null,
            string productVersion = null,
            string edition = null,
            string relType = null,
            bool? includeCertificate = null,
            string canary = null,
            string machineId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2cc58bfd-3b77-4dc1-b0b3-74b0775d41cb");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (rightName != null)
            {
                queryParams.Add("rightName", rightName);
            }
            if (productVersion != null)
            {
                queryParams.Add("productVersion", productVersion);
            }
            if (edition != null)
            {
                queryParams.Add("edition", edition);
            }
            if (relType != null)
            {
                queryParams.Add("relType", relType);
            }
            if (includeCertificate != null)
            {
                queryParams.Add("includeCertificate", includeCertificate.Value.ToString());
            }
            if (canary != null)
            {
                queryParams.Add("canary", canary);
            }
            if (machineId != null)
            {
                queryParams.Add("machineId", machineId);
            }

            return SendAsync<ClientRightsContainer>(
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
        /// <param name="descriptor"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<MsdnEntitlement>> GetEntitlementsAsync(
            string descriptor,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("58dde369-bec9-4f13-93de-e8dfa381293c");
            object routeValues = new { descriptor = descriptor };

            return SendAsync<List<MsdnEntitlement>>(
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
        /// <param name="descriptor"></param>
        /// <param name="machineId"></param>
        /// <param name="majorVersion"></param>
        /// <param name="productFamilyId"></param>
        /// <param name="productEditionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<long> GetVisualStudioTrialExpirationAsync(
            string descriptor,
            string machineId,
            int majorVersion,
            int productFamilyId,
            int productEditionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("2083f3ec-0e90-4267-8122-394a68664a6e");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("machineId", machineId);
            queryParams.Add("majorVersion", majorVersion.ToString(CultureInfo.InvariantCulture));
            queryParams.Add("productFamilyId", productFamilyId.ToString(CultureInfo.InvariantCulture));
            queryParams.Add("productEditionId", productEditionId.ToString(CultureInfo.InvariantCulture));

            return SendAsync<long>(
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
        /// <param name="descriptor"></param>
        /// <param name="majorVersion"></param>
        /// <param name="productFamilyId"></param>
        /// <param name="productEditionId"></param>
        /// <param name="expirationDate"></param>
        /// <param name="createdDate"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual async Task SetVisualStudioTrialInfoAsync(
            string descriptor,
            int majorVersion,
            int productFamilyId,
            int productEditionId,
            DateTime expirationDate,
            DateTime createdDate,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("2083f3ec-0e90-4267-8122-394a68664a6e");
            object routeValues = new { descriptor = descriptor };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("majorVersion", majorVersion.ToString(CultureInfo.InvariantCulture));
            queryParams.Add("productFamilyId", productFamilyId.ToString(CultureInfo.InvariantCulture));
            queryParams.Add("productEditionId", productEditionId.ToString(CultureInfo.InvariantCulture));
            AddDateTimeToQueryParams(queryParams, "expirationDate", expirationDate);
            AddDateTimeToQueryParams(queryParams, "createdDate", createdDate);

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
    }
}
