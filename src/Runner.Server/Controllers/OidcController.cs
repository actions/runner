using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Runner.Server.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route(".well-known")]
    public class OidcController : VssControllerBase {

        public OidcController(IConfiguration conf) : base(conf) {
            
        }

        [HttpGet("openid-configuration")]
        public Task<FileStreamResult> GetOidcConfig() {
            var conf = new {
                issuer = ServerUrl,
                jwks_uri = new Uri(new Uri(ServerUrl), ".well-known/jwks"),
                subject_types_supported = new [] {"public", "pairwise"},
                response_types_supported = new [] {"id_token"},
                claims_supported = new [] {"sub", "aud", "exp", "iat", "iss", "jti", "nbf", "ref", "ref_type", "repository", "repository_id", "repository_owner", "repository_owner_id", "repository_visibility", "run_id", "run_number", "run_attempt", "actor", "actor_id", "workflow", "head_ref", "base_ref", "event_name", "environment", "job_workflow_ref", "job_workflow_sha", "workflow_ref", "workflow_sha"}, 
                id_token_signing_alg_values_supported = new []{"RS256"},
                scopes_supported = new [] {"openid"}
            };
            return Ok(conf, true);
        }

        [HttpGet("jwks")]
        public Task<FileStreamResult> GetJwk() {
            var kid = Startup.KeyId;
            var n = Base64UrlEncoder.Encode(Startup.AccessTokenParameter.Modulus);
            var e = Base64UrlEncoder.Encode(Startup.AccessTokenParameter.Exponent);
            var jwks = new { keys = new [] { new { kid, n, e, alg = SecurityAlgorithms.RsaSha256, kty = "RSA", use = "sig" } } };
            return Ok(jwks, true);
        }
    }
}