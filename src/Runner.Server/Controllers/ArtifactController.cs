
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
using System.Net.Mime;

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

        public ArtifactController(SqLiteDb context, IConfiguration configuration) : base(configuration)
        {
            _context = context;
            _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "artifacts");
            Directory.CreateDirectory(_targetFilePath);
        }

        public async Task<ArtifactFileContainer> CreateContainer(long run, long attempt, CreateActionsStorageArtifactParameters req, long artifactsMinAttempt = -1) {
            var filecontainer = (from fileContainer in _context.ArtifactFileContainer where fileContainer.Container.Attempt.Attempt == attempt && fileContainer.Container.Attempt.WorkflowRun.Id == run && fileContainer.Id == req.ContainerId select fileContainer).Include(f => f.Container).Include(f => f.Files).FirstOrDefault();
            if(filecontainer != null) {
                var files = filecontainer.Files;
                var container = filecontainer.Container;
                filecontainer = (from fileContainer in _context.ArtifactFileContainer where (fileContainer.Container.Attempt.Attempt >= artifactsMinAttempt || artifactsMinAttempt == -1) && (fileContainer.Container.Attempt.Attempt <= attempt || attempt == -1) && fileContainer.Container.Attempt.WorkflowRun.Id == run && fileContainer.Name.ToLower() == req.Name.ToLower() orderby fileContainer.Container.Attempt.Attempt descending select fileContainer).FirstOrDefault();
                if(filecontainer != null) {
                    foreach(var file in files) {
                        var ofile = await (from f in _context.Entry(filecontainer).Collection(f => f.Files).Query() where f.FileName.ToLower() == file.FileName.ToLower() select f).FirstOrDefaultAsync();
                        if(ofile != null) {
                            try {
                                System.IO.File.Delete(Path.Combine(_targetFilePath, ofile.StoreName));
                            } catch {

                            }
                            ofile.StoreName = file.StoreName;
                            ofile.GZip = file.GZip;
                            _context.Remove(file);
                        } else {
                            file.FileContainer = filecontainer;
                        }
                    }
                } else {
                    filecontainer = new ArtifactFileContainer() { Name = req.Name, Size = req.Size, Container = container, Files = files.ToList() };
                    _context.ArtifactFileContainer.Add(filecontainer);
                }
                files.Clear();
            } else {
                filecontainer = (from fileContainer in _context.ArtifactFileContainer where (fileContainer.Container.Attempt.Attempt >= artifactsMinAttempt || artifactsMinAttempt == -1) && (fileContainer.Container.Attempt.Attempt <= attempt || attempt == -1) && fileContainer.Container.Attempt.WorkflowRun.Id == run && fileContainer.Name.ToLower() == req.Name.ToLower() orderby fileContainer.Container.Attempt.Attempt descending select fileContainer).FirstOrDefault();
                if(filecontainer == null) {
                    var artifactContainer = (from artifact in _context.Artifacts where artifact.Attempt.Attempt == attempt && artifact.Attempt.WorkflowRun.Id == run select artifact).First();
                    filecontainer = new ArtifactFileContainer() { Name = req.Name, Container = artifactContainer };
                    _context.ArtifactFileContainer.Add(filecontainer);
                }
            }
            await _context.SaveChangesAsync();
            return filecontainer;
        }

        [HttpPost("{run}/artifacts")]
        public async Task<FileStreamResult> CreateContainer(long run) {
            var req = await FromBody<CreateActionsStorageArtifactParameters>();
            var attempt = Int64.Parse(User.FindFirst("attempt")?.Value ?? "1");
            var artifactsMinAttempt = Int64.Parse(User.FindFirst("artifactsMinAttempt")?.Value ?? "-1");
            // azp build artifact / parse "{\"id\":0,\"name\":\"drop\",\"source\":null,\"resource\":{\"type\":\"Container\",\"data\":\"#/10/drop\",\"properties\":{\"localpath\":\"/home/christopher/.local/share/gharun/a/l53fnlmg.djp/w/1/a\",\"artifactsize\":\"28710\"}}}"
            if(req.ContainerId <= 0) {
                req.ContainerId = Int64.Parse(User.FindFirst("containerid")?.Value ?? "0");
            }
            var filecontainer = await CreateContainer(run, attempt, req, artifactsMinAttempt);
            return await Ok(new ArtifactResponse { name = req.Name, type = "actions_storage", containerId = filecontainer.Id, fileContainerResourceUrl = new Uri(new Uri(ServerUrl), $"_apis/pipelines/workflows/container/{filecontainer.Id}").ToString() } );
        }

        [HttpPatch("{run}/artifacts")]
        public async Task<FileStreamResult> PatchContainer(long run, [FromQuery] string artifactName) {
            var req = await FromBody<CreateActionsStorageArtifactParameters>();
            var attempt = Int64.Parse(User.FindFirst("attempt")?.Value ?? "1");
            var artifactsMinAttempt = Int64.Parse(User.FindFirst("artifactsMinAttempt")?.Value ?? "-1");
            var container = (from fileContainer in _context.ArtifactFileContainer where (fileContainer.Container.Attempt.Attempt >= artifactsMinAttempt || artifactsMinAttempt == -1) && fileContainer.Container.Attempt.Attempt <= attempt && fileContainer.Container.Attempt.WorkflowRun.Id == run && fileContainer.Name.ToLower() == artifactName.ToLower() select fileContainer).First();
            container.Size = req.Size;
            await _context.SaveChangesAsync();
            // This sends the size of the artifact container
            return await Ok(new ArtifactResponse { name = artifactName, type = "actions_storage", fileContainerResourceUrl = new Uri(new Uri(ServerUrl), $"_apis/pipelines/workflows/container/{container.Id}").ToString() } );
        }

        [HttpGet("{run}/artifacts")]
        [AllowAnonymous]
        public async Task<IActionResult> GetContainer(long run, [FromQuery] string artifactName) {
            var attempt = Int64.Parse(User.FindFirst("attempt")?.Value ?? "-1");
            var artifactsMinAttempt = Int64.Parse(User.FindFirst("artifactsMinAttempt")?.Value ?? "-1");
            if(string.IsNullOrEmpty(artifactName)) {
                var container = (from fileContainer in _context.ArtifactFileContainer where (fileContainer.Container.Attempt.Attempt >= artifactsMinAttempt || artifactsMinAttempt == -1) && (fileContainer.Container.Attempt.Attempt <= attempt || attempt == -1) && fileContainer.Container.Attempt.WorkflowRun.Id == run orderby fileContainer.Container.Attempt.Attempt descending select fileContainer).ToList();
                return await Ok(from e in container select new ArtifactResponse{ name = e.Name, type = "actions_storage", containerId = e.Id, fileContainerResourceUrl = new Uri(new Uri(ServerUrl), $"_apis/pipelines/workflows/container/{e.Id}").ToString() } );
            } else {
                var container = (from fileContainer in _context.ArtifactFileContainer where (fileContainer.Container.Attempt.Attempt >= artifactsMinAttempt || artifactsMinAttempt == -1) && (fileContainer.Container.Attempt.Attempt <= attempt || attempt == -1) && fileContainer.Container.Attempt.WorkflowRun.Id == run && fileContainer.Name.ToLower() == artifactName.ToLower() orderby fileContainer.Container.Attempt.Attempt descending select new ArtifactResponse{ name = fileContainer.Name, type = "actions_storage", containerId = fileContainer.Id, fileContainerResourceUrl = new Uri(new Uri(ServerUrl), $"_apis/pipelines/workflows/container/{fileContainer.Id}").ToString() }).First();
                return await Ok(container);
            }
        }

        [HttpPut("container/{id}")]
        public async Task<IActionResult> UploadToContainer(long run, int id, [FromQuery] string itemPath) {
            var container = (from record in _context.ArtifactRecords where record.FileContainer.Id == id && record.FileName.ToLower() == itemPath.ToLower() select record).FirstOrDefault();
            bool created = false;
            var gzip = Request.Headers.TryGetValue("Content-Encoding", out StringValues v) && v.Contains("gzip");
            if(container == null) {
                container = new ArtifactRecord() {FileName = itemPath, StoreName = Path.GetRandomFileName(), GZip = gzip, FileContainer = _context.ArtifactFileContainer.Find(id)} ;
                if(container.FileContainer == null) {
                    container.FileContainer = new ArtifactFileContainer() { Id = id };
                }
                _context.ArtifactRecords.Add(container);
                await _context.SaveChangesAsync();
                created = true;
            } else if(container.GZip != gzip) {
                container.GZip = gzip;
                await _context.SaveChangesAsync();
            }
            var range = Request.Headers["Content-Range"].ToArray()[0];
            int i = range.IndexOf('-');
            int j = range.IndexOf('/');
            var start = Convert.ToInt64(range.Substring(6, i - 6));
            var end = Convert.ToInt64(range.Substring(i + 1, j - (i + 1)));
            var size = Convert.ToInt64(range.Substring(j+1));
            using(var targetStream = new FileStream(Path.Combine(_targetFilePath, container.StoreName), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write)) {
                targetStream.SetLength(size);
                targetStream.Seek(start, SeekOrigin.Begin);
                await Request.Body.CopyToAsync(targetStream);
            }
            return created ? Created(new Uri(new Uri(ServerUrl), $"_apis/pipelines/workflows/artifact/{id}?file={Uri.EscapeDataString(itemPath)}").ToString(), null) : Ok();
        }

        [HttpGet("container/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFilesFromContainer(int id, [FromQuery] string itemPath) {
            var container = (from record in _context.ArtifactRecords where record.FileContainer.Id == id select record).ToList();
            if(string.IsNullOrEmpty(itemPath) || !container.Any(r => string.Equals(r.FileName, itemPath, StringComparison.OrdinalIgnoreCase))) {
                var ret = new List<DownloadInfo>();
                var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in container) {
                    int i = -1;
                    while(true) {
                        i = item.FileName.IndexOfAny(new [] { '/', '\\' }, i + 1);
                        if(i == -1) {
                            break;
                        }
                        var folderPath = item.FileName.Substring(0, i);
                        if(folders.Add(folderPath)) {
                            ret.Add(new DownloadInfo { ContainerId = id, path = folderPath, itemType = "folder"});
                        }
                    }
                    
                    ret.Add(new DownloadInfo { ContainerId = id, path = item.FileName, itemType = "file", fileLength = (int)new FileInfo(Path.Combine(_targetFilePath, item.StoreName)).Length, contentLocation = new Uri(new Uri(ServerUrl), $"_apis/pipelines/workflows/artifact/{id}?file={Uri.EscapeDataString(item.FileName)}").ToString() });
                }
                return await Ok(ret);
            } else {
                return GetFileFromContainer(id, itemPath);
            }
        }

        // Filename have to be in query, because the azure load balancer converts all %2f components to path seperator /
        [HttpGet("artifact/{id}")]
        [AllowAnonymous]
        public IActionResult GetFileFromContainer(int id, [FromQuery] string file) {
            var container = (from record in _context.ArtifactRecords where record.FileContainer.Id == id && (record.FileName.ToLower() == file.ToLower()) select record).FirstOrDefault();
            if(container == null) {
                throw new Exception($"container is null!, id='{id}', file='{file}'");
            }
            if(container.FileName?.Length > 0) {
                var lastPathSep = container.FileName.LastIndexOfAny(new [] { '/', '\\' }) + 1;
                var content = new ContentDisposition();
                content.DispositionType = DispositionTypeNames.Attachment;
                content.FileName = container.FileName.Substring(lastPathSep);
                Response.Headers.Add("Content-Disposition", content.ToString());
            }
            if(container.GZip) {
                Response.Headers.Add("Content-Encoding", "gzip");
            }
            return new FileStreamResult(System.IO.File.OpenRead(Path.Combine(_targetFilePath, container.StoreName)), "application/octet-stream") { EnableRangeProcessing = true };
        }

    }
}
