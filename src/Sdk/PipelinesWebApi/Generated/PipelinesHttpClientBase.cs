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
 *   actions\client\webapi\clientgeneratorconfigs\pipelines.genclient.json
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

namespace GitHub.Actions.Pipelines.WebApi
{
    [ResourceArea(PipelinesArea.IdString)]
    public abstract class PipelinesHttpClientBase : VssHttpClientBase
    {
        public PipelinesHttpClientBase(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        {
        }

        public PipelinesHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public PipelinesHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public PipelinesHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public PipelinesHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        /// <summary>
        /// [Preview API] Associates an artifact with a run.
        /// </summary>
        /// <param name="createArtifactParameters"></param>
        /// <param name="pipelineId">The ID of the pipeline.</param>
        /// <param name="runId">The ID of the run.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Artifact> CreateArtifactAsync(
            CreateArtifactParameters createArtifactParameters,
            int pipelineId,
            int runId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("85023071-bd5e-4438-89b0-2a5bf362a19d");
            object routeValues = new { pipelineId = pipelineId, runId = runId };
            HttpContent content = new ObjectContent<CreateArtifactParameters>(createArtifactParameters, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Artifact>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Associates an artifact with a run.
        /// </summary>
        /// <param name="createArtifactParameters"></param>
        /// <param name="project">Project ID or project name</param>
        /// <param name="pipelineId">The ID of the pipeline.</param>
        /// <param name="runId">The ID of the run.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Artifact> CreateArtifactAsync(
            CreateArtifactParameters createArtifactParameters,
            string project,
            int pipelineId,
            int runId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("85023071-bd5e-4438-89b0-2a5bf362a19d");
            object routeValues = new { project = project, pipelineId = pipelineId, runId = runId };
            HttpContent content = new ObjectContent<CreateArtifactParameters>(createArtifactParameters, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Artifact>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Associates an artifact with a run.
        /// </summary>
        /// <param name="createArtifactParameters"></param>
        /// <param name="project">Project ID</param>
        /// <param name="pipelineId">The ID of the pipeline.</param>
        /// <param name="runId">The ID of the run.</param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Artifact> CreateArtifactAsync(
            CreateArtifactParameters createArtifactParameters,
            Guid project,
            int pipelineId,
            int runId,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("POST");
            Guid locationId = new Guid("85023071-bd5e-4438-89b0-2a5bf362a19d");
            object routeValues = new { project = project, pipelineId = pipelineId, runId = runId };
            HttpContent content = new ObjectContent<CreateArtifactParameters>(createArtifactParameters, new VssJsonMediaTypeFormatter(true));

            return SendAsync<Artifact>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 1),
                userState: userState,
                cancellationToken: cancellationToken,
                content: content);
        }

        /// <summary>
        /// [Preview API] Get a specific artifact
        /// </summary>
        /// <param name="project">Project ID or project name</param>
        /// <param name="pipelineId"></param>
        /// <param name="runId"></param>
        /// <param name="artifactName"></param>
        /// <param name="expand"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Artifact> GetArtifactAsync(
            string project,
            int pipelineId,
            int runId,
            string artifactName,
            GetArtifactExpandOptions? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("85023071-bd5e-4438-89b0-2a5bf362a19d");
            object routeValues = new { project = project, pipelineId = pipelineId, runId = runId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("artifactName", artifactName);
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<Artifact>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a specific artifact
        /// </summary>
        /// <param name="project">Project ID</param>
        /// <param name="pipelineId"></param>
        /// <param name="runId"></param>
        /// <param name="artifactName"></param>
        /// <param name="expand"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Artifact> GetArtifactAsync(
            Guid project,
            int pipelineId,
            int runId,
            string artifactName,
            GetArtifactExpandOptions? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("85023071-bd5e-4438-89b0-2a5bf362a19d");
            object routeValues = new { project = project, pipelineId = pipelineId, runId = runId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("artifactName", artifactName);
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<Artifact>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// [Preview API] Get a specific artifact
        /// </summary>
        /// <param name="pipelineId"></param>
        /// <param name="runId"></param>
        /// <param name="artifactName"></param>
        /// <param name="expand"></param>
        /// <param name="userState"></param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        public virtual Task<Artifact> GetArtifactAsync(
            int pipelineId,
            int runId,
            string artifactName,
            GetArtifactExpandOptions? expand = null,
            object userState = null,
            CancellationToken cancellationToken = default)
        {
            HttpMethod httpMethod = new HttpMethod("GET");
            Guid locationId = new Guid("85023071-bd5e-4438-89b0-2a5bf362a19d");
            object routeValues = new { pipelineId = pipelineId, runId = runId };

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();
            queryParams.Add("artifactName", artifactName);
            if (expand != null)
            {
                queryParams.Add("$expand", expand.Value.ToString());
            }

            return SendAsync<Artifact>(
                httpMethod,
                locationId,
                routeValues: routeValues,
                version: new ApiResourceVersion(6.0, 1),
                queryParameters: queryParams,
                userState: userState,
                cancellationToken: cancellationToken);
        }
    }
}
