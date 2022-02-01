using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
    public class ActionDownloadInfoController : VssControllerBase
    {
        private string GitHubAppPrivateKeyFile { get; }
        private int GitHubAppId { get; }
        private bool AllowPrivateActionAccess { get; }
        private string GITHUB_TOKEN;
        private string GitApiServerUrl;
        private List<ActionDownloadUrls> downloadUrls;
        private class ActionDownloadUrls
        {
            public string TarbalUrl { get => TarballUrl; set => TarballUrl = value; }
            public string ZipbalUrl { get => ZipballUrl; set => ZipballUrl = value; }
            public string TarballUrl { get; set; }
            public string ZipballUrl { get; set; }
        }

        public ActionDownloadInfoController(IConfiguration configuration)
        {
            downloadUrls = configuration.GetSection("Runner.Server:ActionDownloadUrls").Get<List<ActionDownloadUrls>>();
            GitHubAppPrivateKeyFile = configuration.GetSection("Runner.Server")?.GetValue<string>("GitHubAppPrivateKeyFile") ?? "";
            GitHubAppId = configuration.GetSection("Runner.Server")?.GetValue<int>("GitHubAppId") ?? 0;
            AllowPrivateActionAccess = configuration.GetSection("Runner.Server").GetValue<bool>("AllowPrivateActionAccess");
            GITHUB_TOKEN = configuration.GetSection("Runner.Server")?.GetValue<String>("GITHUB_TOKEN") ?? "";
            GitApiServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitApiServerUrl") ?? "";
            ReadConfig(configuration);
        }

        private async Task<string> CreateGithubAppToken(string repository_name) {
            if(!string.IsNullOrEmpty(GitHubAppPrivateKeyFile) && GitHubAppId != 0) {
                try {
                    var ownerAndRepo = repository_name.Split("/", 2);
                    // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
                    var generator = new GitHubJwt.GitHubJwtFactory(
                        new GitHubJwt.FilePrivateKeySource(GitHubAppPrivateKeyFile),
                        new GitHubJwt.GitHubJwtFactoryOptions
                        {
                            AppIntegrationId = GitHubAppId, // The GitHub App Id
                            ExpirationSeconds = 500 // 10 minutes is the maximum time allowed
                        }
                    );
                    var jwtToken = generator.CreateEncodedJwtToken();
                    // Pass the JWT as a Bearer token to Octokit.net
                    var appClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"))
                    {
                        Credentials = new Octokit.Credentials(jwtToken, Octokit.AuthenticationType.Bearer)
                    };
                    var installation = await appClient.GitHubApps.GetRepositoryInstallationForCurrent(ownerAndRepo[0], ownerAndRepo[1]);
                    var response = await appClient.Connection.Post<Octokit.AccessToken>(Octokit.ApiUrls.AccessTokens(installation.Id), new { Permissions = new { metadata = "read", contents = "read" } }, Octokit.AcceptHeaders.GitHubAppsPreview, Octokit.AcceptHeaders.GitHubAppsPreview);
                    return response.Body.Token;
                } catch {

                }
            }
            return null;
        }

        private async Task DeleteGithubAppToken(string token) {
            var appClient2 = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"))
            {
                Credentials = new Octokit.Credentials(token)
            };
            await appClient2.Connection.Delete(new Uri("installation/token", UriKind.Relative));
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        public async Task<IActionResult> Get(Guid scopeIdentifier, string hubName, Guid planId)
        {
            ActionReferenceList reflist = await FromBody<ActionReferenceList>();
            var actions = new Dictionary<string, ActionDownloadInfo>();
            foreach (var item in reflist.Actions) {
                foreach(var downloadUrl in downloadUrls) {
                    if(item.NameWithOwner == "localcheckout") {
                        actions[$"{item.NameWithOwner}@{item.Ref}"] = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref, TarballUrl = $"{ServerUrl}/localcheckout.tar.gz", ZipballUrl = $"{ServerUrl}/localcheckout.zip", ResolvedSha = GitHub.Runner.Sdk.BuildConstants.Source.CommitHash };
                    } else {
                        var downloadinfo = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref, TarballUrl = String.Format(downloadUrl.TarballUrl, item.NameWithOwner, item.Ref), ZipballUrl = String.Format(downloadUrl.ZipballUrl, item.NameWithOwner, item.Ref) };
                        // TODO: How to check on github if url is valid?, maybe use GITHUB_TOKEN?
                        // var client = new HttpClient();
                        // if((await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, downloadinfo.TarballUrl))).IsSuccessStatusCode && (await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, downloadinfo.ZipballUrl))).IsSuccessStatusCode) {
                            var repository = User.FindFirst("repository")?.Value;
                            string ghtoken = null;
                            if(AllowPrivateActionAccess && repository != null && item.NameWithOwner != repository) {
                                ghtoken = await CreateGithubAppToken(item.NameWithOwner);
                                if(!string.IsNullOrEmpty(ghtoken)) {
                                    downloadinfo.Authentication = new ActionDownloadAuthentication() { Token = ghtoken };
                                }
                            }
                            if(ghtoken == null) {
                                ghtoken = await CreateGithubAppToken(repository) ?? GITHUB_TOKEN;
                            }
                            var client = new HttpClient();
                            client.DefaultRequestHeaders.Add("accept", "application/json");
                            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("runner", string.IsNullOrEmpty(GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version) ? "0.0.0" : GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version));
                            if(!string.IsNullOrEmpty(ghtoken)) {
                                client.DefaultRequestHeaders.Add("Authorization", $"token {ghtoken}");
                            }
                            var urlBuilder = new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{item.NameWithOwner}/commits"));
                            urlBuilder.Query = $"?sha={Uri.EscapeDataString(item.Ref)}&page=1&limit=1&per_page=1";
                            var res = await client.GetAsync(urlBuilder.ToString());
                            if(res.StatusCode == System.Net.HttpStatusCode.OK) {
                                var content = await res.Content.ReadAsStringAsync();
                                var o = JsonConvert.DeserializeObject<MessageController.GitCommit[]>(content)[0];
                                if(!string.IsNullOrEmpty(o.Sha)) {
                                    downloadinfo.ResolvedSha = o.Sha;
                                    downloadinfo.TarballUrl = String.Format(downloadUrl.TarballUrl, item.NameWithOwner, o.Sha);
                                    downloadinfo.ZipballUrl = String.Format(downloadUrl.ZipballUrl, item.NameWithOwner, o.Sha);
                                }
                            }
                            actions[$"{item.NameWithOwner}@{item.Ref}"] = downloadinfo;
                            break;
                        // }
                    }
                }
            }
            return await Ok(new ActionDownloadInfoCollection() {Actions = actions });
        }        
    }
}
