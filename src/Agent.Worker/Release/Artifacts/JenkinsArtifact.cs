using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Agent.Worker.Release.Artifacts.Definition;

using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Release;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Agent.Worker.Release.Artifacts
{
    // TODO: Implement serviceLocator pattern to have custom attribute as we have different type of artifacts
    [ServiceLocator(Default = typeof(JenkinsArtifact))]
    public interface IJenkinsArtifact : IAgentService
    {
        Task Download(
            ArtifactDefinition artifactDefinition,
            IExecutionContext executionContext,
            string localFolderPath);

        IArtifactDetails GetArtifactDetails(
            AgentArtifactDefinition agentArtifactDefinition,
            IExecutionContext context);
    }

    public class JenkinsArtifact : AgentService, IJenkinsArtifact
    {
        private const char Backslash = '\\';
        private const char ForwardSlash = '/';

        public async Task Download(
            ArtifactDefinition artifactDefinition,
            IExecutionContext executionContext,
            string localFolderPath)
        {
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNullOrEmpty(localFolderPath, nameof(localFolderPath));

            var jenkinsDetails = artifactDefinition.Details as JenkinsArtifactDetails;
            executionContext.Output(StringUtil.Loc("RMGotJenkinsArtifactDetails"));
            executionContext.Output(StringUtil.Loc("RMJenkinsJobName", jenkinsDetails.JobName));
            executionContext.Output(StringUtil.Loc("RMJenkinsBuildId", jenkinsDetails.BuildId));

            Stream downloadedStream = null;
            using (HttpClient client = new HttpClient())
            {
                SetupHttpClient(client, jenkinsDetails.AccountName, jenkinsDetails.AccountPassword);
                var downloadArtifactsUrl =
                    new Uri(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}/job/{1}/{2}/artifact/{3}/*zip*/",
                            jenkinsDetails.Url,
                            jenkinsDetails.JobName,
                            jenkinsDetails.BuildId,
                            jenkinsDetails.RelativePath));

                executionContext.Output(StringUtil.Loc("RMPrepareToGetFromJenkinsServer"));
                HttpResponseMessage response = client.GetAsync(downloadArtifactsUrl).Result;

                if (response.IsSuccessStatusCode)
                {
                    downloadedStream = response.Content.ReadAsStreamAsync().Result;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ArtifactDownloadException(StringUtil.Loc("RMNoArtifactsFound", jenkinsDetails.RelativePath));
                }
                else
                {
                    throw new ArtifactDownloadException(StringUtil.Loc("RMDownloadArtifactUnexpectedError"));
                }
            }

            var parentFolder = GetParentFolderName(jenkinsDetails.RelativePath);

            executionContext.Output(StringUtil.Loc("RMDownloadingJenkinsArtifacts"));
            var zipStreamDownloader = HostContext.GetService<IZipStreamDownloader>();
            await zipStreamDownloader.DownloadFromStream(
                downloadedStream,
                string.IsNullOrEmpty(parentFolder) ? "archive" : string.Empty,
                parentFolder,
                localFolderPath);
        }

        public IArtifactDetails GetArtifactDetails(AgentArtifactDefinition agentArtifactDefinition, IExecutionContext context)
        {
            Trace.Entering();

            var artifactDetails =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(agentArtifactDefinition.Details);

            ServiceEndpoint jenkinsEndpoint = context.Endpoints.FirstOrDefault(e => string.Equals(e.Name, artifactDetails["ConnectionName"], StringComparison.OrdinalIgnoreCase));
            if (jenkinsEndpoint == null)
            {
                throw new InvalidOperationException(StringUtil.Loc("RMJenkinsEndpointNotFound", agentArtifactDefinition.Name));
            }

            string relativePath;
            var jobName = string.Empty;

            var allFieldsPresents = artifactDetails.TryGetValue("RelativePath", out relativePath)
                                    && artifactDetails.TryGetValue("JobName", out jobName);
            if (allFieldsPresents)
            {
                return new JenkinsArtifactDetails
                {
                    RelativePath = relativePath,
                    AccountName = jenkinsEndpoint.Authorization.Parameters[EndpointAuthorizationParameters.Username],
                    AccountPassword = jenkinsEndpoint.Authorization.Parameters[EndpointAuthorizationParameters.Password],
                    BuildId = Convert.ToInt32(agentArtifactDefinition.Version, CultureInfo.InvariantCulture),
                    JobName = jobName,
                    Url = jenkinsEndpoint.Url
                };
            }
            else
            {
                throw new InvalidOperationException(StringUtil.Loc("RMArtifactDetailsIncomplete"));
            }
        }

        private static void SetupHttpClient(HttpClient httpClient, string userName, string password)
        {
            httpClient.Timeout = Timeout.InfiniteTimeSpan;
            httpClient.DefaultRequestHeaders.Authorization = CreateBasicAuthenticationHeader(userName, password);
        }

        private static AuthenticationHeaderValue CreateBasicAuthenticationHeader(string username, string password)
        {
            var authenticationHeader = string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}",
                username ?? string.Empty,
                password ?? string.Empty);

            return new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationHeader)));
        }

        private static string GetParentFolderName(string relativePath)
        {
            return relativePath.TrimEnd(Backslash).TrimEnd(ForwardSlash).Replace(Backslash, ForwardSlash).Split(ForwardSlash).Last();
        }
    }
}