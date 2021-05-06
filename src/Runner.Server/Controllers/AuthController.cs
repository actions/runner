using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;
using System.Security.Claims;
using System;
using GitHub.Services.OAuth;
using Runner.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;

namespace Runner.Server.Controllers {
    [ApiController]
    [Route("test/auth/v1")]
    public class AuthController : VssControllerBase {
        private SqLiteDb db;


        public AuthController(SqLiteDb _db) {
            db = _db;
        }

        private bool ValidateCurrentToken(RSA rsa, string token)
        {
            var myIssuer = "http://githubactionsserver";
            var myAudience = "http://githubactionsserver";

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidIssuer = myIssuer,
                    ValidAudience = myAudience,
                    IssuerSigningKey = new RsaSecurityKey(rsa)
                }, out SecurityToken validatedToken);
            }
            catch
            {
                return false;
            }
            return true;
        }
        [HttpPost]
        public Task<FileStreamResult> Authenticate() {
            // var ua = Request.Headers["User-Agent"].ToArray()[0];
            // var ridoffset = ua.IndexOf("RunnerId/");
            // if(ridoffset != -1) {
            //     ridoffset += "RunnerId/".Length;
            // }
            // var ridend = ua.IndexOf(' ', ridoffset);
            // var agentid = Guid.Parse(ua.Substring(ridoffset, ridend - ridoffset));
            var token2 = new JwtSecurityToken(Request.Form["client_assertion"]);
            var id = token2.Claims.Where(c => c.Type == "sub").FirstOrDefault();
            var agentid = Guid.Parse(id.Value);
            // "GitHubActionsRunner-linux-arm64/3.0.3 RunnerId/2caee715-9385-4e48-af19-755bb886f814 (Linux 5.4.0-1025-raspi #28-Ubuntu SMP PREEMPT Wed Dec 9 17:10:53 UTC 2020) VSServices/3.0.3.0 (NetStandard; Linux 5.4.0-1025-raspi #28-Ubuntu SMP PREEMPT Wed Dec 9 17:10:53 UTC 2020)"
            //Request.Form["grant_type"] == "client_credentials"
            //Request.Form["client_assertion_type"] == "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"
            //Request.Form["client_assertion"]
            // var assertion = this.Request.Form["Assertion"];
            // var token = assertion.ToArray()[0];

            var ret = db.Agents.Include(a => a.TaskAgent).Include(a => a.TaskAgent.Labels).Include(a => a.Pool).Where(a => a.ClientId == agentid).FirstOrDefault();
            
            if(!ValidateCurrentToken(ret.PublicKey, Request.Form["client_assertion"])) {
                return null;
            }

            var mySecurityKey = new RsaSecurityKey(Startup.AccessTokenParameter);

            var myIssuer = "http://githubactionsserver";
            var myAudience = "http://githubactionsserver";

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.RsaSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            // if(ValidateCurrentToken(token)) {
                // await HttpContext.GetTokenAsync("bearer", "test");
                return Ok(new VssOAuthTokenResponse(){AccessToken = tokenHandler.WriteToken(token), ExpiresIn = 7 * 24 * 60 * 60, Scope = "/", TokenType = "access_token" });
            // }
            // return NotFound();
        }
    }
}