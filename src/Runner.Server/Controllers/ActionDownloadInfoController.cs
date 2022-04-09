using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
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
        private string GitServerUrl;
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

        public ActionDownloadInfoController(IConfiguration configuration) : base(configuration)
        {
            downloadUrls = configuration.GetSection("Runner.Server:ActionDownloadUrls").Get<List<ActionDownloadUrls>>();
            GitHubAppPrivateKeyFile = configuration.GetSection("Runner.Server")?.GetValue<string>("GitHubAppPrivateKeyFile") ?? "";
            GitHubAppId = configuration.GetSection("Runner.Server")?.GetValue<int>("GitHubAppId") ?? 0;
            AllowPrivateActionAccess = configuration.GetSection("Runner.Server").GetValue<bool>("AllowPrivateActionAccess");
            GITHUB_TOKEN = configuration.GetSection("Runner.Server")?.GetValue<String>("GITHUB_TOKEN") ?? "";
            GitApiServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitApiServerUrl") ?? "";
            GitServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitServerUrl") ?? "";
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
                    var appClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"), new Uri(GitServerUrl))
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
            var appClient2 = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"), new Uri(GitServerUrl))
            {
                Credentials = new Octokit.Credentials(token)
            };
            await appClient2.Connection.Delete(new Uri("installation/token", UriKind.Relative));
        }

        [HttpGet("localcheckout")]
        [AllowAnonymous]
        public IActionResult GetLocalCheckout([FromQuery] string format, [FromQuery] string version, [FromQuery] string name) {
            var assembly = Assembly.GetExecutingAssembly();
            var localcheckout_template = default(string);
            using (var stream = assembly.GetManifestResourceStream("Runner.Server.localcheckout_template.yml"))
            using (var streamReader = new StreamReader(stream))
            {
                localcheckout_template = streamReader.ReadToEnd();
            }
            localcheckout_template = localcheckout_template.Replace("_____SHA_____", BuildConstants.Source.CommitHash).Replace("_____REF_____", version).Replace("_____checkout_____", name);
            if(format == "tarball") {
                string tar = WhichUtil.Which("tar", require: true);
                var tempFolder = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
                HttpContext.Response.OnCompleted(() => {
                    System.IO.Directory.Delete(tempFolder, true);
                    return Task.CompletedTask;
                });
                var archiveDir = Path.Join(tempFolder, "archive");
                System.IO.Directory.CreateDirectory(archiveDir);
                System.IO.File.WriteAllText(Path.Join(archiveDir, "action.yml"), localcheckout_template);
                var archiveFile = Path.Join(tempFolder, "archive.tar.gz");
                var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = tar, Arguments = $"-czf \"{archiveFile}\" archive", WorkingDirectory = tempFolder });
                proc.WaitForExit();
                return new FileStreamResult(System.IO.File.OpenRead(archiveFile), "application/octet-stream");
            } else {
                var tempFolder = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
                HttpContext.Response.OnCompleted(() => {
                    System.IO.Directory.Delete(tempFolder, true);
                    return Task.CompletedTask;
                });
                var archiveDir = Path.Join(tempFolder, "archive");
                System.IO.Directory.CreateDirectory(archiveDir);
                System.IO.File.WriteAllText(Path.Join(archiveDir, "action.yml"), localcheckout_template);
                var archiveFile = Path.Join(tempFolder, "archive.zip");
                ZipFile.CreateFromDirectory(archiveDir, archiveFile, CompressionLevel.NoCompression, includeBaseDirectory: true);
                return new FileStreamResult(System.IO.File.OpenRead(archiveFile), "application/octet-stream");
            }
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        public async Task<IActionResult> Get(Guid scopeIdentifier, string hubName, Guid planId)
        {
            var localcheckout = User.FindFirst("localcheckout")?.Value ?? "";
            ActionReferenceList reflist = await FromBody<ActionReferenceList>();
            var actions = new Dictionary<string, ActionDownloadInfo>();
            foreach (var item in reflist.Actions) {
                foreach(var downloadUrl in downloadUrls) {
                    var islocalcheckout = string.Equals(item.NameWithOwner, localcheckout, StringComparison.OrdinalIgnoreCase);
                    if(islocalcheckout && (item.Ref == BuildConstants.Source.CommitHash || !item.Ref.StartsWith(BuildConstants.Source.CommitHash)) ) {
                        actions[$"{item.NameWithOwner}@{item.Ref}"] = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref, TarballUrl = new Uri(new Uri(ServerUrl), item.Ref == BuildConstants.Source.CommitHash ? "localcheckout.tar.gz" : $"_apis/v1/ActionDownloadInfo/localcheckout?format=tarball&version={Uri.EscapeDataString(item.Ref)}&name={Uri.EscapeDataString(localcheckout)}").ToString(), ZipballUrl = new Uri(new Uri(ServerUrl), item.Ref == BuildConstants.Source.CommitHash ? "localcheckout.zip"  : $"_apis/v1/ActionDownloadInfo/localcheckout?format=zipball&version={Uri.EscapeDataString(item.Ref)}&name={Uri.EscapeDataString(localcheckout)}").ToString(), ResolvedSha = GitHub.Runner.Sdk.BuildConstants.Source.CommitHash };                    
                    } else {
                        var downloadinfo = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref };
                        actions[$"{item.NameWithOwner}@{item.Ref}"] = downloadinfo;
                        // Allow access to the original action
                        if(islocalcheckout && item.NameWithOwner == localcheckout && item.Ref.StartsWith(BuildConstants.Source.CommitHash)) {
                            item.Ref = item.Ref.Substring(BuildConstants.Source.CommitHash.Length);
                        }
                        downloadinfo.TarballUrl = String.Format(downloadUrl.TarballUrl, item.NameWithOwner, item.Ref);
                        downloadinfo.ZipballUrl = String.Format(downloadUrl.ZipballUrl, item.NameWithOwner, item.Ref);
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
                            break;
                        // }
                    }
                }
            }
            return await Ok(new ActionDownloadInfoCollection() {Actions = actions });
        }        
    }
}
