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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
    public class ActionDownloadInfoController : GitHubAppIntegrationBase
    {
        private string GitServerUrl;
        private bool AllowPrivateActionAccess { get; }
        private string GITHUB_TOKEN;
        private string GitApiServerUrl;
        private IMemoryCache _cache;
        private List<ActionDownloadUrls> downloadUrls;
        private class ActionDownloadUrls
        {
            public string TarbalUrl { get => TarballUrl; set => TarballUrl = value; }
            public string ZipbalUrl { get => ZipballUrl; set => ZipballUrl = value; }
            public string TarballUrl { get; set; }
            public string ZipballUrl { get; set; }
            public string GitApiServerUrl { get; set; }
            public string GITHUB_TOKEN { get; set; }
            public bool ReturnWithoutResolvingSha { get; set; }
        }

        public ActionDownloadInfoController(IConfiguration configuration, IMemoryCache memoryCache) : base(configuration)
        {
            this._cache = memoryCache;
            downloadUrls = configuration.GetSection("Runner.Server:ActionDownloadUrls").Get<List<ActionDownloadUrls>>();
            AllowPrivateActionAccess = configuration.GetSection("Runner.Server").GetValue<bool>("AllowPrivateActionAccess");
            GITHUB_TOKEN = configuration.GetSection("Runner.Server")?.GetValue<String>("GITHUB_TOKEN") ?? "";
            if(string.IsNullOrEmpty(GITHUB_TOKEN)) {
                GITHUB_TOKEN = configuration.GetSection("Runner.Server")?.GetValue<String>("GITHUB_TOKEN_READ_ONLY") ?? "";
            }
            GitApiServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitApiServerUrl") ?? "";
            GitServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitServerUrl") ?? "";
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
        public async Task<IActionResult> Get(Guid scopeIdentifier, string hubName, Guid planId, [FromBody, Vss] ActionReferenceList reflist)
        {
            var localcheckout = User.FindFirst("localcheckout")?.Value ?? "";
            var runid = User.FindFirst("runid")?.Value ?? "";
            var token = User.FindFirst("github_token")?.Value ?? "";
            if(!string.IsNullOrEmpty(token)) {
                GITHUB_TOKEN = token;
            }
            var repository = User.FindFirst("repository")?.Value;
            var defGhToken = await CreateGithubAppToken(repository);
            try {
                var actions = new Dictionary<string, ActionDownloadInfo>();
                foreach (var item in reflist.Actions) {
                    var islocalcheckout = string.Equals(item.NameWithOwner, localcheckout, StringComparison.OrdinalIgnoreCase);
                    var name = $"{item.NameWithOwner}@{item.Ref}";
                    if(islocalcheckout && (item.Ref == BuildConstants.Source.CommitHash || !item.Ref.StartsWith(BuildConstants.Source.CommitHash)) ) {
                        actions[name] = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref, ResolvedNameWithOwner = item.NameWithOwner, TarballUrl = new Uri(new Uri(ServerUrl), item.Ref == BuildConstants.Source.CommitHash ? "localcheckout.tar.gz" : $"_apis/v1/ActionDownloadInfo/localcheckout?format=tarball&version={Uri.EscapeDataString(item.Ref)}&name={Uri.EscapeDataString(localcheckout)}").ToString(), ZipballUrl = new Uri(new Uri(ServerUrl), item.Ref == BuildConstants.Source.CommitHash ? "localcheckout.zip"  : $"_apis/v1/ActionDownloadInfo/localcheckout?format=zipball&version={Uri.EscapeDataString(item.Ref)}&name={Uri.EscapeDataString(localcheckout)}").ToString(), ResolvedSha = item.Ref != BuildConstants.Source.CommitHash ? GitHub.Runner.Sdk.BuildConstants.Source.CommitHash : $"{GitHub.Runner.Sdk.BuildConstants.Source.CommitHash}${item.Ref}" };
                    } else {
                        if(!string.IsNullOrEmpty(localcheckout) && !string.IsNullOrEmpty(runid) && long.TryParse(runid, out var _runid)) {
                            var handler = new MessageController(Configuration, this._cache, null, null);
                            if(await handler.RepoExists(_runid, name)) {
                                actions[name] = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, ResolvedNameWithOwner = item.NameWithOwner, Ref = item.Ref, TarballUrl = new Uri(new Uri(ServerUrl), $"_apis/v1/Message/tardown/{_runid}?repositoryAndRef={Uri.EscapeDataString(name)}").ToString(), ZipballUrl = new Uri(new Uri(ServerUrl), $"_apis/v1/Message/zipdown/{_runid}?repositoryAndRef={Uri.EscapeDataString(name)}").ToString(), ResolvedSha = "Local-Repository" };
                                continue;
                            }
                        }
                        ActionDownloadInfo defDownloadInfo = null;
                        foreach(var downloadUrl in downloadUrls) {
                            try {
                                var downloadinfo = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref, ResolvedNameWithOwner = item.NameWithOwner, ResolvedSha = item.Ref };
                                // Allow access to the original action
                                if(islocalcheckout && item.NameWithOwner == localcheckout && item.Ref.StartsWith(BuildConstants.Source.CommitHash)) {
                                    item.Ref = item.Ref.Substring(BuildConstants.Source.CommitHash.Length);
                                }
                                downloadinfo.TarballUrl = String.Format(downloadUrl.TarballUrl, item.NameWithOwner, item.Ref);
                                downloadinfo.ZipballUrl = String.Format(downloadUrl.ZipballUrl, item.NameWithOwner, item.Ref);
                                if(defDownloadInfo == null) {
                                    defDownloadInfo = downloadinfo;
                                }
                                if(downloadUrl.ReturnWithoutResolvingSha) {
                                    downloadinfo.Authentication = new ActionDownloadAuthentication() { Token = "dummy-token" };
                                    actions[name] = downloadinfo;
                                    break;
                                }
                                string ghtoken = null;
                                if(!string.IsNullOrEmpty(downloadUrl.GitApiServerUrl) && downloadUrl.GitApiServerUrl != GitApiServerUrl) {
                                    ghtoken = downloadUrl.GITHUB_TOKEN;
                                    // Without an dummy Token we would expose our GITHUB_TOKEN to https://github.com if no token is provided
                                    downloadinfo.Authentication = new ActionDownloadAuthentication() { Token = !string.IsNullOrEmpty(ghtoken) ? ghtoken : "dummy-token" };
                                } else {
                                    if(AllowPrivateActionAccess && repository != null && item.NameWithOwner != repository) {
                                        ghtoken = await CreateGithubAppToken(item.NameWithOwner);
                                        if(!string.IsNullOrEmpty(ghtoken)) {
                                            downloadinfo.Authentication = new ActionDownloadAuthentication() { Token = ghtoken };
                                        }
                                    }
                                    if(ghtoken == null) {
                                        ghtoken = defGhToken ?? GITHUB_TOKEN;
                                    }
                                }
                                // If we have no token and only one source of actions just return the archive url without resolving the sha to reduce api requests
                                if(!string.IsNullOrEmpty(ghtoken) || downloadUrls?.Count > 1) {
                                    var client = new HttpClient();
                                    client.DefaultRequestHeaders.Add("accept", "application/json");
                                    client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("runner", string.IsNullOrEmpty(GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version) ? "0.0.0" : GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version));
                                    if(!string.IsNullOrEmpty(ghtoken)) {
                                        client.DefaultRequestHeaders.Add("Authorization", $"token {ghtoken}");
                                    }
                                    var urlBuilder = new UriBuilder(new Uri(new Uri((!string.IsNullOrEmpty(downloadUrl.GitApiServerUrl) ? downloadUrl.GitApiServerUrl : GitApiServerUrl) + "/"), $"repos/{item.NameWithOwner}/commits"));
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
                                        actions[name] = downloadinfo;
                                        break;
                                    }
                                }
                            } catch {
                                // TODO log exceptions to workflow, job or audit log
                            }
                        }
                        if(defDownloadInfo != null) {
                            // Fallback to default download info, old behavior
                            actions.TryAdd(name, defDownloadInfo);
                        }
                    }
                }
                return await Ok(new ActionDownloadInfoCollection() {Actions = actions });
            } finally {
                if(defGhToken != null) {
                    DeleteGithubAppToken(defGhToken);
                }
            }
        }
    }
}
