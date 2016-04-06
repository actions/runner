using System;
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

using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Release;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;

namespace Microsoft.VisualStudio.Agent.Worker.Release.Artifacts
{
    public class JenkinsArtifact : IArtifact
    {
        private const char Backslash = '\\';
        private const char ForwardSlash = '/';

        public async Task Download(
            ArtifactDefinition artifactDefinition,
            IHostContext hostContext,
            IExecutionContext executionContext,
            string localFolderPath)
        {
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNull(hostContext, nameof(hostContext));
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
            var zipStreamDownloader = hostContext.GetService<IZipStreamDownloader>();
            await zipStreamDownloader.DownloadFromStream(
                downloadedStream,
                string.IsNullOrEmpty(parentFolder) ? "archive" : string.Empty,
                parentFolder,
                localFolderPath);
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