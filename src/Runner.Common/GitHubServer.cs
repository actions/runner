using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;

namespace GitHub.Runner.Common
{

    public class GitHubResult
    {
        public HttpStatusCode StatusCode { get; set; }
        public String Message { get; set; }
        public HttpResponseHeaders Headers { get; set; }
    }


    [ServiceLocator(Default = typeof(GitHubServer))]
    public interface IGitHubServer : IRunnerService
    {
        Task<GitHubResult> RevokeInstallationToken(string GithubApiUrl, string AccessToken);
    }

    public class GitHubServer : RunnerService, IGitHubServer
    {
        public async Task<GitHubResult> RevokeInstallationToken(string GithubApiUrl, string AccessToken)
        {
            var result = new GitHubResult();
            var requestUrl = new UriBuilder(GithubApiUrl);
            requestUrl.Path = requestUrl.Path.TrimEnd('/') + "/installation/token";

            using (var httpClientHandler = HostContext.CreateHttpClientHandler())
            using (var httpClient = HttpClientFactory.Create(httpClientHandler, new VssHttpRetryMessageHandler(3)))
            {
                httpClient.DefaultRequestHeaders.UserAgent.Add(HostContext.UserAgent);
                var base64EncodingToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"x-access-token:{AccessToken}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodingToken);
                var count = 1;
                while (true)
                {
                    try
                    {
                        using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUrl.Uri))
                        {
                            requestMessage.Headers.Add("Accept", "application/vnd.github.gambit-preview+json");
                            var response = await httpClient.SendAsync(requestMessage, CancellationToken.None);
                            result.StatusCode = response.StatusCode;
                            result.Headers = response.Headers;
                            result.Message = await response.Content.ReadAsStringAsync();
                            return result;
                        }
                    }
                    catch (Exception ex) when (count++ < 3)
                    {
                        Trace.Error("Fail to revoke GITHUB_TOKEN, will try again later");
                        Trace.Error(ex);
                        var backoff = BackoffTimerHelper.GetRandomBackoff(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
                        await Task.Delay(backoff);
                    }
                }
            }
        }
    }
}
