using GitHub.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.WebApi;
using GitHub.Services.Common;
using GitHub.Runner.Sdk;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(RunnerDotcomServer))]
    public interface IRunnerDotcomServer : IRunnerService
    {
        Task<List<TaskAgent>> GetRunnerByNameAsync(string githubUrl, string githubToken, string agentName);

        Task<DistributedTask.WebApi.Runner> AddRunnerAsync(int runnerGroupId, TaskAgent agent, string githubUrl, string githubToken, string publicKey);
        Task<DistributedTask.WebApi.Runner> ReplaceRunnerAsync(int runnerGroupId, TaskAgent agent, string githubUrl, string githubToken, string publicKey);
        Task DeleteRunnerAsync(string githubUrl, string githubToken, ulong runnerId);
        Task<List<TaskAgentPool>> GetRunnerGroupsAsync(string githubUrl, string githubToken);
    }

    public enum RequestType
    {
        Get,
        Post,
        Patch,
        Delete
    }

    public class RunnerDotcomServer : RunnerService, IRunnerDotcomServer
    {
        private ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
        }


        public async Task<List<TaskAgent>> GetRunnerByNameAsync(string githubUrl, string githubToken, string agentName)
        {
            var githubApiUrl = $"{GetEntityUrl(githubUrl)}/runners?name={Uri.EscapeDataString(agentName)}";
            var runnersList = await RetryRequest<ListRunnersResponse>(githubApiUrl, githubToken, RequestType.Get, 3, "Failed to get agents pools");
            return runnersList.ToTaskAgents();
        }

        public async Task<List<TaskAgentPool>> GetRunnerGroupsAsync(string githubUrl, string githubToken)
        {
            var githubApiUrl = $"{GetEntityUrl(githubUrl)}/runner-groups";
            var agentPools = await RetryRequest<RunnerGroupList>(githubApiUrl, githubToken, RequestType.Get, 3, "Failed to get agents pools");
            return agentPools?.ToAgentPoolList();
        }

        public async Task<DistributedTask.WebApi.Runner> AddRunnerAsync(int runnerGroupId, TaskAgent agent, string githubUrl, string githubToken, string publicKey)
        {
            return await AddOrReplaceRunner(runnerGroupId, agent, githubUrl, githubToken, publicKey, false);
        }

        public async Task<DistributedTask.WebApi.Runner> ReplaceRunnerAsync(int runnerGroupId, TaskAgent agent, string githubUrl, string githubToken, string publicKey)
        {
            return await AddOrReplaceRunner(runnerGroupId, agent, githubUrl, githubToken, publicKey, true);
        }

        private async Task<DistributedTask.WebApi.Runner> AddOrReplaceRunner(int runnerGroupId, TaskAgent agent, string githubUrl, string githubToken, string publicKey, bool replace)
        {
            var gitHubUrlBuilder = new UriBuilder(githubUrl);
            var path = gitHubUrlBuilder.Path.Split('/', '\\', StringSplitOptions.RemoveEmptyEntries);
            string githubApiUrl;
            if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
            {
                githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/actions/runners/register";
            }
            else
            {
                githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/actions/runners/register";
            }

            var bodyObject = new Dictionary<string, Object>()
                    {
                        {"url", githubUrl},
                        {"group_id", runnerGroupId},
                        {"name", agent.Name},
                        {"version", agent.Version},
                        {"updates_disabled", agent.DisableUpdate},
                        {"ephemeral", agent.Ephemeral},
                        {"labels", agent.Labels},
                        {"public_key", publicKey},
                    };

            if (replace)
            {
                bodyObject.Add("runner_id", agent.Id);
                bodyObject.Add("replace", replace);
            }

            var body = new StringContent(StringUtil.ConvertToJson(bodyObject), null, "application/json");

            return await RetryRequest<DistributedTask.WebApi.Runner>(githubApiUrl, githubToken, RequestType.Post, 3, "Failed to add agent", body);
        }

        public async Task DeleteRunnerAsync(string githubUrl, string githubToken, ulong runnerId)
        {
            var githubApiUrl = $"{GetEntityUrl(githubUrl)}/runners/{runnerId}";
            await RetryRequest<DistributedTask.WebApi.Runner>(githubApiUrl, githubToken, RequestType.Delete, 3, "Failed to delete agent");
        }

        private async Task<T> RetryRequest<T>(string githubApiUrl, string githubToken, RequestType requestType, int maxRetryAttemptsCount = 5, string errorMessage = null, StringContent body = null)
        {
            int retry = 0;
            while (true)
            {
                retry++;
                using (var httpClientHandler = HostContext.CreateHttpClientHandler())
                using (var httpClient = new HttpClient(httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("RemoteAuth", githubToken);
                    httpClient.DefaultRequestHeaders.UserAgent.AddRange(HostContext.UserAgents);

                    var responseStatus = System.Net.HttpStatusCode.OK;
                    try
                    {
                        HttpResponseMessage response = null;
                        switch (requestType)
                        {
                            case RequestType.Get:
                                response = await httpClient.GetAsync(githubApiUrl);
                                break;
                            case RequestType.Post:
                                response = await httpClient.PostAsync(githubApiUrl, body);
                                break;
                            case RequestType.Patch:
                                response = await httpClient.PatchAsync(githubApiUrl, body);
                                break;
                            case RequestType.Delete:
                                response = await httpClient.DeleteAsync(githubApiUrl);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null);
                        }

                        if (response != null)
                        {
                            responseStatus = response.StatusCode;
                            var githubRequestId = UrlUtil.GetGitHubRequestId(response.Headers);

                            if (response.IsSuccessStatusCode)
                            {
                                Trace.Info($"Http response code: {response.StatusCode} from '{requestType.ToString()} {githubApiUrl}' ({githubRequestId})");
                                var jsonResponse = await response.Content.ReadAsStringAsync();
                                return StringUtil.ConvertFromJson<T>(jsonResponse);
                            }
                            else
                            {
                                _term.WriteError($"Http response code: {response.StatusCode} from '{requestType.ToString()} {githubApiUrl}' (Request Id: {githubRequestId})");
                                var errorResponse = await response.Content.ReadAsStringAsync();
                                _term.WriteError(errorResponse);
                                response.EnsureSuccessStatusCode();
                            }
                        }

                    }
                    catch (Exception ex) when (retry < maxRetryAttemptsCount && responseStatus != System.Net.HttpStatusCode.NotFound)
                    {
                        Trace.Error($"{errorMessage} -- Attempt: {retry}");
                        Trace.Error(ex);
                    }
                }
                var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
                Trace.Info($"Retrying in {backOff.Seconds} seconds");
                await Task.Delay(backOff);
            }
        }

        private string GetEntityUrl(string githubUrl)
        {
            var githubApiUrl = "";
            var gitHubUrlBuilder = new UriBuilder(githubUrl);
            var path = gitHubUrlBuilder.Path.Split('/', '\\', StringSplitOptions.RemoveEmptyEntries);
            var isOrgRunner = path.Length == 1;
            var isRepoOrEnterpriseRunner = path.Length == 2;
            var isRepoRunner = isRepoOrEnterpriseRunner && !string.Equals(path[0], "enterprises", StringComparison.OrdinalIgnoreCase);

            if (isOrgRunner)
            {
                // org runner
                if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/orgs/{path[0]}/actions";
                }
                else
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/orgs/{path[0]}/actions";
                }
            }
            else if (isRepoOrEnterpriseRunner)
            {
                // Repository Runner
                if (isRepoRunner)
                {
                    if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
                    {
                        githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/repos/{path[0]}/{path[1]}/actions";
                    }
                    else
                    {
                        githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/repos/{path[0]}/{path[1]}/actions";
                    }
                }
                else
                {
                    // Enterprise Runner
                    if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
                    {
                        githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/{path[0]}/{path[1]}/actions";
                    }
                    else
                    {
                        githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/{path[0]}/{path[1]}/actions";
                    }
                }
            }
            else
            {
                throw new ArgumentException($"'{githubUrl}' should point to an org or enterprise.");
            }

            return githubApiUrl;
        }
    }
}
