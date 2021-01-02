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
    [Route("_apis/v1/[controller]")]
    public class ActionDownloadInfoController : ControllerBase
    {

        private readonly ILogger<ActionDownloadInfoController> _logger;

        public ActionDownloadInfoController(ILogger<ActionDownloadInfoController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        public ActionDownloadInfoCollection Get(Guid scopeIdentifier, string hubName, Guid planId, [FromBody] ActionReferenceList reflist)
        {
            return new ActionDownloadInfoCollection() {Actions = new Dictionary<string, ActionDownloadInfo>{
                { "actions/checkout@v2", new ActionDownloadInfo() {NameWithOwner = "actions/checkout", Ref = "v2", TarballUrl = "https://api.github.com/repos/actions/checkout/tarball/v2", ZipballUrl = "https://api.github.com/repos/actions/checkout/zipball/v2" } }
            }};
        }

        
    }
}
