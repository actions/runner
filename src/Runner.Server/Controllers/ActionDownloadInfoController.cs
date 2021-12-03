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

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
    public class ActionDownloadInfoController : VssControllerBase
    {
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
            ReadConfig(configuration);
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        public async Task<IActionResult> Get(Guid scopeIdentifier, string hubName, Guid planId)
        {
            ActionReferenceList reflist = await FromBody<ActionReferenceList>();
            var actions = new Dictionary<string, ActionDownloadInfo>();
            foreach (var item in reflist.Actions) {
                foreach(var downloadUrl in downloadUrls) {
                    if(item.NameWithOwner == "localcheckout") {
                        actions[$"{item.NameWithOwner}@{item.Ref}"] = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref, TarballUrl = $"{ServerUrl}/localcheckout.tar.gz", ZipballUrl = $"{ServerUrl}/localcheckout.zip" };
                    } else {
                        var downloadinfo = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref, TarballUrl = String.Format(downloadUrl.TarballUrl, item.NameWithOwner, item.Ref), ZipballUrl = String.Format(downloadUrl.ZipballUrl, item.NameWithOwner, item.Ref) };
                        // TODO: How to check on github if url is valid?, maybe use GITHUB_TOKEN?
                        // var client = new HttpClient();
                        // if((await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, downloadinfo.TarballUrl))).IsSuccessStatusCode && (await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, downloadinfo.ZipballUrl))).IsSuccessStatusCode) {
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
