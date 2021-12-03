
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;
using Runner.Server.Models;

namespace Runner.Server.Controllers {

    [ApiController]
    [Route("_apis/pipelines/workflows/{run}/artifacts")]
    [Route("{owner}/{repo}/_apis/pipelines/workflows/{run}/artifacts")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
    public class ArtifactController : VssControllerBase{


        private string _targetFilePath;

        private class ArtifactResponse {
            public string containerId {get;set;}
            public int size {get;set;}
            public string signedContent {get;set;}
            public string fileContainerResourceUrl {get;set;}
            public string type {get;set;}
            public string name {get;set;}
            public string url {get;set;}
        }

        private class DownloadInfo {
            public string path {get;set;}
            public string itemType {get;set;}
            public int fileLength {get;set;}
            public string contentLocation {get;set;}
        }

        private class CreateContainerRequest {
            public string Type {get;set;}//actions_storage
            public string Name {get;set;}

            public int? size {get;set;}
        }

        private SqLiteDb _context;

        public ArtifactController(SqLiteDb context, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _context = context;
            _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "artifacts");
            Directory.CreateDirectory(_targetFilePath);
            ReadConfig(configuration);
        }

        [HttpPost]
        public async Task<FileStreamResult> CreateContainer(int run) {
            var req = await FromBody<CreateContainerRequest>();
            var attempt = Int64.Parse(User.FindFirst("attempt")?.Value ?? "1");
            var artifactContainer = (from artifact in _context.Artifacts where artifact.Attempt.Attempt == attempt && artifact.Attempt.WorkflowRun.Id == run select artifact).First();
            var filecontainer = new ArtifactFileContainer() { Name = req.Name, Container = artifactContainer };
            _context.ArtifactFileContainer.Add(filecontainer);
            await _context.SaveChangesAsync();
            return await Ok(new ArtifactResponse { name = req.Name, type = "actions_storage", fileContainerResourceUrl = $"{ServerUrl}/_apis/pipelines/workflows/{run}/artifacts/container/{filecontainer.Id}" } );
        }

        [HttpPatch]
        public async Task<FileStreamResult> PatchContainer(int run, [FromQuery] string artifactName) {
            var req = await FromBody<CreateContainerRequest>();
            var attempt = Int64.Parse(User.FindFirst("attempt")?.Value ?? "1");
            var container = (from fileContainer in _context.ArtifactFileContainer where fileContainer.Container.Attempt.Attempt == attempt && fileContainer.Container.Attempt.WorkflowRun.Id == run && fileContainer.Name == artifactName select fileContainer).First();
            container.Size = req.size;
            await _context.SaveChangesAsync();
            // This sends the size of the artifact container
            return await Ok(new ArtifactResponse { name = artifactName, type = "actions_storage", fileContainerResourceUrl = $"{ServerUrl}/_apis/pipelines/workflows/{run}/artifacts/container/{container.Id}" } );
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetContainer(int run) {
            var attempt = Int64.Parse(User.FindFirst("attempt")?.Value ?? "1");
            var container = (from fileContainer in _context.ArtifactFileContainer where fileContainer.Container.Attempt.Attempt <= attempt && fileContainer.Container.Attempt.WorkflowRun.Id == run orderby fileContainer.Container.Attempt.Attempt descending select fileContainer).ToList();
            return await Ok(from e in container select new ArtifactResponse{ name = e.Name, type = "actions_storage", fileContainerResourceUrl = $"{ServerUrl}/_apis/pipelines/workflows/{run}/artifacts/container/{e.Id}" } );
        }

        [HttpPut("container/{id}")]
        public async Task<IActionResult> UploadToContainer(int run, int id, [FromQuery] string itemPath) {
            var container = (from record in _context.ArtifactRecords where record.FileContainer.Id == id && record.FileName == itemPath select record).FirstOrDefault();
            if(container == null) {
                container = new ArtifactRecord() {FileName = itemPath, StoreName = Path.GetRandomFileName(), GZip = Request.Headers.TryGetValue("Content-Encoding", out StringValues v) && v.Contains("gzip"), FileContainer = _context.ArtifactFileContainer.Find(id)} ;
                _context.ArtifactRecords.Add(container);
                await _context.SaveChangesAsync();
            }
            var range = Request.Headers["Content-Range"].ToArray()[0];
            int i = range.IndexOf('-');
            int j = range.IndexOf('/');
            var start = Convert.ToInt64(range.Substring(6, i - 6));
            var end = Convert.ToInt64(range.Substring(i + 1, j - (i + 1)));
            using(var targetStream = new FileStream(Path.Combine(_targetFilePath, container.StoreName), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write)) {
                targetStream.Seek(start, SeekOrigin.Begin);
                await Request.Body.CopyToAsync(targetStream);
            }
            return Ok();
        }

        [HttpGet("container/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult> GetFilesFromContainer(int run, int id) {
            var container = (from record in _context.ArtifactRecords where record.FileContainer.Id == id select record).ToList();
            var ret = new List<DownloadInfo>();
            foreach (var item in container) {
                ret.Add(new DownloadInfo { path = item.FileName, itemType = "file", fileLength = (int)new FileInfo(Path.Combine(_targetFilePath, item.StoreName)).Length, contentLocation = $"{ServerUrl}/_apis/pipelines/workflows/{run}/artifacts/artifact/{id}/{Uri.EscapeDataString(item.FileName)}"});
            }
            return await Ok(ret);
        }

        [HttpGet("artifact/{id}/{file}")]
        [AllowAnonymous]
        public IActionResult GetFileFromContainer(int run, int id, string file) {
            var container = (from record in _context.ArtifactRecords where record.FileContainer.Id == id && record.FileName == file select record).FirstOrDefault();
            if(container.FileName?.Length > 0) {
                Response.Headers.Add("Content-Disposition", $"attachment; filename={container.FileName}");
            }
            if(container.GZip) {
                Response.Headers.Add("Content-Encoding", "gzip");
            }
            return new FileStreamResult(System.IO.File.OpenRead(Path.Combine(_targetFilePath, container.StoreName)), "application/octet-stream") { EnableRangeProcessing = true };
        }

    }
}
