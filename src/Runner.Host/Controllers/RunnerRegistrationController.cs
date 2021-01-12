using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("/api/v3/actions/runner-registration")]
    public class RunnerRegistrationController : ControllerBase
    {

        private readonly ILogger<RunnerRegistrationController> _logger;

        public RunnerRegistrationController(ILogger<RunnerRegistrationController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public string Get([FromBody] Dictionary<string, string> dict)
        {
            return StringUtil.ConvertToJson(new Dictionary<string, string> {
                { "url", "http://ubuntu.fritz.box"},
                { "token_schema", "OAuthAccessToken"},
                { "token", "njuadbueegfgrgrsgd"}
            });
        }
    }
}
