using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
    public class LogfilesController : ControllerBase
    {

        private readonly ILogger<LogfilesController> _logger;
        
        Dictionary<int, TaskLog> logs = new Dictionary<int, TaskLog>();

        public LogfilesController(ILogger<LogfilesController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}")]
        public TaskLog CreateLog(Guid scopeIdentifier, string hubName, Guid planId, [FromBody] TaskLog log)
        {
            log.Id = logs.Keys.LastOrDefault() + 1;
            logs.Add(log.Id, log);
            return log;
        }
        

        [HttpPost("{scopeIdentifier}/{hubName}/{planId}/{logId}")]
        public async Task AppendLogContent(Guid scopeIdentifier, string hubName, Guid planId, int logId/* , [FromBody] string body */)
        {
            string scontent = "";
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {  
                scontent = await reader.ReadToEndAsync();
            }
            Console.Out.WriteLine(scontent);
        }
    }
}
