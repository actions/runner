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

using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    public class JenkinsArtifact : AgentService, IArtifactExtension
    {
        public Type ExtensionType => typeof(IArtifactExtension);
        public AgentArtifactType ArtifactType => AgentArtifactType.Jenkins;
        private const char Backslash = '\\';
        private const char ForwardSlash = '/';
        public static int CommitDataVersion = 1;
        public static string CommitIdKey = "commitId";
        public static string CommitDateKey = "date";
        public static string AuthorKey = "author";
        public static string FullNameKey = "fullName";
        public static string CommitMessageKey = "msg";

        public async Task DownloadAsync(
            IExecutionContext executionContext,
            ArtifactDefinition artifactDefinition,
            string localFolderPath)
        {
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNullOrEmpty(localFolderPath, nameof(localFolderPath));

            var jenkinsDetails = artifactDefinition.Details as JenkinsArtifactDetails;
            executionContext.Output(StringUtil.Loc("RMGotJenkinsArtifactDetails"));
            executionContext.Output(StringUtil.Loc("RMJenkinsJobName", jenkinsDetails.JobName));
            executionContext.Output(StringUtil.Loc("RMJenkinsBuildId", jenkinsDetails.BuildId));

            IGenericHttpClient client = HostContext.GetService<IGenericHttpClient>();
            if (!IsValidBuild(client, jenkinsDetails))
            {
                throw new ArtifactDownloadException(StringUtil.Loc("RMJenkinsInvalidBuild", jenkinsDetails.BuildId));
            }

            Stream downloadedStream = null;
            string downloadArtifactsUrl =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/job/{1}/{2}/artifact/{3}/*zip*/",
                        jenkinsDetails.Url,
                        jenkinsDetails.JobName,
                        jenkinsDetails.BuildId,
                        jenkinsDetails.RelativePath);

            executionContext.Output(StringUtil.Loc("RMPrepareToGetFromJenkinsServer"));
            HttpResponseMessage response = client.GetAsync(downloadArtifactsUrl, jenkinsDetails.AccountName, jenkinsDetails.AccountPassword, jenkinsDetails.AcceptUntrustedCertificates).Result;

            if (response.IsSuccessStatusCode)
            {
                downloadedStream = response.Content.ReadAsStreamAsync().Result;
            }
            else if (response.StatusCode == HttpStatusCode.NotFound)
            {
                executionContext.Warning(StringUtil.Loc("RMJenkinsNoArtifactsFound", jenkinsDetails.BuildId));
                return;
            }
            else
            {
                throw new ArtifactDownloadException(StringUtil.Loc("RMDownloadArtifactUnexpectedError"));
            }

            var parentFolder = GetParentFolderName(jenkinsDetails.RelativePath);
            Trace.Info($"Found parentFolder {parentFolder} for relative path {jenkinsDetails.RelativePath}");

            executionContext.Output(StringUtil.Loc("RMDownloadingJenkinsArtifacts"));
            var zipStreamDownloader = HostContext.GetService<IZipStreamDownloader>();
            await zipStreamDownloader.DownloadFromStream(
                executionContext,
                downloadedStream,
                string.IsNullOrEmpty(parentFolder) ? "archive" : string.Empty,
                parentFolder,
                localFolderPath);
        }

        public async Task DownloadCommitsAsync(IExecutionContext context, ArtifactDefinition artifactDefinition, string commitsWorkFolder)
        {
            Trace.Entering();

            var jenkinsDetails = artifactDefinition.Details as JenkinsArtifactDetails;
            int startJobId = 0, endJobId = 0;

            if (!string.IsNullOrEmpty(jenkinsDetails.EndCommitArtifactVersion))
            {
                if (int.TryParse(jenkinsDetails.EndCommitArtifactVersion, out endJobId))
                {
                    if (int.TryParse(jenkinsDetails.StartCommitArtifactVersion, out startJobId))
                    {
                        if (startJobId < endJobId)
                        {
                            context.Output(StringUtil.Loc("DownloadingJenkinsCommitsBetween", startJobId, endJobId));
                        }
                        else if (startJobId > endJobId)
                        {
                            context.Output(StringUtil.Loc("JenkinsRollbackDeployment"));

                            // we do not support fetching the commits for the rollback deployment fully yet.
                            // until then we will return from here.
                            return;
                        }
                        else if (startJobId == endJobId)
                        {
                            context.Output(StringUtil.Loc("JenkinsNoCommitsToFetch"));
                            return;
                        }
                    }
                    else 
                    {
                        context.Debug(StringUtil.Loc("JenkinsDownloadingChangeFromCurrentBuild"));
                    }

                    try 
                    {
                        IEnumerable<Change> changes = await DownloadCommits(context, jenkinsDetails, startJobId, endJobId);

                        if (changes.Any())
                        {
                            string commitsFileName = GetCommitsFileName(jenkinsDetails.Alias);
                            string commitsFilePath = Path.Combine(commitsWorkFolder, commitsFileName);

                            context.Debug($"Commits will be written to {commitsFilePath}");
                            WriteCommitsToFile(context, changes, commitsFilePath);
                            context.Debug($"Commits written to {commitsFilePath}");

                            context.QueueAttachFile(CoreAttachmentType.FileAttachment, commitsFileName, commitsFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        context.AddIssue(new Issue { Type=IssueType.Warning, Message = StringUtil.Loc("DownloadingJenkinsCommitsFailedWithException", jenkinsDetails.Alias, ex.ToString()) });
                        return;
                    }
                }
                else 
                {
                    context.AddIssue(new Issue { Type=IssueType.Warning, Message = StringUtil.Loc("JenkinsCommitsInvalidEndJobId", jenkinsDetails.EndCommitArtifactVersion, jenkinsDetails.Alias) });
                    return;
                }
            }
            else 
            {
                context.Debug("No commit details found in the agent artifact. Not downloading the commits");
            }
        }

        private string GetCommitsFileName(string artifactAlias)
        {
            return StringUtil.Format("commits_{0}_v{1}.json", artifactAlias, CommitDataVersion);
        }

        private void WriteCommitsToFile(IExecutionContext context, IEnumerable<Change> commits, string commitsFilePath)
        {
            IOUtil.DeleteFile(commitsFilePath);
            if (commits.Any())
            {
                using(StreamWriter sw = File.CreateText(commitsFilePath))
                using(JsonTextWriter jw = new JsonTextWriter(sw))
                {
                    jw.Formatting = Formatting.Indented;
                    jw.WriteStartArray();
                    foreach (Change commit in commits)
                    {
                        JObject.FromObject(commit).WriteTo(jw);
                    }
                    jw.WriteEnd();
                }
            }
        }

        private Change ConvertCommitToChange(IExecutionContext context, JToken token)
        {
            Trace.Entering();

            // Use mustache parser?
            Change change = new Change();
            var resultDictionary = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(token.ToString());
            if (resultDictionary.ContainsKey(CommitIdKey))
            {
                change.Id = resultDictionary[CommitIdKey].ToString();
            }

            if (resultDictionary.ContainsKey(CommitMessageKey))
            {
                change.Message = resultDictionary[CommitMessageKey].ToString();
            }

            if (resultDictionary.ContainsKey(AuthorKey))
            {
                string authorDetail = resultDictionary[AuthorKey].ToString();
                var author = JsonConvert.DeserializeObject<Dictionary<string, string>>(authorDetail);
                change.Author = new IdentityRef { DisplayName = author[FullNameKey] };
            }

            if (resultDictionary.ContainsKey(CommitDateKey))
            {
                DateTime value;
                if (DateTime.TryParse(resultDictionary[CommitDateKey].ToString(), out value))
                {
                    change.Timestamp = value;
                }
            }

            context.Debug(StringUtil.Format("Found commit {0}", change.Id));
            return change;
        }

        private Tuple<int, int> GetCommitJobIdIndex(IExecutionContext context, JenkinsArtifactDetails artifactDetails, int startJobId, int endJobId)
        {
            Trace.Entering();

            string url = StringUtil.Format("{0}/job/{1}/api/json?tree=allBuilds[number]", artifactDetails.Url, artifactDetails.JobName);
            int startIndex = -1, endIndex = -1, index = 0;
            var listOfBuildResult = DownloadCommitsJsonContent(context, url, artifactDetails, "$.allBuilds[*].number").Result;
            foreach (JToken token in listOfBuildResult)
            {
                long value = 0;
                if (long.TryParse((string)token, out value)) 
                {
                    if (value == startJobId)
                    {
                        startIndex = index;
                    }

                    if (value == endJobId)
                    {
                        endIndex = index;
                    }

                    if (startIndex > 0 && endIndex > 0)
                    {
                        break;
                    }

                    index++;
                }
            }

            context.Debug(StringUtil.Format("Found startIndex {0} and endIndex {1}", startIndex, endIndex));
            if (startIndex < 0 || endIndex < 0)
            {
                throw new CommitsDownloadException(StringUtil.Loc("JenkinsBuildDoesNotExistsForCommits", startJobId, endJobId, startIndex, endIndex));
            }

            return Tuple.Create<int, int>(startIndex, endIndex);
        }

        private async Task<IEnumerable<Change>> DownloadCommits(IExecutionContext context, JenkinsArtifactDetails artifactDetails, int jobId)
        {
            context.Output(StringUtil.Format("Getting changeSet associated with build {0} ", jobId));
            string commitsUrl = StringUtil.Format("{0}/job/{1}/{2}/api/json?tree=number,result,changeSet[items[commitId,date,msg,author[fullName]]]", artifactDetails.Url, artifactDetails.JobName, jobId);
            var commitsResult = await DownloadCommitsJsonContent(context, commitsUrl, artifactDetails, "$.changeSet.items[*]");
            return commitsResult.Select(x => ConvertCommitToChange(context, x));
        }

        private async Task<IEnumerable<Change>> DownloadCommits(IExecutionContext context, JenkinsArtifactDetails artifactDetails, int startJobId, int endJobId)
        {
            Trace.Entering();

            if (startJobId == 0)
            {
                context.Debug($"StartJobId does not exist, downloading changeSet from build {endJobId}");
                return await DownloadCommits(context, artifactDetails, endJobId);
            }

            //#1. Figure out the index of build numbers
            Tuple<int, int> result = GetCommitJobIdIndex(context, artifactDetails, startJobId, endJobId);
            int startIndex = result.Item1;
            int endIndex = result.Item2;

            //#2. Download the commits using range
            string buildParameter = (startIndex >= 100 || endIndex >= 100) ? "allBuilds" : "builds"; // jenkins by default will return only 100 top builds. Have to use "allBuilds" if we are dealing with build which are older than 100 builds
            string commitsUrl = StringUtil.Format("{0}/job/{1}/api/json?tree={2}[number,result,changeSet[items[commitId,date,msg,author[fullName]]]]{{{3},{4}}}", artifactDetails.Url, artifactDetails.JobName, buildParameter, endIndex, startIndex);
            var changeSetResult = await DownloadCommitsJsonContent(context, commitsUrl, artifactDetails, StringUtil.Format("$.{0}[*].changeSet.items[*]", buildParameter));
            return changeSetResult.Select(x => ConvertCommitToChange(context, x));
        }

        private async Task<IEnumerable<JToken>> DownloadCommitsJsonContent(IExecutionContext executionContext, string url, JenkinsArtifactDetails artifactDetails, string jsonPath) 
        {
            Trace.Entering();

            executionContext.Debug($"Querying Jenkins server with the api {url} and the results will be filtered with {jsonPath}");
            string result = await HostContext.GetService<IGenericHttpClient>()
                                            .GetStringAsync(url, artifactDetails.AccountName, artifactDetails.AccountPassword, artifactDetails.AcceptUntrustedCertificates);
            if (!string.IsNullOrEmpty(result))
            {
                executionContext.Debug($"Found result from Jenkins server: {result}");
                JObject parsedJson = JObject.Parse(result);
                return parsedJson.SelectTokens(jsonPath);
            }

            return new List<JToken>();
        }

        public IArtifactDetails GetArtifactDetails(IExecutionContext context, AgentArtifactDefinition agentArtifactDefinition)
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

            bool acceptUntrusted = jenkinsEndpoint.Data != null &&
                                   jenkinsEndpoint.Data.ContainsKey("acceptUntrustedCerts") &&
                                   StringUtil.ConvertToBoolean(jenkinsEndpoint.Data["acceptUntrustedCerts"]);

            string startCommitArtifactVersion = string.Empty;
            string endCommitArtifactVersion = string.Empty;

            artifactDetails.TryGetValue("StartCommitArtifactVersion", out startCommitArtifactVersion);
            artifactDetails.TryGetValue("EndCommitArtifactVersion", out endCommitArtifactVersion);

            if (allFieldsPresents)
            {
                return new JenkinsArtifactDetails
                {
                    RelativePath = relativePath,
                    AccountName = jenkinsEndpoint.Authorization.Parameters[EndpointAuthorizationParameters.Username],
                    AccountPassword = jenkinsEndpoint.Authorization.Parameters[EndpointAuthorizationParameters.Password],
                    BuildId = Convert.ToInt32(agentArtifactDefinition.Version, CultureInfo.InvariantCulture),
                    JobName = jobName,
                    Url = jenkinsEndpoint.Url,
                    AcceptUntrustedCertificates = acceptUntrusted,
                    StartCommitArtifactVersion = startCommitArtifactVersion,
                    EndCommitArtifactVersion = endCommitArtifactVersion,
                    Alias = agentArtifactDefinition.Alias
                };
            }
            else
            {
                throw new InvalidOperationException(StringUtil.Loc("RMArtifactDetailsIncomplete"));
            }
        }

        private bool IsValidBuild(IGenericHttpClient client, JenkinsArtifactDetails jenkinsDetails)
        {
            var buildUrl =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/job/{1}/{2}",
                        jenkinsDetails.Url,
                        jenkinsDetails.JobName,
                        jenkinsDetails.BuildId);

            HttpResponseMessage response = client.GetAsync(buildUrl, jenkinsDetails.AccountName, jenkinsDetails.AccountPassword, jenkinsDetails.AcceptUntrustedCertificates).Result;
            return response.IsSuccessStatusCode;
        }

        private static string GetParentFolderName(string relativePath)
        {
            // Sometime the Jenkins artifact relative path would be simply / indicating read from root. This will retrun empty string at such scenarios.
            return relativePath.TrimEnd(Backslash).TrimEnd(ForwardSlash).Replace(Backslash, ForwardSlash).Split(ForwardSlash).Last();
        }
    }
}