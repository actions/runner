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
        Task<List<TaskAgent>> GetAgentsAsync(int runnerGroupId, string githubUrl, string githubToken, string agentName);

        Task<TaskAgent> AddAgentAsync(int runnerGroupId, TaskAgent agent, string githubUrl, string githubToken);
        Task<List<TaskAgentPool>> GetAgentPoolsAsync(string githubUrl, string githubToken);

        string GetGitHubRequestId(HttpResponseHeaders headers);
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


        public async Task<List<TaskAgent>> GetAgentsAsync(int runnerGroupId, string githubUrl, string githubToken, string agentName = null)
        {
            var githubApiUrl = "";
            var gitHubUrlBuilder = new UriBuilder(githubUrl);
            var path = gitHubUrlBuilder.Path.Split('/', '\\', StringSplitOptions.RemoveEmptyEntries);
            if (path.Length == 1)
            {
                // org runner
                if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/orgs/{path[0]}/actions/runner-groups/{runnerGroupId}/runners";
                }
                else
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/orgs/{path[0]}/actions/runner-groups/{runnerGroupId}/runners";
                }
            }
            else if (path.Length == 2)
            {
                // repo or enterprise runner.
                if (!string.Equals(path[0], "enterprises", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/{path[0]}/{path[1]}/actions/runner-groups/{runnerGroupId}/runners";
                }
                else
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/{path[0]}/{path[1]}/actions/runner-groups/{runnerGroupId}/runners";
                }
            }
            else
            {
                throw new ArgumentException($"'{githubUrl}' should point to an org or enterprise.");
            }

            var response = await RetryRequest(githubApiUrl, githubToken, RequestType.Get, 3, "Failed to get agents pools");
            var agents = StringUtil.ConvertFromJson<ListRunnersResponse>(response);

            if (string.IsNullOrEmpty(agentName))
            {
                return agents.Runners;
            }

            return agents.Runners.Where(x => string.Equals(x.Name, agentName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<List<TaskAgentPool>> GetAgentPoolsAsync(string githubUrl, string githubToken)
        {
            var githubApiUrl = "";
            var gitHubUrlBuilder = new UriBuilder(githubUrl);
            var path = gitHubUrlBuilder.Path.Split('/', '\\', StringSplitOptions.RemoveEmptyEntries);
            if (path.Length == 1)
            {
                // org runner
                if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/orgs/{path[0]}/actions/runner-groups";
                }
                else
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/orgs/{path[0]}/actions/runner-groups";
                }
            }
            else if (path.Length == 2)
            {
                // repo or enterprise runner.
                if (!string.Equals(path[0], "enterprises", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                if (UrlUtil.IsHostedServer(gitHubUrlBuilder))
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://api.{gitHubUrlBuilder.Host}/{path[0]}/{path[1]}/actions/runner-groups";
                }
                else
                {
                    githubApiUrl = $"{gitHubUrlBuilder.Scheme}://{gitHubUrlBuilder.Host}/api/v3/{path[0]}/{path[1]}/actions/runner-groups";
                }
            }
            else
            {
                throw new ArgumentException($"'{githubUrl}' should point to an org or enterprise.");
            }

            var response = await RetryRequest(githubApiUrl, githubToken, RequestType.Get, 3, "Failed to get agents pools");
            var agentPools = StringUtil.ConvertFromJson<RunnerGroupList>(response);

            return agentPools?.ToAgentPoolList();
        }

        public async Task<TaskAgent> AddAgentAsync(int runnerGroupId, TaskAgent agent, string githubUrl, string githubToken)
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
                    };

            var body = new StringContent(StringUtil.ConvertToJson(bodyObject), null, "application/json");
            var response = await RetryRequest(githubApiUrl, githubToken, RequestType.Post, 3, "Failed to add agent", body);
            var responseAgent = StringUtil.ConvertFromJson<TaskAgent>(response);
            agent.Id = responseAgent.Id;
            return agent;
        }

        private async Task<string> RetryRequest(string githubApiUrl, string githubToken, RequestType requestType, int maxRetryAttemptsCount = 5, string errorMessage = null, StringContent body = null)
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
                        if (requestType == RequestType.Get)
                        {
                            response = await httpClient.GetAsync(githubApiUrl);
                        }
                        else
                        {
                            response = await httpClient.PostAsync(githubApiUrl, body);
                        }

                        if (response != null)
                        {
                            responseStatus = response.StatusCode;
                            var githubRequestId = GetGitHubRequestId(response.Headers);

                            if (response.IsSuccessStatusCode)
                            {
                                Trace.Info($"Http response code: {response.StatusCode} from '{requestType.ToString()} {githubApiUrl}' ({githubRequestId})");
                                var jsonResponse = await response.Content.ReadAsStringAsync();
                                return jsonResponse;
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
                        Trace.Error($"{errorMessage} -- Atempt: {retry}");
                        Trace.Error(ex);
                    }
                }
                var backOff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
                Trace.Info($"Retrying in {backOff.Seconds} seconds");
                await Task.Delay(backOff);
            }
        }

        public string GetGitHubRequestId(HttpResponseHeaders headers)
        {
            if (headers.TryGetValues("x-github-request-id", out var headerValues))
            {
                return headerValues.FirstOrDefault();
            }
            return string.Empty;
        }
    }
}