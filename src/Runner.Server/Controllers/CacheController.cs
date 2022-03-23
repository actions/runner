
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
using Microsoft.Extensions.Configuration;
using Runner.Server.Models;

namespace Runner.Server.Controllers {

    [ApiController]
    [Route("_apis/artifactcache")]
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
        private SqLiteDb _context;
        public CacheController(SqLiteDb context, IWebHostEnvironment environment, IConfiguration configuration) : base(configuration)
        {
            _context = context;
            _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "cache");
            Directory.CreateDirectory(_targetFilePath);
        }

        [HttpPost("caches")]
        public async Task<FileStreamResult> ReserveCache(string owner, string repo) {
            var req = await FromBody<ReserveCacheRequest>();
            var filename = Path.GetRandomFileName();
            var reference = User.FindFirst("ref");
            var repository = User.FindFirst("repository");
            var record = new CacheRecord() { Key = req.key, LastUpdated = DateTime.Now, Ref = reference.Value, Storage = filename, Repo = repository.Value };
            _context.Caches.Add(record);
            await _context.SaveChangesAsync();
            return await Ok(new ReserveCacheResponse { cacheId = record.Id });
        }

        [HttpGet("cache")]
        public async Task<IActionResult> GetCacheEntry( string owner, string repo, [FromQuery] string keys, [FromQuery] string version) {
            var a = keys.Split(',');
            var defaultRef = User.FindFirst("defaultRef");
            var reference = User.FindFirst("ref");
            var repository = User.FindFirst("repository");
            foreach(var cref in reference.Value != defaultRef.Value ? new [] { reference.Value, defaultRef.Value } : new [] { reference.Value }) {
                foreach (var item in a) {
                    var record = (from rec in _context.Caches where rec.Repo == repository.Value && rec.Ref == cref && rec.Key == item orderby rec.LastUpdated descending select rec).FirstOrDefault();
                    if(record != null) {
                        return await Ok(new ArtifactCacheEntry{ cacheKey = item, scope = cref, creationTime = record.LastUpdated.ToLongDateString(), archiveLocation = new Uri(new Uri(ServerUrl), $"_apis/artifactcache/get/{record.Id}").ToString() });
                    }
                }
                CacheRecord partialMatch = null;
                foreach (var item in a.Skip(1)) {
                    var record = (from rec in _context.Caches where rec.Repo == repository.Value && rec.Ref == cref && rec.Key.StartsWith(item) orderby rec.LastUpdated descending select rec).FirstOrDefault();
                    if(record != null && (partialMatch == null || record.LastUpdated > partialMatch.LastUpdated)) {
                        partialMatch = record;
                    }
                }
                if(partialMatch != null) {
                    return await Ok(new ArtifactCacheEntry{ cacheKey = partialMatch.Key, scope = cref, creationTime = partialMatch.LastUpdated.ToLongDateString(), archiveLocation = new Uri(new Uri(ServerUrl), $"_apis/artifactcache/get/{partialMatch.Id}").ToString() });
                }
            }
            return NoContent();
        }

        [HttpGet("get/{cacheId}")]
        [Produces("application/octet-stream", Type = typeof(IActionResult))]
        [AllowAnonymous]
        public IActionResult GetCacheEntry(int cacheId) {
            var record = _context.Caches.Find(cacheId);
            return new FileStreamResult(System.IO.File.OpenRead(System.IO.Path.Combine(_targetFilePath, record.Storage)), "application/octet-stream") { EnableRangeProcessing = true };
        }

        [HttpPatch("caches/{cacheId}")]
        public async Task<IActionResult> PatchCache(int cacheId) {
            var range = Request.Headers["Content-Range"].ToArray()[0];
            int i = range.IndexOf('-');
            int j = range.IndexOf('/');
            var start = Convert.ToInt64(range.Substring(6, i - 6));
            var end = Convert.ToInt64(range.Substring(i + 1, j - (i + 1)));
            var record = _context.Caches.Find(cacheId);
            using(var targetStream = new FileStream(Path.Combine(_targetFilePath, record.Storage), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write)) {
                targetStream.Seek(start, SeekOrigin.Begin);
                await Request.Body.CopyToAsync(targetStream);
            }
            
            // Content-Range, bytes ${start}-${end}
            return Ok();
        }

        [HttpPost("caches/{cacheId}")]
        public IActionResult CommitCache(int cacheId) {
            return Ok();
        }
    }
}