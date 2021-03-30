using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class ActionDownloadInfoController : VssControllerBase
    {
        private List<ActionDownloadUrls> downloadUrls;
        private class ActionDownloadUrls
        {
            public string TarbalUrl { get; set; }
            public string ZipbalUrl { get; set; }
        }

        public ActionDownloadInfoController(IConfiguration configuration)
        {
            downloadUrls = configuration.GetSection("Runner.Host:ActionDownloadUrls").Get<List<ActionDownloadUrls>>();
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        public async Task<IActionResult> Get(Guid scopeIdentifier, string hubName, Guid planId)
        {
            ActionReferenceList reflist = await FromBody<ActionReferenceList>();
            var actions = new Dictionary<string, ActionDownloadInfo>();
            foreach (var item in reflist.Actions) {
                foreach(var downloadUrl in downloadUrls) {
                    var downloadinfo = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref, TarballUrl = String.Format(downloadUrl.TarbalUrl, item.NameWithOwner, item.Ref), ZipballUrl = String.Format(downloadUrl.ZipbalUrl, item.NameWithOwner, item.Ref) };
                    var client = new HttpClient();
                    if((await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, downloadinfo.TarballUrl))).IsSuccessStatusCode && (await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, downloadinfo.ZipballUrl))).IsSuccessStatusCode) {
                        actions[$"{item.NameWithOwner}@{item.Ref}"] = downloadinfo;
                        break;
                    }
                }
            }
            return await Ok(new ActionDownloadInfoCollection() {Actions = actions });
        }        
    }
}
