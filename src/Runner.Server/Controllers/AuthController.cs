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
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace Runner.Server.Controllers {
    [ApiController]
    [AllowAnonymous]
    [Route("_apis/v1/auth")]
    [Route("test/auth/v1")]
    public class AuthController : VssControllerBase {
        private SqLiteDb db;


        public AuthController(SqLiteDb _db, IConfiguration conf) : base(conf) {
            db = _db;
        }

        private bool ValidateCurrentToken(RSA rsa, string token)
        {
            var myIssuer = "http://githubactionsserver";
            var myAudience = "http://githubactionsserver";

            var tokenHandler = new JwtSecurityTokenHandler();
            var now = DateTime.Now.ToString();
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
            catch (SecurityTokenNotYetValidException ex)
            {
                var nbf = ex.NotBefore.ToString();
                Console.WriteLine($"Security Token not valid yet: {nbf}, now {now}");
                return false;
            }
            catch (SecurityTokenExpiredException ex) {
                var exp = ex.Expires.ToString();
                Console.WriteLine($"Security Token expired: {exp}, now {now}");
                return false;
            }
            catch
            {
                Console.WriteLine($"Security Token is invalid");
                return false;
            }
            return true;
        }
        [HttpPost]
        public async Task<IActionResult> Authenticate() {
            var token2 = new JwtSecurityToken(Request.Form["client_assertion"]);
            var id = token2.Claims.Where(c => c.Type == "sub").FirstOrDefault();
            var agentid = Guid.Parse(id.Value);

            var ret = db.Agents.Where(a => a.ClientId == agentid).Include(a => a.TaskAgent).Include(a => a.TaskAgent.Labels).Include(a => a.Pool).FirstOrDefault();
            if(ret == null || !ValidateCurrentToken(ret.PublicKey, Request.Form["client_assertion"])) {
                return NotFound();
            }

            var mySecurityKey = new RsaSecurityKey(Startup.AccessTokenParameter);

            var myIssuer = "http://githubactionsserver";
            var myAudience = "http://githubactionsserver";

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("Agent", "oauth"),
                    new Claim("Agent", "job")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.RsaSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            return await Ok(new VssOAuthTokenResponse(){AccessToken = tokenHandler.WriteToken(token), ExpiresIn = 7 * 24 * 60 * 60, Scope = "/", TokenType = "access_token" });
        }
    }
}