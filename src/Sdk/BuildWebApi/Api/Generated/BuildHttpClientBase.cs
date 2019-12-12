/*
 * ---------------------------------------------------------
 * Copyright(C) Microsoft Corporation. All rights reserved.
 * ---------------------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    [ResourceArea(BuildResourceIds.AreaId)]
    public abstract class BuildHttpClientBase : VssHttpClientBase
    {
        public BuildHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public BuildHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public BuildHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public BuildHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public BuildHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Associates an artifact with a build.
        /// </summary>
        /// <param name="artifact">The artifact.</param>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildArtifact> CreateArtifactAsync(
            BuildArtifact artifact,
            Guid project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("1db06c96-014e-44e1-ac91-90b2d4b3e984");
            object routeValues = new { project = project, buildId = buildId };
            HttpContent content = new ObjectContent<BuildArtifact>(artifact, new VssJsonMediaTypeFormatter(true));

            return SendAsync<BuildArtifact>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Gets a specific artifact for a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="artifactName">The name of the artifact.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildArtifact> GetArtifactAsync(
            Guid project,
            int buildId,
            string artifactName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1db06c96-014e-44e1-ac91-90b2d4b3e984");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("artifactName", artifactName);

            return SendAsync<BuildArtifact>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
