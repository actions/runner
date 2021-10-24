
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;

namespace Runner.Server.Controllers {

    [ApiController]
    [Route("{owner}/{repo}/_apis/artifactcache")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
    public class CacheController : VssControllerBase{
        
        private string _targetFilePath;
    
        private class ArtifactCacheEntry {
            public string cacheKey {get;set;}
            public string scope {get;set;}
            public string creationTime {get;set;}
            public string archiveLocation {get;set;}
        }

        private class ReserveCacheResponse {
            public int cacheId {get;set;}
        }

        private class ReserveCacheRequest {
            public string key {get;set;}
            public string version  {get;set;}
        }

        private static ConcurrentDictionary<string, ConcurrentDictionary<string, string>> cache = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

        private IMemoryCache _cache;

        private static int id;

        public CacheController(IMemoryCache memoryCache, IWebHostEnvironment environment)
        {
            _cache = memoryCache;
            _targetFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "gharun", "cache");
            Directory.CreateDirectory(_targetFilePath);
        }

        [HttpPost("caches")]
        public async Task<FileStreamResult> ReserveCache(string owner, string repo) {
            var req = await FromBody<ReserveCacheRequest>();
            int _id = Interlocked.Increment(ref id);
            var filename = Path.GetRandomFileName();
            _cache.Set("Cache_" + _id, filename);
            var repocache = cache.GetOrAdd($"{owner}/{repo}", k => new ConcurrentDictionary<string, string>());
            repocache.AddOrUpdate(req.key, (a) => { return filename; }, (a, b) => {
                System.IO.File.Delete(System.IO.Path.Combine(_targetFilePath, b));
                return filename;
            });
            return await Ok(new ReserveCacheResponse { cacheId = _id });
        }

        [HttpGet("cache")]
        public async Task<ActionResult>/* IActionResult */ GetCacheEntry( string owner, string repo, [FromQuery] string keys, [FromQuery] string version) {
            var a = keys.Split(',');
            string val;
            var repocache = cache.GetOrAdd($"{owner}/{repo}", k => new ConcurrentDictionary<string, string>());
            if(repocache.TryGetValue(a[0], out val)) {
                return await Ok(new ArtifactCacheEntry{ cacheKey = a[0], scope = "*", creationTime = DateTime.UtcNow.ToLongDateString(), archiveLocation = $"{Request.Scheme}://{Request.Host.Host ?? (HttpContext.Connection.RemoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? ("[" + HttpContext.Connection.LocalIpAddress.ToString() + "]") : HttpContext.Connection.LocalIpAddress.ToString())}:{Request.Host.Port ?? (Request.Host.Host != null ? 80 : HttpContext.Connection.LocalPort)}/runner/host/_apis/artifactcache/get/{Uri.EscapeDataString(val)}" });
            } else {
                var b = repocache.ToArray();
                foreach (var item in a) {
                    var res = (from c in b where item.StartsWith(c.Key) select c).FirstOrDefault();
                    if(res.Value != null) {
                        return await Ok(new ArtifactCacheEntry{ cacheKey = res.Key, scope = "*", creationTime = DateTime.UtcNow.ToLongDateString(), archiveLocation = $"{Request.Scheme}://{Request.Host.Host ?? (HttpContext.Connection.RemoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? ("[" + HttpContext.Connection.LocalIpAddress.ToString() + "]") : HttpContext.Connection.LocalIpAddress.ToString())}:{Request.Host.Port ?? (Request.Host.Host != null ? 80 : HttpContext.Connection.LocalPort)}/runner/host/_apis/artifactcache/get/{Uri.EscapeDataString(res.Value)}" });
                    }
                }
            }
            return NoContent();
        }

        [HttpGet("get/{filename}")]
        [AllowAnonymous]
        public IActionResult GetCacheEntry(string filename) {
            if(!new System.Text.RegularExpressions.Regex("(\\.?[^\\.\\\\/])+").IsMatch(filename)) {
                return NotFound();
            }
            return new FileStreamResult(System.IO.File.OpenRead(System.IO.Path.Combine(_targetFilePath, filename)), "application/octet-stream") { EnableRangeProcessing = true };
        }

        [HttpPatch("caches/{cacheId}")]
        public async Task<IActionResult> PatchCache(int cacheId) {
            var range = Request.Headers["Content-Range"].ToArray()[0];
            int i = range.IndexOf('-');
            int j = range.IndexOf('/');
            var start = Convert.ToInt64(range.Substring(6, i - 6));
            var end = Convert.ToInt64(range.Substring(i + 1, j - (i + 1)));
            var trustedFileNameForFileStorage = _cache.Get<string>("Cache_" + cacheId);
            using(var targetStream = new FileStream(Path.Combine(_targetFilePath, trustedFileNameForFileStorage), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write)) {
                targetStream.Seek(start, SeekOrigin.Begin);
                await Request.Body.CopyToAsync(targetStream);
            }
            
            // Content-Range, bytes ${start}-${end}
            return Ok();
        }

        [HttpPost("caches/{cacheId}")]
        public IActionResult CommitCache(int cacheId) {
            _cache.Remove("Cache_" + cacheId);
            return Ok();
        }
    }
}