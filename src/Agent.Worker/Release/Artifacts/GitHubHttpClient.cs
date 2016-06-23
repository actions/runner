using System;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Util;

using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    [ServiceLocator(Default = typeof(GitHubHttpClient))]
    public interface IGitHubHttpClient : IAgentService
    {
        GitHubRepository GetUserRepo(string accessToken, string repository);
    }

    public class GitHubHttpClient : AgentService, IGitHubHttpClient
    {
        private const string GithubRepoUrlFormat = "https://api.github.com/repos/{0}";

        public GitHubRepository GetUserRepo(string accessToken, string repositoryName)
        {
            string errorMessage;
            string url = StringUtil.Format(GithubRepoUrlFormat, repositoryName);
            GitHubRepository repository = QueryItem<GitHubRepository>(accessToken, url, out errorMessage);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                throw new InvalidOperationException(errorMessage);
            }

            return repository;
        }

        private static T QueryItem<T>(string accessToken, string url, out string errorMessage)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

            request.Headers.Add("Accept", "application/vnd.GitHubData.V3+json");
            request.Headers.Add("Authorization", "Token " + accessToken);
            request.Headers.Add("User-Agent", "VSTS-Agent/" + Constants.Agent.Version);

            using (var httpClientHandler = new HttpClientHandler())
            using (var httpClient = new HttpClient(httpClientHandler) { Timeout = new TimeSpan(0, 0, 30) })
            {
                errorMessage = string.Empty;
                Task<HttpResponseMessage> sendAsyncTask = httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                HttpResponseMessage response = sendAsyncTask.GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    errorMessage = response.StatusCode.ToString();
                    return default(T);
                }

                string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<T>(result);
            }
        }
    }

    [DataContract]
    public class GitHubRepository
    {
        [DataMember(EmitDefaultValue = false)]
        public int? Id { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Clone_url { get; set; }
   }
}