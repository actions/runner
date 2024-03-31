
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
using Swashbuckle.AspNetCore.Annotations;

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

        public class ReserveCacheRequest {
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
        [SwaggerResponse(200, type: typeof(ReserveCacheResponse))]
        public async Task<FileStreamResult> ReserveCache(string owner, string repo, [FromBody, Vss] ReserveCacheRequest req) {
            var filename = Path.GetRandomFileName();
            var reference = User.FindFirst("ref")?.Value ?? "refs/heads/main";
            var repository = User.FindFirst("repository")?.Value ?? "Unknown/Unknown";
            var record = new CacheRecord() { Key = req.key, LastUpdated = DateTime.Now, Ref = reference, Version = req.version, Storage = filename, Repo = repository };
            _context.Caches.Add(record);
            await _context.SaveChangesAsync();
            return await Ok(new ReserveCacheResponse { cacheId = record.Id });
        }

        [HttpGet("cache")]
        [SwaggerResponse(200, type: typeof(ArtifactCacheEntry))]
        public async Task<IActionResult> GetCacheEntry( string owner, string repo, [FromQuery] string keys, [FromQuery] string version) {
            var a = keys.Split(',');
            var defaultRef = User.FindFirst("defaultRef")?.Value ?? "refs/heads/main";
            var reference = User.FindFirst("ref")?.Value ?? "refs/heads/main";
            var repository = User.FindFirst("repository")?.Value ?? "Unknown/Unknown";
            foreach(var cref in reference != defaultRef ? new [] { reference, defaultRef } : new [] { reference }) {
                foreach (var item in a) {
                    var record = (from rec in _context.Caches where rec.Repo.ToLower() == repository.ToLower() && rec.Ref == cref && rec.Key.ToLower() == item.ToLower() && (rec.Version == null || rec.Version == "" || rec.Version == version) orderby rec.LastUpdated descending select rec).FirstOrDefault()
                        ?? (from rec in _context.Caches where rec.Repo.ToLower() == repository.ToLower() && rec.Ref == cref && rec.Key.ToLower().StartsWith(item.ToLower()) && (rec.Version == null || rec.Version == "" || rec.Version == version) orderby rec.LastUpdated descending select rec).FirstOrDefault();
                    if(record != null) {
                        return await Ok(new ArtifactCacheEntry{ cacheKey = record.Key, scope = cref, creationTime = record.LastUpdated.ToLongDateString(), archiveLocation = new Uri(new Uri(ServerUrl), $"_apis/artifactcache/get/{record.Id}").ToString() });
                    }
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
