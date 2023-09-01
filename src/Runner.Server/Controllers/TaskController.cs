using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Services.Location;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/tasks")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
    public class TaskController : VssControllerBase
    {
        private IMemoryCache cache;

        public TaskController(IConfiguration configuration, IMemoryCache cache) : base(configuration)
        {
            this.cache = cache;
        }

        [HttpPatch]
        [HttpPost]
        [HttpPut]
        [AllowAnonymous]
        public async Task<IActionResult> GetTask() {
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            return Ok();
        }

        [HttpGet("{taskid}")]
        [AllowAnonymous]
        public IActionResult GetTask(Guid taskid) {
            return Ok();
        }

        [HttpGet("{taskid}/{version}")]
        [AllowAnonymous]
        public IActionResult GetTaskArchive(Guid taskid, string version) {
            var srunid = User.FindFirst("runid")?.Value;
            long runid = -1;
            var tasksByNameAndVersion = srunid != null && long.TryParse(srunid, out runid) && MessageController.WorkflowStates.TryGetValue(runid, out var state) && state.TasksByNameAndVersion != null ? state.TasksByNameAndVersion : cache.GetOrCreate("tasksByNameAndVersion", ce => {
                var ( tasks, tasksByNameAndVersion ) = TaskMetaData.LoadTasks(Path.Combine(GharunUtil.GetLocalStorage(), "AzureTasks"));
                return tasksByNameAndVersion;
            });
            var archivePath = tasksByNameAndVersion[$"{taskid}@{version}"].ArchivePath;
            var prefix = "localtaskzip://";
            if(archivePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
                var handler = new MessageController(Configuration, cache, null);
                handler.ControllerContext = ControllerContext;
                handler.HttpContext.Response.GetTypedHeaders().ContentType = new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
                handler.GetZip(runid, false, false, archivePath.Substring(prefix.Length), true).GetAwaiter().GetResult();
                return new EmptyResult();
            }
            return new FileStreamResult(System.IO.File.OpenRead(tasksByNameAndVersion[$"{taskid}@{version}"].ArchivePath), "application/zip") { EnableRangeProcessing = true };
        }
    }
}
