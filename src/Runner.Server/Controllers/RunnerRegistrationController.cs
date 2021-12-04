using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Runner.Server.Models;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("/api/v3/actions/runner-registration")]
    public class RunnerRegistrationController : VssControllerBase
    {
        private string RUNNER_TOKEN { get; }

        public RunnerRegistrationController(IConfiguration configuration)
        {
            RUNNER_TOKEN = configuration.GetSection("Runner.Server")?.GetValue<String>("RUNNER_TOKEN") ?? "";
            ReadConfig(configuration);
        }

        class AddRemoveRunner
        {
            [DataMember(Name = "url")]
            public string Url {get;set;}

            [DataMember(Name = "runner_event")]
            public string RunnerEvent {get;set;}
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Get()
        {
            StringValues auth;
            if(!Request.Headers.TryGetValue("Authorization", out auth)) {
                return Unauthorized();
            }
            if(auth.FirstOrDefault()?.StartsWith("RemoteAuth ") != true || (RUNNER_TOKEN.Length > 0 && auth.First() != "RemoteAuth " + RUNNER_TOKEN) ) {
                return NotFound();
            }
            var payload = await FromBody<AddRemoveRunner>();
            var mySecurityKey = new RsaSecurityKey(Startup.AccessTokenParameter);

            var myIssuer = "http://githubactionsserver";
            var myAudience = "http://githubactionsserver";

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("Agent", "management")
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.RsaSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var payloadUrl = new Uri(payload.Url);
            var components = payloadUrl.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return await Ok(new Runner.Server.Models.GitHubAuthResult() {
                TenantUrl = new Uri(new Uri(ServerUrl), components.Length == 0 ? "runner/server" : components.Length > 1 ?  $"{components[0]}/{components[1]}" : $"{components[0]}/server").ToString(),
                Token = tokenHandler.WriteToken(token),
                TokenSchema = "OAuthAccessToken"
            });
        }
    }
}
