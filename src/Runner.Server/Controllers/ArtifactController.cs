
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
using GitHub.Actions.Pipelines.WebApi;
using Microsoft.EntityFrameworkCore;

namespace Runner.Server.Controllers {

    [ApiController]
    [Route("_apis/pipelines/workflows")]
    [Route("{owner}/{repo}/_apis/pipelines/workflows")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
    public class ArtifactController : VssControllerBase{


        private string _targetFilePath;

        private class ArtifactResponse {
            public long containerId {get;set;}
            public int size {get;set;}
            public string signedContent {get;set;}
            public string fileContainerResourceUrl {get;set;}
            public string type {get;set;}
            public string name {get;set;}
            public string url {get;set;}
        }

        private class DownloadInfo {
            public DownloadInfo() {
                Status = "created";
            }

            public string path {get;set;}
            public string itemType {get;set;}
            public int fileLength {get;set;}
            public string contentLocation {get;set;}
            public long ContainerId {get;set;}
            public string Status {get;set;}
        }

        private SqLiteDb _context;

        public ArtifactController(SqLiteDb context, IConfiguration configuration)
        {
            _context = context;
            _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "artifacts");
            Directory.CreateDirectory(_targetFilePath);
            ReadConfig(configuration);
        }

        public async Task<ArtifactFileContainer> CreateContainer(long run, long attempt, CreateActionsStorageArtifactParameters req) {
            var filecontainer = (from fileContainer in _context.ArtifactFileContainer where fileContainer.Container.Attempt.Attempt == attempt && fileContainer.Container.Attempt.WorkflowRun.Id == run && fileContainer.Id == req.ContainerId select fileContainer).Include(f => f.Container).Include(f => f.Files).FirstOrDefault();
            if(filecontainer != null) {
                var files = filecontainer.Files;
                filecontainer = new ArtifactFileContainer() { Name = req.Name, Size = req.Size, Container = filecontainer.Container, Files = filecontainer.Files.ToList() };
                files.Clear();
                _context.ArtifactFileContainer.Add(filecontainer);
            } else {
                var artifactContainer = (from artifact in _context.Artifacts where artifact.Attempt.Attempt == attempt && artifact.Attempt.WorkflowRun.Id == run select artifact).First();
                filecontainer = new ArtifactFileContainer() { Name = req.Name, Container = artifactContainer };
                _context.ArtifactFileContainer.Add(filecontainer);
            }
            await _context.SaveChangesAsync();
            return filecontainer;
        }

        [HttpPost("{run}/artifacts")]
        public async Task<FileStreamResult> CreateContainer(long run) {
            var req = await FromBody<CreateActionsStorageArtifactParameters>();
            var attempt = Int64.Parse(User.FindFirst("attempt")?.Value ?? "1");
            var filecontainer = await CreateContainer(run, attempt, req);
            return await Ok(new ArtifactResponse { name = req.Name, type = "actions_storage", containerId = filecontainer.Id, fileContainerResourceUrl = $"{ServerUrl}/_apis/pipelines/workflows/container/{filecontainer.Id}" } );
        }

        [HttpPatch("{run}/artifacts")]
        public async Task<FileStreamResult> PatchContainer(long run, [FromQuery] string artifactName) {
            var req = await FromBody<CreateActionsStorageArtifactParameters>();
            var attempt = Int64.Parse(User.FindFirst("attempt")?.Value ?? "1");
            var container = (from fileContainer in _context.ArtifactFileContainer where fileContainer.Container.Attempt.Attempt == attempt && fileContainer.Container.Attempt.WorkflowRun.Id == run && fileContainer.Name == artifactName select fileContainer).First();
            container.Size = req.Size;
            await _context.SaveChangesAsync();
            // This sends the size of the artifact container
            return await Ok(new ArtifactResponse { name = artifactName, type = "actions_storage", fileContainerResourceUrl = $"{ServerUrl}/_apis/pipelines/workflows/container/{container.Id}" } );
        }

        [HttpGet("{run}/artifacts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetContainer(long run, [FromQuery] string artifactName) {
            var attempt = Int64.Parse(User.FindFirst("attempt")?.Value ?? "1");
            if(string.IsNullOrEmpty(artifactName)) {
                var container = (from fileContainer in _context.ArtifactFileContainer where fileContainer.Container.Attempt.Attempt <= attempt && fileContainer.Container.Attempt.WorkflowRun.Id == run && fileContainer.Files.Count > 0 orderby fileContainer.Container.Attempt.Attempt descending select fileContainer).ToList();
                return await Ok(from e in container select new ArtifactResponse{ name = e.Name, type = "actions_storage", containerId = e.Id, fileContainerResourceUrl = $"{ServerUrl}/_apis/pipelines/workflows/container/{e.Id}" } );
            } else {
                var container = (from fileContainer in _context.ArtifactFileContainer where fileContainer.Container.Attempt.Attempt <= attempt && fileContainer.Container.Attempt.WorkflowRun.Id == run && fileContainer.Files.Count > 0 && fileContainer.Name == artifactName orderby fileContainer.Container.Attempt.Attempt descending select new ArtifactResponse{ name = fileContainer.Name, type = "actions_storage", containerId = fileContainer.Id, fileContainerResourceUrl = $"{ServerUrl}/_apis/pipelines/workflows/container/{fileContainer.Id}" }).First();
                return await Ok(container);
            }
        }

        [HttpPut("container/{id}")]
        public async Task<IActionResult> UploadToContainer(long run, int id, [FromQuery] string itemPath) {
            var container = (from record in _context.ArtifactRecords where record.FileContainer.Id == id && record.FileName == itemPath select record).FirstOrDefault();
            bool created = false;
            if(container == null) {
                container = new ArtifactRecord() {FileName = itemPath, StoreName = Path.GetRandomFileName(), GZip = Request.Headers.TryGetValue("Content-Encoding", out StringValues v) && v.Contains("gzip"), FileContainer = _context.ArtifactFileContainer.Find(id)} ;
                _context.ArtifactRecords.Add(container);
                await _context.SaveChangesAsync();
                created = true;
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
            return created ? Created($"{ServerUrl}/_apis/pipelines/workflows/artifact/{id}/{Uri.EscapeDataString(itemPath)}", null) : Ok();
        }

        [HttpGet("container/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFilesFromContainer(int id, [FromQuery] string itemPath) {
            var container = (from record in _context.ArtifactRecords where record.FileContainer.Id == id select record).ToList();
            if(string.IsNullOrEmpty(itemPath) || !container.Any(r => r.FileName == itemPath)) {
                var ret = new List<DownloadInfo>();
                foreach (var item in container) {
                    int i = -1;
                    while(true) {
                        i = item.FileName.IndexOfAny(new [] { '/', '\\' }, i + 1);
                        if(i == -1) {
                            break;
                        }
                        ret.Add(new DownloadInfo { ContainerId = id, path = item.FileName.Substring(0, i), itemType = "folder"});
                    }
                    
                    ret.Add(new DownloadInfo { ContainerId = id, path = item.FileName, itemType = "file", fileLength = (int)new FileInfo(Path.Combine(_targetFilePath, item.StoreName)).Length, contentLocation = $"{ServerUrl}/_apis/pipelines/workflows/artifact/{id}/{Uri.EscapeDataString(item.FileName)}"});
                }
                return await Ok(ret);
            } else {
                return GetFileFromContainer(id, itemPath);
            }
        }

        [HttpGet("artifact/{id}/{file}")]
        [AllowAnonymous]
        public IActionResult GetFileFromContainer(int id, string file) {
            // It seems like aspnetcore 5 doesn't unescape the uri component on linux and macOS, while on windows it does!
            // Try to match both to avoid bugs
            var unescapedFile = Uri.UnescapeDataString(file);
            var container = (from record in _context.ArtifactRecords where record.FileContainer.Id == id && (record.FileName == file || record.FileName == unescapedFile) select record).FirstOrDefault();
            if(container == null) {
                throw new Exception($"container is null!, id='{id}', file='{file}'");
            }
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
