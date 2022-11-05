using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/")]
    public class ApiResponder : VssControllerBase
    {

        public ApiResponder(IConfiguration conf) : base(conf) {
            
        }

        [HttpOptions]
        [AllowAnonymous]
        public IActionResult Get()
        {
            return Ok();
        }
    }
}
