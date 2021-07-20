
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

namespace Runner.Server.Controllers {

    [ApiController]
    [Route("{owner}/{repo}/_apis/pipelines/workflows/{run}/artifacts")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ArtifactController : VssControllerBase{

        private struct ArtifactRecord {
            public string FileName {get;set;}
            public bool GZip {get;set;}
        }

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

        private IMemoryCache _cache;

        public ArtifactController(IMemoryCache memoryCache, IWebHostEnvironment environment)
        {
            _cache = memoryCache;
            _targetFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "gharun", "artifacts");
            Directory.CreateDirectory(_targetFilePath);
        }

        [HttpPost]
        public async Task<FileStreamResult> CreateContainer(int run) {
            var req = await FromBody<CreateContainerRequest>();
            var c = _cache.GetOrCreate("artifact_run_" + run, e => new ConcurrentDictionary<string, ConcurrentDictionary<string, ArtifactRecord>>());
            c.AddOrUpdate(req.Name, s => new ConcurrentDictionary<string, ArtifactRecord>(), (a,b) => {
                return b;
            });
            return await Ok(new ArtifactResponse { name = req.Name, fileContainerResourceUrl = $"{Request.Scheme}://{Request.Host.Host ?? (HttpContext.Connection.RemoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? ("[" + HttpContext.Connection.LocalIpAddress.ToString() + "]") : HttpContext.Connection.LocalIpAddress.ToString())}:{Request.Host.Port ?? (Request.Host.Host != null ? 80 : HttpContext.Connection.LocalPort)}/runner/host/_apis/pipelines/workflows/{run}/artifacts/container/{req.Name}" } );
        }

        [HttpPatch]
        public async Task<FileStreamResult> PatchContainer(int run, [FromQuery] string artifactName) {
            var req = await FromBody<CreateContainerRequest>();
            // This sends the size of the artifact container
            return await Ok(new ArtifactResponse { name = artifactName, fileContainerResourceUrl = $"{Request.Scheme}://{Request.Host.Host ?? (HttpContext.Connection.RemoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? ("[" + HttpContext.Connection.LocalIpAddress.ToString() + "]") : HttpContext.Connection.LocalIpAddress.ToString())}:{Request.Host.Port ?? (Request.Host.Host != null ? 80 : HttpContext.Connection.LocalPort)}/runner/host/_apis/pipelines/workflows/{run}/artifacts/container/{artifactName}" } );
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetContainer(int run) {
            var c = _cache.Get<ConcurrentDictionary<string, ConcurrentDictionary<string, ArtifactRecord>>>("artifact_run_" + run);
            if(c == null) {
                return NotFound();
            }
            return await Ok(from e in c select new ArtifactResponse{ name = e.Key, fileContainerResourceUrl = $"{Request.Scheme}://{Request.Host.Host ?? (HttpContext.Connection.RemoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? ("[" + HttpContext.Connection.LocalIpAddress.ToString() + "]") : HttpContext.Connection.LocalIpAddress.ToString())}:{Request.Host.Port ?? (Request.Host.Host != null ? 80 : HttpContext.Connection.LocalPort)}/runner/host/_apis/pipelines/workflows/{run}/artifacts/container/{e.Key}" } );
        }

        [HttpPut("container/{containername}")]
        public async Task<IActionResult> UploadToContainer(int run, string containername, [FromQuery] string itemPath) {
            var c = _cache.Get<ConcurrentDictionary<string, ConcurrentDictionary<string, ArtifactRecord>>>("artifact_run_" + run);
            ConcurrentDictionary<string, ArtifactRecord> val;
            if(c == null || !c.TryGetValue(containername, out val)) {
                return NotFound();
            }
            var range = Request.Headers["Content-Range"].ToArray()[0];
            int i = range.IndexOf('-');
            int j = range.IndexOf('/');
            var start = Convert.ToInt64(range.Substring(6, i - 6));
            var end = Convert.ToInt64(range.Substring(i + 1, j - (i + 1)));
            var trustedFileNameForFileStorage = val.GetOrAdd(itemPath, s => new ArtifactRecord { FileName = Path.GetRandomFileName(), GZip = Request.Headers.TryGetValue("Content-Encoding", out StringValues v) && v.Contains("gzip") });
            using(var targetStream = new FileStream(Path.Combine(_targetFilePath, trustedFileNameForFileStorage.FileName), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write)) {
                targetStream.Seek(start, SeekOrigin.Begin);
                await Request.Body.CopyToAsync(targetStream);
            }
            return Ok();
        }

        [HttpGet("container/{containername}")]
        [AllowAnonymous]
        public async Task<ActionResult> GetFilesFromContainer(int run, string containername, [FromQuery] string itemPath) {
            var c = _cache.Get<ConcurrentDictionary<string, ConcurrentDictionary<string, ArtifactRecord>>>("artifact_run_" + run);
            ConcurrentDictionary<string, ArtifactRecord> val;
            if(c == null || !c.TryGetValue(containername, out val)) {
                return NotFound();
            }
            var ret = new List<DownloadInfo>();
            foreach (var item in val) {
                var builder = new Microsoft.AspNetCore.Http.Extensions.QueryBuilder();
                builder.Add("filename", Path.GetFileName(item.Key));
                builder.Add("gzip", item.Value.GZip.ToString());
                ret.Add(new DownloadInfo { path = item.Key, itemType = "file", fileLength = 1 /* TODO do we need the real filesize? this works for now */, contentLocation = $"{Request.Scheme}://{Request.Host.Host ?? (HttpContext.Connection.RemoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? ("[" + HttpContext.Connection.LocalIpAddress.ToString() + "]") : HttpContext.Connection.LocalIpAddress.ToString())}:{Request.Host.Port ?? (Request.Host.Host != null ? 80 : HttpContext.Connection.LocalPort)}/runner/host/_apis/pipelines/workflows/{run}/artifacts/artifact/{containername}/{item.Value.FileName}{builder.ToString()}"});
            }
            return await Ok(ret);
        }

        [HttpGet("artifact/{containername}/{file}")]
        [AllowAnonymous]
        public FileStreamResult GetFileFromContainer(int run, string containername, string file, [FromQuery] string filename, [FromQuery] bool gzip) {
            if(filename?.Length > 0) {
                Response.Headers.Add("Content-Disposition", $"attachment; filename={filename}");
            }
            if(gzip) {
                Response.Headers.Add("Content-Encoding", "gzip");
            }
            return new FileStreamResult(System.IO.File.OpenRead(Path.Combine(_targetFilePath, file)), "application/octet-stream") { EnableRangeProcessing = true };
        }

    }
}
