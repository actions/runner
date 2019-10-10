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
 *   tfs\client\build2\api\clientgeneratorconfigs\genclient.json
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

namespace GitHub.Build.WebApi
{
    [ResourceArea(BuildResourceIds.AreaId)]
    public abstract class BuildHttpClientBase : BuildHttpClientCompatBase
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildArtifact> CreateArtifactAsync(
            BuildArtifact artifact,
            string project,
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
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="artifactName">The name of the artifact.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildArtifact> GetArtifactAsync(
            string project,
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

        /// <summary>
        /// [Preview API] Gets a specific artifact for a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="artifactName">The name of the artifact.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetArtifactContentZipAsync(
            string project,
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
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.5"),
                queryParameters: queryParams,
                mediaType: "application/zip",
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
        /// [Preview API] Gets a specific artifact for a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="artifactName">The name of the artifact.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetArtifactContentZipAsync(
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
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.5"),
                queryParameters: queryParams,
                mediaType: "application/zip",
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
        /// [Preview API] Gets all artifacts for a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildArtifact>> GetArtifactsAsync(
            string project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1db06c96-014e-44e1-ac91-90b2d4b3e984");
            object routeValues = new { project = project, buildId = buildId };

            return SendAsync<List<BuildArtifact>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets all artifacts for a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildArtifact>> GetArtifactsAsync(
            Guid project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1db06c96-014e-44e1-ac91-90b2d4b3e984");
            object routeValues = new { project = project, buildId = buildId };

            return SendAsync<List<BuildArtifact>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a file from the build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="artifactName">The name of the artifact.</param>
        /// <param name="fileId">The primary key for the file.</param>
        /// <param name="fileName">The name that the file will be set to.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetFileAsync(
            string project,
            int buildId,
            string artifactName,
            string fileId,
            string fileName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1db06c96-014e-44e1-ac91-90b2d4b3e984");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("artifactName", artifactName);
            queryParams.Add("fileId", fileId);
            queryParams.Add("fileName", fileName);
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.5"),
                queryParameters: queryParams,
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
        /// [Preview API] Gets a file from the build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="artifactName">The name of the artifact.</param>
        /// <param name="fileId">The primary key for the file.</param>
        /// <param name="fileName">The name that the file will be set to.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetFileAsync(
            Guid project,
            int buildId,
            string artifactName,
            string fileId,
            string fileName,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("1db06c96-014e-44e1-ac91-90b2d4b3e984");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("artifactName", artifactName);
            queryParams.Add("fileId", fileId);
            queryParams.Add("fileName", fileName);
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.5"),
                queryParameters: queryParams,
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
        /// [Preview API] Gets the list of attachments of a specific type that are associated with a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="type">The type of attachment.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Attachment>> GetAttachmentsAsync(
            string project,
            int buildId,
            string type,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f2192269-89fa-4f94-baf6-8fb128c55159");
            object routeValues = new { project = project, buildId = buildId, type = type };

            return SendAsync<List<Attachment>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the list of attachments of a specific type that are associated with a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="type">The type of attachment.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Attachment>> GetAttachmentsAsync(
            Guid project,
            int buildId,
            string type,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f2192269-89fa-4f94-baf6-8fb128c55159");
            object routeValues = new { project = project, buildId = buildId, type = type };

            return SendAsync<List<Attachment>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a specific attachment.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="timelineId">The ID of the timeline.</param>
        /// <param name="recordId">The ID of the timeline record.</param>
        /// <param name="type">The type of the attachment.</param>
        /// <param name="name">The name of the attachment.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetAttachmentAsync(
            string project,
            int buildId,
            Guid timelineId,
            Guid recordId,
            string type,
            string name,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("af5122d3-3438-485e-a25a-2dbbfde84ee6");
            object routeValues = new { project = project, buildId = buildId, timelineId = timelineId, recordId = recordId, type = type, name = name };
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.2"),
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
        /// [Preview API] Gets a specific attachment.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="timelineId">The ID of the timeline.</param>
        /// <param name="recordId">The ID of the timeline record.</param>
        /// <param name="type">The type of the attachment.</param>
        /// <param name="name">The name of the attachment.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetAttachmentAsync(
            Guid project,
            int buildId,
            Guid timelineId,
            Guid recordId,
            string type,
            string name,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("af5122d3-3438-485e-a25a-2dbbfde84ee6");
            object routeValues = new { project = project, buildId = buildId, timelineId = timelineId, recordId = recordId, type = type, name = name };
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.2"),
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
        /// <param name="resources"></param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DefinitionResourceReference>> AuthorizeProjectResourcesAsync(
            IEnumerable<DefinitionResourceReference> resources,
            string project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("398c85bc-81aa-4822-947c-a194a05f0fef");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<IEnumerable<DefinitionResourceReference>>(resources, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<DefinitionResourceReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="project">Project ID</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DefinitionResourceReference>> AuthorizeProjectResourcesAsync(
            IEnumerable<DefinitionResourceReference> resources,
            Guid project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("398c85bc-81aa-4822-947c-a194a05f0fef");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<IEnumerable<DefinitionResourceReference>>(resources, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<DefinitionResourceReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DefinitionResourceReference>> GetProjectResourcesAsync(
            string project,
            string type = null,
            string id = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("398c85bc-81aa-4822-947c-a194a05f0fef");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (type != null)
            {
                queryParams.Add("type", type);
            }
            if (id != null)
            {
                queryParams.Add("id", id);
            }

            return SendAsync<List<DefinitionResourceReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DefinitionResourceReference>> GetProjectResourcesAsync(
            Guid project,
            string type = null,
            string id = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("398c85bc-81aa-4822-947c-a194a05f0fef");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (type != null)
            {
                queryParams.Add("type", type);
            }
            if (id != null)
            {
                queryParams.Add("id", id);
            }

            return SendAsync<List<DefinitionResourceReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a badge that indicates the status of the most recent build for a definition. Note that this API is deprecated. Prefer StatusBadgeController.GetStatusBadge.
        /// </summary>
        /// <param name="project">The project ID or name.</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="branchName">The name of the branch.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        [Obsolete("This endpoint is deprecated. Please see the Build Status REST endpoint.")]
        public virtual Task<string> GetBadgeAsync(
            Guid project,
            int definitionId,
            string branchName = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("de6a4df8-22cd-44ee-af2d-39f6aa7a4261");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }

            return SendAsync<string>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of branches for the given source code repository.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">The vendor-specific identifier or the name of the repository to get branches. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="branchName">If supplied, the name of the branch to check for specifically.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> ListBranchesAsync(
            string project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            string branchName = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e05d4403-9b81-4244-8763-20fde28d1976");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of branches for the given source code repository.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">The vendor-specific identifier or the name of the repository to get branches. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="branchName">If supplied, the name of the branch to check for specifically.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> ListBranchesAsync(
            Guid project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            string branchName = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e05d4403-9b81-4244-8763-20fde28d1976");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a badge that indicates the status of the most recent build for the specified branch.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="repoType">The repository type.</param>
        /// <param name="repoId">The repository ID.</param>
        /// <param name="branchName">The branch name.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildBadge> GetBuildBadgeAsync(
            string project,
            string repoType,
            string repoId = null,
            string branchName = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("21b3b9ce-fad5-4567-9ad0-80679794e003");
            object routeValues = new { project = project, repoType = repoType };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (repoId != null)
            {
                queryParams.Add("repoId", repoId);
            }
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }

            return SendAsync<BuildBadge>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a badge that indicates the status of the most recent build for the specified branch.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="repoType">The repository type.</param>
        /// <param name="repoId">The repository ID.</param>
        /// <param name="branchName">The branch name.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildBadge> GetBuildBadgeAsync(
            Guid project,
            string repoType,
            string repoId = null,
            string branchName = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("21b3b9ce-fad5-4567-9ad0-80679794e003");
            object routeValues = new { project = project, repoType = repoType };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (repoId != null)
            {
                queryParams.Add("repoId", repoId);
            }
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }

            return SendAsync<BuildBadge>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a badge that indicates the status of the most recent build for the specified branch.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="repoType">The repository type.</param>
        /// <param name="repoId">The repository ID.</param>
        /// <param name="branchName">The branch name.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<string> GetBuildBadgeDataAsync(
            string project,
            string repoType,
            string repoId = null,
            string branchName = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("21b3b9ce-fad5-4567-9ad0-80679794e003");
            object routeValues = new { project = project, repoType = repoType };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (repoId != null)
            {
                queryParams.Add("repoId", repoId);
            }
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }

            return SendAsync<string>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a badge that indicates the status of the most recent build for the specified branch.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="repoType">The repository type.</param>
        /// <param name="repoId">The repository ID.</param>
        /// <param name="branchName">The branch name.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<string> GetBuildBadgeDataAsync(
            Guid project,
            string repoType,
            string repoId = null,
            string branchName = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("21b3b9ce-fad5-4567-9ad0-80679794e003");
            object routeValues = new { project = project, repoType = repoType };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (repoId != null)
            {
                queryParams.Add("repoId", repoId);
            }
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }

            return SendAsync<string>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Deletes a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteBuildAsync(
            string project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project, buildId = buildId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Deletes a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteBuildAsync(
            Guid project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project, buildId = buildId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Gets a build
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId"></param>
        /// <param name="propertyFilters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Build> GetBuildAsync(
            string project,
            int buildId,
            string propertyFilters = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (propertyFilters != null)
            {
                queryParams.Add("propertyFilters", propertyFilters);
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a build
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId"></param>
        /// <param name="propertyFilters"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Build> GetBuildAsync(
            Guid project,
            int buildId,
            string propertyFilters = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (propertyFilters != null)
            {
                queryParams.Add("propertyFilters", propertyFilters);
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of builds.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitions">A comma-delimited list of definition IDs. If specified, filters to builds for these definitions.</param>
        /// <param name="queues">A comma-delimited list of queue IDs. If specified, filters to builds that ran against these queues.</param>
        /// <param name="buildNumber">If specified, filters to builds that match this build number. Append * to do a prefix search.</param>
        /// <param name="minTime">If specified, filters to builds that finished/started/queued after this date based on the queryOrder specified.</param>
        /// <param name="maxTime">If specified, filters to builds that finished/started/queued before this date based on the queryOrder specified.</param>
        /// <param name="requestedFor">If specified, filters to builds requested for the specified user.</param>
        /// <param name="reasonFilter">If specified, filters to builds that match this reason.</param>
        /// <param name="statusFilter">If specified, filters to builds that match this status.</param>
        /// <param name="resultFilter">If specified, filters to builds that match this result.</param>
        /// <param name="tagFilters">A comma-delimited list of tags. If specified, filters to builds that have the specified tags.</param>
        /// <param name="properties">A comma-delimited list of properties to retrieve.</param>
        /// <param name="top">The maximum number of builds to return.</param>
        /// <param name="continuationToken">A continuation token, returned by a previous call to this method, that can be used to return the next set of builds.</param>
        /// <param name="maxBuildsPerDefinition">The maximum number of builds to return per definition.</param>
        /// <param name="deletedFilter">Indicates whether to exclude, include, or only return deleted builds.</param>
        /// <param name="queryOrder">The order in which builds should be returned.</param>
        /// <param name="branchName">If specified, filters to builds that built branches that built this branch.</param>
        /// <param name="buildIds">A comma-delimited list that specifies the IDs of builds to retrieve.</param>
        /// <param name="repositoryId">If specified, filters to builds that built from this repository.</param>
        /// <param name="repositoryType">If specified, filters to builds that built from repositories of this type.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Build>> GetBuildsAsync(
            string project,
            IEnumerable<int> definitions = null,
            IEnumerable<int> queues = null,
            string buildNumber = null,
            DateTime? minTime = null,
            DateTime? maxTime = null,
            string requestedFor = null,
            BuildReason? reasonFilter = null,
            BuildStatus? statusFilter = null,
            BuildResult? resultFilter = null,
            IEnumerable<string> tagFilters = null,
            IEnumerable<string> properties = null,
            int? top = null,
            string continuationToken = null,
            int? maxBuildsPerDefinition = null,
            QueryDeletedOption? deletedFilter = null,
            BuildQueryOrder? queryOrder = null,
            string branchName = null,
            IEnumerable<int> buildIds = null,
            string repositoryId = null,
            string repositoryType = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (definitions != null && definitions.Any())
            {
                queryParams.Add("definitions", string.Join(",", definitions));
            }
            if (queues != null && queues.Any())
            {
                queryParams.Add("queues", string.Join(",", queues));
            }
            if (buildNumber != null)
            {
                queryParams.Add("buildNumber", buildNumber);
            }
            if (minTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minTime", minTime.Value);
            }
            if (maxTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "maxTime", maxTime.Value);
            }
            if (requestedFor != null)
            {
                queryParams.Add("requestedFor", requestedFor);
            }
            if (reasonFilter != null)
            {
                queryParams.Add("reasonFilter", reasonFilter.Value.ToString());
            }
            if (statusFilter != null)
            {
                queryParams.Add("statusFilter", statusFilter.Value.ToString());
            }
            if (resultFilter != null)
            {
                queryParams.Add("resultFilter", resultFilter.Value.ToString());
            }
            if (tagFilters != null && tagFilters.Any())
            {
                queryParams.Add("tagFilters", string.Join(",", tagFilters));
            }
            if (properties != null && properties.Any())
            {
                queryParams.Add("properties", string.Join(",", properties));
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (maxBuildsPerDefinition != null)
            {
                queryParams.Add("maxBuildsPerDefinition", maxBuildsPerDefinition.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (deletedFilter != null)
            {
                queryParams.Add("deletedFilter", deletedFilter.Value.ToString());
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }
            if (buildIds != null && buildIds.Any())
            {
                queryParams.Add("buildIds", string.Join(",", buildIds));
            }
            if (repositoryId != null)
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (repositoryType != null)
            {
                queryParams.Add("repositoryType", repositoryType);
            }

            return SendAsync<List<Build>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of builds.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitions">A comma-delimited list of definition IDs. If specified, filters to builds for these definitions.</param>
        /// <param name="queues">A comma-delimited list of queue IDs. If specified, filters to builds that ran against these queues.</param>
        /// <param name="buildNumber">If specified, filters to builds that match this build number. Append * to do a prefix search.</param>
        /// <param name="minTime">If specified, filters to builds that finished/started/queued after this date based on the queryOrder specified.</param>
        /// <param name="maxTime">If specified, filters to builds that finished/started/queued before this date based on the queryOrder specified.</param>
        /// <param name="requestedFor">If specified, filters to builds requested for the specified user.</param>
        /// <param name="reasonFilter">If specified, filters to builds that match this reason.</param>
        /// <param name="statusFilter">If specified, filters to builds that match this status.</param>
        /// <param name="resultFilter">If specified, filters to builds that match this result.</param>
        /// <param name="tagFilters">A comma-delimited list of tags. If specified, filters to builds that have the specified tags.</param>
        /// <param name="properties">A comma-delimited list of properties to retrieve.</param>
        /// <param name="top">The maximum number of builds to return.</param>
        /// <param name="continuationToken">A continuation token, returned by a previous call to this method, that can be used to return the next set of builds.</param>
        /// <param name="maxBuildsPerDefinition">The maximum number of builds to return per definition.</param>
        /// <param name="deletedFilter">Indicates whether to exclude, include, or only return deleted builds.</param>
        /// <param name="queryOrder">The order in which builds should be returned.</param>
        /// <param name="branchName">If specified, filters to builds that built branches that built this branch.</param>
        /// <param name="buildIds">A comma-delimited list that specifies the IDs of builds to retrieve.</param>
        /// <param name="repositoryId">If specified, filters to builds that built from this repository.</param>
        /// <param name="repositoryType">If specified, filters to builds that built from repositories of this type.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Build>> GetBuildsAsync(
            Guid project,
            IEnumerable<int> definitions = null,
            IEnumerable<int> queues = null,
            string buildNumber = null,
            DateTime? minTime = null,
            DateTime? maxTime = null,
            string requestedFor = null,
            BuildReason? reasonFilter = null,
            BuildStatus? statusFilter = null,
            BuildResult? resultFilter = null,
            IEnumerable<string> tagFilters = null,
            IEnumerable<string> properties = null,
            int? top = null,
            string continuationToken = null,
            int? maxBuildsPerDefinition = null,
            QueryDeletedOption? deletedFilter = null,
            BuildQueryOrder? queryOrder = null,
            string branchName = null,
            IEnumerable<int> buildIds = null,
            string repositoryId = null,
            string repositoryType = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (definitions != null && definitions.Any())
            {
                queryParams.Add("definitions", string.Join(",", definitions));
            }
            if (queues != null && queues.Any())
            {
                queryParams.Add("queues", string.Join(",", queues));
            }
            if (buildNumber != null)
            {
                queryParams.Add("buildNumber", buildNumber);
            }
            if (minTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minTime", minTime.Value);
            }
            if (maxTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "maxTime", maxTime.Value);
            }
            if (requestedFor != null)
            {
                queryParams.Add("requestedFor", requestedFor);
            }
            if (reasonFilter != null)
            {
                queryParams.Add("reasonFilter", reasonFilter.Value.ToString());
            }
            if (statusFilter != null)
            {
                queryParams.Add("statusFilter", statusFilter.Value.ToString());
            }
            if (resultFilter != null)
            {
                queryParams.Add("resultFilter", resultFilter.Value.ToString());
            }
            if (tagFilters != null && tagFilters.Any())
            {
                queryParams.Add("tagFilters", string.Join(",", tagFilters));
            }
            if (properties != null && properties.Any())
            {
                queryParams.Add("properties", string.Join(",", properties));
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (maxBuildsPerDefinition != null)
            {
                queryParams.Add("maxBuildsPerDefinition", maxBuildsPerDefinition.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (deletedFilter != null)
            {
                queryParams.Add("deletedFilter", deletedFilter.Value.ToString());
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }
            if (buildIds != null && buildIds.Any())
            {
                queryParams.Add("buildIds", string.Join(",", buildIds));
            }
            if (repositoryId != null)
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (repositoryType != null)
            {
                queryParams.Add("repositoryType", repositoryType);
            }

            return SendAsync<List<Build>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Queues a build
        /// </summary>
        /// <param name="build"></param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="ignoreWarnings"></param>
        /// <param name="checkInTicket"></param>
        /// <param name="sourceBuildId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Build> QueueBuildAsync(
            Build build,
            string project,
            bool? ignoreWarnings = null,
            string checkInTicket = null,
            int? sourceBuildId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<Build>(build, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (ignoreWarnings != null)
            {
                queryParams.Add("ignoreWarnings", ignoreWarnings.Value.ToString());
            }
            if (checkInTicket != null)
            {
                queryParams.Add("checkInTicket", checkInTicket);
            }
            if (sourceBuildId != null)
            {
                queryParams.Add("sourceBuildId", sourceBuildId.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Queues a build
        /// </summary>
        /// <param name="build"></param>
        /// <param name="project">Project ID</param>
        /// <param name="ignoreWarnings"></param>
        /// <param name="checkInTicket"></param>
        /// <param name="sourceBuildId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Build> QueueBuildAsync(
            Build build,
            Guid project,
            bool? ignoreWarnings = null,
            string checkInTicket = null,
            int? sourceBuildId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<Build>(build, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (ignoreWarnings != null)
            {
                queryParams.Add("ignoreWarnings", ignoreWarnings.Value.ToString());
            }
            if (checkInTicket != null)
            {
                queryParams.Add("checkInTicket", checkInTicket);
            }
            if (sourceBuildId != null)
            {
                queryParams.Add("sourceBuildId", sourceBuildId.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates a build.
        /// </summary>
        /// <param name="build">The build.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="retry"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        private protected virtual Task<Build> UpdateBuildAsync(
            Build build,
            string project,
            int buildId,
            bool? retry = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project, buildId = buildId };
            HttpContent content = new ObjectContent<Build>(build, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (retry != null)
            {
                queryParams.Add("retry", retry.Value.ToString());
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates a build.
        /// </summary>
        /// <param name="build">The build.</param>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="retry"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        private protected virtual Task<Build> UpdateBuildAsync(
            Build build,
            Guid project,
            int buildId,
            bool? retry = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project, buildId = buildId };
            HttpContent content = new ObjectContent<Build>(build, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (retry != null)
            {
                queryParams.Add("retry", retry.Value.ToString());
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates multiple builds.
        /// </summary>
        /// <param name="builds">The builds to update.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Build>> UpdateBuildsAsync(
            IEnumerable<Build> builds,
            string project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<IEnumerable<Build>>(builds, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<Build>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates multiple builds.
        /// </summary>
        /// <param name="builds">The builds to update.</param>
        /// <param name="project">Project ID</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Build>> UpdateBuildsAsync(
            IEnumerable<Build> builds,
            Guid project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("0cd358e1-9217-4d94-8269-1c1ee6f93dcf");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<IEnumerable<Build>>(builds, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<Build>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 5),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Gets the changes associated with a build
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top">The maximum number of changes to return</param>
        /// <param name="includeSourceChange"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Change>> GetBuildChangesAsync(
            string project,
            int buildId,
            string continuationToken = null,
            int? top = null,
            bool? includeSourceChange = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("54572c7b-bbd3-45d4-80dc-28be08941620");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (includeSourceChange != null)
            {
                queryParams.Add("includeSourceChange", includeSourceChange.Value.ToString());
            }

            return SendAsync<List<Change>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the changes associated with a build
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId"></param>
        /// <param name="continuationToken"></param>
        /// <param name="top">The maximum number of changes to return</param>
        /// <param name="includeSourceChange"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Change>> GetBuildChangesAsync(
            Guid project,
            int buildId,
            string continuationToken = null,
            int? top = null,
            bool? includeSourceChange = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("54572c7b-bbd3-45d4-80dc-28be08941620");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (includeSourceChange != null)
            {
                queryParams.Add("includeSourceChange", includeSourceChange.Value.ToString());
            }

            return SendAsync<List<Change>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the changes made to the repository between two given builds.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="fromBuildId">The ID of the first build.</param>
        /// <param name="toBuildId">The ID of the last build.</param>
        /// <param name="top">The maximum number of changes to return.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Change>> GetChangesBetweenBuildsAsync(
            string project,
            int? fromBuildId = null,
            int? toBuildId = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f10f0ea5-18a1-43ec-a8fb-2042c7be9b43");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (fromBuildId != null)
            {
                queryParams.Add("fromBuildId", fromBuildId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (toBuildId != null)
            {
                queryParams.Add("toBuildId", toBuildId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<Change>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the changes made to the repository between two given builds.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="fromBuildId">The ID of the first build.</param>
        /// <param name="toBuildId">The ID of the last build.</param>
        /// <param name="top">The maximum number of changes to return.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Change>> GetChangesBetweenBuildsAsync(
            Guid project,
            int? fromBuildId = null,
            int? toBuildId = null,
            int? top = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("f10f0ea5-18a1-43ec-a8fb-2042c7be9b43");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (fromBuildId != null)
            {
                queryParams.Add("fromBuildId", fromBuildId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (toBuildId != null)
            {
                queryParams.Add("toBuildId", toBuildId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<Change>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Creates a new definition.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionToCloneId"></param>
        /// <param name="definitionToCloneRevision"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinition> CreateDefinitionAsync(
            BuildDefinition definition,
            string project,
            int? definitionToCloneId = null,
            int? definitionToCloneRevision = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<BuildDefinition>(definition, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (definitionToCloneId != null)
            {
                queryParams.Add("definitionToCloneId", definitionToCloneId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (definitionToCloneRevision != null)
            {
                queryParams.Add("definitionToCloneRevision", definitionToCloneRevision.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Creates a new definition.
        /// </summary>
        /// <param name="definition">The definition.</param>
        /// <param name="project">Project ID</param>
        /// <param name="definitionToCloneId"></param>
        /// <param name="definitionToCloneRevision"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinition> CreateDefinitionAsync(
            BuildDefinition definition,
            Guid project,
            int? definitionToCloneId = null,
            int? definitionToCloneRevision = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<BuildDefinition>(definition, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (definitionToCloneId != null)
            {
                queryParams.Add("definitionToCloneId", definitionToCloneId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (definitionToCloneRevision != null)
            {
                queryParams.Add("definitionToCloneRevision", definitionToCloneRevision.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Deletes a definition and all associated builds.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteDefinitionAsync(
            string project,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project, definitionId = definitionId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Deletes a definition and all associated builds.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteDefinitionAsync(
            Guid project,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project, definitionId = definitionId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Gets a definition, optionally at a specific revision.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="revision">The revision number to retrieve. If this is not specified, the latest version will be returned.</param>
        /// <param name="minMetricsTime">If specified, indicates the date from which metrics should be included.</param>
        /// <param name="propertyFilters">A comma-delimited list of properties to include in the results.</param>
        /// <param name="includeLatestBuilds"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinition> GetDefinitionAsync(
            string project,
            int definitionId,
            int? revision = null,
            DateTime? minMetricsTime = null,
            IEnumerable<string> propertyFilters = null,
            bool? includeLatestBuilds = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (revision != null)
            {
                queryParams.Add("revision", revision.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (propertyFilters != null && propertyFilters.Any())
            {
                queryParams.Add("propertyFilters", string.Join(",", propertyFilters));
            }
            if (includeLatestBuilds != null)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a definition, optionally at a specific revision.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="revision">The revision number to retrieve. If this is not specified, the latest version will be returned.</param>
        /// <param name="minMetricsTime">If specified, indicates the date from which metrics should be included.</param>
        /// <param name="propertyFilters">A comma-delimited list of properties to include in the results.</param>
        /// <param name="includeLatestBuilds"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinition> GetDefinitionAsync(
            Guid project,
            int definitionId,
            int? revision = null,
            DateTime? minMetricsTime = null,
            IEnumerable<string> propertyFilters = null,
            bool? includeLatestBuilds = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (revision != null)
            {
                queryParams.Add("revision", revision.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (propertyFilters != null && propertyFilters.Any())
            {
                queryParams.Add("propertyFilters", string.Join(",", propertyFilters));
            }
            if (includeLatestBuilds != null)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of definitions.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="name">If specified, filters to definitions whose names match this pattern.</param>
        /// <param name="repositoryId">A repository ID. If specified, filters to definitions that use this repository.</param>
        /// <param name="repositoryType">If specified, filters to definitions that have a repository of this type.</param>
        /// <param name="queryOrder">Indicates the order in which definitions should be returned.</param>
        /// <param name="top">The maximum number of definitions to return.</param>
        /// <param name="continuationToken">A continuation token, returned by a previous call to this method, that can be used to return the next set of definitions.</param>
        /// <param name="minMetricsTime">If specified, indicates the date from which metrics should be included.</param>
        /// <param name="definitionIds">A comma-delimited list that specifies the IDs of definitions to retrieve.</param>
        /// <param name="path">If specified, filters to definitions under this folder.</param>
        /// <param name="builtAfter">If specified, filters to definitions that have builds after this date.</param>
        /// <param name="notBuiltAfter">If specified, filters to definitions that do not have builds after this date.</param>
        /// <param name="includeAllProperties">Indicates whether the full definitions should be returned. By default, shallow representations of the definitions are returned.</param>
        /// <param name="includeLatestBuilds">Indicates whether to return the latest and latest completed builds for this definition.</param>
        /// <param name="taskIdFilter">If specified, filters to definitions that use the specified task.</param>
        /// <param name="processType">If specified, filters to definitions with the given process type.</param>
        /// <param name="yamlFilename">If specified, filters to YAML definitions that match the given filename.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            string project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            bool? includeAllProperties = null,
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (repositoryId != null)
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (repositoryType != null)
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (path != null)
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }
            if (includeAllProperties != null)
            {
                queryParams.Add("includeAllProperties", includeAllProperties.Value.ToString());
            }
            if (includeLatestBuilds != null)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }
            if (taskIdFilter != null)
            {
                queryParams.Add("taskIdFilter", taskIdFilter.Value.ToString());
            }
            if (processType != null)
            {
                queryParams.Add("processType", processType.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (yamlFilename != null)
            {
                queryParams.Add("yamlFilename", yamlFilename);
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of definitions.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="name">If specified, filters to definitions whose names match this pattern.</param>
        /// <param name="repositoryId">A repository ID. If specified, filters to definitions that use this repository.</param>
        /// <param name="repositoryType">If specified, filters to definitions that have a repository of this type.</param>
        /// <param name="queryOrder">Indicates the order in which definitions should be returned.</param>
        /// <param name="top">The maximum number of definitions to return.</param>
        /// <param name="continuationToken">A continuation token, returned by a previous call to this method, that can be used to return the next set of definitions.</param>
        /// <param name="minMetricsTime">If specified, indicates the date from which metrics should be included.</param>
        /// <param name="definitionIds">A comma-delimited list that specifies the IDs of definitions to retrieve.</param>
        /// <param name="path">If specified, filters to definitions under this folder.</param>
        /// <param name="builtAfter">If specified, filters to definitions that have builds after this date.</param>
        /// <param name="notBuiltAfter">If specified, filters to definitions that do not have builds after this date.</param>
        /// <param name="includeAllProperties">Indicates whether the full definitions should be returned. By default, shallow representations of the definitions are returned.</param>
        /// <param name="includeLatestBuilds">Indicates whether to return the latest and latest completed builds for this definition.</param>
        /// <param name="taskIdFilter">If specified, filters to definitions that use the specified task.</param>
        /// <param name="processType">If specified, filters to definitions with the given process type.</param>
        /// <param name="yamlFilename">If specified, filters to YAML definitions that match the given filename.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        protected virtual Task<List<BuildDefinitionReference>> GetDefinitionsAsync(
            Guid project,
            string name = null,
            string repositoryId = null,
            string repositoryType = null,
            DefinitionQueryOrder? queryOrder = null,
            int? top = null,
            string continuationToken = null,
            DateTime? minMetricsTime = null,
            IEnumerable<int> definitionIds = null,
            string path = null,
            DateTime? builtAfter = null,
            DateTime? notBuiltAfter = null,
            bool? includeAllProperties = null,
            bool? includeLatestBuilds = null,
            Guid? taskIdFilter = null,
            int? processType = null,
            string yamlFilename = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (name != null)
            {
                queryParams.Add("name", name);
            }
            if (repositoryId != null)
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (repositoryType != null)
            {
                queryParams.Add("repositoryType", repositoryType);
            }
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }
            if (top != null)
            {
                queryParams.Add("$top", top.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }
            if (definitionIds != null && definitionIds.Any())
            {
                queryParams.Add("definitionIds", string.Join(",", definitionIds));
            }
            if (path != null)
            {
                queryParams.Add("path", path);
            }
            if (builtAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "builtAfter", builtAfter.Value);
            }
            if (notBuiltAfter != null)
            {
                AddDateTimeToQueryParams(queryParams, "notBuiltAfter", notBuiltAfter.Value);
            }
            if (includeAllProperties != null)
            {
                queryParams.Add("includeAllProperties", includeAllProperties.Value.ToString());
            }
            if (includeLatestBuilds != null)
            {
                queryParams.Add("includeLatestBuilds", includeLatestBuilds.Value.ToString());
            }
            if (taskIdFilter != null)
            {
                queryParams.Add("taskIdFilter", taskIdFilter.Value.ToString());
            }
            if (processType != null)
            {
                queryParams.Add("processType", processType.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (yamlFilename != null)
            {
                queryParams.Add("yamlFilename", yamlFilename);
            }

            return SendAsync<List<BuildDefinitionReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Restores a deleted definition
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The identifier of the definition to restore.</param>
        /// <param name="deleted">When false, restores a deleted definition.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinition> RestoreDefinitionAsync(
            string project,
            int definitionId,
            bool deleted,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("deleted", deleted.ToString());

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Restores a deleted definition
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The identifier of the definition to restore.</param>
        /// <param name="deleted">When false, restores a deleted definition.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinition> RestoreDefinitionAsync(
            Guid project,
            int definitionId,
            bool deleted,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("deleted", deleted.ToString());

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates an existing definition.
        /// </summary>
        /// <param name="definition">The new version of the defintion.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="secretsSourceDefinitionId"></param>
        /// <param name="secretsSourceDefinitionRevision"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinition> UpdateDefinitionAsync(
            BuildDefinition definition,
            string project,
            int definitionId,
            int? secretsSourceDefinitionId = null,
            int? secretsSourceDefinitionRevision = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project, definitionId = definitionId };
            HttpContent content = new ObjectContent<BuildDefinition>(definition, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (secretsSourceDefinitionId != null)
            {
                queryParams.Add("secretsSourceDefinitionId", secretsSourceDefinitionId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (secretsSourceDefinitionRevision != null)
            {
                queryParams.Add("secretsSourceDefinitionRevision", secretsSourceDefinitionRevision.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates an existing definition.
        /// </summary>
        /// <param name="definition">The new version of the defintion.</param>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="secretsSourceDefinitionId"></param>
        /// <param name="secretsSourceDefinitionRevision"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinition> UpdateDefinitionAsync(
            BuildDefinition definition,
            Guid project,
            int definitionId,
            int? secretsSourceDefinitionId = null,
            int? secretsSourceDefinitionRevision = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("dbeaf647-6167-421a-bda9-c9327b25e2e6");
            object routeValues = new { project = project, definitionId = definitionId };
            HttpContent content = new ObjectContent<BuildDefinition>(definition, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (secretsSourceDefinitionId != null)
            {
                queryParams.Add("secretsSourceDefinitionId", secretsSourceDefinitionId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (secretsSourceDefinitionRevision != null)
            {
                queryParams.Add("secretsSourceDefinitionRevision", secretsSourceDefinitionRevision.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<BuildDefinition>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 7),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Gets the contents of a file in the given source code repository.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of the repository to get branches. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="commitOrBranch">The identifier of the commit or branch from which a file's contents are retrieved.</param>
        /// <param name="path">The path to the file to retrieve, relative to the root of the repository.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetFileContentsAsync(
            string project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            string commitOrBranch = null,
            string path = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("29d12225-b1d9-425f-b668-6c594a981313");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }
            if (commitOrBranch != null)
            {
                queryParams.Add("commitOrBranch", commitOrBranch);
            }
            if (path != null)
            {
                queryParams.Add("path", path);
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.1"),
                queryParameters: queryParams,
                mediaType: "text/plain",
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
        /// [Preview API] Gets the contents of a file in the given source code repository.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of the repository to get branches. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="commitOrBranch">The identifier of the commit or branch from which a file's contents are retrieved.</param>
        /// <param name="path">The path to the file to retrieve, relative to the root of the repository.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetFileContentsAsync(
            Guid project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            string commitOrBranch = null,
            string path = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("29d12225-b1d9-425f-b668-6c594a981313");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }
            if (commitOrBranch != null)
            {
                queryParams.Add("commitOrBranch", commitOrBranch);
            }
            if (path != null)
            {
                queryParams.Add("path", path);
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.1"),
                queryParameters: queryParams,
                mediaType: "text/plain",
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
        /// [Preview API] Creates a new folder.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="path">The full path of the folder.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Folder> CreateFolderAsync(
            Folder folder,
            string project,
            string path,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("a906531b-d2da-4f55-bda7-f3e676cc50d9");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<Folder>(folder, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("path", path);

            return SendAsync<Folder>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Creates a new folder.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="project">Project ID</param>
        /// <param name="path">The full path of the folder.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Folder> CreateFolderAsync(
            Folder folder,
            Guid project,
            string path,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("a906531b-d2da-4f55-bda7-f3e676cc50d9");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<Folder>(folder, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("path", path);

            return SendAsync<Folder>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Deletes a definition folder. Definitions and their corresponding builds will also be deleted.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="path">The full path to the folder.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteFolderAsync(
            string project,
            string path,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("a906531b-d2da-4f55-bda7-f3e676cc50d9");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("path", path);

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Deletes a definition folder. Definitions and their corresponding builds will also be deleted.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="path">The full path to the folder.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteFolderAsync(
            Guid project,
            string path,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("a906531b-d2da-4f55-bda7-f3e676cc50d9");
            object routeValues = new { project = project };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("path", path);

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Gets a list of build definition folders.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="path">The path to start with.</param>
        /// <param name="queryOrder">The order in which folders should be returned.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Folder>> GetFoldersAsync(
            string project,
            string path = null,
            FolderQueryOrder? queryOrder = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a906531b-d2da-4f55-bda7-f3e676cc50d9");
            object routeValues = new { project = project, path = path };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }

            return SendAsync<List<Folder>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of build definition folders.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="path">The path to start with.</param>
        /// <param name="queryOrder">The order in which folders should be returned.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<Folder>> GetFoldersAsync(
            Guid project,
            string path = null,
            FolderQueryOrder? queryOrder = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("a906531b-d2da-4f55-bda7-f3e676cc50d9");
            object routeValues = new { project = project, path = path };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (queryOrder != null)
            {
                queryParams.Add("queryOrder", queryOrder.Value.ToString());
            }

            return SendAsync<List<Folder>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates an existing folder at given  existing path
        /// </summary>
        /// <param name="folder">The new version of the folder.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="path">The full path to the folder.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Folder> UpdateFolderAsync(
            Folder folder,
            string project,
            string path,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("a906531b-d2da-4f55-bda7-f3e676cc50d9");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<Folder>(folder, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("path", path);

            return SendAsync<Folder>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates an existing folder at given  existing path
        /// </summary>
        /// <param name="folder">The new version of the folder.</param>
        /// <param name="project">Project ID</param>
        /// <param name="path">The full path to the folder.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Folder> UpdateFolderAsync(
            Folder folder,
            Guid project,
            string path,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("a906531b-d2da-4f55-bda7-f3e676cc50d9");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<Folder>(folder, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("path", path);

            return SendAsync<Folder>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Gets the latest build for a definition, optionally scoped to a specific branch.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definition">definition name with optional leading folder path, or the definition id</param>
        /// <param name="branchName">optional parameter that indicates the specific branch to use</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Build> GetLatestBuildAsync(
            string project,
            string definition,
            string branchName = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("54481611-01f4-47f3-998f-160da0f0c229");
            object routeValues = new { project = project, definition = definition };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the latest build for a definition, optionally scoped to a specific branch.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definition">definition name with optional leading folder path, or the definition id</param>
        /// <param name="branchName">optional parameter that indicates the specific branch to use</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Build> GetLatestBuildAsync(
            Guid project,
            string definition,
            string branchName = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("54481611-01f4-47f3-998f-160da0f0c229");
            object routeValues = new { project = project, definition = definition };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }

            return SendAsync<Build>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets an individual log file for a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="logId">The ID of the log file.</param>
        /// <param name="startLine">The start line.</param>
        /// <param name="endLine">The end line.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetBuildLogAsync(
            string project,
            int buildId,
            int logId,
            long? startLine = null,
            long? endLine = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("35a80daf-7f30-45fc-86e8-6b813d9c90df");
            object routeValues = new { project = project, buildId = buildId, logId = logId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (startLine != null)
            {
                queryParams.Add("startLine", startLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (endLine != null)
            {
                queryParams.Add("endLine", endLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.2"),
                queryParameters: queryParams,
                mediaType: "text/plain",
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
        /// [Preview API] Gets an individual log file for a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="logId">The ID of the log file.</param>
        /// <param name="startLine">The start line.</param>
        /// <param name="endLine">The end line.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetBuildLogAsync(
            Guid project,
            int buildId,
            int logId,
            long? startLine = null,
            long? endLine = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("35a80daf-7f30-45fc-86e8-6b813d9c90df");
            object routeValues = new { project = project, buildId = buildId, logId = logId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (startLine != null)
            {
                queryParams.Add("startLine", startLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (endLine != null)
            {
                queryParams.Add("endLine", endLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.2"),
                queryParameters: queryParams,
                mediaType: "text/plain",
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
        /// [Preview API] Gets an individual log file for a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="logId">The ID of the log file.</param>
        /// <param name="startLine">The start line.</param>
        /// <param name="endLine">The end line.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> GetBuildLogLinesAsync(
            string project,
            int buildId,
            int logId,
            long? startLine = null,
            long? endLine = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("35a80daf-7f30-45fc-86e8-6b813d9c90df");
            object routeValues = new { project = project, buildId = buildId, logId = logId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (startLine != null)
            {
                queryParams.Add("startLine", startLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (endLine != null)
            {
                queryParams.Add("endLine", endLine.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets an individual log file for a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="logId">The ID of the log file.</param>
        /// <param name="startLine">The start line.</param>
        /// <param name="endLine">The end line.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> GetBuildLogLinesAsync(
            Guid project,
            int buildId,
            int logId,
            long? startLine = null,
            long? endLine = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("35a80daf-7f30-45fc-86e8-6b813d9c90df");
            object routeValues = new { project = project, buildId = buildId, logId = logId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (startLine != null)
            {
                queryParams.Add("startLine", startLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (endLine != null)
            {
                queryParams.Add("endLine", endLine.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the logs for a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildLog>> GetBuildLogsAsync(
            string project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("35a80daf-7f30-45fc-86e8-6b813d9c90df");
            object routeValues = new { project = project, buildId = buildId };

            return SendAsync<List<BuildLog>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the logs for a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildLog>> GetBuildLogsAsync(
            Guid project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("35a80daf-7f30-45fc-86e8-6b813d9c90df");
            object routeValues = new { project = project, buildId = buildId };

            return SendAsync<List<BuildLog>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the logs for a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetBuildLogsZipAsync(
            string project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("35a80daf-7f30-45fc-86e8-6b813d9c90df");
            object routeValues = new { project = project, buildId = buildId };
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.2"),
                mediaType: "application/zip",
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
        /// [Preview API] Gets the logs for a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetBuildLogsZipAsync(
            Guid project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("35a80daf-7f30-45fc-86e8-6b813d9c90df");
            object routeValues = new { project = project, buildId = buildId };
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.2"),
                mediaType: "application/zip",
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
        /// [Preview API] Gets an individual log file for a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="logId">The ID of the log file.</param>
        /// <param name="startLine">The start line.</param>
        /// <param name="endLine">The end line.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetBuildLogZipAsync(
            string project,
            int buildId,
            int logId,
            long? startLine = null,
            long? endLine = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("35a80daf-7f30-45fc-86e8-6b813d9c90df");
            object routeValues = new { project = project, buildId = buildId, logId = logId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (startLine != null)
            {
                queryParams.Add("startLine", startLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (endLine != null)
            {
                queryParams.Add("endLine", endLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.2"),
                queryParameters: queryParams,
                mediaType: "application/zip",
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
        /// [Preview API] Gets an individual log file for a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="logId">The ID of the log file.</param>
        /// <param name="startLine">The start line.</param>
        /// <param name="endLine">The end line.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetBuildLogZipAsync(
            Guid project,
            int buildId,
            int logId,
            long? startLine = null,
            long? endLine = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("35a80daf-7f30-45fc-86e8-6b813d9c90df");
            object routeValues = new { project = project, buildId = buildId, logId = logId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (startLine != null)
            {
                queryParams.Add("startLine", startLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (endLine != null)
            {
                queryParams.Add("endLine", endLine.Value.ToString(CultureInfo.InvariantCulture));
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.2"),
                queryParameters: queryParams,
                mediaType: "application/zip",
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
        /// [Preview API] Gets build metrics for a project.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="metricAggregationType">The aggregation type to use (hourly, daily).</param>
        /// <param name="minMetricsTime">The date from which to calculate metrics.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildMetric>> GetProjectMetricsAsync(
            string project,
            string metricAggregationType = null,
            DateTime? minMetricsTime = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7433fae7-a6bc-41dc-a6e2-eef9005ce41a");
            object routeValues = new { project = project, metricAggregationType = metricAggregationType };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }

            return SendAsync<List<BuildMetric>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets build metrics for a project.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="metricAggregationType">The aggregation type to use (hourly, daily).</param>
        /// <param name="minMetricsTime">The date from which to calculate metrics.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildMetric>> GetProjectMetricsAsync(
            Guid project,
            string metricAggregationType = null,
            DateTime? minMetricsTime = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7433fae7-a6bc-41dc-a6e2-eef9005ce41a");
            object routeValues = new { project = project, metricAggregationType = metricAggregationType };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }

            return SendAsync<List<BuildMetric>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets build metrics for a definition.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="minMetricsTime">The date from which to calculate metrics.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildMetric>> GetDefinitionMetricsAsync(
            string project,
            int definitionId,
            DateTime? minMetricsTime = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d973b939-0ce0-4fec-91d8-da3940fa1827");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }

            return SendAsync<List<BuildMetric>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets build metrics for a definition.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="minMetricsTime">The date from which to calculate metrics.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildMetric>> GetDefinitionMetricsAsync(
            Guid project,
            int definitionId,
            DateTime? minMetricsTime = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d973b939-0ce0-4fec-91d8-da3940fa1827");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (minMetricsTime != null)
            {
                AddDateTimeToQueryParams(queryParams, "minMetricsTime", minMetricsTime.Value);
            }

            return SendAsync<List<BuildMetric>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets all build definition options supported by the system.
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildOptionDefinition>> GetBuildOptionDefinitionsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("591cb5a4-2d46-4f3a-a697-5cd42b6bd332");

            return SendAsync<List<BuildOptionDefinition>>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets all build definition options supported by the system.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildOptionDefinition>> GetBuildOptionDefinitionsAsync(
            string project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("591cb5a4-2d46-4f3a-a697-5cd42b6bd332");
            object routeValues = new { project = project };

            return SendAsync<List<BuildOptionDefinition>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets all build definition options supported by the system.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildOptionDefinition>> GetBuildOptionDefinitionsAsync(
            Guid project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("591cb5a4-2d46-4f3a-a697-5cd42b6bd332");
            object routeValues = new { project = project };

            return SendAsync<List<BuildOptionDefinition>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the contents of a directory in the given source code repository.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of the repository to get branches. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="commitOrBranch">The identifier of the commit or branch from which a file's contents are retrieved.</param>
        /// <param name="path">The path contents to list, relative to the root of the repository.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<SourceRepositoryItem>> GetPathContentsAsync(
            string project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            string commitOrBranch = null,
            string path = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7944d6fb-df01-4709-920a-7a189aa34037");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }
            if (commitOrBranch != null)
            {
                queryParams.Add("commitOrBranch", commitOrBranch);
            }
            if (path != null)
            {
                queryParams.Add("path", path);
            }

            return SendAsync<List<SourceRepositoryItem>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the contents of a directory in the given source code repository.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of the repository to get branches. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="commitOrBranch">The identifier of the commit or branch from which a file's contents are retrieved.</param>
        /// <param name="path">The path contents to list, relative to the root of the repository.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<SourceRepositoryItem>> GetPathContentsAsync(
            Guid project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            string commitOrBranch = null,
            string path = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7944d6fb-df01-4709-920a-7a189aa34037");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }
            if (commitOrBranch != null)
            {
                queryParams.Add("commitOrBranch", commitOrBranch);
            }
            if (path != null)
            {
                queryParams.Add("path", path);
            }

            return SendAsync<List<SourceRepositoryItem>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets properties for a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="filter">A comma-delimited list of properties. If specified, filters to these specific properties.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<PropertiesCollection> GetBuildPropertiesAsync(
            string project,
            int buildId,
            IEnumerable<string> filter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0a6312e9-0627-49b7-8083-7d74a64849c9");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (filter != null && filter.Any())
            {
                queryParams.Add("filter", string.Join(",", filter));
            }

            return SendAsync<PropertiesCollection>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets properties for a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="filter">A comma-delimited list of properties. If specified, filters to these specific properties.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<PropertiesCollection> GetBuildPropertiesAsync(
            Guid project,
            int buildId,
            IEnumerable<string> filter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("0a6312e9-0627-49b7-8083-7d74a64849c9");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (filter != null && filter.Any())
            {
                queryParams.Add("filter", string.Join(",", filter));
            }

            return SendAsync<PropertiesCollection>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets properties for a definition.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="filter">A comma-delimited list of properties. If specified, filters to these specific properties.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<PropertiesCollection> GetDefinitionPropertiesAsync(
            string project,
            int definitionId,
            IEnumerable<string> filter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d9826ad7-2a68-46a9-a6e9-677698777895");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (filter != null && filter.Any())
            {
                queryParams.Add("filter", string.Join(",", filter));
            }

            return SendAsync<PropertiesCollection>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets properties for a definition.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="filter">A comma-delimited list of properties. If specified, filters to these specific properties.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<PropertiesCollection> GetDefinitionPropertiesAsync(
            Guid project,
            int definitionId,
            IEnumerable<string> filter = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d9826ad7-2a68-46a9-a6e9-677698777895");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (filter != null && filter.Any())
            {
                queryParams.Add("filter", string.Join(",", filter));
            }

            return SendAsync<PropertiesCollection>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a pull request object from source provider.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="pullRequestId">Vendor-specific id of the pull request.</param>
        /// <param name="repositoryId">Vendor-specific identifier or the name of the repository that contains the pull request.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<PullRequest> GetPullRequestAsync(
            string project,
            string providerName,
            string pullRequestId,
            string repositoryId = null,
            Guid? serviceEndpointId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d8763ec7-9ff0-4fb4-b2b2-9d757906ff14");
            object routeValues = new { project = project, providerName = providerName, pullRequestId = pullRequestId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (repositoryId != null)
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }

            return SendAsync<PullRequest>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a pull request object from source provider.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="pullRequestId">Vendor-specific id of the pull request.</param>
        /// <param name="repositoryId">Vendor-specific identifier or the name of the repository that contains the pull request.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<PullRequest> GetPullRequestAsync(
            Guid project,
            string providerName,
            string pullRequestId,
            string repositoryId = null,
            Guid? serviceEndpointId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d8763ec7-9ff0-4fb4-b2b2-9d757906ff14");
            object routeValues = new { project = project, providerName = providerName, pullRequestId = pullRequestId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (repositoryId != null)
            {
                queryParams.Add("repositoryId", repositoryId);
            }
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }

            return SendAsync<PullRequest>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a build report.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="type"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildReportMetadata> GetBuildReportAsync(
            string project,
            int buildId,
            string type = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("45bcaa88-67e1-4042-a035-56d3b4a7d44c");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (type != null)
            {
                queryParams.Add("type", type);
            }

            return SendAsync<BuildReportMetadata>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a build report.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="type"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildReportMetadata> GetBuildReportAsync(
            Guid project,
            int buildId,
            string type = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("45bcaa88-67e1-4042-a035-56d3b4a7d44c");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (type != null)
            {
                queryParams.Add("type", type);
            }

            return SendAsync<BuildReportMetadata>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a build report.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="type"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetBuildReportHtmlContentAsync(
            string project,
            int buildId,
            string type = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("45bcaa88-67e1-4042-a035-56d3b4a7d44c");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (type != null)
            {
                queryParams.Add("type", type);
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.2"),
                queryParameters: queryParams,
                mediaType: "text/html",
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
        /// [Preview API] Gets a build report.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="type"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task<Stream> GetBuildReportHtmlContentAsync(
            Guid project,
            int buildId,
            string type = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("45bcaa88-67e1-4042-a035-56d3b4a7d44c");
            object routeValues = new { project = project, buildId = buildId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (type != null)
            {
                queryParams.Add("type", type);
            }
            HttpResponseMessage response;
            using (HttpRequestMessage requestMessage = await CreateRequestMessageAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion("5.2-preview.2"),
                queryParameters: queryParams,
                mediaType: "text/html",
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
        /// [Preview API] Gets a list of source code repositories.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of a single repository to get.</param>
        /// <param name="resultSet">'top' for the repositories most relevant for the endpoint. If not set, all repositories are returned. Ignored if 'repository' is set.</param>
        /// <param name="pageResults">If set to true, this will limit the set of results and will return a continuation token to continue the query.</param>
        /// <param name="continuationToken">When paging results, this is a continuation token, returned by a previous call to this method, that can be used to return the next set of repositories.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<SourceRepositories> ListRepositoriesAsync(
            string project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            ResultSet? resultSet = null,
            bool? pageResults = null,
            string continuationToken = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d44d1680-f978-4834-9b93-8c6e132329c9");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }
            if (resultSet != null)
            {
                queryParams.Add("resultSet", resultSet.Value.ToString());
            }
            if (pageResults != null)
            {
                queryParams.Add("pageResults", pageResults.Value.ToString());
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }

            return SendAsync<SourceRepositories>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of source code repositories.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of a single repository to get.</param>
        /// <param name="resultSet">'top' for the repositories most relevant for the endpoint. If not set, all repositories are returned. Ignored if 'repository' is set.</param>
        /// <param name="pageResults">If set to true, this will limit the set of results and will return a continuation token to continue the query.</param>
        /// <param name="continuationToken">When paging results, this is a continuation token, returned by a previous call to this method, that can be used to return the next set of repositories.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<SourceRepositories> ListRepositoriesAsync(
            Guid project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            ResultSet? resultSet = null,
            bool? pageResults = null,
            string continuationToken = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d44d1680-f978-4834-9b93-8c6e132329c9");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }
            if (resultSet != null)
            {
                queryParams.Add("resultSet", resultSet.Value.ToString());
            }
            if (pageResults != null)
            {
                queryParams.Add("pageResults", pageResults.Value.ToString());
            }
            if (continuationToken != null)
            {
                queryParams.Add("continuationToken", continuationToken);
            }

            return SendAsync<SourceRepositories>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DefinitionResourceReference>> AuthorizeDefinitionResourcesAsync(
            IEnumerable<DefinitionResourceReference> resources,
            string project,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("ea623316-1967-45eb-89ab-e9e6110cf2d6");
            object routeValues = new { project = project, definitionId = definitionId };
            HttpContent content = new ObjectContent<IEnumerable<DefinitionResourceReference>>(resources, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<DefinitionResourceReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DefinitionResourceReference>> AuthorizeDefinitionResourcesAsync(
            IEnumerable<DefinitionResourceReference> resources,
            Guid project,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("ea623316-1967-45eb-89ab-e9e6110cf2d6");
            object routeValues = new { project = project, definitionId = definitionId };
            HttpContent content = new ObjectContent<IEnumerable<DefinitionResourceReference>>(resources, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<DefinitionResourceReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DefinitionResourceReference>> GetDefinitionResourcesAsync(
            string project,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("ea623316-1967-45eb-89ab-e9e6110cf2d6");
            object routeValues = new { project = project, definitionId = definitionId };

            return SendAsync<List<DefinitionResourceReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API]
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<DefinitionResourceReference>> GetDefinitionResourcesAsync(
            Guid project,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("ea623316-1967-45eb-89ab-e9e6110cf2d6");
            object routeValues = new { project = project, definitionId = definitionId };

            return SendAsync<List<DefinitionResourceReference>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets information about build resources in the system.
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildResourceUsage> GetResourceUsageAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3813d06c-9e36-4ea1-aac3-61a485d60e3d");

            return SendAsync<BuildResourceUsage>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets all revisions of a definition.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildDefinitionRevision>> GetDefinitionRevisionsAsync(
            string project,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7c116775-52e5-453e-8c5d-914d9762d8c4");
            object routeValues = new { project = project, definitionId = definitionId };

            return SendAsync<List<BuildDefinitionRevision>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 3),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets all revisions of a definition.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildDefinitionRevision>> GetDefinitionRevisionsAsync(
            Guid project,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("7c116775-52e5-453e-8c5d-914d9762d8c4");
            object routeValues = new { project = project, definitionId = definitionId };

            return SendAsync<List<BuildDefinitionRevision>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 3),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the build settings.
        /// </summary>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildSettings> GetBuildSettingsAsync(
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("aa8c1c9c-ef8b-474a-b8c4-785c7b191d0d");

            return SendAsync<BuildSettings>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the build settings.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildSettings> GetBuildSettingsAsync(
            string project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("aa8c1c9c-ef8b-474a-b8c4-785c7b191d0d");
            object routeValues = new { project = project };

            return SendAsync<BuildSettings>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the build settings.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildSettings> GetBuildSettingsAsync(
            Guid project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("aa8c1c9c-ef8b-474a-b8c4-785c7b191d0d");
            object routeValues = new { project = project };

            return SendAsync<BuildSettings>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates the build settings.
        /// </summary>
        /// <param name="settings">The new settings.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildSettings> UpdateBuildSettingsAsync(
            BuildSettings settings,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("aa8c1c9c-ef8b-474a-b8c4-785c7b191d0d");
            HttpContent content = new ObjectContent<BuildSettings>(settings, new VssJsonMediaTypeFormatter(true));

            return SendAsync<BuildSettings>(
                httpMethod,
                locationId,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates the build settings.
        /// </summary>
        /// <param name="settings">The new settings.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildSettings> UpdateBuildSettingsAsync(
            BuildSettings settings,
            string project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("aa8c1c9c-ef8b-474a-b8c4-785c7b191d0d");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<BuildSettings>(settings, new VssJsonMediaTypeFormatter(true));

            return SendAsync<BuildSettings>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates the build settings.
        /// </summary>
        /// <param name="settings">The new settings.</param>
        /// <param name="project">Project ID</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildSettings> UpdateBuildSettingsAsync(
            BuildSettings settings,
            Guid project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PATCH");
            Guid locationId = new Guid("aa8c1c9c-ef8b-474a-b8c4-785c7b191d0d");
            object routeValues = new { project = project };
            HttpContent content = new ObjectContent<BuildSettings>(settings, new VssJsonMediaTypeFormatter(true));

            return SendAsync<BuildSettings>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Get a list of source providers and their capabilities.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<SourceProviderAttributes>> ListSourceProvidersAsync(
            string project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3ce81729-954f-423d-a581-9fea01d25186");
            object routeValues = new { project = project };

            return SendAsync<List<SourceProviderAttributes>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a list of source providers and their capabilities.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<SourceProviderAttributes>> ListSourceProvidersAsync(
            Guid project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("3ce81729-954f-423d-a581-9fea01d25186");
            object routeValues = new { project = project };

            return SendAsync<List<SourceProviderAttributes>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] <p>Gets the build status for a definition, optionally scoped to a specific branch, stage, job, and configuration.</p> <p>If there are more than one, then it is required to pass in a <paramref name="stageName" /> value when specifying a <paramref name="jobName" />, and the same rule then applies for both if passing a <paramref name="configuration" /> parameter.</p>
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definition">Either the definition name with optional leading folder path, or the definition id.</param>
        /// <param name="branchName">Only consider the most recent build for this branch.</param>
        /// <param name="stageName">Use this stage within the pipeline to render the status.</param>
        /// <param name="jobName">Use this job within a stage of the pipeline to render the status.</param>
        /// <param name="configuration">Use this job configuration to render the status</param>
        /// <param name="label">Replaces the default text on the left side of the badge.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<string> GetStatusBadgeAsync(
            string project,
            string definition,
            string branchName = null,
            string stageName = null,
            string jobName = null,
            string configuration = null,
            string label = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("07acfdce-4757-4439-b422-ddd13a2fcc10");
            object routeValues = new { project = project, definition = definition };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }
            if (stageName != null)
            {
                queryParams.Add("stageName", stageName);
            }
            if (jobName != null)
            {
                queryParams.Add("jobName", jobName);
            }
            if (configuration != null)
            {
                queryParams.Add("configuration", configuration);
            }
            if (label != null)
            {
                queryParams.Add("label", label);
            }

            return SendAsync<string>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] <p>Gets the build status for a definition, optionally scoped to a specific branch, stage, job, and configuration.</p> <p>If there are more than one, then it is required to pass in a <paramref name="stageName" /> value when specifying a <paramref name="jobName" />, and the same rule then applies for both if passing a <paramref name="configuration" /> parameter.</p>
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definition">Either the definition name with optional leading folder path, or the definition id.</param>
        /// <param name="branchName">Only consider the most recent build for this branch.</param>
        /// <param name="stageName">Use this stage within the pipeline to render the status.</param>
        /// <param name="jobName">Use this job within a stage of the pipeline to render the status.</param>
        /// <param name="configuration">Use this job configuration to render the status</param>
        /// <param name="label">Replaces the default text on the left side of the badge.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<string> GetStatusBadgeAsync(
            Guid project,
            string definition,
            string branchName = null,
            string stageName = null,
            string jobName = null,
            string configuration = null,
            string label = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("07acfdce-4757-4439-b422-ddd13a2fcc10");
            object routeValues = new { project = project, definition = definition };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (branchName != null)
            {
                queryParams.Add("branchName", branchName);
            }
            if (stageName != null)
            {
                queryParams.Add("stageName", stageName);
            }
            if (jobName != null)
            {
                queryParams.Add("jobName", jobName);
            }
            if (configuration != null)
            {
                queryParams.Add("configuration", configuration);
            }
            if (label != null)
            {
                queryParams.Add("label", label);
            }

            return SendAsync<string>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Adds a tag to a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="tag">The tag to add.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> AddBuildTagAsync(
            string project,
            int buildId,
            string tag,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("6e6114b2-8161-44c8-8f6c-c5505782427f");
            object routeValues = new { project = project, buildId = buildId, tag = tag };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Adds a tag to a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="tag">The tag to add.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> AddBuildTagAsync(
            Guid project,
            int buildId,
            string tag,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("6e6114b2-8161-44c8-8f6c-c5505782427f");
            object routeValues = new { project = project, buildId = buildId, tag = tag };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Adds tags to a build.
        /// </summary>
        /// <param name="tags">The tags to add.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> AddBuildTagsAsync(
            IEnumerable<string> tags,
            string project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("6e6114b2-8161-44c8-8f6c-c5505782427f");
            object routeValues = new { project = project, buildId = buildId };
            HttpContent content = new ObjectContent<IEnumerable<string>>(tags, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Adds tags to a build.
        /// </summary>
        /// <param name="tags">The tags to add.</param>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> AddBuildTagsAsync(
            IEnumerable<string> tags,
            Guid project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("6e6114b2-8161-44c8-8f6c-c5505782427f");
            object routeValues = new { project = project, buildId = buildId };
            HttpContent content = new ObjectContent<IEnumerable<string>>(tags, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Removes a tag from a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="tag">The tag to remove.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> DeleteBuildTagAsync(
            string project,
            int buildId,
            string tag,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("6e6114b2-8161-44c8-8f6c-c5505782427f");
            object routeValues = new { project = project, buildId = buildId, tag = tag };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Removes a tag from a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="tag">The tag to remove.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> DeleteBuildTagAsync(
            Guid project,
            int buildId,
            string tag,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("6e6114b2-8161-44c8-8f6c-c5505782427f");
            object routeValues = new { project = project, buildId = buildId, tag = tag };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the tags for a build.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> GetBuildTagsAsync(
            string project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6e6114b2-8161-44c8-8f6c-c5505782427f");
            object routeValues = new { project = project, buildId = buildId };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the tags for a build.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId">The ID of the build.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> GetBuildTagsAsync(
            Guid project,
            int buildId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("6e6114b2-8161-44c8-8f6c-c5505782427f");
            object routeValues = new { project = project, buildId = buildId };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Adds a tag to a definition
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="tag">The tag to add.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> AddDefinitionTagAsync(
            string project,
            int definitionId,
            string tag,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("cb894432-134a-4d31-a839-83beceaace4b");
            object routeValues = new { project = project, definitionId = definitionId, tag = tag };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Adds a tag to a definition
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="tag">The tag to add.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> AddDefinitionTagAsync(
            Guid project,
            int definitionId,
            string tag,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("cb894432-134a-4d31-a839-83beceaace4b");
            object routeValues = new { project = project, definitionId = definitionId, tag = tag };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Adds multiple tags to a definition.
        /// </summary>
        /// <param name="tags">The tags to add.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> AddDefinitionTagsAsync(
            IEnumerable<string> tags,
            string project,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("cb894432-134a-4d31-a839-83beceaace4b");
            object routeValues = new { project = project, definitionId = definitionId };
            HttpContent content = new ObjectContent<IEnumerable<string>>(tags, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Adds multiple tags to a definition.
        /// </summary>
        /// <param name="tags">The tags to add.</param>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> AddDefinitionTagsAsync(
            IEnumerable<string> tags,
            Guid project,
            int definitionId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("cb894432-134a-4d31-a839-83beceaace4b");
            object routeValues = new { project = project, definitionId = definitionId };
            HttpContent content = new ObjectContent<IEnumerable<string>>(tags, new VssJsonMediaTypeFormatter(true));

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Removes a tag from a definition.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="tag">The tag to remove.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> DeleteDefinitionTagAsync(
            string project,
            int definitionId,
            string tag,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("cb894432-134a-4d31-a839-83beceaace4b");
            object routeValues = new { project = project, definitionId = definitionId, tag = tag };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Removes a tag from a definition.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="tag">The tag to remove.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> DeleteDefinitionTagAsync(
            Guid project,
            int definitionId,
            string tag,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("cb894432-134a-4d31-a839-83beceaace4b");
            object routeValues = new { project = project, definitionId = definitionId, tag = tag };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the tags for a definition.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="revision">The definition revision number. If not specified, uses the latest revision of the definition.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> GetDefinitionTagsAsync(
            string project,
            int definitionId,
            int? revision = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("cb894432-134a-4d31-a839-83beceaace4b");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (revision != null)
            {
                queryParams.Add("revision", revision.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets the tags for a definition.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="definitionId">The ID of the definition.</param>
        /// <param name="revision">The definition revision number. If not specified, uses the latest revision of the definition.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> GetDefinitionTagsAsync(
            Guid project,
            int definitionId,
            int? revision = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("cb894432-134a-4d31-a839-83beceaace4b");
            object routeValues = new { project = project, definitionId = definitionId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (revision != null)
            {
                queryParams.Add("revision", revision.Value.ToString(CultureInfo.InvariantCulture));
            }

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of all build and definition tags in the project.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> GetTagsAsync(
            string project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d84ac5c6-edc7-43d5-adc9-1b34be5dea09");
            object routeValues = new { project = project };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of all build and definition tags in the project.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<string>> GetTagsAsync(
            Guid project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("d84ac5c6-edc7-43d5-adc9-1b34be5dea09");
            object routeValues = new { project = project };

            return SendAsync<List<string>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Deletes a build definition template.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="templateId">The ID of the template.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteTemplateAsync(
            string project,
            string templateId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("e884571e-7f92-4d6a-9274-3f5649900835");
            object routeValues = new { project = project, templateId = templateId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 3),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Deletes a build definition template.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="templateId">The ID of the template.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task DeleteTemplateAsync(
            Guid project,
            string templateId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("DELETE");
            Guid locationId = new Guid("e884571e-7f92-4d6a-9274-3f5649900835");
            object routeValues = new { project = project, templateId = templateId };

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 3),
                userState: userState,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Gets a specific build definition template.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="templateId">The ID of the requested template.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinitionTemplate> GetTemplateAsync(
            string project,
            string templateId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e884571e-7f92-4d6a-9274-3f5649900835");
            object routeValues = new { project = project, templateId = templateId };

            return SendAsync<BuildDefinitionTemplate>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 3),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a specific build definition template.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="templateId">The ID of the requested template.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinitionTemplate> GetTemplateAsync(
            Guid project,
            string templateId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e884571e-7f92-4d6a-9274-3f5649900835");
            object routeValues = new { project = project, templateId = templateId };

            return SendAsync<BuildDefinitionTemplate>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 3),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets all definition templates.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildDefinitionTemplate>> GetTemplatesAsync(
            string project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e884571e-7f92-4d6a-9274-3f5649900835");
            object routeValues = new { project = project };

            return SendAsync<List<BuildDefinitionTemplate>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 3),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets all definition templates.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<BuildDefinitionTemplate>> GetTemplatesAsync(
            Guid project,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("e884571e-7f92-4d6a-9274-3f5649900835");
            object routeValues = new { project = project };

            return SendAsync<List<BuildDefinitionTemplate>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 3),
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Updates an existing build definition template.
        /// </summary>
        /// <param name="template">The new version of the template.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="templateId">The ID of the template.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinitionTemplate> SaveTemplateAsync(
            BuildDefinitionTemplate template,
            string project,
            string templateId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("e884571e-7f92-4d6a-9274-3f5649900835");
            object routeValues = new { project = project, templateId = templateId };
            HttpContent content = new ObjectContent<BuildDefinitionTemplate>(template, new VssJsonMediaTypeFormatter(true));

            return SendAsync<BuildDefinitionTemplate>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 3),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Updates an existing build definition template.
        /// </summary>
        /// <param name="template">The new version of the template.</param>
        /// <param name="project">Project ID</param>
        /// <param name="templateId">The ID of the template.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<BuildDefinitionTemplate> SaveTemplateAsync(
            BuildDefinitionTemplate template,
            Guid project,
            string templateId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("PUT");
            Guid locationId = new Guid("e884571e-7f92-4d6a-9274-3f5649900835");
            object routeValues = new { project = project, templateId = templateId };
            HttpContent content = new ObjectContent<BuildDefinitionTemplate>(template, new VssJsonMediaTypeFormatter(true));

            return SendAsync<BuildDefinitionTemplate>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 3),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Gets details for a build
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="buildId"></param>
        /// <param name="timelineId"></param>
        /// <param name="changeId"></param>
        /// <param name="planId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Timeline> GetBuildTimelineAsync(
            string project,
            int buildId,
            Guid? timelineId = null,
            int? changeId = null,
            Guid? planId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8baac422-4c6e-4de5-8532-db96d92acffa");
            object routeValues = new { project = project, buildId = buildId, timelineId = timelineId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (changeId != null)
            {
                queryParams.Add("changeId", changeId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (planId != null)
            {
                queryParams.Add("planId", planId.Value.ToString());
            }

            return SendAsync<Timeline>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets details for a build
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="buildId"></param>
        /// <param name="timelineId"></param>
        /// <param name="changeId"></param>
        /// <param name="planId"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Timeline> GetBuildTimelineAsync(
            Guid project,
            int buildId,
            Guid? timelineId = null,
            int? changeId = null,
            Guid? planId = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8baac422-4c6e-4de5-8532-db96d92acffa");
            object routeValues = new { project = project, buildId = buildId, timelineId = timelineId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (changeId != null)
            {
                queryParams.Add("changeId", changeId.Value.ToString(CultureInfo.InvariantCulture));
            }
            if (planId != null)
            {
                queryParams.Add("planId", planId.Value.ToString());
            }

            return SendAsync<Timeline>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 2),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Recreates the webhooks for the specified triggers in the given source code repository.
        /// </summary>
        /// <param name="triggerTypes">The types of triggers to restore webhooks for.</param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of the repository to get webhooks. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task RestoreWebhooksAsync(
            List<DefinitionTriggerType> triggerTypes,
            string project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("793bceb8-9736-4030-bd2f-fb3ce6d6b478");
            object routeValues = new { project = project, providerName = providerName };
            HttpContent content = new ObjectContent<List<DefinitionTriggerType>>(triggerTypes, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Recreates the webhooks for the specified triggers in the given source code repository.
        /// </summary>
        /// <param name="triggerTypes">The types of triggers to restore webhooks for.</param>
        /// <param name="project">Project ID</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of the repository to get webhooks. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual async Task RestoreWebhooksAsync(
            List<DefinitionTriggerType> triggerTypes,
            Guid project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("793bceb8-9736-4030-bd2f-fb3ce6d6b478");
            object routeValues = new { project = project, providerName = providerName };
            HttpContent content = new ObjectContent<List<DefinitionTriggerType>>(triggerTypes, new VssJsonMediaTypeFormatter(true));

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }

            using (HttpResponseMessage response = await SendAsync(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken,
                content: content).ConfigureAwait(false))
            {
                return;
            }
        }

        /// <summary>
        /// [Preview API] Gets a list of webhooks installed in the given source code repository.
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of the repository to get webhooks. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<RepositoryWebhook>> ListWebhooksAsync(
            string project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8f20ff82-9498-4812-9f6e-9c01bdc50e99");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }

            return SendAsync<List<RepositoryWebhook>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Gets a list of webhooks installed in the given source code repository.
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="providerName">The name of the source provider.</param>
        /// <param name="serviceEndpointId">If specified, the ID of the service endpoint to query. Can only be omitted for providers that do not use service endpoints, e.g. TFVC or TFGit.</param>
        /// <param name="repository">If specified, the vendor-specific identifier or the name of the repository to get webhooks. Can only be omitted for providers that do not support multiple repositories.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<List<RepositoryWebhook>> ListWebhooksAsync(
            Guid project,
            string providerName,
            Guid? serviceEndpointId = null,
            string repository = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("8f20ff82-9498-4812-9f6e-9c01bdc50e99");
            object routeValues = new { project = project, providerName = providerName };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            if (serviceEndpointId != null)
            {
                queryParams.Add("serviceEndpointId", serviceEndpointId.Value.ToString());
            }
            if (repository != null)
            {
                queryParams.Add("repository", repository);
            }

            return SendAsync<List<RepositoryWebhook>>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(5.2, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
