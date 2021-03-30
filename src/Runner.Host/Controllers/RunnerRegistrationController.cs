using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Runner.Host.Models;

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("/api/v3/actions/runner-registration")]
    public class RunnerRegistrationController : VssControllerBase
    {

        private readonly ILogger<RunnerRegistrationController> _logger;

        public RunnerRegistrationController(ILogger<RunnerRegistrationController> logger)
        {
            _logger = logger;
        }

        class AddRemoveRunner
        {
            [DataMember(Name = "url")]
            public string Url {get;set;}

            [DataMember(Name = "runner_event")]
            public string RunnerEvent {get;set;}
        }

        [HttpPost]
        public async Task<IActionResult> Get()
        {
            StringValues auth;
            if(!Request.Headers.TryGetValue("Authorization", out auth) || auth.FirstOrDefault()?.StartsWith("RemoteAuth ") != true) {
                return NotFound();
            }
            var payload = await FromBody<AddRemoveRunner>();
            // Request.Headers.HeaderAuthorization = RemoteAuth AKWETFL3YIUV34LTWCZ5M4275R3HQ
            // HeaderUserAgent = GitHubActionsRunner-
            return await Ok(new Runner.Host.Models.GitHubAuthResult() {
                TenantUrl = payload.Url,
                Token = "njuadbueegfgrgrsgd",
                TokenSchema = "OAuthAccessToken"
            });
        }
    }
}
