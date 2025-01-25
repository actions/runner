using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Google.Protobuf;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Http.Extensions;
using Runner.Server.Models;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Xml.Linq;

namespace Runner.Server.Controllers
{

    [ApiController]
    [Route("twirp/github.actions.results.api.v1.CacheService")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
    public class CacheControllerV2 : VssControllerBase{
        private readonly SqLiteDb _context;
        private readonly JsonFormatter formatter;

        public CacheControllerV2(SqLiteDb _context, IConfiguration configuration) : base(configuration)
        {
            this._context = _context;
            formatter = new JsonFormatter(JsonFormatter.Settings.Default.WithIndentation().WithPreserveProtoFieldNames(true).WithFormatDefaultValues(false));
        }

        private string CreateSignature(int id) {
            using var rsa = RSA.Create(Startup.AccessTokenParameter);
            return Base64UrlEncoder.Encode(rsa.SignData(Encoding.UTF8.GetBytes(id.ToString()), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        }

        private bool VerifySignature(int id, string sig) {
            using var rsa = RSA.Create(Startup.AccessTokenParameter);
            return rsa.VerifyData(Encoding.UTF8.GetBytes(id.ToString()), Base64UrlEncoder.DecodeBytes(sig), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        [HttpPost("CreateCacheEntry")]
        public async Task<string> CreateCacheEntry([FromBody, Protobuf] Github.Actions.Results.Api.V1.CreateCacheEntryRequest body) {
            var filename = Path.GetRandomFileName();
            var reference = User.FindFirst("ref")?.Value ?? "refs/heads/main";
            var repository = User.FindFirst("repository")?.Value ?? "Unknown/Unknown";
            var record = new CacheRecord() { Key = body.Key, LastUpdated = DateTime.Now, Ref = reference, Version = body.Version, Storage = filename, Repo = repository };
            _context.Caches.Add(record);
            await _context.SaveChangesAsync();
            var resp = new Github.Actions.Results.Api.V1.CreateCacheEntryResponse
            {
                Ok = true,
                SignedUploadUrl = new Uri(new Uri(ServerUrl), $"twirp/github.actions.results.api.v1.CacheService/UploadCache?id={record.Id}&sig={CreateSignature(record.Id)}").ToString()
            };
            return formatter.Format(resp);
        }

        [HttpPut("UploadCache")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadCache(int id, string sig, string comp = null, bool seal = false, string blockid = null) {
            if(string.IsNullOrEmpty(sig) || !VerifySignature(id, sig)) {
                return NotFound();
            }
            if(comp == "block" || comp == "appendBlock" || comp == null) {
                var record = await _context.Caches.FindAsync(id);
                var _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "cache");
                Directory.CreateDirectory(_targetFilePath);
                using(var targetStream = new FileStream(Path.Combine(_targetFilePath, string.IsNullOrWhiteSpace(blockid) ? record.Storage : $"{record.Storage}-{System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(blockid))}"), FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write, FileShare.Write)) {
                    await Request.Body.CopyToAsync(targetStream);
                }
                return Created(HttpContext.Request.GetEncodedUrl(), null);
            }
            if(comp == "blocklist") {
                XElement blockList = await XElement.LoadAsync(Request.Body, LoadOptions.None, Request.HttpContext.RequestAborted);
                var record = await _context.Caches.FindAsync(id);
                var _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "cache");
                Directory.CreateDirectory(_targetFilePath);
                using(var targetStream = new FileStream(Path.Combine(_targetFilePath, record.Storage), FileMode.Create, FileAccess.Write, FileShare.Write))
                foreach(var block in from item in blockList.Descendants("Latest") select item.Value) {
                    var filename = $"{record.Storage}-{System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(block))}";
                    using(var sourceStream = new FileStream(Path.Combine(_targetFilePath, filename), FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        await sourceStream.CopyToAsync(targetStream);
                    }
                    System.IO.File.Delete(Path.Combine(_targetFilePath, filename));
                }
                return Created(HttpContext.Request.GetEncodedUrl(), null);
            }
            return Ok();
        }

        [HttpPost("FinalizeCacheEntryUpload")]
        public string FinalizeCacheEntryUpload([FromBody, Protobuf] Github.Actions.Results.Api.V1.FinalizeCacheEntryUploadRequest body) {
            var record = _context.Caches.First(c => c.Key == body.Key && c.Version == body.Version);
            var resp = new Github.Actions.Results.Api.V1.FinalizeCacheEntryUploadResponse
            {
                Ok = true,
                EntryId = record.Id
            };
            return formatter.Format(resp);
        }

        [HttpPost("GetCacheEntryDownloadURL")]
        public string GetCacheEntryDownloadURL([FromBody, Protobuf] Github.Actions.Results.Api.V1.GetCacheEntryDownloadURLRequest body) {
            var a = body.RestoreKeys.Prepend(body.Key).ToArray();
            var version = body.Version;
            var defaultRef = User.FindFirst("defaultRef")?.Value ?? "refs/heads/main";
            var reference = User.FindFirst("ref")?.Value ?? "refs/heads/main";
            var repository = User.FindFirst("repository")?.Value ?? "Unknown/Unknown";
            foreach(var cref in reference != defaultRef ? new [] { reference, defaultRef } : new [] { reference }) {
                foreach (var item in a) {
                    var record = (from rec in _context.Caches where rec.Repo.ToLower() == repository.ToLower() && rec.Ref == cref && rec.Key.ToLower() == item.ToLower() && (rec.Version == null || rec.Version == "" || rec.Version == version) orderby rec.LastUpdated descending select rec).FirstOrDefault()
                        ?? (from rec in _context.Caches where rec.Repo.ToLower() == repository.ToLower() && rec.Ref == cref && rec.Key.ToLower().StartsWith(item.ToLower()) && (rec.Version == null || rec.Version == "" || rec.Version == version) orderby rec.LastUpdated descending select rec).FirstOrDefault();
                    if(record != null) {
                        var resp = new Github.Actions.Results.Api.V1.GetCacheEntryDownloadURLResponse
                        {
                            Ok = true,
                            MatchedKey = record.Key,
                            SignedDownloadUrl = new Uri(new Uri(ServerUrl), $"twirp/github.actions.results.api.v1.CacheService/DownloadCache?id={record.Id}&sig={CreateSignature(record.Id)}").ToString()
                        };
                        return formatter.Format(resp);
                    }
                }
            }
            return formatter.Format(new Github.Actions.Results.Api.V1.GetCacheEntryDownloadURLResponse { Ok = false });
        }

        [AllowAnonymous]
        [HttpGet("DownloadCache")]
        public IActionResult DownloadCache(int id, string sig) {
            if(string.IsNullOrEmpty(sig) || !VerifySignature(id, sig)) {
                return NotFound();
            }
            var container = _context.Caches.Find(id);
            var _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "cache");
            return new FileStreamResult(System.IO.File.OpenRead(Path.Combine(_targetFilePath, container.Storage)), "application/octet-stream") { EnableRangeProcessing = true };
        }
    }
}
