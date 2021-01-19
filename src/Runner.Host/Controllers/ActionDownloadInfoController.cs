using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("runner/host/_apis/v1/[controller]")]
    public class ActionDownloadInfoController : VssControllerBase
    {

        private readonly ILogger<ActionDownloadInfoController> _logger;

        public ActionDownloadInfoController(ILogger<ActionDownloadInfoController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        public async Task<IActionResult> Get(Guid scopeIdentifier, string hubName, Guid planId)
        {
            ActionReferenceList reflist = await FromBody<ActionReferenceList>();
            var actions = new Dictionary<string, ActionDownloadInfo>();
            foreach (var item in reflist.Actions) {
                // actions[$"{item.NameWithOwner}@{item.Ref}"] = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref, TarballUrl = $"https://api.github.com/repos/{item.NameWithOwner}/tarball/{item.Ref}", ZipballUrl = $"https://api.github.com/repos/{item.NameWithOwner}/zipball/{item.Ref}"};
                actions[$"{item.NameWithOwner}@{item.Ref}"] = new ActionDownloadInfo() {NameWithOwner = item.NameWithOwner, Ref = item.Ref, TarballUrl = $"http://ubuntu:3042/{item.NameWithOwner}/archive/{item.Ref}.tar.gz", ZipballUrl = $"http://ubuntu:3042/{item.NameWithOwner}/archive/{item.Ref}.zip"};
            }
            return await Ok(new ActionDownloadInfoCollection() {Actions = actions });
        }

        
    }
}
