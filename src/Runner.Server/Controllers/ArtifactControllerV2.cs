using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Google.Protobuf;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Reflection;
using Google.Protobuf.Reflection;
using Microsoft.AspNetCore.Http.Extensions;
using Runner.Server.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using GitHub.Actions.Pipelines.WebApi;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Runner.Server.Controllers
{

    [ApiController]
    [Route("twirp/github.actions.results.api.v1.ArtifactService")]
    [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
    public class ArtifactControllerV2 : VssControllerBase{
        private readonly SqLiteDb _context;
        private readonly JsonFormatter formatter;

        public ArtifactControllerV2(SqLiteDb _context, IConfiguration configuration) : base(configuration)
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

        [HttpPost("CreateArtifact")]
        public async Task<string> CreateArtifact([FromBody, Protobuf] Github.Actions.Results.Api.V1.CreateArtifactRequest body) {
            var guid = Guid.Parse(body.WorkflowJobRunBackendId);
            var jobInfo = (from j in _context.Jobs where j.JobId == guid select new { j.runid, j.WorkflowRunAttempt.Attempt }).FirstOrDefault();
            var artifacts = new ArtifactController(_context, Configuration);
            var fname = $"{body.Name}.zip";
            var container = await artifacts.CreateContainer(jobInfo.runid, jobInfo.Attempt, new CreateActionsStorageArtifactParameters() { Name = body.Name }, jobInfo.Attempt);
            if(_context.Entry(container).Collection(c => c.Files).Query().Any()) {
                //var files = _context.Entry(container).Collection(c => c.Files).Query().ToList();
                // Duplicated Artifact of the same name in the same Attempt => fail
                return formatter.Format(new Github.Actions.Results.Api.V1.CreateArtifactResponse() {
                    Ok = false
                });
            }
            var record = new ArtifactRecord() {FileName = fname, StoreName = Path.GetRandomFileName(), GZip = false, FileContainer = container} ;
            _context.ArtifactRecords.Add(record);
            await _context.SaveChangesAsync();

            var resp = new Github.Actions.Results.Api.V1.CreateArtifactResponse
            {
                Ok = true,
                SignedUploadUrl = new Uri(new Uri(ServerUrl), $"twirp/github.actions.results.api.v1.ArtifactService/UploadArtifact?id={record.Id}&sig={CreateSignature(record.Id)}").ToString()
            };
            return formatter.Format(resp);
        }

        [HttpPut("UploadArtifact")]
        [AllowAnonymous]
        public async Task<IActionResult> UploadArtifact(int id, string sig, string comp = null, bool seal = false) {
            if(string.IsNullOrEmpty(sig) || !VerifySignature(id, sig)) {
                return NotFound();
            }
            if(comp == "block" || comp == "appendBlock" || comp == null) {
                var record = await _context.ArtifactRecords.FindAsync(id);
                var _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "artifacts");
                using(var targetStream = new FileStream(Path.Combine(_targetFilePath, record.StoreName), FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write, FileShare.Write)) {
                    await Request.Body.CopyToAsync(targetStream);
                }
                return Created(HttpContext.Request.GetEncodedUrl(), null);
            }
            if(comp == "blocklist") {
                return Created(HttpContext.Request.GetEncodedUrl(), null);
            }
            return Ok();
        }

        [HttpPost("FinalizeArtifact")]
        public string FinalizeArtifact([FromBody, Protobuf] Github.Actions.Results.Api.V1.FinalizeArtifactRequest body) {
            var attempt = long.Parse(User.FindFirst("attempt")?.Value ?? "-1");
            var artifactsMinAttempt = long.Parse(User.FindFirst("artifactsMinAttempt")?.Value ?? "-1");
            var runid = long.Parse(body.WorkflowRunBackendId);

            var container = (from fileContainer in _context.ArtifactFileContainer where (fileContainer.Container.Attempt.Attempt >= artifactsMinAttempt || artifactsMinAttempt == -1) && (fileContainer.Container.Attempt.Attempt <= attempt || attempt == -1) && fileContainer.Container.Attempt.WorkflowRun.Id == runid && fileContainer.Files.Count == 1  && body.Name.ToLower() == fileContainer.Name.ToLower() orderby fileContainer.Container.Attempt.Attempt descending select fileContainer).First();
            container.Size = body.Size;
            var resp = new Github.Actions.Results.Api.V1.FinalizeArtifactResponse
            {
                Ok = true,
                ArtifactId = container.Id
            };
            return formatter.Format(resp);
        }

        [HttpPost("ListArtifacts")]
        public string ListArtifacts([FromBody, Protobuf] Github.Actions.Results.Api.V1.ListArtifactsRequest body) {
            var resp = new Github.Actions.Results.Api.V1.ListArtifactsResponse();

            var attempt = long.Parse(User.FindFirst("attempt")?.Value ?? "-1");
            var artifactsMinAttempt = long.Parse(User.FindFirst("artifactsMinAttempt")?.Value ?? "-1");
            var runid = long.Parse(body.WorkflowRunBackendId);
            resp.Artifacts.AddRange(from fileContainer in _context.ArtifactFileContainer where (fileContainer.Container.Attempt.Attempt >= artifactsMinAttempt || artifactsMinAttempt == -1) && (fileContainer.Container.Attempt.Attempt <= attempt || attempt == -1) && fileContainer.Container.Attempt.WorkflowRun.Id == runid && fileContainer.Files.Count == 1 && !fileContainer.Files.FirstOrDefault().FileName.Contains('/') && fileContainer.Files.FirstOrDefault().FileName.EndsWith(".zip") && (body.IdFilter == null || body.IdFilter == fileContainer.Id)  && (body.NameFilter == null || body.NameFilter.ToLower() == fileContainer.Name.ToLower()) orderby fileContainer.Container.Attempt.Attempt descending select new Github.Actions.Results.Api.V1.ListArtifactsResponse_MonolithArtifact
            {
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTimeOffset(System.DateTimeOffset.UtcNow),
                DatabaseId = fileContainer.Id,
                Name = fileContainer.Name,
                Size = fileContainer.Size ?? 0,
                WorkflowRunBackendId = body.WorkflowRunBackendId,
                WorkflowJobRunBackendId = body.WorkflowJobRunBackendId
            });
            return formatter.Format(resp);
        }

        [HttpPost("GetSignedArtifactURL")]
        public async Task<string> GetSignedArtifactURL([FromBody, Protobuf] Github.Actions.Results.Api.V1.GetSignedArtifactURLRequest body) {
            var attempt = long.Parse(User.FindFirst("attempt")?.Value ?? "-1");
            var artifactsMinAttempt = long.Parse(User.FindFirst("artifactsMinAttempt")?.Value ?? "-1");
            var runid = long.Parse(body.WorkflowRunBackendId);
            var file = await (from fileContainer in _context.ArtifactFileContainer where (fileContainer.Container.Attempt.Attempt >= artifactsMinAttempt || artifactsMinAttempt == -1) && (fileContainer.Container.Attempt.Attempt <= attempt || attempt == -1) && fileContainer.Container.Attempt.WorkflowRun.Id == runid && fileContainer.Files.Count == 1 && fileContainer.Files.FirstOrDefault().FileName.ToLower() == $"{body.Name}.zip".ToLower() orderby fileContainer.Container.Attempt.Attempt descending select fileContainer.Files.FirstOrDefault()).FirstAsync();
            var resp = new Github.Actions.Results.Api.V1.GetSignedArtifactURLResponse
            {
                SignedUrl = new Uri(new Uri(ServerUrl), $"twirp/github.actions.results.api.v1.ArtifactService/DownloadArtifact?id={file.Id}&sig={CreateSignature(file.Id)}").ToString()
            };
            return formatter.Format(resp);
        }

        [AllowAnonymous]
        [HttpGet("DownloadArtifact")]
        public IActionResult DownloadArtifact(int id, string sig) {
            if(string.IsNullOrEmpty(sig) || !VerifySignature(id, sig)) {
                return NotFound();
            }
            var container = _context.ArtifactRecords.Find(id);
            var _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "artifacts");
            return new FileStreamResult(System.IO.File.OpenRead(Path.Combine(_targetFilePath, container.StoreName)), "application/octet-stream") { EnableRangeProcessing = true };
        }

        [AllowAnonymous]
        [HttpPost("DeleteArtifact")]
        public async Task<IActionResult> DeleteArtifact([FromBody, Protobuf] Github.Actions.Results.Api.V1.DeleteArtifactRequest body) {
            var attempt = long.Parse(User.FindFirst("attempt")?.Value ?? "-1");
            var artifactsMinAttempt = long.Parse(User.FindFirst("artifactsMinAttempt")?.Value ?? "-1");
            var runid = long.Parse(body.WorkflowRunBackendId);
            var res = await (from fileContainer in _context.ArtifactFileContainer where (fileContainer.Container.Attempt.Attempt >= artifactsMinAttempt || artifactsMinAttempt == -1) && (fileContainer.Container.Attempt.Attempt <= attempt || attempt == -1) && fileContainer.Container.Attempt.WorkflowRun.Id == runid && fileContainer.Files.Count == 1 && fileContainer.Files.FirstOrDefault().FileName.ToLower() == $"{body.Name}.zip".ToLower() orderby fileContainer.Container.Attempt.Attempt descending select new { fileContainer, file = fileContainer.Files.FirstOrDefault(), fileContainer.Container }).FirstAsync();
            if(res.file == null || res.fileContainer == null || res.Container == null) {
                return NotFound();
            }
            _context.ArtifactRecords.Remove(res.file);
            _context.ArtifactFileContainer.Remove(res.fileContainer);
            var _targetFilePath = Path.Combine(GitHub.Runner.Sdk.GharunUtil.GetLocalStorage(), "artifacts");
            System.IO.File.Delete(Path.Combine(_targetFilePath, res.file.StoreName));
            await _context.SaveChangesAsync();

            var resp = new Github.Actions.Results.Api.V1.DeleteArtifactResponse {
                Ok = true,
                ArtifactId = res?.fileContainer?.Id ?? 0
            };
            return new OkObjectResult(formatter.Format(resp));
        }
    }
}
