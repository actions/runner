using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.ObjectTemplating;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Logging;
using GitHub.DistributedTask.Pipelines.ContextData;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Net.Http;
using System.Runtime.Serialization;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using GitHub.DistributedTask.Expressions2.Sdk;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using Runner.Server.Models;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GitHub.Runner.Sdk;
using GitHub.Actions.Pipelines.WebApi;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class MessageController : VssControllerBase
    {
        private string GitServerUrl;
        private string GitApiServerUrl;
        private string GitGraphQlServerUrl;
        private IMemoryCache _cache;
        private SqLiteDb _context;
        private string GITHUB_TOKEN;
        private string WebhookHMACAlgorithmName { get; }
        private string WebhookSignatureHeader { get; }
        private string WebhookSignaturePrefix { get; }
        private string WebhookSecret { get; }
        private bool AllowPullRequests { get; }
        private bool NoRecursiveNeedsCtx { get; }
        private bool QueueJobsWithoutRunner { get; }
        private bool WriteAccessForPullRequestsFromForks { get; }
        private bool AllowJobNameOnJobProperties { get; }
        private bool HasPullRequestMergePseudoBranch { get; }
        private string GitHubAppPrivateKeyFile { get; }
        private int GitHubAppId { get; }
        private List<Secret> secrets;

        private IConfiguration configuration;

        private MessageController Clone() {
            return new MessageController(configuration, _cache, new SqLiteDb(_context.Options));
        }

        private class Secret {
            public string Name {get;set;}
            public string Value {get;set;}
        }

        public MessageController(IConfiguration configuration, IMemoryCache memoryCache, SqLiteDb context)
        {
            this.configuration = configuration;
            GitServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitServerUrl") ?? "";
            GitApiServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitApiServerUrl") ?? "";
            GitGraphQlServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitGraphQlServerUrl") ?? "";
            GITHUB_TOKEN = configuration.GetSection("Runner.Server")?.GetValue<String>("GITHUB_TOKEN") ?? "";
            WebhookHMACAlgorithmName = configuration.GetSection("Runner.Server")?.GetValue<String>("WebhookHMACAlgorithmName") ?? "";
            WebhookSignatureHeader = configuration.GetSection("Runner.Server")?.GetValue<String>("WebhookSignatureHeader") ?? "";
            WebhookSignaturePrefix = configuration.GetSection("Runner.Server")?.GetValue<String>("WebhookSignaturePrefix") ?? "";
            WebhookSecret = configuration.GetSection("Runner.Server")?.GetValue<String>("WebhookSecret") ?? "";
            AllowPullRequests = configuration.GetSection("Runner.Server")?.GetValue<bool>("AllowPullRequests") ?? false;
            NoRecursiveNeedsCtx = configuration.GetSection("Runner.Server")?.GetValue<bool>("NoRecursiveNeedsCtx") ?? false;
            QueueJobsWithoutRunner = configuration.GetSection("Runner.Server")?.GetValue<bool>("QueueJobsWithoutRunner") ?? false;
            WriteAccessForPullRequestsFromForks = configuration.GetSection("Runner.Server")?.GetValue<bool>("WriteAccessForPullRequestsFromForks") ?? false;
            AllowJobNameOnJobProperties = configuration.GetSection("Runner.Server")?.GetValue<bool>("AllowJobNameOnJobProperties") ?? false;
            HasPullRequestMergePseudoBranch = configuration.GetSection("Runner.Server")?.GetValue<bool>("HasPullRequestMergePseudoBranch") ?? false;
            GitHubAppPrivateKeyFile = configuration.GetSection("Runner.Server")?.GetValue<string>("GitHubAppPrivateKeyFile") ?? "";
            GitHubAppId = configuration.GetSection("Runner.Server")?.GetValue<int>("GitHubAppId") ?? 0;
            
            secrets = configuration.GetSection("Runner.Server:Secrets")?.Get<List<Secret>>() ?? new List<Secret>();
            _cache = memoryCache;
            _context = context;
            ReadConfig(configuration);
        }

        [HttpDelete("{poolId}/{messageId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "Agent")]
        public IActionResult DeleteMessage(int poolId, long messageId, Guid sessionId)
        {
            Session session;
            if(_cache.TryGetValue(sessionId, out session) && session.TaskAgentSession.SessionId == sessionId) {
                session.MessageLock.Wait(50000);
                session.Timer.Stop();
                session.Timer.Start();
                session.DropMessage = null;
                session.MessageLock.Release();
                return Ok();
            } else {
                return NotFound();
            }
        }

        class Equality : IEqualityComparer<TemplateToken>
        {
            public bool Equals(TemplateToken x, TemplateToken y)
            {
                return TemplateTokenEqual(x, y);
            }

            public int GetHashCode([DisallowNull] TemplateToken obj)
            {
                throw new NotImplementedException();
            }
        }
        private static bool TemplateTokenEqual(TemplateToken token, TemplateToken other) {
            if (token.Type != other.Type) {
                return false;
            } else {
                switch(token.Type) {
                    case TokenType.Mapping:
                    var mapping = token as MappingToken;
                    var othermapping = other as MappingToken;
                    if(mapping.Count != othermapping.Count) {
                        return false;
                    }
                    Dictionary<string, TemplateToken> dictionary = new Dictionary<string, TemplateToken>();
                    if (mapping.Count > 0)
                    {
                        foreach (var pair in mapping)
                        {
                            var keyLiteral = pair.Key.AssertString("dictionary context data key");
                            var key = keyLiteral.Value;
                            var value = pair.Value;
                            dictionary.Add(key, value);
                        }
                        foreach (var pair in othermapping)
                        {
                            var keyLiteral = pair.Key.AssertString("dictionary context data key");
                            var key = keyLiteral.Value;
                            var value = pair.Value;
                            TemplateToken otherv;
                            if(!dictionary.TryGetValue(key, out otherv) || !TemplateTokenEqual(value, otherv)) {
                                return false;
                            }
                        }
                    }
                    return true;

                case TokenType.Sequence:
                    var sequence = token as SequenceToken;
                    var otherseq = other as SequenceToken;
                    if(sequence.Count != otherseq.Count) {
                        return false;
                    }
                    
                    return sequence.SequenceEqual(otherseq, new Equality());

                case TokenType.Null:
                    return true;

                case TokenType.Boolean:
                    return (token as BooleanToken).Value == (other as BooleanToken).Value;

                case TokenType.Number:
                    return (token as NumberToken).Value == (other as NumberToken).Value;

                case TokenType.String:
                    return (token as StringToken).Value == (other as StringToken).Value;

                default:
                    throw new NotSupportedException($"Unexpected {nameof(TemplateToken)} type '{token.Type}'");
                }
            }
        }

        public sealed class AlwaysFunction : Function
        {
            protected override Object EvaluateCore(EvaluationContext context, out ResultMemory resultMemory)
            {
                resultMemory = null;
                return true;
            }
        }

        public sealed class SuccessFunction : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var templateContext = evaluationContext.State as TemplateContext;
                var executionContext = templateContext.State[nameof(ExecutionContext)] as ExecutionContext;
                if(Parameters?.Any() ?? false) {
                    foreach(var parameter in Parameters) {
                        var s = parameter.Evaluate(evaluationContext).ConvertToString();
                        JobItem item = null;
                        if(executionContext.JobContext.Dependencies?.TryGetValue(s, out item) ?? false) {
                            if(item?.Status != TaskResult.Succeeded && item?.Status != TaskResult.SucceededWithIssues) {
                                return false;
                            }
                        } else {
                            return false;
                        }
                    }
                    return true;
                }
                return !executionContext.Cancelled.IsCancellationRequested && executionContext.JobContext.Success;
            }
        }

        public sealed class FailureFunction : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var templateContext = evaluationContext.State as TemplateContext;
                var executionContext = templateContext.State[nameof(ExecutionContext)] as ExecutionContext;
                if(Parameters?.Any() ?? false) {
                    foreach(var parameter in Parameters) {
                        var s = parameter.Evaluate(evaluationContext).ConvertToString();
                        JobItem item = null;
                        if(executionContext.JobContext.Dependencies?.TryGetValue(s, out item) ?? false) {
                            if(item?.Status == TaskResult.Failed) {
                                return true;
                            }
                        }
                    }
                    return false;
                }
                return !executionContext.Cancelled.IsCancellationRequested && executionContext.JobContext.Failure;
            }
        }
        public sealed class CancelledFunction : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var templateContext = evaluationContext.State as TemplateContext;
                var executionContext = templateContext.State[nameof(ExecutionContext)] as ExecutionContext;
                return executionContext.Cancelled.IsCancellationRequested;
            }
        }

        class ExecutionContext
        {
            public List<JobItem> workflow { get; set; }
            public CancellationToken Cancelled { get; set; }
            public bool Success { get => workflow?.All(job => job.Status == TaskResult.Succeeded || job.Status == TaskResult.SucceededWithIssues || job.Status == TaskResult.Skipped) ?? false; }
            public JobItem JobContext { get; set; }
        }

        class JobItem {
            public JobItem() {
                RequestId = Interlocked.Increment(ref reqId);
                ActionStatusQueue = new System.Threading.Tasks.Dataflow.ActionBlock<Func<Task>>(action => action().Wait());
                Cancel = new CancellationTokenSource();
            }
            public CancellationTokenSource Cancel {get;}

            public System.Threading.Tasks.Dataflow.ActionBlock<Func<Task>> ActionStatusQueue {get;}

            public string name {get;set;}
            public string DisplayName {get;set;}
            public string[] Needs {get;set;}
            public AgentJobRequestMessage message {get;set;}

            public FinishJobController.JobCompleted OnJobEvaluatable { get;set;}

            public Func<bool> EvaluateIf { get;set;}

            public Guid Id { get; set;}
            public long RequestId { get; set;}
            public Guid TimelineId { get; set;}

            public List<JobItem> Childs { get; set; }

            private TaskResult? stat;

            public bool ContinueOnError {get;set;}
            public bool NoFailFast {get;set;}

            public TaskResult? Status { get => Childs?.Any() ?? false ? Childs.Any(c => c.Status == TaskResult.Failed) ? TaskResult.Failed : Childs.All(c => c.Status == TaskResult.Succeeded) ? TaskResult.Succeeded : null : stat; set => stat = ContinueOnError && value == TaskResult.Failed ? TaskResult.Succeeded : value; }
            public Dictionary<string, JobItem> Dependencies { get; set;}

            // Ref: https://docs.microsoft.com/en-us/azure/devops/pipelines/process/expressions?view=azure-devops#job-status-functions
            public bool Success { get => Dependencies?.All(p => p.Value.Status == TaskResult.Succeeded || p.Value.Status == TaskResult.SucceededWithIssues) ?? true; }
            public bool Failure { get => Dependencies?.Any(p => p.Value.Status == TaskResult.Failed) ?? false; }

            public bool Completed { get; set; }
            public bool NoStatusCheck { get; set; }
            public bool CheckRunStarted { get; set; }

            public JobCompletedEvent JobCompletedEvent { get; set; }

            // public List<Task<IEnumerable<AgentJobRequestMessage>>> enum
        }

        [DataContract]
        private enum NeedsTaskResult
        {
            [EnumMember]
            Success = 0,

            [EnumMember]
            Failure = 2,

            [EnumMember]
            Cancelled = 3,

            [EnumMember]
            Skipped = 4,
        }

        private class TraceWriter : GitHub.DistributedTask.ObjectTemplating.ITraceWriter, GitHub.DistributedTask.Expressions2.ITraceWriter
        {
            public void Error(string format, params object[] args)
            {
                try {
                    Console.Error.WriteLine(format, args);
                } catch {
                    Console.Error.WriteLine("%s", format);
                }
            }

            public void Info(string format, params object[] args)
            {
                try {
                    Console.Out.WriteLine(format, args);
                } catch {
                    Console.Out.WriteLine("%s", format);
                }
            }

            public void Info(string message)
            {
                Console.Out.WriteLine(message);
            }

            public void Verbose(string format, params object[] args)
            {
                try {
                    Console.Out.WriteLine(format, args);
                } catch {
                    Console.Out.WriteLine("%s", format);
                }
            }

            public void Verbose(string message)
            {
                Console.Out.WriteLine(message);
            }
        }

        private class TraceWriter2 : GitHub.DistributedTask.ObjectTemplating.ITraceWriter, GitHub.DistributedTask.Expressions2.ITraceWriter
        {
            private Action<string> callback;
            private Regex regex;
            public TraceWriter2(Action<string> callback) {
                this.callback = callback;
                regex = new Regex("\r?\n");
            }

            public void Callback(string lines) {
                foreach(var line in regex.Split(lines)) {
                    callback(line);
                }
            }

            public void Error(string format, params object[] args)
            {
                if(args?.Length == 1 && args[0] is Exception ex) {
                    Callback(string.Format("{0} {1}", format, ex.Message));
                    return;
                }
                try {
                    Callback(string.Format(format, args));
                } catch {
                    Callback(format);
                }
            }

            public void Info(string format, params object[] args)
            {
                try {
                    Callback(string.Format(format, args));
                } catch {
                    Callback(format);
                }
            }

            public void Info(string message)
            {
                Callback(message);
            }
            public void Verbose(string format, params object[] args)
            {
                try {
                    Callback(string.Format(format, args));
                } catch {
                    Callback(format);
                }
            }

            public void Verbose(string message)
            {
                Callback(message);
            }
        }

        KeyValuePair<string, Minimatch.Minimatcher>[] CompileMinimatch(SequenceToken sequence) {
            return (from item in sequence select new KeyValuePair<string,Minimatch.Minimatcher>(item.AssertString("pattern").Value, new Minimatch.Minimatcher(item.AssertString("pattern").Value))).ToArray();
        }

        bool skip(KeyValuePair<string, Minimatch.Minimatcher>[] sequence, IEnumerable<string> input) {
            
            return sequence != null && sequence.Length > 0 && !input.Any(file => {
                bool matched = false;
                foreach (var item in sequence) {
                    var pattern = item.Key;
                    if(item.Value.IsMatch(file) && !(pattern.StartsWith("!**") && file.EndsWith(pattern.Substring(3))) || pattern.StartsWith("**") && file.EndsWith(pattern.Substring(2))) {
                        matched = true;
                    } else if(pattern.StartsWith("!")) {
                        matched = false;
                    }
                }
                return matched;
            });
        }

        bool filter(KeyValuePair<string, Minimatch.Minimatcher>[] sequence, IEnumerable<string> input) {
            return sequence != null && sequence.Length > 0 && input.All(file => {
                foreach (var item in sequence)
                {
                    var pattern = item.Key;
                    if(item.Value.IsMatch(file) && !(pattern.StartsWith("!**") && file.EndsWith(pattern.Substring(3))) || pattern.StartsWith("**") && file.EndsWith(pattern.Substring(2))) {
                        return true;
                    }
                }
                return false;
            });
        }

        private class JobListItem {
            public string Name {get;set;}
            public string[] Needs {get;set;}
        }

        private class HookResponse {
            public string repo {get;set;}
            public long run_id {get;set;}
            public bool skipped {get;set;}
            public bool failed {get;set;}
            public List<JobListItem> jobList {get;set;}
        }

        private static void LoadEnvSec(string[] contents, Action<string, string> kvhandler)
        {
            foreach (var env in contents)
            {
                if (!string.IsNullOrEmpty(env))
                {
                    var separatorIndex = env.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        string envKey = env.Substring(0, separatorIndex);
                        string envValue = null;
                        if (env.Length > separatorIndex + 1)
                        {
                            envValue = env.Substring(separatorIndex + 1);
                        }
                        kvhandler.Invoke(envKey, envValue);
                    }
                }
            }
        }

        private static MappingToken LoadEnv(string[] contents)
        {
            var environment = new MappingToken(null, null, null);
            LoadEnvSec(contents, (envKey, envValue) => environment.Add(new KeyValuePair<ScalarToken, TemplateToken>(new StringToken(null, null, null, envKey), new StringToken(null, null, null, envValue))));
            return environment;
        }
        private enum JobStatus {
            Pending,
            Success,
            Failure
        }
        private struct StatusCheck {
            public JobStatus State {get;set;}
            public string Context {get;set;}
            public string Description {get;set;}
            [JsonProperty(PropertyName = "target_url")]
            public string TargetUrl {get;set;}
        }

        private static Dictionary<long, CancellationTokenSource> cancelWorkflows = new Dictionary<long, CancellationTokenSource>();
        private class ConcurrencyEntry {
            public Action<bool> CancelRunning {get;set;}
            public Action CancelPending {get;set;}
            public Action Run {get;set;}
        }
        private class ConcurrencyGroup {
            public string Key {get;set;}
            public ConcurrencyEntry Running {get;set;}
            public ConcurrencyEntry Pending {get;set;}
            public void PushEntry(ConcurrencyEntry entry, bool cancelInProgress) {
                lock(this) {
                    if(Running == null) {
                        Running = entry;
                        Running.Run();
                    } else {
                        Pending?.CancelPending();
                        Pending = entry;
                        Running.CancelRunning(cancelInProgress);
                    }
                }
            }
            public void FinishRunning(ConcurrencyEntry entry) {
                lock(this) {
                    if(Running == entry) {
                        Running = Pending;
                        Pending = null;
                        Running?.Run();
                    }
                    if(Running == null && Pending == null) {
                        concurrencyGroups.Remove(Key, out _);
                    }
                }
            }
        }
        private static ConcurrentDictionary<string, ConcurrencyGroup> concurrencyGroups = new ConcurrentDictionary<string, ConcurrencyGroup>();

        private HookResponse ConvertYaml(string fileRelativePath, string content, string repository, string giteaUrl, GiteaHook hook, JObject payloadObject, string e = "push", string selectedJob = null, bool list = false, string[] env = null, string[] secrets = null, string[] _matrix = null, string[] platform = null, bool localcheckout = false, KeyValuePair<string, string>[] workflows = null, Action<long> workflowrun = null) {
            string repository_name = hook?.repository?.full_name ?? "Unknown/Unknown";
            string owner_name = repository_name.Split('/', 2)[0];
            string repo_name = repository_name.Split('/', 2)[1];
            var run = new WorkflowRun { FileName = fileRelativePath, Workflow = (from w in _context.Set<Workflow>() where w.FileName == fileRelativePath && w.Repository.Owner.Name == owner_name && w.Repository.Name == repo_name select w).FirstOrDefault() ?? new Workflow { FileName = fileRelativePath, Repository = (from r in _context.Set<Repository>() where r.Owner.Name == owner_name && r.Name == repo_name select r).FirstOrDefault() ?? new Repository { Name = repo_name, Owner = (from o in _context.Set<Owner>() where o.Name == owner_name select o).FirstOrDefault() ?? new Owner { Name = owner_name } } } };
            long attempt = 1;
            var _attempt = new WorkflowRunAttempt() { Attempt = (int) attempt++, WorkflowRun = run, EventPayload = payloadObject.ToString(), EventName = e, Workflow = content };
            _context.Artifacts.Add(new ArtifactContainer() { Attempt = _attempt } );
            _context.SaveChanges();
            workflowrun?.Invoke(run.Id);
            var runid = run.Id;
            long runnumber = run.Id;

            var Ref = hook?.Ref;
            if(Ref == null) {
                if(e == "pull_request_target") {
                    var tmp = hook?.pull_request?.Base?.Ref;
                    if(tmp != null) {
                        Ref = "refs/heads/" + tmp;
                    }
                } else if(e == "pull_request" && hook?.Number != null) {
                    if(hook?.merge_commit_sha != null && HasPullRequestMergePseudoBranch) {
                        Ref = $"refs/pull/{hook.Number}/merge";
                    } else {
                        Ref = $"refs/pull/{hook.Number}/head";
                    }
                }
            } else if(hook?.ref_type != null) {
                if(e == "create") {
                    // Fixup create hooks to have a git ref
                    if(hook?.ref_type == "branch") {
                        Ref = "refs/heads/" + Ref;
                    } else if(hook?.ref_type == "tag") {
                        Ref = "refs/tags/" + Ref;
                    }
                    hook.After = hook?.Sha;
                } else {
                    Ref = null;
                }
            }
            if(Ref == null && hook?.repository?.default_branch != null) {
                Ref = "refs/heads/" + hook?.repository?.default_branch;
            }
            string Sha;
            if(e == "pull_request_target") {
                Sha = hook?.pull_request?.Base?.Sha;
            } else if(e == "pull_request") {
                if(hook?.merge_commit_sha == null || !HasPullRequestMergePseudoBranch) {
                    Sha = hook?.pull_request?.head?.Sha;
                } else {
                    Sha = hook.merge_commit_sha;
                }
            } else {
                Sha = hook.After;
            }
            return Clone().ConvertYaml2(fileRelativePath, content, repository, giteaUrl, hook, payloadObject, e, selectedJob, list, env, secrets, _matrix, platform, localcheckout, runid, runnumber, Ref, Sha, workflows: workflows, attempt: _attempt, statusSha: e == "pull_request_target" ? hook?.pull_request?.head?.Sha : Sha);
        }

        private void AddJob(Job job) {
            try {
                _cache.Set(job.JobId, job);
                job.WorkflowRunAttempt = _context.Set<WorkflowRunAttempt>().Find(job.WorkflowRunAttempt.Id);
                if(_context.Jobs.Find(job.JobId) == null) {
                    Task.Run(() => jobevent?.Invoke(this, job.repo, job));
                    _context.Jobs.Add(job);
                    _context.SaveChanges();
                }
                initializingJobs.Remove(job.JobId, out _);
            } catch (Exception m){
                throw new Exception("Failed to add job  " + job.name + " / " + job.WorkflowIdentifier + " / " + job.Matrix + " / " + job.JobId  + ": " + m.Message);
            }
        }

        private Job GetJob(Guid id) {
            return (from job in _context.Jobs where job.JobId == id select job).Include(j => j.WorkflowRunAttempt).FirstOrDefault();
        }

        private class LocalJobCompletedEvents {
            public event FinishJobController.JobCompleted JobCompleted;
            public void Invoke(JobCompletedEvent ev) {
                JobCompleted?.Invoke(ev);
            }
        }

        private async Task<string> CreateGithubAppToken(string repository_name) {
            if(!string.IsNullOrEmpty(GitHubAppPrivateKeyFile) && GitHubAppId != 0) {
                try {
                    var ownerAndRepo = repository_name.Split("/", 2);
                    // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
                    var generator = new GitHubJwt.GitHubJwtFactory(
                        new GitHubJwt.FilePrivateKeySource(GitHubAppPrivateKeyFile),
                        new GitHubJwt.GitHubJwtFactoryOptions
                        {
                            AppIntegrationId = GitHubAppId, // The GitHub App Id
                            ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
                        }
                    );
                    var jwtToken = generator.CreateEncodedJwtToken();
                    // Pass the JWT as a Bearer token to Octokit.net
                    var appClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"))
                    {
                        Credentials = new Octokit.Credentials(jwtToken, Octokit.AuthenticationType.Bearer)
                    };
                    var installation = await appClient.GitHubApps.GetRepositoryInstallationForCurrent(ownerAndRepo[0], ownerAndRepo[1]);
                    var response = await appClient.Connection.Post<Octokit.AccessToken>(Octokit.ApiUrls.AccessTokens(installation.Id), new { Permissions = new { metadata = "read", checks = "write" } }, Octokit.AcceptHeaders.GitHubAppsPreview, Octokit.AcceptHeaders.GitHubAppsPreview);
                    return response.Body.Token;
                } catch {

                }
            }
            return null;
        }

        private async Task DeleteGithubAppToken(string token) {
            var appClient2 = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"))
            {
                Credentials = new Octokit.Credentials(token)
            };
            await appClient2.Connection.Delete(new Uri("installation/token", UriKind.Relative));
        }

        private class CallingJob {
            public string Id {get;set;}
            public Guid TimelineId {get;set;}
            public Guid RecordId {get;set;}
            public string WorkflowName {get;set;}
            public string Name {get;set;}
            public string Event {get;set;}
            public PipelineContextData Inputs {get;set;}
            public CancellationToken? CancellationToken {get;set;}
            public Action<CallingJob, WorkflowEventArgs> Workflowfinish {get;set;}
            // Set by the called workflow to indicate whether to clean cached job dependencies
            public bool RanJob { get; set; }
        }

        private HookResponse ConvertYaml2(string fileRelativePath, string content, string repository, string giteaUrl, GiteaHook hook, JObject payloadObject, string e, string selectedJob, bool list, string[] env, string[] secrets, string[] _matrix, string[] platform, bool localcheckout, long runid, long runnumber, string Ref, string Sha, CallingJob callingJob = null, KeyValuePair<string, string>[] workflows = null, WorkflowRunAttempt attempt = null, string statusSha = null, Dictionary<string, List<Job>> finishedJobs = null) {
            attempt = _context.Set<WorkflowRunAttempt>().Find(attempt.Id);
            _context.Entry(attempt).Reference(a => a.WorkflowRun).Load();
            bool asyncProcessing = false;
            Guid workflowTimelineId = callingJob?.TimelineId ?? attempt.TimeLineId;
            if(workflowTimelineId == Guid.Empty) {
                workflowTimelineId = Guid.NewGuid();
                attempt.TimeLineId = workflowTimelineId;
                _context.SaveChanges();
                var records = new List<TimelineRecord>{ new TimelineRecord{ Id = workflowTimelineId, Name = fileRelativePath } };
                TimelineController.dict[workflowTimelineId] = (records, new System.Collections.Concurrent.ConcurrentDictionary<System.Guid, System.Collections.Generic.List<GitHub.DistributedTask.WebApi.TimelineRecordLogLine>>() );
                new TimelineController(_context).UpdateTimeLine(workflowTimelineId, new VssJsonCollectionWrapper<List<TimelineRecord>>(records));
            }
            if(workflowTimelineId == attempt.TimeLineId) {
                // Add workflow as dummy job, to improve early cancellation of Runner.Client
                initializingJobs.TryAdd(workflowTimelineId, new Job() { JobId = workflowTimelineId, TimeLineId = workflowTimelineId, runid = runid } );
            }
            Guid workflowRecordId = callingJob?.RecordId ?? attempt.TimeLineId;
            TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Initialize Workflow Run {runid}" }), workflowTimelineId, workflowRecordId);
            string event_name = e;
            string repository_name = hook?.repository?.full_name ?? "Unknown/Unknown";
            MappingToken workflowOutputs = null;
            var jobsctx = new DictionaryContextData();
            var workflowTraceWriter = new TraceWriter2(line => {
                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ line }), workflowTimelineId, workflowRecordId);
            });
            var workflowname = fileRelativePath;
            Func<JobItem, TaskResult?, Task> updateJobStatus = async (next, status) => {
                var effective_event = callingJob?.Event ?? event_name;
                if(!string.IsNullOrEmpty(hook.repository.full_name) && !string.IsNullOrEmpty(statusSha) && !next.NoStatusCheck && (effective_event == "push" || ((effective_event == "pull_request" || effective_event == "pull_request_target") && (new [] { "opened", "synchronize", "synchronized", "reopened" }).Any(t => t == hook?.Action))) && !localcheckout) {
                    var ctx = string.Format("{0} / {1} ({2})", workflowname, next.DisplayName, callingJob?.Event ?? event_name);
                    var targetUrl = "";
                    var ownerAndRepo = repository_name.Split("/", 2);
                    if(!string.IsNullOrEmpty(ServerUrl)) {
                        var targetUrlBuilder = new UriBuilder(ServerUrl);
                        // old url
                        // targetUrlBuilder.Fragment  = $"/master/runner/server/detail/{next.Id}";
                        targetUrlBuilder.Fragment  = $"/0/{ownerAndRepo[0]}/0/{ownerAndRepo[1]}/0/{runid}/0/{(next.Id != Guid.Empty ? next.Id : "")}";
                        targetUrl = targetUrlBuilder.ToString();
                    }
                    if(!string.IsNullOrEmpty(GITHUB_TOKEN)) {
                        try {
                            JobStatus jobstatus = JobStatus.Pending;
                            var description = status?.ToString() ?? "Pending";
                            if(status == TaskResult.Succeeded || status == TaskResult.SucceededWithIssues) {
                                jobstatus = JobStatus.Success;
                            }
                            if(status == TaskResult.Skipped) {
                                jobstatus = JobStatus.Pending;
                            }
                            if(status == TaskResult.Failed || status == TaskResult.Abandoned || status == TaskResult.Canceled) {
                                jobstatus = JobStatus.Failure;
                            }
                            var client = new HttpClient();
                            client.DefaultRequestHeaders.Add("accept", "application/json");
                            client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("runner", string.IsNullOrEmpty(GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version) ? "0.0.0" : GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version));
                            if(!string.IsNullOrEmpty(GITHUB_TOKEN)) {
                                client.DefaultRequestHeaders.Add("Authorization", $"token {GITHUB_TOKEN}");
                            }
                            var url = new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{hook.repository.full_name}/statuses/{statusSha}"));
                            (await client.PostAsync(url.ToString(), new ObjectContent<StatusCheck>(new StatusCheck { State = jobstatus, Context = ctx, Description = description, TargetUrl = targetUrl }, new VssJsonMediaTypeFormatter()))).EnsureSuccessStatusCode();
                        } catch {

                        }
                    } else if(!string.IsNullOrEmpty(GitHubAppPrivateKeyFile) && GitHubAppId != 0) {
                        try {
                            // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
                            var generator = new GitHubJwt.GitHubJwtFactory(
                                new GitHubJwt.FilePrivateKeySource(GitHubAppPrivateKeyFile),
                                new GitHubJwt.GitHubJwtFactoryOptions
                                {
                                    AppIntegrationId = GitHubAppId, // The GitHub App Id
                                    ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
                                }
                            );
                            var jwtToken = generator.CreateEncodedJwtToken();
                            // Pass the JWT as a Bearer token to Octokit.net
                            var appClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"))
                            {
                                Credentials = new Octokit.Credentials(jwtToken, Octokit.AuthenticationType.Bearer)
                            };
                            var installation = await appClient.GitHubApps.GetRepositoryInstallationForCurrent(ownerAndRepo[0], ownerAndRepo[1]);
                            var response = await appClient.Connection.Post<Octokit.AccessToken>(Octokit.ApiUrls.AccessTokens(installation.Id), new { Permissions = new { metadata = "read", checks = "write" } }, Octokit.AcceptHeaders.GitHubAppsPreview, Octokit.AcceptHeaders.GitHubAppsPreview);
                            var appClient2 = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"))
                            {
                                Credentials = new Octokit.Credentials(response.Body.Token)
                            };
                            try {
                                Octokit.CheckConclusion? conclusion = null;
                                if(status == TaskResult.Skipped) {
                                    conclusion = Octokit.CheckConclusion.Skipped;
                                } else if(status == TaskResult.Succeeded || status == TaskResult.SucceededWithIssues) {
                                    conclusion = Octokit.CheckConclusion.Success;
                                } else if(status == TaskResult.Failed || status == TaskResult.Abandoned) {
                                    conclusion = Octokit.CheckConclusion.Failure;
                                } else if(status == TaskResult.Canceled) {
                                    conclusion = Octokit.CheckConclusion.Cancelled;
                                }
                                var checkrun = (await appClient2.Check.Run.GetAllForReference(ownerAndRepo[0], ownerAndRepo[1], statusSha, new Octokit.CheckRunRequest() { CheckName = ctx }, new Octokit.ApiOptions() { PageSize = 1 })).CheckRuns.FirstOrDefault() ?? await appClient2.Check.Run.Create(ownerAndRepo[0], ownerAndRepo[1], new Octokit.NewCheckRun(ctx, statusSha) );
                                var result = await appClient2.Check.Run.Update(ownerAndRepo[0], ownerAndRepo[1], checkrun.Id, new Octokit.CheckRunUpdate() { Status = conclusion == null ? Octokit.CheckStatus.InProgress : Octokit.CheckStatus.Completed, StartedAt = conclusion != null && next.CheckRunStarted ? checkrun.StartedAt : DateTimeOffset.UtcNow, CompletedAt = conclusion == null ? null : DateTimeOffset.UtcNow, Conclusion = conclusion, DetailsUrl = targetUrl, Output = new Octokit.NewCheckRunOutput(next.name, "") });
                                next.CheckRunStarted = true;
                            } finally {
                                await appClient2.Connection.Delete(new Uri("installation/token", UriKind.Relative));
                            }
                        } catch {

                        }
                        
                    }
                }
            };
            try {
                List<JobItem> jobgroup = new List<JobItem>();
                List<JobItem> dependentjobgroup = new List<JobItem>();
                var templateContext = new TemplateContext(){
                    CancellationToken = CancellationToken.None,
                    Errors = new TemplateValidationErrors(10, 500),
                    Memory = new TemplateMemory(
                        maxDepth: 100,
                        maxEvents: 1000000,
                        maxBytes: 10 * 1024 * 1024),
                    TraceWriter = workflowTraceWriter,
                    Schema = PipelineTemplateSchemaFactory.GetSchema()
                };
                ExecutionContext exctx = new ExecutionContext();
                templateContext.State[nameof(ExecutionContext)] = exctx;
                templateContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
                templateContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
                templateContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, Int32.MaxValue));
                templateContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, Int32.MaxValue));
                foreach (var func in ExpressionConstants.WellKnownFunctions.Values)
                {
                    templateContext.ExpressionFunctions.Add(func);
                }

                var token = default(TemplateToken);
                // Get the file ID
                var fileId = templateContext.GetFileId(fileRelativePath);

                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ "Parsing Workflow..." }), workflowTimelineId, workflowRecordId);

                // Read the file
                var fileContent = content ?? System.IO.File.ReadAllText(fileRelativePath);
                using (var stringReader = new StringReader(fileContent))
                {
                    var yamlObjectReader = new YamlObjectReader(fileId, stringReader);
                    token = TemplateReader.Read(templateContext, "workflow-root", yamlObjectReader, fileId, out _);
                }

                templateContext.Errors.Check();
                if(token == null) {
                    throw new Exception("token is null after parsing your workflow, this should never happen");
                }
                var actionMapping = token.AssertMapping("root");

                TemplateToken workflowDefaults = null;
                List<TemplateToken> workflowEnvironment = new List<TemplateToken>();
                if(env?.Length > 0) {
                    workflowEnvironment.Add(LoadEnv(env));
                }

                var localJobCompletedEvents = new LocalJobCompletedEvents();
                Action<JobCompletedEvent> jobCompleted = e => {
                    foreach (var item in dependentjobgroup.ToArray()) {
                        try {
                            item.OnJobEvaluatable(e);
                        } catch(Exception ex) {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    if(e != null) {
                        try {
                            localJobCompletedEvents.Invoke(e);
                        } catch(Exception ex) {
                            Console.WriteLine(ex.Message);
                        }
                    }
                };

                TemplateToken tk = (from r in actionMapping where r.Key.AssertString("on").Value == "on" select r).FirstOrDefault().Value;
                if(tk == null) {
                    throw new Exception("Your workflow is invalid, missing 'on' property");
                }
                MappingToken mappingEvent = null;
                switch(tk.Type) {
                    case TokenType.String:
                        if(tk.AssertString("str").Value != e) {
                            // Skip, not the right event
                            TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping the Workflow, '{tk.AssertString("str").Value}' isn't is the requested event '{e}'" }), workflowTimelineId, workflowRecordId);
                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                        }
                        break;
                    case TokenType.Sequence:
                        if((from r in tk.AssertSequence("seq") where r.AssertString(e).Value == e select r).FirstOrDefault() == null) {
                            // Skip, not the right event
                            TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping the Workflow, [{string.Join(',', from r in tk.AssertSequence("seq") select "'" + r.AssertString(e).Value + "'")}] doesn't contain the requested event '{e}'" }), workflowTimelineId, workflowRecordId);
                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                        }
                        break;
                    case TokenType.Mapping:
                        var e2 = (from r in tk.AssertMapping("seq") where r.Key.AssertString(e).Value == e select r).FirstOrDefault();
                        var rawEvent = e2.Value;
                        if(rawEvent == null) {
                            TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping the Workflow, [{string.Join(',', from r in tk.AssertMapping("mao") select "'" + r.Key.AssertString(e).Value + "'")}] doesn't contain the requested event '{e}'" }), workflowTimelineId, workflowRecordId);
                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                        }
                        if(e == "schedule") {
                            var crons = e2.Value.AssertSequence("cron");
                            var cm = (from cron in crons select cron.AssertMapping("cron")).ToArray();
                            if(cm.Length == 0 || !cm.All(c => c.Count == 1 && c.First().Key.AssertString("cron key").Value == "cron")) {
                                throw new Exception("Only cron is supported!");
                            }
                            var values = (from c in cm select c.First().Value.AssertString("cron value").Value).ToArray();
                            var validator = new Regex("^(((\\d+,)+\\d+|((\\d+|\\*)\\/\\d+|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)|(\\d+-\\d+)|\\d+|\\*|MON|TUE|WED|THU|FRI|SAT|SUN) ?){5,7}$");
                            if(!values.All(s => validator.IsMatch(s))) {
                                var z = 0;
                                var sb = new StringBuilder();
                                foreach (var prop in (from s in values where !validator.IsMatch(s) select s)) {
                                    if(z++ != 0) {
                                        sb.Append(", ");
                                    }
                                    sb.Append(prop);
                                }
                                throw new Exception($"cron validation failed for: {sb.ToString()}");
                            }
                        } else {
                            mappingEvent = rawEvent.Type != TokenType.Null ? rawEvent.AssertMapping($"expected mapping for event '{e}'") : null;
                        }
                        break;
                    default:
                        throw new Exception($"Error: Your workflow is invalid, 'on' property has an unexpected yaml Type {tk.Type}");
                }
                if(e != "schedule") {
                    List<string> allowed = new List<string>();
                    allowed.Add("types");

                    if(e == "push" || e == "pull_request" || e == "pull_request_target" || e == "workflow_run") {
                        allowed.Add("branches");
                        allowed.Add("branches-ignore");
                    }
                    if(e == "workflow_run") {
                        allowed.Add("workflows");
                    }
                    if(e == "push" || e == "pull_request" || e == "pull_request_target") {
                        allowed.Add("tags");
                        allowed.Add("tags-ignore");
                        allowed.Add("paths");
                        allowed.Add("paths-ignore");
                    }
                    if(e == "workflow_dispatch") {
                        allowed.Add("inputs");
                        // Validate inputs and apply defaults
                        var workflowInputs = mappingEvent != null ? (from r in mappingEvent where r.Key.AssertString("inputs").Value == "inputs" select r).FirstOrDefault().Value?.AssertMapping("map") : null;
                        List<string> validInputs = new List<string>();
                        var dispatchInputs = payloadObject["inputs"] as JObject;
                        if(dispatchInputs == null) {
                            dispatchInputs = new JObject();
                            payloadObject["inputs"] = dispatchInputs;
                        }
                        if(workflowInputs != null) {
                            foreach(var input in workflowInputs) {
                                var inputName = input.Key.AssertString("input key must be a string").Value;
                                validInputs.Add(inputName);
                                var inputInfo = input.Value?.AssertMapping("map");
                                if(inputInfo != null) {
                                    var workflowDispatchMappingKey = "on.workflow_dispatch mapping key";
                                    bool required = (from r in inputInfo where r.Key.AssertString(workflowDispatchMappingKey).Value == "required" select r.Value.AssertBoolean("on.workflow_dispatch.required").Value).FirstOrDefault();
                                    string type = (from r in inputInfo where r.Key.AssertString(workflowDispatchMappingKey).Value == "type" select r.Value.AssertString("on.workflow_dispatch.type").Value).FirstOrDefault();
                                    SequenceToken options = (from r in inputInfo where r.Key.AssertString(workflowDispatchMappingKey).Value == "options" select r.Value.AssertSequence("on.workflow_dispatch.options")).FirstOrDefault();
                                    var def = (from r in inputInfo where r.Key.AssertString(workflowDispatchMappingKey).Value == "default" select r.Value).FirstOrDefault()?.AssertString("on.workflow_dispatch.default")?.ToContextData()?.ToJToken();
                                    if(def == null) {
                                        def = "";
                                    }
                                    if(!dispatchInputs.TryGetValue(inputName, out _)) {
                                        if(required) {
                                            throw new Exception($"This workflow requires the input: {inputName}, but no such input were provided");
                                        }
                                        dispatchInputs[inputName] = def;
                                    }
                                }
                            }
                        }
                        foreach(var providedInput in dispatchInputs) {
                            if(!validInputs.Contains(providedInput.Key)) {
                                throw new Exception($"This workflow doesn't define input {providedInput.Key}");
                            }
                        }
                    }
                    if(e == "workflow_call") {
                        allowed.Add("inputs");
                        allowed.Add("outputs");
                        allowed.Add("secrets");
                        // Validate inputs and apply defaults
                        var workflowInputs = mappingEvent != null ? (from r in mappingEvent where r.Key.AssertString("inputs").Value == "inputs" select r).FirstOrDefault().Value?.AssertMapping("map") : null;
                        workflowOutputs = mappingEvent != null ? (from r in mappingEvent where r.Key.AssertString("outputs").Value == "outputs" select r).FirstOrDefault().Value?.AssertMapping("map") : null;
                        List<string> validInputs = new List<string>();
                        if(callingJob?.Inputs == null) {
                            callingJob.Inputs = new DictionaryContextData();
                        }
                        if(workflowInputs != null) {
                            foreach(var input in workflowInputs) {
                                var inputName = input.Key.AssertString("input key must be a string").Value;
                                validInputs.Add(inputName);
                                var inputInfo = input.Value?.AssertMapping("map");
                                if(inputInfo != null) {
                                    var workflowCallInputMappingKey = "on.workflow_call.inputs.{inputName} mapping key";
                                    bool required = (from r in inputInfo where r.Key.AssertString(workflowCallInputMappingKey).Value == "required" select r.Value.AssertBoolean("on.workflow_call.inputs.{inputName}.required").Value).FirstOrDefault();
                                    string type = (from r in inputInfo where r.Key.AssertString(workflowCallInputMappingKey).Value == "type" select r.Value.AssertString("on.workflow_call.inputs.{inputName}.type").Value).First();
                                    var defassertMessage = $"on.workflow_call.inputs.{inputName}.default";
                                    var def = (from r in inputInfo where r.Key.AssertString(workflowCallInputMappingKey).Value == "default" select r.Value).FirstOrDefault()?.ToContextData();
                                    switch(type) {
                                    case "string":
                                        if(def == null) {
                                            def = new StringContextData("");
                                        }
                                        def.AssertString(defassertMessage);
                                    break;
                                    case "number":
                                        if(def == null) {
                                            def = new NumberContextData(0);
                                        }
                                        def.AssertNumber(defassertMessage);
                                    break;
                                    case "boolean":
                                        if(def == null) {
                                            def = new BooleanContextData(false);
                                        }
                                        def.AssertBoolean(defassertMessage);
                                    break;
                                    default:
                                        throw new Exception($"on.workflow_call.inputs.{inputName}.type assigned to invalid type: {type}, expected 'string', 'number' or 'boolean'");
                                    }
                                    var inputsDict = callingJob?.Inputs.AssertDictionary("dict");
                                    var assertMessage = $"This workflow requires that the input: {inputName}, to have type {type}";
                                    if(inputsDict.TryGetValue(inputName, out var val)) {
                                        switch(type) {
                                        case "string":
                                            val.AssertString(assertMessage);
                                        break;
                                        case "number":
                                            val.AssertNumber(assertMessage);
                                        break;
                                        case "boolean":
                                            val.AssertBoolean(assertMessage);
                                        break;
                                        }
                                    } else if(required) {
                                        throw new Exception($"This workflow requires the input: {inputName}, but no such input were provided");
                                    } else {
                                        inputsDict[inputName] = def;
                                    }
                                }
                            }
                        }
                        foreach(var providedInput in callingJob?.Inputs.AssertDictionary("")) {
                            if(!validInputs.Contains(providedInput.Key)) {
                                throw new Exception($"This workflow doesn't define input {providedInput.Key}");
                            }
                        }
                        // Validate secrets
                        var workflowSecrets = mappingEvent != null ? (from r in mappingEvent where r.Key.AssertString("secrets").Value == "secrets" select r).FirstOrDefault().Value?.AssertMapping("map") : null;
                        List<string> validSecrets = new List<string> { "system.github.token" };
                        if(workflowSecrets != null) {
                            foreach(var input in workflowSecrets) {
                                var inputName = input.Key.AssertString("input key must be a string").Value;
                                if(inputName.Contains(".") || StringComparer.OrdinalIgnoreCase.Compare("github_token", inputName) == 0) {
                                    throw new Exception($"This workflow defines the reserved secret {inputName}, using it can cause undefined behavior");
                                }
                                var inputInfo = input.Value?.AssertMapping("map");
                                if(inputInfo != null) {
                                    var workflowCallSecretsMappingKey = "on.workflow_call.secrets.{inputName} mapping key";
                                    validSecrets.Add(inputName.ToLowerInvariant());
                                    bool required = (from r in inputInfo where r.Key.AssertString(workflowCallSecretsMappingKey).Value == "required" select r.Value.AssertBoolean("on.workflow_call.secrets.{inputName}.required").Value).FirstOrDefault();
                                    
                                    if(!secrets.Any(s => s.ToLowerInvariant().StartsWith(inputName.ToLowerInvariant() + "=")) && required) {
                                        throw new Exception($"This workflow requires the secret: {inputName}, but no such secret were provided");
                                    }
                                }
                            }
                        }
                        foreach(var secret in secrets) {
                            var name = secret.Substring(0, secret.IndexOf('=')).ToLowerInvariant();
                            if(!validSecrets.Contains(name)) {
                                throw new Exception($"This workflow doesn't define secret {name}");
                            }
                        }
                    }

                    if(mappingEvent != null && !mappingEvent.All(p => allowed.Any(s => s == p.Key.AssertString("Key").Value))) {
                        var z = 0;
                        var sb = new StringBuilder();
                        foreach (var prop in (from p in mappingEvent where !allowed.Any(s => s == p.Key.AssertString("Key").Value) select p.Key.AssertString("Key").Value)) {
                            if(z++ != 0) {
                                sb.Append(", ");
                            }
                            sb.Append(prop);
                        }
                        TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"The following event properties are invalid: {sb.ToString()}, please remove them from {e}" }), workflowTimelineId, workflowRecordId);
                    }

                    // Offical github action server ignores the filter on non push / pull_request (workflow_run) events
                    // It seems we need to accept scalars as well as sequences, branches: branchname is valid
                    Func<string, SequenceToken> extractFilter = name => mappingEvent != null ? (from r in mappingEvent where r.Key.AssertString($"on.{event_name} mapping key").Value == name select r).FirstOrDefault().Value?.AssertScalarOrSequence($"on.{event_name}.{name}") : null;
                    var branches = extractFilter("branches");
                    var branchesIgnore = extractFilter("branches-ignore");
                    var tags = extractFilter("tags");
                    var tagsIgnore = extractFilter("tags-ignore");
                    var paths = extractFilter("paths");
                    var pathsIgnore = extractFilter("paths-ignore");
                    var types = extractFilter("types");

                    if(branches != null && branchesIgnore != null) {
                        throw new Exception("branches and branches-ignore shall not be used at the same time");
                    }
                    if(tags != null && tagsIgnore != null) {
                        throw new Exception("tags and tags-ignore shall not be used at the same time");
                    }
                    if(paths != null && pathsIgnore != null) {
                        throw new Exception("paths and paths-ignore shall not be used at the same time");
                    }
                    
                    if(hook?.Action != null) {
                        if(types != null) {
                            if(!(from t in types select t.AssertString("type").Value).Any(t => t == hook?.Action)) {
                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping Workflow, due to types filter. Requested Action was {hook?.Action}, but require {string.Join(',', from t in types select "'" + t.AssertString("type").Value + "'")}" }), workflowTimelineId, workflowRecordId);
                                return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                            }
                        } else if(e == "pull_request" || e == "pull_request_target"){
                            if(!(new [] { "opened", "synchronize", "synchronized", "reopened" }).Any(t => t == hook?.Action)) {
                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping Workflow, due to default types filter of the {e} trigger" }), workflowTimelineId, workflowRecordId);
                                return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                            }
                        }
                    }

                    var heads = "refs/heads/";
                    var rtags = "refs/tags/";

                    var Ref2 = Ref;
                    // Only evaluate base ref https://docs.github.com/en/actions/reference/workflow-syntax-for-github-actions#onpushpull_requestbranchestags
                    if(e == "pull_request_target" || e == "pull_request") {
                        var tmp = hook?.pull_request?.Base?.Ref;
                        if(tmp != null) {
                            Ref2 = "refs/heads/" + tmp;
                        }
                    }
                    if(Ref2 != null) {
                        if(Ref2.StartsWith(heads) == true) {
                            var branch = Ref2.Substring(heads.Length);

                            if(branchesIgnore != null && filter(CompileMinimatch(branchesIgnore), new[] { branch })) {
                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping Workflow, due to branches-ignore filter. github.ref='{Ref2}'" }), workflowTimelineId, workflowRecordId);
                                return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                            }
                            if(branches != null && skip(CompileMinimatch(branches), new[] { branch })) {
                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping Workflow, due to branches filter. github.ref='{Ref2}'" }), workflowTimelineId, workflowRecordId);
                                return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                            }
                            if((tags != null || tagsIgnore != null) && branches == null && branchesIgnore == null) {
                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping Workflow, due to existense of tag filter. github.ref='{Ref2}'" }), workflowTimelineId, workflowRecordId);
                                return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                            }
                        } else if(Ref2.StartsWith(rtags) == true) {
                            var tag = Ref2.Substring(rtags.Length);

                            if(tagsIgnore != null && filter(CompileMinimatch(tagsIgnore), new[] { tag })) {
                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping Workflow, due to tags-ignore filter. github.ref='{Ref2}'" }), workflowTimelineId, workflowRecordId);
                                return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                            }
                            if(tags != null && skip(CompileMinimatch(tags), new[] { tag })) {
                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping Workflow, due to tags filter. github.ref='{Ref2}'" }), workflowTimelineId, workflowRecordId);
                                return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                            }
                            if((branches != null || branchesIgnore != null) && tags == null && tagsIgnore == null) {
                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping Workflow, due to existense of branch filter. github.ref='{Ref2}'" }), workflowTimelineId, workflowRecordId);
                                return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                            }
                        }
                    }
                    if(hook.Commits != null) {
                        var changedFiles = hook.Commits.SelectMany(commit => (commit.Added ?? new List<string>()).Concat(commit.Removed ?? new List<string>()).Concat(commit.Modified ?? new List<string>()));
                        if(pathsIgnore != null && filter(CompileMinimatch(pathsIgnore), changedFiles)) {
                            TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping Workflow, due to paths-ignore filter.'" }), workflowTimelineId, workflowRecordId);
                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                        }
                        if(paths != null && skip(CompileMinimatch(paths), changedFiles)) {
                            TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Skipping Workflow, due to paths filter." }), workflowTimelineId, workflowRecordId);
                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                        }
                    }
                }
                workflowname = callingJob?.WorkflowName ?? (from r in actionMapping where r.Key.AssertString("name").Value == "name" select r).FirstOrDefault().Value?.AssertString("val").Value ?? fileRelativePath;
                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Updated Workflow Name: {workflowname}" }), workflowTimelineId, workflowRecordId);
                if(attempt.WorkflowRun != null && attempt.WorkflowRun.DisplayName == null && !string.IsNullOrEmpty(workflowname)) {
                    attempt.WorkflowRun.DisplayName = workflowname;
                    _context.SaveChanges();
                }
                Func<string, DictionaryContextData> createContext = jobname => {
                    var contextData = new GitHub.DistributedTask.Pipelines.ContextData.DictionaryContextData();
                    contextData["inputs"] = callingJob?.Inputs;
                    var githubctx = new DictionaryContextData();
                    contextData.Add("github", githubctx);
                    githubctx.Add("server_url", new StringContextData(GitServerUrl));
                    githubctx.Add("api_url", new StringContextData(GitApiServerUrl));
                    githubctx.Add("graphql_url", new StringContextData(GitGraphQlServerUrl));
                    githubctx.Add("workflow", new StringContextData(workflowname));
                    githubctx.Add("repository", new StringContextData(repository_name));
                    githubctx.Add("sha", new StringContextData(Sha ?? "000000000000000000000000000000000"));
                    githubctx.Add("repository_owner", new StringContextData(hook?.repository?.Owner?.login ?? "Unknown"));
                    githubctx.Add("ref", new StringContextData(Ref));
                    // TODO check if it is protected
                    githubctx.Add("ref_protected", new BooleanContextData(false));
                    githubctx.Add("ref_type", new StringContextData(Ref.StartsWith("refs/tags/") ? "tag" : Ref.StartsWith("refs/heads/") ? "branch" : ""));
                    githubctx.Add("ref_name", new StringContextData(Ref.StartsWith("refs/tags/") ? Ref.Substring("refs/tags/".Length) : Ref.StartsWith("refs/heads/") ? Ref.Substring("refs/heads/".Length) : ""));
                    if(AllowJobNameOnJobProperties) {
                        githubctx.Add("job", new StringContextData(jobname));
                    }
                    githubctx.Add("head_ref", new StringContextData(hook?.pull_request?.head?.Ref ?? ""));// only for PR
                    githubctx.Add("base_ref", new StringContextData(hook?.pull_request?.Base?.Ref ?? ""));// only for PR
                    // event_path is filled by event
                    githubctx.Add("event", payloadObject.ToPipelineContextData());
                    githubctx.Add("event_name", new StringContextData(callingJob?.Event ?? event_name));
                    githubctx.Add("actor", new StringContextData(hook?.sender?.login));
                    githubctx.Add("run_id", new StringContextData(runid.ToString()));
                    githubctx.Add("run_number", new StringContextData(runnumber.ToString()));
                    githubctx.Add("retention_days", new StringContextData("90"));
                    githubctx.Add("run_attempt", new StringContextData(attempt.Attempt.ToString()));
                    return contextData;
                };
                Action<DictionaryContextData, JobItem, JobCompletedEvent> updateNeedsCtx = (needsctx, job, e) => {
                    NeedsTaskResult? oldstatus = null;
                    PipelineContextData oldjobctx;
                    IDictionary<string, VariableValue> dependentOutputs = e.Outputs != null ? new Dictionary<string, VariableValue>(e.Outputs) : new Dictionary<string, VariableValue>();
                    if(needsctx.TryGetValue(job.name, out oldjobctx) && oldjobctx is DictionaryContextData _ctx) {
                        if(_ctx.ContainsKey("result") && _ctx["result"] is StringContextData res) {
                            oldstatus = Enum.Parse<NeedsTaskResult>(res, true);
                        }
                        // Parity: empty job outputs doesn't override non empty outputs of matrix jobs
                        if(_ctx.ContainsKey("outputs") && _ctx["outputs"] is DictionaryContextData outputs) {
                            foreach(var output in outputs) {
                                if(!dependentOutputs.TryGetValue(output.Key, out var val) || string.IsNullOrEmpty(val?.Value)) {
                                    dependentOutputs[output.Key] = new VariableValue(output.Value.AssertString($"needs.{job.name}.outputs.{output.Key}").Value, false);
                                }
                            }
                        }
                    }
                    DictionaryContextData jobctx = new DictionaryContextData();
                    needsctx[job.name] = jobctx;
                    var outputsctx = new DictionaryContextData();
                    jobctx["outputs"] = outputsctx;
                    foreach (var item in dependentOutputs) {
                        outputsctx.Add(item.Key, new StringContextData(item.Value.Value));
                    }
                    NeedsTaskResult result = NeedsTaskResult.Failure;
                    job.Status = e.Result;
                    switch(e.Result) {
                        case TaskResult.Failed:
                        case TaskResult.Abandoned:
                            result = job.ContinueOnError ? NeedsTaskResult.Success : NeedsTaskResult.Failure;
                            break;
                        case TaskResult.Canceled:
                            result = job.ContinueOnError ? NeedsTaskResult.Success : NeedsTaskResult.Cancelled;
                            break;
                        case TaskResult.Succeeded:
                        case TaskResult.SucceededWithIssues:
                            result = NeedsTaskResult.Success;
                            break;
                        case TaskResult.Skipped:
                            result = NeedsTaskResult.Skipped;
                            break;
                    }
                    if(result != oldstatus && oldstatus != null) {
                        if(result  == NeedsTaskResult.Cancelled || oldstatus == NeedsTaskResult.Cancelled) {
                            result = NeedsTaskResult.Cancelled;
                        } else if(result  == NeedsTaskResult.Failure || oldstatus == NeedsTaskResult.Failure) {
                            result = NeedsTaskResult.Failure;
                        } else if(result  == NeedsTaskResult.Success || oldstatus == NeedsTaskResult.Success) {
                            result = NeedsTaskResult.Success;
                        }
                    }
                    jobctx.Add("result", new StringContextData(result.ToString().ToLowerInvariant()));
                };
                FinishJobController.JobCompleted workflowcomplete = null;
                TemplateToken workflowPermissions = null;
                TemplateToken workflowConcurrency = null;
                var jobnamebuilder = new ReferenceNameBuilder();
                foreach (var actionPair in actionMapping)
                {
                    var propertyName = actionPair.Key.AssertString($"action.yml property key");

                    switch (propertyName.Value)
                    {
                        case "jobs":
                        var jobs = actionPair.Value.AssertMapping("jobs");
                        List<string> errors = new List<string>();
                        foreach (var job in jobs) {
                            var jn = job.Key.AssertString($"action.yml property key");
                            var jnerror = "";
                            // Validate Jobname
                            if(!jobnamebuilder.TryAddKnownName(jn.Value, out jnerror)) {
                                errors.Add(jnerror);
                            }
                        }
                        if(errors.Count > 0) {
                            var b = new StringBuilder();
                            int i = 0;
                            foreach (var error in errors) {
                                if(i++ != 0) {
                                    b.Append(". ");
                                }
                                b.Append(error);
                            }
                            throw new Exception(b.ToString());
                        }
                        foreach (var job in jobs) {
                            var jn = job.Key.AssertString($"action.yml property key");
                            var jobname = jn.Value;
                            var run = job.Value.AssertMapping("jobs");
                            var jobitem = new JobItem() { name = jobname, Id = Guid.NewGuid() };
                            dependentjobgroup.Add(jobitem);

                            var needs = (from r in run where r.Key.AssertString("needs").Value == "needs" select r).FirstOrDefault().Value;
                            List<string> neededJobs = new List<string>();
                            if (needs != null) {
                                if(needs is SequenceToken sq) {
                                    neededJobs.AddRange(from need in sq select need.AssertString("list of strings").Value);
                                } else {
                                    neededJobs.Add(needs.AssertString("needs is invalid").Value);
                                }
                            }
                            var contextData = createContext(jobname);
                            
                            var needsctx = new DictionaryContextData();
                            contextData.Add("needs", needsctx);
                            contextData["strategy"] = null;
                            contextData["matrix"] = null;

                            FinishJobController.JobCompleted handler = e => {
                                try {
                                    if(neededJobs.Count > 0) {
                                        if(e == null) {
                                            if(!NoRecursiveNeedsCtx && jobitem.Dependencies != null) {
                                                neededJobs = jobitem.Dependencies.Keys.ToList();
                                            }
                                            return;
                                        }
                                        if(neededJobs.RemoveAll(name => {
                                            var job = (from j in jobgroup where j.name == name && j.Id == e.JobId select j).FirstOrDefault();
                                            if(job != null) {
                                                updateNeedsCtx(needsctx, job, e);
                                                return true;
                                            }
                                            return false;
                                        }) == 0 || neededJobs.Count > 0) {
                                            return;
                                        }
                                    }

                                    dependentjobgroup.Remove(jobitem);
                                    if(!dependentjobgroup.Any()) {
                                        jobgroup.Clear();
                                    } else {
                                        jobgroup.Add(jobitem);
                                    }

                                    exctx.JobContext = jobitem;
                                    var jid = jobitem.Id;
                                    jobitem.TimelineId = Guid.NewGuid();
                                    var jobrecord = new TimelineRecord{ Id = jobitem.Id, Name = jobitem.name };
                                    TimelineController.dict[jobitem.TimelineId] = ( new List<TimelineRecord>{ jobrecord }, new System.Collections.Concurrent.ConcurrentDictionary<System.Guid, System.Collections.Generic.List<GitHub.DistributedTask.WebApi.TimelineRecordLogLine>>() );
                                    templateContext.TraceWriter = new TraceWriter2(line => TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(jobitem.Id, new List<string>{ line }), jobitem.TimelineId, jobitem.Id));
                                    templateContext.Errors.Clear();
                                    var _jobdisplayname = (from r in run where r.Key.AssertString("name").Value == "name" select r.Value.ToString()).FirstOrDefault() ?? jobitem.name;
                                    if(callingJob?.Name != null) {
                                        _jobdisplayname = callingJob.Name + " / " + _jobdisplayname;
                                    }
                                    jobrecord.Name = _jobdisplayname;
                                    jobitem.DisplayName = _jobdisplayname;
                                    // For Runner.Client to show the workflowname
                                    initializingJobs.TryAdd(jobitem.Id, new Job() { JobId = jobitem.Id, TimeLineId = jobitem.TimelineId, name = jobitem.DisplayName, workflowname = workflowname, runid = runid, RequestId = jobitem.RequestId } );
                                    new TimelineController(_context).UpdateTimeLine(jobitem.TimelineId, new VssJsonCollectionWrapper<List<TimelineRecord>>(TimelineController.dict[jobitem.TimelineId].Item1));
                                    templateContext.TraceWriter.Info("{0}", $"Evaluate if");
                                    var ifexpr = (from r in run where r.Key.AssertString("str").Value == "if" select r).FirstOrDefault().Value;//?.AssertString("if")?.Value;
                                    var condition = new BasicExpressionToken(null, null, null, PipelineTemplateConverter.ConvertToIfCondition(templateContext, ifexpr, true));
                                    var recusiveNeedsctx = needsctx;
                                    if(!NoRecursiveNeedsCtx) {
                                        needsctx = new DictionaryContextData();
                                        contextData["needs"] = needsctx;
                                        foreach(var need in jobitem.Needs) {
                                            needsctx[need] = recusiveNeedsctx[need];
                                        }
                                    }
                                    jobitem.EvaluateIf = () => {
                                        templateContext.ExpressionValues.Clear();
                                        foreach (var pair in contextData) {
                                            templateContext.ExpressionValues[pair.Key] = pair.Value;
                                        }
                                        // It seems that the offical actions service does provide a recusive needs ctx, but only for if expressions.
                                        templateContext.ExpressionValues["needs"] = recusiveNeedsctx;
                                        templateContext.Errors.Clear();
                                        var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.JobIfResult, condition, 0, fileId, true);
                                        templateContext.Errors.Check();
                                        return PipelineTemplateConverter.ConvertToIfResult(templateContext, eval);
                                    };
                                    Action<TaskResult> sendFinishJob = result => {
                                        var _job = new Job() { message = null, repo = repository_name, WorkflowRunAttempt = attempt, WorkflowIdentifier = callingJob?.Id != null ? callingJob.Id + "/" + jobitem.name : jobitem.name, name = _jobdisplayname, workflowname = workflowname, runid = runid, JobId = jid, RequestId = jobitem.RequestId, TimeLineId = jobitem.TimelineId};
                                        AddJob(_job);
                                        new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = jobitem.Id, Result = result, RequestId = jobitem.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                                    };
                                    try {
                                        var _res = jobitem.EvaluateIf();
                                        if(!_res) {
                                            sendFinishJob(TaskResult.Skipped);
                                            return;
                                        }
                                        var rawstrategy = (from r in run where r.Key.AssertString("strategy").Value == "strategy" select r).FirstOrDefault().Value;
                                        var flatmatrix = new List<Dictionary<string, TemplateToken>> { new Dictionary<string, TemplateToken>() };
                                        var includematrix = new List<Dictionary<string, TemplateToken>> { };
                                        SequenceToken include = null;
                                        SequenceToken exclude = null;
                                        bool failFast = true;
                                        double? max_parallel = null;
                                        if (rawstrategy != null) {
                                            templateContext.TraceWriter.Info("{0}", "Evaluate strategy");
                                            templateContext.Errors.Clear();
                                            var strategy = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.Strategy, rawstrategy, 0, fileId, true)?.AssertMapping("strategy");
                                            templateContext.Errors.Check();
                                            failFast = (from r in strategy where r.Key.AssertString("fail-fast").Value == "fail-fast" select r).FirstOrDefault().Value?.AssertBoolean("fail-fast")?.Value ?? failFast;
                                            max_parallel = (from r in strategy where r.Key.AssertString("max-parallel").Value == "max-parallel" select r).FirstOrDefault().Value?.AssertNumber("max-parallel")?.Value;
                                            var matrix = (from r in strategy where r.Key.AssertString("matrix").Value == "matrix" select r).FirstOrDefault().Value?.AssertMapping("matrix");
                                            if(matrix != null) {
                                                foreach (var item in matrix)
                                                {
                                                    var key = item.Key.AssertString("Key").Value;
                                                    switch (key)
                                                    {
                                                        case "include":
                                                            include = item.Value?.AssertSequence("include");
                                                            break;
                                                        case "exclude":
                                                            exclude = item.Value?.AssertSequence("exclude");
                                                            break;
                                                        default:
                                                            var val = item.Value.AssertSequence("seq");
                                                            var next = new List<Dictionary<string, TemplateToken>>();
                                                            foreach (var mel in flatmatrix)
                                                            {
                                                                foreach (var n in val)
                                                                {
                                                                    var ndict = new Dictionary<string, TemplateToken>(mel);
                                                                    ndict.Add(key, n);
                                                                    next.Add(ndict);
                                                                }
                                                            }
                                                            flatmatrix = next;
                                                            break;
                                                    }
                                                }
                                                if (exclude != null)
                                                {
                                                    foreach (var item in exclude)
                                                    {
                                                        var map = item.AssertMapping("exclude item").ToDictionary(k => k.Key.AssertString("key").Value, k => k.Value);
                                                        flatmatrix.RemoveAll(dict =>
                                                        {
                                                            foreach (var item in map)
                                                            {
                                                                TemplateToken val;
                                                                if (!dict.TryGetValue(item.Key, out val) || !TemplateTokenEqual(item.Value, val)) {
                                                                    return false;
                                                                }
                                                            }
                                                            templateContext.TraceWriter.Info("{0}", $"Removing {string.Join(',', from m in dict select m.Key + ":" + (m.Value?.ToContextData()?.ToJToken()?.ToString() ?? "null"))} from matrix, due exclude entry {string.Join(',', from m in map select m.Key + ":" + (m.Value?.ToContextData()?.ToJToken()?.ToString() ?? "null"))}");
                                                            return true;
                                                        });
                                                    }
                                                }
                                            }
                                            if(flatmatrix.Count == 0) {
                                                templateContext.TraceWriter.Info("{0}", $"Matrix is empty, adding an empty entry");
                                                // Fix empty matrix after exclude
                                                flatmatrix.Add(new Dictionary<string, TemplateToken>());
                                            }
                                        }
                                        // Enforce job matrix limit of github
                                        if(flatmatrix.Count > 256) {
                                            templateContext.TraceWriter.Info("{0}", $"Failure: Matrix contains more than 256 entries after exclude");
                                            sendFinishJob(TaskResult.Failed);
                                            return;
                                        }
                                        var strategyctx = new DictionaryContextData();
                                        contextData["strategy"] = strategyctx;
                                        strategyctx["fail-fast"] = new BooleanContextData(failFast);
                                        strategyctx["max-parallel"] = max_parallel.HasValue ? new NumberContextData(max_parallel.Value) : null;
                                        var keys = flatmatrix.First().Keys.ToArray();
                                        if (include != null) {
                                            foreach (var item in include) {
                                                var map = item.AssertMapping("include item").ToDictionary(k => k.Key.AssertString("key").Value, k => k.Value);
                                                bool matched = false;
                                                if(keys.Length > 0) {
                                                    flatmatrix.ForEach(dict => {
                                                        foreach (var item in keys) {
                                                            TemplateToken val;
                                                            if (map.TryGetValue(item, out val) && !TemplateTokenEqual(dict[item], val)) {
                                                                return;
                                                            }
                                                        }
                                                        matched = true;
                                                        // Add missing keys
                                                        templateContext.TraceWriter.Info("{0}", $"Add missing keys to {string.Join(',', from m in dict select m.Key + ":" + (m.Value?.ToContextData()?.ToJToken()?.ToString() ?? "null"))}, due to include entry {string.Join(',', from m in map select m.Key + ":" + (m.Value?.ToContextData()?.ToJToken()?.ToString() ?? "null"))}");
                                                        foreach (var item in map) {
                                                            dict[item.Key] = item.Value;
                                                        }
                                                    });
                                                }
                                                if (!matched) {
                                                    templateContext.TraceWriter.Info("{0}", $"Append include entry {string.Join(',', from m in map select m.Key + ":" + (m.Value?.ToContextData()?.ToJToken()?.ToString() ?? "null"))}, due to match miss");
                                                    includematrix.Add(map);
                                                }
                                            }
                                        }

                                        // Filter matrix from cli
                                        if(jobname == selectedJob && _matrix?.Length > 0) {
                                            var mdict = new Dictionary<string, TemplateToken>();
                                            foreach(var m_ in _matrix) {
                                                var i = m_.IndexOf(":");
                                                using (var stringReader = new StringReader(m_.Substring(i + 1))) {
                                                    var yamlObjectReader = new YamlObjectReader(fileId, stringReader);
                                                    mdict[m_.Substring(0, i)] = TemplateReader.Read(templateContext, "any", yamlObjectReader, null, out _);
                                                }
                                            }
                                            Predicate<Dictionary<string, TemplateToken>> match = dict => {
                                                foreach(var kv in mdict) {
                                                    TemplateToken val;
                                                    if(!dict.TryGetValue(kv.Key, out val) || !TemplateTokenEqual(kv.Value, val)) {
                                                        return true;
                                                    }
                                                }
                                                return false;
                                            };
                                            flatmatrix.RemoveAll(match);
                                            includematrix.RemoveAll(match);
                                            if(flatmatrix.Count + includematrix.Count == 0) {
                                                templateContext.TraceWriter.Info("{0}", $"Your specified matrix filter doesn't match any matrix entries");
                                                sendFinishJob(TaskResult.Skipped);
                                                return;
                                            }
                                        }
                                        var jobTotal = flatmatrix.Count + includematrix.Count;
                                        if(flatmatrix.Count == 1 && keys.Length == 0 && jobTotal > 1) {
                                            jobTotal--;
                                        }
                                        // Enforce job matrix limit of github
                                        if(jobTotal > 256) {
                                            templateContext.TraceWriter.Info("{0}", $"Failure: Matrix contains more than 256 entries after exclude");
                                            sendFinishJob(TaskResult.Failed);
                                            return;
                                        }
                                        bool? _canBeCancelled = null;
                                        Func<bool> canBeCancelled = () => {
                                            if(_canBeCancelled != null) {
                                                return _canBeCancelled.Value;
                                            }
                                            bool ret = !jobitem.EvaluateIf();
                                            _canBeCancelled = ret;
                                            return ret;
                                        };
                                        if(exctx.Cancelled.IsCancellationRequested && canBeCancelled()) {
                                            sendFinishJob(TaskResult.Canceled);
                                            return;
                                        }
                                        strategyctx["job-total"] = new NumberContextData( jobTotal );
                                        if(jobTotal > 1) {
                                            jobitem.Childs = new List<JobItem>();
                                            var _job = new Job() { message = null, repo = repository_name, WorkflowRunAttempt = attempt, WorkflowIdentifier = callingJob?.Id != null ? callingJob.Id + "/" + jobitem.name : jobitem.name, name = jobitem.DisplayName, workflowname = workflowname, runid = runid, JobId = jid, RequestId = jobitem.RequestId, TimeLineId = jobitem.TimelineId};
                                            AddJob(_job);
                                        }
                                        {
                                            int i = 0;
                                            Func<IEnumerable<string>, string> defaultDisplayName = item => {
                                                var displayname = new StringBuilder(jobname);
                                                int z = 0;
                                                foreach (var mk in item) {
                                                    displayname.Append(z++ == 0 ? " (" : ", ");
                                                    displayname.Append(mk);
                                                }
                                                if(z > 0) {
                                                    displayname.Append( ")");
                                                }
                                                return displayname.ToString();
                                            };
                                            var usesJob = (from r in run where r.Key.AssertString("str").Value == "uses" select r).FirstOrDefault().Value != null;
                                            Func<string, Dictionary<string, TemplateToken>, Func<bool, Job>> act = (displayname, item) => {
                                                int c = i++;
                                                strategyctx["job-index"] = new NumberContextData((double)(c));
                                                DictionaryContextData matrixContext = null;
                                                if(item.Any()) {
                                                    matrixContext = new DictionaryContextData();
                                                    foreach (var mk in item) {
                                                        PipelineContextData data = mk.Value.ToContextData();
                                                        matrixContext.Add(mk.Key, data);
                                                    }
                                                }
                                                contextData["matrix"] = matrixContext;
                                                if(finishedJobs != null && !usesJob) {
                                                    if(finishedJobs.TryGetValue(jobname, out var fjobs)) {
                                                        foreach(var fjob in fjobs) {
                                                            if(TemplateTokenEqual(matrixContext?.ToTemplateToken() ?? new NullToken(null, null, null), fjob.MatrixToken)) {
                                                                var _next = jobTotal > 1 ? new JobItem() { name = jobitem.name, Id = fjob.JobId, NoFailFast = true } : jobitem;
                                                                _next.TimelineId = fjob.TimeLineId;
                                                                jobitem.Childs?.Add(_next);
                                                                return b => {
                                                                    var jevent = new JobCompletedEvent(_next.RequestId, _next.Id, fjob.Result.Value, fjob.Outputs.ToDictionary(o => o.Name, o => new VariableValue(o.Value, false)));
                                                                    workflowcomplete(jevent);
                                                                    return fjob;
                                                                };
                                                            }
                                                        }
                                                    }
                                                    if(callingJob != null) {
                                                        callingJob.RanJob = true;
                                                    }
                                                    Array.ForEach(finishedJobs.ToArray(), fjobs => {
                                                        foreach(var djob in dependentjobgroup) {
                                                            if(djob.Dependencies != null && (fjobs.Key == djob.name || fjobs.Key.StartsWith(djob.name + "/"))) {
                                                                foreach(var dep in djob.Dependencies) {
                                                                    if(dep.Key == jobname) {
                                                                        finishedJobs.Remove(fjobs.Key);
                                                                        return;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    });
                                                }
                                                var next = jobTotal > 1 ? new JobItem() { name = jobitem.name, Id = Guid.NewGuid() } : jobitem;
                                                if(jobTotal > 1) {
                                                    next.TimelineId = Guid.NewGuid();
                                                    // For Runner.Client to show the workflowname
                                                    initializingJobs.TryAdd(next.Id, new Job() { JobId = next.Id, TimeLineId = next.TimelineId, name = displayname, workflowname = workflowname, runid = runid, RequestId = next.RequestId } );
                                                    TimelineController.dict[next.TimelineId] = ( new List<TimelineRecord>{ new TimelineRecord{ Id = next.Id, Name = displayname } }, new System.Collections.Concurrent.ConcurrentDictionary<System.Guid, System.Collections.Generic.List<GitHub.DistributedTask.WebApi.TimelineRecordLogLine>>() );
                                                    new TimelineController(_context).UpdateTimeLine(next.TimelineId, new VssJsonCollectionWrapper<List<TimelineRecord>>(TimelineController.dict[next.TimelineId].Item1));
                                                }
                                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ $"Prepare Job for execution" }), next.TimelineId, next.Id);
                                                templateContext.TraceWriter = new TraceWriter2(line => {
                                                    TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ line }), next.TimelineId, next.Id);
                                                });
                                                jobitem.Childs?.Add(next);
                                                templateContext.ExpressionValues.Clear();
                                                foreach (var pair in contextData) {
                                                    templateContext.ExpressionValues[pair.Key] = pair.Value;
                                                }
                                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ $"Evaluate job name" }), next.TimelineId, next.Id);
                                                var _jobdisplayname = (from r in run where r.Key.AssertString("name").Value == "name" select GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "string-strategy-context", r.Value, 0, fileId, true).AssertString("job name must be a string").Value).FirstOrDefault() ?? displayname;
                                                templateContext.Errors.Check();
                                                if(callingJob?.Name != null) {
                                                    _jobdisplayname = callingJob.Name + " / " + _jobdisplayname;
                                                }
                                                next.DisplayName = _jobdisplayname;
                                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ $"Evaluate job ContinueOnError" }), next.TimelineId, next.Id);
                                                next.ContinueOnError = (from r in run where r.Key.AssertString("continue-on-error").Value == "continue-on-error" select GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "boolean-strategy-context", r.Value, 0, fileId, true).AssertBoolean("continue-on-error be a boolean").Value).FirstOrDefault();
                                                templateContext.Errors.Check();
                                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ $"Evaluate job timeoutMinutes" }), next.TimelineId, next.Id);
                                                var timeoutMinutes = (from r in run where r.Key.AssertString("timeout-minutes").Value == "timeout-minutes" select GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "number-strategy-context", r.Value, 0, fileId, true).AssertNumber("timeout-minutes be a number").Value).Append(360).First();
                                                templateContext.Errors.Check();
                                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ $"Evaluate job cancelTimeoutMinutes" }), next.TimelineId, next.Id);
                                                var cancelTimeoutMinutes = (from r in run where r.Key.AssertString("cancel-timeout-minutes").Value == "cancel-timeout-minutes" select GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "number-strategy-context", r.Value, 0, fileId, true).AssertNumber("cancel-timeout-minutes be a number").Value).Append(5).First();
                                                templateContext.Errors.Check();
                                                next.NoStatusCheck = usesJob;
                                                next.ActionStatusQueue.Post(() => updateJobStatus(next, null));
                                                return queueJob(templateContext, workflowDefaults, workflowEnvironment, _jobdisplayname, run, contextData.Clone() as DictionaryContextData, next.Id, next.TimelineId, repository_name, jobname, workflowname, runid, runnumber, secrets, timeoutMinutes, cancelTimeoutMinutes, next.ContinueOnError, platform ?? new String[] { }, localcheckout, next.RequestId, Ref, Sha, callingJob?.Event ?? event_name, callingJob?.Event, workflows, statusSha, callingJob?.Id, finishedJobs, attempt, next, workflowPermissions, callingJob, dependentjobgroup);
                                            };
                                            ConcurrentQueue<Func<bool, Job>> jobs = new ConcurrentQueue<Func<bool, Job>>();
                                            if(keys.Length != 0 || includematrix.Count == 0) {
                                                foreach (var item in flatmatrix) {
                                                    if(exctx.Cancelled.IsCancellationRequested && canBeCancelled()) {
                                                        while(jobs.TryDequeue(out var cb)) {
                                                            cb(true);
                                                        }
                                                        return;
                                                    }
                                                    var j = act(defaultDisplayName(from displayitem in keys.SelectMany(key => item[key].Traverse(true)) where !(displayitem is SequenceToken || displayitem is MappingToken) select displayitem.ToString()), item);
                                                    if(j != null) {
                                                        jobs.Enqueue(j);
                                                    }
                                                }
                                            }
                                            foreach (var item in includematrix) {
                                                if(exctx.Cancelled.IsCancellationRequested && canBeCancelled()) {
                                                    while(jobs.TryDequeue(out var cb)) {
                                                        cb(true);
                                                    }
                                                    return;
                                                }
                                                var j = act(defaultDisplayName(from displayitem in item.SelectMany(it => it.Value.Traverse(true)) where !(displayitem is SequenceToken || displayitem is MappingToken) select displayitem.ToString()), item);
                                                if(j != null) {
                                                    jobs.Enqueue(j);
                                                }
                                            }
                                            List<Job> scheduled = new List<Job>();
                                            FinishJobController.JobCompleted handler2 = null;
                                            Action cleanupOnFinish = () => {
                                                if (scheduled.Count == 0) {
                                                    localJobCompletedEvents.JobCompleted -= handler2;
                                                    if(jobTotal > 1) {
                                                        new FinishJobController(_cache, _context).InvokeJobCompleted(jobitem.JobCompletedEvent);
                                                    }
                                                }
                                            };
                                            Action cancelAll = () => {
                                                FinishJobController.OnJobCompleted -= handler2;
                                                foreach (var _j in scheduled) {
                                                    if(!(exctx.Cancelled.IsCancellationRequested && canBeCancelled())) {
                                                        TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(_j.JobId, new List<string>{ "FailFast Matrix job with ContinueOnError=false requests Cancellation" }), _j.TimeLineId, _j.JobId);
                                                    }
                                                    _j.CancelRequest?.Cancel();
                                                    if(_j.SessionId == Guid.Empty) {
                                                        new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = _j.JobId, Result = TaskResult.Canceled, RequestId = _j.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                                                    }
                                                }
                                                scheduled.Clear();
                                                while(jobs.TryDequeue(out var cb)) {
                                                    cb(true);
                                                }
                                                cleanupOnFinish();
                                            };
                                            handler2 = e => {
                                                if(scheduled.RemoveAll(j => j.JobId == e.JobId) > 0) {
                                                    var currentItem = jobitem.Childs?.Find(ji => ji.Id == e.JobId) ?? (jobitem.Id == e.JobId ? jobitem : null);
                                                    if(jobitem.JobCompletedEvent == null) {
                                                        jobitem.JobCompletedEvent = new JobCompletedEvent() { JobId = jobitem.Id, Result = e.Result, RequestId = jobitem.RequestId, Outputs = new Dictionary<String, VariableValue>(e.Outputs)};
                                                    } else {
                                                        if((e.Result == TaskResult.Failed || e.Result == TaskResult.Canceled || e.Result == TaskResult.Abandoned) && (currentItem == null || currentItem.ContinueOnError != true)) {
                                                            jobitem.JobCompletedEvent.Result = TaskResult.Failed;
                                                        } else if(jobitem.JobCompletedEvent.Result == TaskResult.Succeeded || jobitem.JobCompletedEvent.Result == TaskResult.SucceededWithIssues || e.Result != TaskResult.Skipped) {
                                                            jobitem.JobCompletedEvent.Result = TaskResult.Succeeded;
                                                        }
                                                        foreach(var output in e.Outputs) {
                                                            if(!string.IsNullOrEmpty(output.Value.Value)) {
                                                                jobitem.JobCompletedEvent.Outputs[output.Key] = output.Value;
                                                            }
                                                        }
                                                    }
                                                    if(failFast && (e.Result == TaskResult.Failed || e.Result == TaskResult.Canceled || e.Result == TaskResult.Abandoned) && (currentItem == null || (currentItem.ContinueOnError != true && currentItem.NoFailFast != true))) {
                                                        cancelAll();
                                                    } else {
                                                        while((!max_parallel.HasValue || scheduled.Count < max_parallel.Value) && jobs.TryDequeue(out var cb)) {
                                                            if(exctx.Cancelled.IsCancellationRequested && canBeCancelled()) {
                                                                cb(true);
                                                                cancelAll();
                                                                return;
                                                            }
                                                            var jret = cb(false);
                                                            if(jret != null) {
                                                                scheduled.Add(jret);
                                                                return;
                                                            } else if(failFast) {
                                                                cancelAll();
                                                                return;
                                                            }
                                                        }
                                                        cleanupOnFinish();
                                                    }
                                                }
                                            };
                                            localJobCompletedEvents.JobCompleted += handler2;
                                            for (int j = 0; j < (max_parallel.HasValue ? (int)max_parallel.Value : jobTotal) && jobs.TryDequeue(out var cb2); j++) {
                                                if(exctx.Cancelled.IsCancellationRequested && canBeCancelled()) {
                                                    cb2(true);
                                                    cancelAll();
                                                    return;
                                                }
                                                var jret = cb2(false);
                                                if(jret != null) {
                                                    scheduled.Add(jret);
                                                } else if (failFast) {
                                                    cancelAll();
                                                    return;
                                                }
                                            }
                                            cleanupOnFinish();
                                        }
                                    } catch(Exception ex) {
                                        templateContext.TraceWriter.Info("{0}", $"Internal Error: {ex.Message}, {ex.StackTrace}");
                                        sendFinishJob(TaskResult.Failed);
                                    }
                                } catch(Exception ex) {
                                    Console.WriteLine($"Internal Error: {ex.Message}, {ex.StackTrace}"); 
                                    dependentjobgroup.Remove(jobitem);
                                    if(!dependentjobgroup.Any()) {
                                        jobgroup.Clear();
                                    } else {
                                        jobgroup.Add(jobitem);
                                    }
                                    if(!(jobitem.Childs?.RemoveAll(ji => {
                                        Job job = GetJob(ji.Id);
                                        if(job != null) {
                                            job.CancelRequest?.Cancel();
                                            if(job.SessionId == Guid.Empty) {
                                                new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Failed, RequestId = job.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                                            }
                                        }
                                        return true;
                                    }) > 0)) {
                                        new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = jobitem.Id, Result = TaskResult.Failed, Outputs = new Dictionary<String, VariableValue>() });
                                    }
                                }
                            };
                            jobitem.OnJobEvaluatable = handler;
                            jobitem.Needs = neededJobs.ToArray();
                        }
                        break;
                        case "defaults":
                        workflowDefaults = actionPair.Value;
                        break;
                        case "env":
                        workflowEnvironment.Add(actionPair.Value);
                        break;
                        case "permissions":
                        workflowPermissions = actionPair.Value;
                        break;
                        case "concurrency":
                        {
                            templateContext.TraceWriter.Info("{0}", $"Evaluate workflow concurrency");
                            var contextData = createContext("");
                            templateContext.ExpressionValues.Clear();
                            foreach (var pair in contextData) {
                                templateContext.ExpressionValues[pair.Key] = pair.Value;
                            }
                            workflowConcurrency = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "workflow-concurrency", actionPair.Value, 0, null, true);
                            templateContext.Errors.Check();
                        }
                        break;
                    }
                }
                if(!dependentjobgroup.Any()) {
                    throw new Exception("Your workflow is invalid, you have to define at least one job");
                }
                if(selectedJob != null) {
                    List<JobItem> next = new List<JobItem>();
                    dependentjobgroup.RemoveAll(j => {
                        if(j.name == selectedJob) {
                            next.Add(j);
                            return true;
                        }
                        return false;
                    });
                    while(true) {
                        int oldCount = next.Count;
                        dependentjobgroup.RemoveAll(j => {
                            foreach(var j2 in next) {
                                foreach(var need in j2.Needs) {
                                    if(j.name == need) {
                                        next.Add(j);
                                        return true;
                                    }
                                }
                            }
                            return false;
                        });
                        if(oldCount == next.Count) {
                            break;
                        }
                    }
                    dependentjobgroup = next;
                    exctx.workflow = dependentjobgroup.ToList();
                    if(exctx.workflow.Count == 0) {
                        return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                    }
                }
                dependentjobgroup.ForEach(ji => {
                    if(ji.Needs?.Length > 0) {
                        Func<JobItem, List<string>, Dictionary<string, JobItem>> pred = null;
                        pred = (cur, cyclic) => {
                            var ret = new Dictionary<string, JobItem>();
                            if(cur.Needs?.Length > 0) {
                                var pcyclic = cyclic.Append(cur.name).ToList();
                                List<string> missingDeps = cur.Needs.ToList();
                                dependentjobgroup.ForEach(d => {
                                    if(cur.Needs.Contains(d.name)) {
                                        if(pcyclic.Contains(d.name)) {
                                            throw new Exception("Cyclic Dependency detected");
                                        }
                                        ret[d.name] = d;
                                        if(d.Dependencies == null) {
                                            d.Dependencies = pred?.Invoke(d, pcyclic);
                                            foreach (var k in d.Dependencies) {
                                                ret[k.Key] = k.Value;
                                            }
                                        } else {
                                            foreach (var k in d.Dependencies) {
                                                if(pcyclic.Contains(k.Key)) {
                                                    throw new Exception("Cyclic Dependency detected");
                                                }
                                                ret[k.Key] = k.Value;
                                            }
                                        }
                                        missingDeps.Remove(d.name);
                                    }
                                });
                                if(missingDeps.Any()) {
                                    throw new Exception("Missing Dependency detected");
                                }
                            }
                            return ret;
                        };
                        if(ji.Dependencies == null)
                            ji.Dependencies = pred(ji, new List<string>());
                    }
                });
                if(list) {
                    return new HookResponse { repo = repository_name, run_id = runid, skipped = false, jobList = (from ji in dependentjobgroup select new JobListItem{Name= ji.name, Needs = ji.Needs}).ToList()};
                } else {
                    var jobs = dependentjobgroup.ToArray();
                    var finished = new CancellationTokenSource();
                    FinishJobController.JobCompleted withoutlock = e => {
                        var ja = jobs.Where(j => e.JobId == j.Id || (j.Childs?.Where(ji => e.JobId == ji.Id).Any() ?? false)).FirstOrDefault();
                        Action<JobItem> updateStatus = job => {
                            job.Status = e.Result;
                        };
                        if(ja != null) {
                            var ji = ja.Childs?.Where(ji => e.JobId == ji.Id).FirstOrDefault() ?? ja;
                            if(workflowOutputs != null && ja.Childs == null) {
                                updateNeedsCtx(jobsctx, ji, e);
                            }
                            ji.ActionStatusQueue.Post(() => {
                                return updateJobStatus(ji, e.Result);
                            });
                            if(e.JobId != ja.Id) {
                                var c = ja.Childs.Where(ji => e.JobId == ji.Id).First();
                                c.Completed = true;
                                updateStatus(c);
                                ja.Completed = ja.Childs.All(ji => ji.Completed);
                            } else {
                                ja.Completed = true;
                                updateStatus(ja);
                            }
                            if(jobs.All(j => j.Completed)) {
                                FinishJobController.OnJobCompletedAfter -= workflowcomplete;
                                exctx.workflow = jobs.ToList();
                                var evargs = new WorkflowEventArgs { runid = runid, Success = exctx.Success };
                                if(workflowOutputs != null) {
                                    try {
                                        templateContext.Errors.Clear();
                                        templateContext.TraceWriter = workflowTraceWriter;
                                        templateContext.TraceWriter.Info("{0}", $"Evaluate workflow_call outputs");
                                        var contextData = createContext("");
                                        contextData.Add("jobs", jobsctx);
                                        templateContext.ExpressionValues.Clear();
                                        foreach (var pair in contextData) {
                                            templateContext.ExpressionValues[pair.Key] = pair.Value;
                                        }
                                        var outputs = new Dictionary<string, VariableValue>();
                                        foreach(var entry in workflowOutputs) {
                                            var key = entry.Key.AssertString("on.workflow_call.outputs mapping key").Value;
                                            var value = entry.Value != null ? GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "workflow_call-output-context", (from kv in entry.Value.AssertMapping($"on.workflow_call.outputs.{key}") where kv.Key.AssertString($"on.workflow_call.outputs.{key} mapping key").Value == "value" select kv.Value).First(), 0, null, true).AssertString($"on.workflow_call.outputs.{key}.value").Value : null;
                                            templateContext.Errors.Check();
                                            outputs[key] = new VariableValue(value, false);
                                        }
                                        evargs.Outputs = outputs;
                                    } catch {
                                        evargs.Success = false;
                                    }
                                }
                                finished.Cancel();
                                if(callingJob?.Workflowfinish != null) {
                                    callingJob.Workflowfinish.Invoke(callingJob, evargs);
                                } else {
                                    cancelWorkflows.Remove(runid);
                                    workflowevent?.Invoke(evargs);
                                    _context.Dispose();
                                }
                            } else {
                                jobCompleted(e);
                            }
                        }
                    };
                    ConcurrentQueue<JobCompletedEvent> queue = new ConcurrentQueue<JobCompletedEvent>();
                    SemaphoreSlim s = new SemaphoreSlim(1, 1);
                    workflowcomplete = (e) => {
                        if(s.Wait(0)) {      
                            try {
                                if(e != null) {
                                    withoutlock(e);
                                }
                                JobCompletedEvent ev;
                                while(queue.TryDequeue(out ev)) {
                                    withoutlock(ev);
                                }
                            } finally {
                                s.Release();
                            }
                        } else {
                            queue.Enqueue(e);
                        }
                    };
                    CancellationTokenSource cancellationToken = null;
                    if(callingJob?.CancellationToken != null) {
                        cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(callingJob.CancellationToken.Value);
                        exctx.Cancelled = cancellationToken.Token;
                    } else {
                        var ctoken = new CancellationTokenSource();
                        if(cancelWorkflows.TryAdd(runid, ctoken)) {
                            exctx.Cancelled = ctoken.Token;
                            cancellationToken = ctoken;
                        }
                    }
                    asyncProcessing = true;
                    Task.Run(async () => {
                        try {
                            await Task.Delay(-1, finished.Token);
                        } catch {
                        }
                        if(callingJob == null) {
                            new TimelineController(_context).SyncLiveLogsToDb(workflowTimelineId);
                        }
                        // Cleanup dummy job for this workflow
                        if(workflowTimelineId == attempt.TimeLineId) {
                            initializingJobs.Remove(workflowTimelineId, out _);
                        }
                        // Cleanup dummy jobs, which allows Runner.Client to display the workflowname
                        foreach(var job in jobs) {
                            if(job.Childs != null) {
                                foreach(var ji in job.Childs) {
                                    initializingJobs.Remove(ji.Id, out _);
                                }
                            }
                            initializingJobs.Remove(job.Id, out _);
                        }
                    });
                    Action runWorkflow = () => {
                        s.Wait();
                        try {
                            Task.Run(async () => {
                                try {
                                    await Task.Delay(-1, CancellationTokenSource.CreateLinkedTokenSource(exctx.Cancelled, finished.Token).Token);
                                } catch {

                                }
                                if(!finished.IsCancellationRequested) {
                                    s.Wait();
                                    try {
                                        TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ "Workflow Cancellation Requested" }), workflowTimelineId, workflowRecordId);
                                        templateContext.TraceWriter = workflowTraceWriter;
                                        foreach(var job2 in jobs) {
                                            if(job2.Status == null && job2.EvaluateIf != null) {
                                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ $"Reevaluate Condition of {job2.DisplayName ?? job2.name}" }), workflowTimelineId, workflowRecordId);
                                                if(job2.EvaluateIf.Invoke() == false) {
                                                    foreach(var ji in job2.Childs ?? new List<MessageController.JobItem>{job2}) {
                                                        ji.Cancel.Cancel();
                                                        Job job = _cache.Get<Job>(ji.Id);
                                                        if(job != null) {
                                                            job.CancelRequest.Cancel();
                                                            if(job.SessionId == Guid.Empty) {
                                                                new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    } finally {
                                        s.Release();
                                    }
                                }
                            });
                            FinishJobController.OnJobCompletedAfter += workflowcomplete;
                            jobCompleted(null);
                        } finally {
                            s.Release();
                        }
                        workflowcomplete(null);
                    };
                    if(workflowConcurrency == null) {
                        runWorkflow();
                    } else {
                        string group = null;
                        bool cancelInprogress = false;
                        if(workflowConcurrency is StringToken stkn) {
                            group = stkn.Value;
                        } else {
                            var cmapping = workflowConcurrency.AssertMapping("workflowConcurrency must be a string or mapping");
                            group = (from r in cmapping where r.Key.AssertString("key").Value == "group" select r).FirstOrDefault().Value?.AssertString("group")?.Value;
                            cancelInprogress = (from r in cmapping where r.Key.AssertString("key").Value == "cancel-in-progress" select r).FirstOrDefault().Value?.AssertBoolean("cancel-in-progress")?.Value ?? cancelInprogress;
                        }
                        Action cancelPendingWorkflow = () => {
                            workflowTraceWriter.Info("{0}", "Workflow was cancelled by another workflow or job, while it was pending in the concurrency group");
                            finished.Cancel();
                            var evargs = new WorkflowEventArgs { runid = runid, Success = false };
                            if(callingJob?.Workflowfinish != null) {
                                callingJob.Workflowfinish.Invoke(callingJob, evargs);
                            } else {
                                cancelWorkflows.Remove(runid);
                                workflowevent?.Invoke(evargs);
                                _context.Dispose();
                            }
                        };

                        var key = $"{repository_name}/{group}";
                        while(true) {
                            ConcurrencyGroup cgroup = concurrencyGroups.GetOrAdd(key, name => new ConcurrencyGroup() { Key = name });
                            lock(cgroup) {
                                if(concurrencyGroups.TryGetValue(key, out var _cgroup) && cgroup != _cgroup) {
                                    continue;
                                }
                                ConcurrencyEntry centry = new ConcurrencyEntry();
                                centry.Run = async () => {
                                    if(exctx.Cancelled.IsCancellationRequested) {
                                        workflowTraceWriter.Info("{0}", $"Workflow was cancelled, while it was pending in the concurrency group");
                                    } else {
                                        workflowTraceWriter.Info("{0}", $"Starting Workflow run by concurrency group: {group}");
                                        runWorkflow();
                                        try {
                                            await Task.Delay(-1, finished.Token);
                                        } catch {
                                        }
                                    }
                                    cgroup.FinishRunning(centry);
                                };
                                centry.CancelPending = cancelPendingWorkflow;
                                centry.CancelRunning = cancelInProgress => {
                                    if(cancelInProgress) {
                                        workflowTraceWriter.Info("{0}", $"Workflow was cancelled by another workflow or job, while it was in progress in the concurrency group: {group}");
                                        cancellationToken.Cancel();
                                    }
                                };
                                cgroup.PushEntry(centry, cancelInprogress);
                                workflowTraceWriter.Info("{0}", $"Workflow was added to the concurrency group: {group}, cancel-in-progress: {cancelInprogress}");
                            }
                            break;
                        }
                    }
                }
            } catch(Exception ex) {
                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, ex.Message.Split('\n').ToList()), workflowTimelineId, workflowRecordId);
                updateJobStatus.Invoke(new JobItem() { DisplayName = "Fatal Failure", Status = TaskResult.Failed }, TaskResult.Failed);
                return new HookResponse { repo = repository_name, run_id = runid, skipped = false, failed = true };
            } finally {
                if(!asyncProcessing && callingJob == null) {
                    new TimelineController(_context).SyncLiveLogsToDb(workflowTimelineId);
                }
            }
            return new HookResponse { repo = repository_name, run_id = runid, skipped = false };
        }

        private static int reqId = 0;

        private class shared {
            public Channel<Task> Channel;
            public HttpResponse response;
            public MemoryStream stream { get; internal set; }
        }

        [HttpPost("multipartup/{id}")]
        public async Task UploadMulti(string id) {
            var sh = _cache.Get<shared>(id);
            var type = Request.Headers["Content-Type"].First();
            var ntype = "multipart/form-data" + type.Substring("application/octet-stream".Length);
            sh.response.Headers["Content-Type"] = new StringValues(ntype);
            var task = Request.Body.CopyToAsync(sh.response.Body);
            await sh.Channel.Writer.WriteAsync(task, HttpContext.RequestAborted);
            await task;
        }

        [HttpGet("multipart/{runid}")]
        public async Task GetMulti(long runid, [FromQuery] bool submodules, [FromQuery] bool nestedSubmodules) {
            var channel = Channel.CreateBounded<Task>(1);
            var sh = new shared();
            sh.Channel = channel;
            string id = runid + "__,dfuusnd" + reqId + "_" + new Random().NextDouble();
            _cache.Set(id, sh);
            OnRepoDownload?.Invoke(runid, "/test/host/_apis/v1/Message/multipartup/" + id, submodules, nestedSubmodules);
            sh.response = Response;
            var task = await channel.Reader.ReadAsync(HttpContext.RequestAborted);
            await task;
            _cache.Remove(id);
        }
        private Func<bool, Job> queueJob(TemplateContext templateContext, TemplateToken workflowDefaults, List<TemplateToken> workflowEnvironment, string displayname, MappingToken run, DictionaryContextData contextData, Guid jobId, Guid timelineId, string repo, string name, string workflowname, long runid, long runnumber, string[] secrets, double timeoutMinutes, double cancelTimeoutMinutes, bool continueOnError, string[] platform, bool localcheckout, long requestId, string Ref, string Sha, string wevent, string parentEvent, KeyValuePair<string, string>[] workflows = null, string statusSha = null, string parentId = null, Dictionary<string, List<Job>> finishedJobs = null, WorkflowRunAttempt attempt = null, JobItem ji = null, TemplateToken workflowPermissions = null, CallingJob callingJob = null, List<JobItem> dependentjobgroup = null)
        {
            var variables = new Dictionary<String, GitHub.DistributedTask.WebApi.VariableValue>(StringComparer.OrdinalIgnoreCase);
            variables.Add("system.github.job", new VariableValue(name, false));
            variables.Add("system.github.token", new VariableValue(GITHUB_TOKEN, true));
            variables.Add("system.github.token.permissions", new VariableValue("{}", false));
            Regex special = new Regex("[*'\",_&#^@\\/\r\n ]");
            variables.Add("system.phaseDisplayName", new VariableValue(special.Replace($"{workflowname}_{parentId}_{name}", "-"), false));
            variables.Add("system.runnerGroupName", new VariableValue("misc", false));
            variables.Add("system.runner.lowdiskspacethreshold", new VariableValue("100", false)); // actions/runner warns if free space is less than 100MB
            variables.Add("DistributedTask.NewActionMetadata", new VariableValue("true", false));
            variables.Add("DistributedTask.EnableCompositeActions", new VariableValue("true", false));
            variables.Add("DistributedTask.EnhancedAnnotations", new VariableValue("true", false));
            // For actions/upload-artifact@v1, actions/download-artifact@v1
            variables.Add(SdkConstants.Variables.Build.BuildId, new VariableValue(runid.ToString(), false));
            variables.Add(SdkConstants.Variables.Build.BuildNumber, new VariableValue(runid.ToString(), false));
            var resp = new ArtifactController(_context, configuration).CreateContainer(runid, attempt.Attempt, new CreateActionsStorageArtifactParameters() { Name = $"Artifact of {displayname}",  }).GetAwaiter().GetResult();
            variables.Add(SdkConstants.Variables.Build.ContainerId, new VariableValue(resp.Id.ToString(), false));
            foreach (var secret in this.secrets) {
                variables[secret.Name] = new VariableValue(secret.Value, true);
            }
            if(secrets != null) {
                LoadEnvSec(secrets, (name, value) => {
                    if(StringComparer.OrdinalIgnoreCase.Compare("github_token", name) == 0) {
                        variables["system.github.token"] = new VariableValue(value, true);
                    } else {
                        variables[name] = new VariableValue(value, true);
                    }
                });
            }
            var rawSteps = (from r in run where r.Key.AssertString("str").Value == "steps" select r).FirstOrDefault().Value?.AssertSequence("seq");
            if(rawSteps == null) {
                var rawUses = (from r in run where r.Key.AssertString("str").Value == "uses" select r).FirstOrDefault().Value?.AssertString("str");
                var rawWith = (from r in run where r.Key.AssertString("str").Value == "with" select r).FirstOrDefault().Value?.AssertMapping("map");
                var rawSecrets = (from r in run where r.Key.AssertString("str").Value == "secrets" select r).FirstOrDefault().Value?.AssertMapping("map");
                var uses = rawUses;
                RepositoryPathReference reference = null;
                if (uses.Value.StartsWith("./") || uses.Value.StartsWith(".\\"))
                {
                    reference = new RepositoryPathReference
                    {
                        RepositoryType = PipelineConstants.SelfAlias,
                        Path = uses.Value.Substring(2).Replace('\\', '/')
                    };
                }
                else
                {
                    var usesSegments = uses.Value.Split('@');
                    var pathSegments = usesSegments[0].Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    var gitRef = usesSegments.Length == 2 ? usesSegments[1] : String.Empty;
                    if (usesSegments.Length != 2 ||
                        pathSegments.Length < 2 ||
                        String.IsNullOrEmpty(pathSegments[0]) ||
                        String.IsNullOrEmpty(pathSegments[1]) ||
                        String.IsNullOrEmpty(gitRef))
                    {
                        // todo: loc
                        // context.Error(uses, $"Expected format {{org}}/{{repo}}[/path]@ref. Actual '{uses.Value}'");
                    }
                    else
                    {
                        var repositoryName = $"{pathSegments[0]}/{pathSegments[1]}";
                        var directoryPath = pathSegments.Length > 2 ? String.Join("/", pathSegments.Skip(2)) : String.Empty;

                        reference = new RepositoryPathReference
                        {
                            RepositoryType = RepositoryTypes.GitHub,
                            Name = repositoryName,
                            Ref = gitRef,
                            Path = directoryPath,
                        };
                    }
                }
                SecretMasker masker = new SecretMasker();
                var linesplitter = new Regex("\r?\n");
                foreach(var variable in variables) {
                    if(variable.Value.IsSecret) {
                        masker.AddValue(variable.Value.Value);
                        if(variable.Value.Value.Contains('\r') || variable.Value.Value.Contains('\n')) {
                            foreach(var line in linesplitter.Split(variable.Value.Value)) {
                                masker.AddValue(line);
                            }
                        }
                    }
                }
                masker.AddValueEncoder(ValueEncoders.Base64StringEscape);
                masker.AddValueEncoder(ValueEncoders.Base64StringEscapeShift1);
                masker.AddValueEncoder(ValueEncoders.Base64StringEscapeShift2);
                masker.AddValueEncoder(ValueEncoders.CommandLineArgumentEscape);
                masker.AddValueEncoder(ValueEncoders.ExpressionStringEscape);
                masker.AddValueEncoder(ValueEncoders.JsonStringEscape);
                masker.AddValueEncoder(ValueEncoders.UriDataEscape);
                masker.AddValueEncoder(ValueEncoders.XmlDataEscape);
                masker.AddValueEncoder(ValueEncoders.TrimDoubleQuotes);
                masker.AddValueEncoder(ValueEncoders.PowerShellPreAmpersandEscape);
                masker.AddValueEncoder(ValueEncoders.PowerShellPostAmpersandEscape);
                var insecuretraceWriter = templateContext.TraceWriter;
                templateContext.TraceWriter = new TraceWriter2(line => {
                    insecuretraceWriter.Info("{0}", masker.MaskSecrets(line));
                });
                var traceWriter = templateContext.TraceWriter;
                var _job = new Job() { message = null, repo = repo, WorkflowRunAttempt = attempt, WorkflowIdentifier = parentId != null ? parentId + "/" + name : name, name = displayname, workflowname = workflowname, runid = runid, JobId = jobId, RequestId = requestId, TimeLineId = timelineId, Matrix = contextData["matrix"]?.ToJToken()?.ToString() };
                AddJob(_job);
                Action<string> failedtoInstantiateWorkflow = message => {
                    traceWriter.Verbose("Failed to instantiate Workflow: {0}", message);
                    new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = jobId, Result = TaskResult.Failed, RequestId = requestId, Outputs = new Dictionary<String, VariableValue>() });
                };
                (new Func<Task>(async () => {
                    if(reference == null) {
                        failedtoInstantiateWorkflow($"Invalid reference format: {uses.Value}");
                        return;
                    }
                    Action<string, string> workflow_call = (filename, filecontent) => {
                        // This does only work with static github tokens
                        if(variables.TryGetValue("system.github.token", out var val)) {
                            ((DictionaryContextData)contextData["github"])["token"] = new StringContextData(val.Value);
                        }
                        var hook = (JObject)((DictionaryContextData) contextData["github"])["event"].ToJToken();
                        var ghook = hook.ToObject<GiteaHook>();

                        foreach (var pair in contextData)
                        {
                            templateContext.ExpressionValues[pair.Key] = pair.Value;
                        }
                        var eval = rawWith != null ? GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "job-with", rawWith, 0, null, true) : null;
                        templateContext.Errors.Check();
                        var result = new DictionaryContextData();
                        foreach (var variable in variables)
                        {
                            if (variable.Value.IsSecret)
                            {
                                if(string.Equals(variable.Key, "system.github.token", StringComparison.OrdinalIgnoreCase)) {
                                    result["github_token"] = new StringContextData(variable.Value.Value);
                                } else {
                                    result[variable.Key] = new StringContextData(variable.Value.Value);
                                }
                            }
                        }
                        templateContext.ExpressionValues["secrets"] = result;
                        var evalSec = rawSecrets != null ? GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "job-secrets", rawSecrets, 0, null, true).AssertMapping("") : null;
                        templateContext.Errors.Check();
                        List<string> _secrets = new List<string>();
                        if(evalSec != null) {
                            foreach(var entry in evalSec) {
                                _secrets.Add(entry.Key.AssertString($"jobs.{ji.name}.secrets mapping key") + "=" + entry.Value.AssertString($"jobs.{ji.name}.secrets mapping value"));
                            }
                        }
                        if(result.TryGetValue("github_token", out var ghtoken)) {
                            // Enshure to share a customized github_token secret with the called workflow
                            _secrets.Add("system.github.token=" + ghtoken);
                        }
                        var clone = Clone();
                        var callerJob = new CallingJob() { Name = displayname, Event = wevent, Inputs = eval?.ToContextData(), Workflowfinish = (callerJob, e) => {
                            if(callerJob.RanJob) {
                                if(callingJob != null) {
                                    callingJob.RanJob = true;
                                }
                                Array.ForEach(finishedJobs.ToArray(), fjobs => {
                                    foreach(var djob in dependentjobgroup) {
                                        if(djob.Dependencies != null && (fjobs.Key == djob.name || fjobs.Key.StartsWith(djob.name + "/"))) {
                                            foreach(var dep in djob.Dependencies) {
                                                if(dep.Key == name) {
                                                    finishedJobs.Remove(fjobs.Key);
                                                    return;
                                                }
                                            }
                                        }
                                    }
                                });
                            }
                            new FinishJobController(_cache, clone._context).InvokeJobCompleted(new JobCompletedEvent() { JobId = jobId, Result = e.Success ? TaskResult.Succeeded : TaskResult.Failed, RequestId = requestId, Outputs = e.Outputs ?? new Dictionary<String, VariableValue>() });
                        }, Id = parentId != null ? parentId + "/" + name : name, CancellationToken = ji.Cancel.Token, TimelineId = ji.TimelineId, RecordId = ji.Id, WorkflowName = workflowname};
                        var resp = clone.ConvertYaml2(filename, filecontent, ghook.repository.full_name, GitServerUrl, ghook, hook, "workflow_call", null, false, null, _secrets.ToArray(), null, platform, localcheckout, runid, runnumber, Ref, Sha, callingJob: callerJob, workflows, attempt, statusSha: statusSha, finishedJobs: finishedJobs?.Where(kv => kv.Key.StartsWith(name + "/"))?.ToDictionary(kv => kv.Key.Substring(name.Length + 1), kv => kv.Value));
                        if(resp == null || resp.failed || resp.skipped) {
                            failedtoInstantiateWorkflow(filename);
                            return;
                        }
                    };
                    if((reference.RepositoryType == PipelineConstants.SelfAlias || localcheckout && reference.Name == repo && (("refs/heads/" + reference.Ref) == Ref || ("refs/tags/" + reference.Ref) == Ref)) && workflows != null && workflows.ToDictionary(v => v.Key, v => v.Value).TryGetValue(reference.Path, out var _content)) {
                        try {
                            workflow_call(reference.Path, _content);
                        } catch (Exception ex) {
                            failedtoInstantiateWorkflow(ex.Message);
                            await Console.Error.WriteLineAsync(ex.Message);
                            await Console.Error.WriteLineAsync(ex.StackTrace);
                        }
                    } else {
                        if(reference.RepositoryType == PipelineConstants.SelfAlias) {
                            reference.RepositoryType = RepositoryTypes.GitHub;
                            reference.Ref = ((DictionaryContextData) contextData["github"])["sha"].ToString();
                            reference.Name = repo;
                        }
                        var client = new HttpClient();
                        client.DefaultRequestHeaders.Add("accept", "application/json");
                        client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("runner", string.IsNullOrEmpty(GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version) ? "0.0.0" : GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version));
                        string githubAppToken = null;
                        try {
                            if(!string.IsNullOrEmpty(GITHUB_TOKEN)) {
                                client.DefaultRequestHeaders.Add("Authorization", $"token {GITHUB_TOKEN}");
                            } else {
                                githubAppToken = await CreateGithubAppToken(repo);
                                if(githubAppToken != null) {
                                    client.DefaultRequestHeaders.Add("Authorization", $"token {githubAppToken}");
                                }
                            }
                            var url = new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{reference.Name}/contents/{Uri.EscapeDataString(reference.Path)}"));
                            url.Query = $"ref={Uri.EscapeDataString(reference.Ref)}";
                            var res = await client.GetAsync(url.ToString());
                            if(res.StatusCode == System.Net.HttpStatusCode.OK) {
                                var content = await res.Content.ReadAsStringAsync();
                                var item = Newtonsoft.Json.JsonConvert.DeserializeObject<UnknownItem>(content);
                                {
                                    try {
                                        var fileRes = await client.GetAsync(item.download_url);
                                        var filecontent = await fileRes.Content.ReadAsStringAsync();
                                        workflow_call(item.path, filecontent);
                                    } catch (Exception ex) {
                                        failedtoInstantiateWorkflow(ex.Message);
                                        await Console.Error.WriteLineAsync(ex.Message);
                                        await Console.Error.WriteLineAsync(ex.StackTrace);
                                    }
                                }
                            } else {
                                failedtoInstantiateWorkflow($"No such callable workflow: {uses.Value}");
                            }
                        } finally {
                            if(githubAppToken != null) {
                                await DeleteGithubAppToken(githubAppToken);
                            }
                        }
                    }
                }))().Wait();
                return null;
            }
            var runsOn = (from r in run where r.Key.AssertString("str").Value == "runs-on" select r).FirstOrDefault().Value;
            HashSet<string> runsOnMap = new HashSet<string>();
            if (runsOn != null) {
                foreach (var pair in contextData)
                {
                    templateContext.ExpressionValues[pair.Key] = pair.Value;
                }
                templateContext.TraceWriter.Info("{0}", "Evaluate runs-on");
                var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.RunsOn, runsOn, 0, null, true);
                templateContext.Errors.Check();
                runsOn = eval;

                if(runsOn is SequenceToken seq2) {
                    foreach(var t in seq2) {
                        runsOnMap.Add(t.AssertString("runs-on member must be a str").Value.ToLowerInvariant());
                    }
                } else {
                    runsOnMap.Add(runsOn.AssertString("runs-on must be a str or array of string").Value.ToLowerInvariant());
                }
            }

            // Jobcontainer
            TemplateToken jobContainer = (from r in run where r.Key.AssertString("container").Value == "container" select r).FirstOrDefault().Value;

            foreach(var p in platform.Reverse()) {
                var eq = p.IndexOf('=');
                var set = p.Substring(0, eq).Split(",").Select(e => e.ToLowerInvariant()).ToHashSet();
                if(runsOnMap.IsSubsetOf(set) && p.Length > (eq + 1)) {
                    if(p[eq + 1] == '-') {
                        runsOnMap = p.Substring(eq + 2, p.Length - (eq + 2)).Split(',').Select(e => e.ToLowerInvariant()).ToHashSet();
                    } else {
                        runsOnMap = new HashSet<string> { "self-hosted", "container-host" };
                        if(jobContainer == null) {
                            // Set just the container property of the workflow, the runner will use it
                            jobContainer = new StringToken(null, null, null, p.Substring(eq + 1, p.Length - (eq + 1)));
                        }
                        // If jobContainer != null, nothing we need to do other than use a special runner
                    }
                    break;
                }
            }

            if(!QueueJobsWithoutRunner) {
                var sessionsfreeze = sessions.ToArray();
                var x = (from s in sessionsfreeze where runsOnMap.IsSubsetOf(from l in s.Value.Agent.TaskAgent.Labels select l.Name.ToLowerInvariant()) select s.Key).FirstOrDefault();
                if(x == null) {
                    StringBuilder b = new StringBuilder();
                    int i = 0;
                    foreach(var e in runsOnMap) {
                        if(i++ != 0) {
                            b.Append(", ");
                        }
                        b.Append(e);
                    }
                    StringBuilder b2 = new StringBuilder();
                    i = 0;
                    foreach(var s in sessionsfreeze) {
                        if(i++ != 0) {
                            b2.Append(", ");
                        }
                        b2.Append($"Name: `{s.Value.TaskAgentSession.Agent.Name}` OSDescription: `{s.Value.TaskAgentSession.Agent.OSDescription}` Labels [");
                        int j = 0;
                        foreach(var l in s.Value.Agent.TaskAgent.Labels) {
                            if(j++ != 0) {
                                b2.Append(", ");
                            }
                            b2.Append(l.Name);
                        }
                        b2.Append("]");
                    }
                    templateContext.TraceWriter.Info("{0}", $"No runner is registered for the requested runs-on labels: [{b.ToString()}], please register and run a self-hosted runner with at least these labels. Available runner: {(i == 0 ? "No Runner available!" : b2.ToString())}");

                    var jid = jobId;
                    var _job = new Job() { message = null, repo = repo, WorkflowRunAttempt = attempt, WorkflowIdentifier = parentId != null ? parentId + "/" + name : name, name = displayname, workflowname = workflowname, runid = runid, JobId = jid, RequestId = requestId, TimeLineId = timelineId, Matrix = contextData["matrix"]?.ToJToken()?.ToString() };
                    AddJob(_job);
                    new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = jobId, Result = TaskResult.Failed, RequestId = requestId, Outputs = new Dictionary<String, VariableValue>() });
                    return null;
                }
            }
            var steps = PipelineTemplateConverter.ConvertToSteps(templateContext, rawSteps);

            if(localcheckout) {
                // Rewrite checkout step to copy repo via custom protocol
                for (int i = 0; i < steps.Count; i++) {
                    if(steps[i] is ActionStep astep && astep.Reference is RepositoryPathReference p && String.Compare(p.Name, "actions/checkout", true) == 0 && (p.Path == null || p.Path == "")) {
                        var _localcheckout = astep.Clone() as ActionStep;
                        _localcheckout.Reference = new RepositoryPathReference { Name = "localcheckout", Ref = "V1", RepositoryType = RepositoryTypes.GitHub, Path = "" };
                        _localcheckout.ContextName = "_" + Guid.NewGuid().ToString();
                        var inmap = _localcheckout.Inputs?.AssertMapping("inputs");
                        if(inmap != null) {
                            inmap.Add(new StringToken(null, null, null, "checkoutref"), new StringToken(null, null, null, p.Ref));
                        }
                        astep.Condition = $"({astep.Condition}) && !fromJSON(steps.{_localcheckout.ContextName}.outputs.skip)";
                        steps.Insert(i++, _localcheckout);
                    }
                }
            }

            foreach (var step in steps)
            {
                step.Id = Guid.NewGuid();
            }
            
            var environmentToken = (from r in run where r.Key.AssertString("env").Value == "env" select r).FirstOrDefault().Value;

            List<TemplateToken> environment = new List<TemplateToken>();
            if(workflowEnvironment != null) {
                environment.AddRange(workflowEnvironment);
            }
            if (environmentToken != null)
            {
                environment.Add(environmentToken);
            }

            // Jobservicecontainer
            TemplateToken jobServiceContainer = (from r in run where r.Key.AssertString("services").Value == "services" select r).FirstOrDefault().Value;
            // Job outputs
            TemplateToken outputs = (from r in run where r.Key.AssertString("outputs").Value == "outputs" select r).FirstOrDefault().Value;
            // Environment
            TemplateToken deploymentEnvironment = (from r in run where r.Key.AssertString("environment").Value == "environment" select r).FirstOrDefault().Value;
            GitHub.DistributedTask.WebApi.ActionsEnvironmentReference deploymentEnvironmentValue = null;
            if(deploymentEnvironment != null) {
                templateContext.ExpressionValues.Clear();
                foreach (var pair in contextData) {
                    templateContext.ExpressionValues[pair.Key] = pair.Value;
                }
                templateContext.TraceWriter.Info("{0}", $"Evaluate environment");
                deploymentEnvironment = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "environment", deploymentEnvironment, 0, null, true);
                if(deploymentEnvironment != null) {
                    if(deploymentEnvironment is StringToken ename) {
                        deploymentEnvironmentValue = new GitHub.DistributedTask.WebApi.ActionsEnvironmentReference(ename.Value);
                    } else {
                        var mtoken = deploymentEnvironment.AssertMapping("Environment must be a mapping or string");
                        deploymentEnvironmentValue = new GitHub.DistributedTask.WebApi.ActionsEnvironmentReference((from r in mtoken where r.Key.AssertString("name").Value == "name" select r.Value).First().AssertString("name").Value);
                        deploymentEnvironmentValue.Url = (from r in mtoken where r.Key.AssertString("url").Value == "url" select r.Value).FirstOrDefault();
                    }
                }
            }
            // Job permissions
            TemplateToken jobPermissions = (from r in run where r.Key.AssertString("permissions").Value == "permissions" select r).FirstOrDefault().Value ?? workflowPermissions;

            var defaultToken = (from r in run where r.Key.AssertString("defaults").Value == "defaults" select r).FirstOrDefault().Value;

            List<TemplateToken> jobDefaults = new List<TemplateToken>();
            if(workflowDefaults != null) {
                jobDefaults.Add(workflowDefaults);
            }
            if (defaultToken != null) {
                jobDefaults.Add(defaultToken);
            }

            var jobConcurrency = (from r in run where r.Key.AssertString("concurrency").Value == "concurrency" select r).FirstOrDefault().Value;
            if(jobConcurrency != null) {
                templateContext.TraceWriter.Info("{0}", $"Evaluate job concurrency");
                templateContext.ExpressionValues.Clear();
                foreach (var pair in contextData) {
                    templateContext.ExpressionValues[pair.Key] = pair.Value;
                }
                jobConcurrency = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "job-concurrency", jobConcurrency, 0, null, true);
            }
            var job = new Job() { message = (apiUrl) => {
                try {
                    var auth = new GitHub.DistributedTask.WebApi.EndpointAuthorization() { Scheme = GitHub.DistributedTask.WebApi.EndpointAuthorizationSchemes.OAuth };
                    var mySecurityKey = new RsaSecurityKey(Startup.AccessTokenParameter);

                    var myIssuer = "http://githubactionsserver";
                    var myAudience = "http://githubactionsserver";

                    var tokenHandler = new JwtSecurityTokenHandler();
                    var hook = (JObject)((DictionaryContextData) contextData["github"])["event"].ToJToken();
                    var ghook = hook.ToObject<GiteaHook>();
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new Claim[]
                        {
                            new Claim("Agent", "job"),
                            new Claim("repository", repo),
                            new Claim("ref", Ref),
                            new Claim("defaultRef", "refs/heads/" + ghook.repository.default_branch),
                            new Claim("attempt", attempt.Attempt.ToString()),
                        }),
                        Expires = DateTime.UtcNow.AddMinutes(timeoutMinutes),
                        Issuer = myIssuer,
                        Audience = myAudience,
                        SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.RsaSha256)
                    };

                    var resources = new JobResources();
                    var token = tokenHandler.CreateToken(tokenDescriptor);
                    var stoken = tokenHandler.WriteToken(token);
                    auth.Parameters.Add(GitHub.DistributedTask.WebApi.EndpointAuthorizationParameters.AccessToken, stoken);
                    var systemVssConnection = new GitHub.DistributedTask.WebApi.ServiceEndpoint() { Id = Guid.NewGuid(), Name = WellKnownServiceEndpointNames.SystemVssConnection, Authorization = auth, Url = new Uri(apiUrl) };
                    systemVssConnection.Data["CacheServerUrl"] = apiUrl;
                    resources.Endpoints.Add(systemVssConnection);

                    if(!string.IsNullOrEmpty(GitHubAppPrivateKeyFile) && GitHubAppId != 0 && (!variables.TryGetValue("system.github.token", out var _token) || string.IsNullOrEmpty(_token?.Value))) {
                        try {
                            var ownerAndRepo = repo.Split("/", 2);
                            // Use GitHubJwt library to create the GitHubApp Jwt Token using our private certificate PEM file
                            var generator = new GitHubJwt.GitHubJwtFactory(
                                new GitHubJwt.FilePrivateKeySource(GitHubAppPrivateKeyFile),
                                new GitHubJwt.GitHubJwtFactoryOptions
                                {
                                    AppIntegrationId = GitHubAppId, // The GitHub App Id
                                    ExpirationSeconds = 600 // 10 minutes is the maximum time allowed
                                }
                            );
                            var jwtToken = generator.CreateEncodedJwtToken();
                            // Pass the JWT as a Bearer token to Octokit.net
                            var appClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"))
                            {
                                Credentials = new Octokit.Credentials(jwtToken, Octokit.AuthenticationType.Bearer)
                            };
                            var calculatedPermissions = new Dictionary<string, string>();
                            var _hook = (JObject)((DictionaryContextData) contextData["github"])["event"].ToJToken();
                            var _ghook = _hook.ToObject<GiteaHook>();
                            var isFork = !WriteAccessForPullRequestsFromForks && wevent == "pull_request" && (_ghook?.pull_request?.head?.Repo?.Fork ?? false);
                            calculatedPermissions["metadata"] = "read";
                            var stkn = jobPermissions as StringToken;
                            if(jobPermissions == null || stkn != null) {
                                if(stkn?.Value != "none") {
                                    if(stkn?.Value == "read-all" || (isFork && stkn?.Value == "write-all")) {
                                        calculatedPermissions["actions"] = "read";
                                        calculatedPermissions["checks"] = "read";
                                        calculatedPermissions["contents"] = "read";
                                        calculatedPermissions["deployments"] = "read";
                                        calculatedPermissions["issues"] = "read";
                                        calculatedPermissions["packages"] = "read";
                                        calculatedPermissions["pull_requests"] = "read";
                                        calculatedPermissions["security_events"] = "read";
                                        calculatedPermissions["statuses"] = "read";
                                    } else if(isFork) {
                                        calculatedPermissions["contents"] = "read";
                                    } else if(jobPermissions == null || stkn?.Value == "write-all") {
                                        calculatedPermissions["actions"] = "write";
                                        calculatedPermissions["checks"] = "write";
                                        calculatedPermissions["contents"] = "write";
                                        calculatedPermissions["deployments"] = "write";
                                        calculatedPermissions["issues"] = "write";
                                        calculatedPermissions["packages"] = "write";
                                        calculatedPermissions["pull_requests"] = "write";
                                        calculatedPermissions["security_events"] = "write";
                                        calculatedPermissions["statuses"] = "write";
                                    }
                                }
                            } else {
                                foreach(var kv in jobPermissions.AssertMapping("Only String or Mapping expected for permission key")) {
                                    var keyname = kv.Key.AssertString("permission key has to be a string").Value.Replace("-", "_");
                                    var keyvalue = kv.Value.AssertString("permission value has to be a string").Value;
                                    if(keyname != "id_token") {
                                        if(keyvalue == "none") {
                                            calculatedPermissions.Remove(keyname);
                                        } else {
                                            calculatedPermissions[keyname] = isFork ? "read" : keyvalue;
                                        }
                                    }
                                }
                            }
                            var installation = appClient.GitHubApps.GetRepositoryInstallationForCurrent(ownerAndRepo[0], ownerAndRepo[1]).GetAwaiter().GetResult();
                            var response = appClient.Connection.Post<Octokit.AccessToken>(Octokit.ApiUrls.AccessTokens(installation.Id), new { Permissions = calculatedPermissions }, Octokit.AcceptHeaders.GitHubAppsPreview, Octokit.AcceptHeaders.GitHubAppsPreview).GetAwaiter().GetResult();
                            variables["system.github.token"] = new VariableValue(response.Body.Token, true);
                            variables["system.github.token.permissions"] = new VariableValue(Newtonsoft.Json.JsonConvert.SerializeObject(calculatedPermissions), false);
                        } catch {

                        }
                    }

                    // Enshure secrets.github_token is available in the runner
                    if(!variables.TryGetValue("github_token", out _)) {
                        variables["github_token"] = new VariableValue(variables["system.github.token"].Value, true);
                    }
                    
                    var req = new AgentJobRequestMessage(new GitHub.DistributedTask.WebApi.TaskOrchestrationPlanReference() { PlanType = "free", ContainerId = 0, ScopeIdentifier = Guid.NewGuid(), PlanGroup = "free", PlanId = Guid.NewGuid(), Owner = new GitHub.DistributedTask.WebApi.TaskOrchestrationOwner() { Id = 0, Name = "Community" }, Version = 12 }, new GitHub.DistributedTask.WebApi.TimelineReference() { Id = timelineId, Location = null, ChangeId = 1 }, jobId, displayname, name, jobContainer, jobServiceContainer, environment, variables, new List<GitHub.DistributedTask.WebApi.MaskHint>(), resources, contextData, new WorkspaceOptions(), steps.Cast<JobStep>(), templateContext.GetFileTable().ToList(), outputs, jobDefaults, deploymentEnvironmentValue );
                    req.RequestId = requestId;
                    return req;
                } catch(Exception ex) {
                    Console.WriteLine($"Internal Error: {ex.Message}, {ex.StackTrace}");
                    return null;
                }
            }, repo = repo, WorkflowRunAttempt = attempt, WorkflowIdentifier = parentId != null ? parentId + "/" + name : name, name = displayname, workflowname = workflowname, runid = runid, /* SessionId = sessionId,  */JobId = jobId, RequestId = requestId, TimeLineId = timelineId, TimeoutMinutes = timeoutMinutes, CancelTimeoutMinutes = cancelTimeoutMinutes, ContinueOnError = continueOnError, Matrix = contextData["matrix"]?.ToJToken()?.ToString() };
            AddJob(job);
            //ConcurrencyGroup
            string group = null;
            bool cancelInprogress = false;
            if(jobConcurrency != null) {
                if(jobConcurrency is StringToken stkn) {
                    group = stkn.Value;
                } else {
                    var cmapping = jobConcurrency.AssertMapping("workflowConcurrency must be a string or mapping");
                    group = (from r in cmapping where r.Key.AssertString("key").Value == "group" select r).FirstOrDefault().Value?.AssertString("group")?.Value;
                    cancelInprogress = (from r in cmapping where r.Key.AssertString("key").Value == "cancel-in-progress" select r).FirstOrDefault().Value?.AssertBoolean("cancel-in-progress")?.Value ?? cancelInprogress;
                }
            }
            return cancel => {
                if(cancel || job.CancelRequest.IsCancellationRequested) {
                    job.CancelRequest.Cancel();
                    new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                } else {
                    Action _queueJob = () => {
                        Channel<Job> queue = jobqueue.GetOrAdd(runsOnMap, (a) => Channel.CreateUnbounded<Job>());

                        TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Queued Job: {job.name} for queue {string.Join(",", runsOnMap)}" }), job.TimeLineId, job.JobId);
                        queue.Writer.WriteAsync(job);
                    };
                    if(string.IsNullOrEmpty(group)) {
                        _queueJob();
                    } else {
                        var key = $"{repo}/{group}";
                        while(true) {
                            ConcurrencyGroup cgroup = concurrencyGroups.GetOrAdd(key, name => new ConcurrencyGroup() { Key = name });
                            lock(cgroup) {
                                if(concurrencyGroups.TryGetValue(key, out var _cgroup) && cgroup != _cgroup) {
                                    continue;
                                }
                                ConcurrencyEntry centry = new ConcurrencyEntry();
                                centry.Run = () => {
                                    if(job.CancelRequest.IsCancellationRequested) {
                                        TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Job was cancelled, while it was pending in the concurrency group" }), job.TimeLineId, job.JobId);
                                        cgroup.FinishRunning(centry);
                                    } else {
                                        FinishJobController.OnJobCompleted += evdata => {
                                            if(evdata.JobId == job.JobId) {
                                                cgroup.FinishRunning(centry);
                                            }
                                        };
                                        _queueJob();
                                    }
                                };
                                centry.CancelPending = () => {
                                    TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Job was cancelled by another workflow or job, while it was pending in the concurrency group" }), job.TimeLineId, job.JobId);
                                    job.CancelRequest.Cancel();
                                    new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                                };
                                centry.CancelRunning = cancelInProgress => {
                                    if(job.SessionId != Guid.Empty) {
                                        if(cancelInProgress) {
                                            TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Job was cancelled by another workflow or job, while it was in progress in the concurrency group: {group}" }), job.TimeLineId, job.JobId);
                                            job.CancelRequest.Cancel();
                                        }
                                        // Keep Job running, since the cancelInProgress is false
                                    } else {
                                        job.CancelRequest.Cancel();
                                        new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                                    }
                                };
                                cgroup.PushEntry(centry, cancelInprogress);
                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Job was added to the concurrency group: {group}, cancel-in-progress: {cancelInprogress}" }), job.TimeLineId, job.JobId);
                            }
                            break;
                        }
                    }
                }
                return job;
            };
        }

        public delegate AgentJobRequestMessage MessageFactory(string apiUrl);

        private class EqualityComparer : IEqualityComparer<HashSet<string>> {
            public bool Equals(HashSet<string> a, HashSet<string> b) {
                return a.SetEquals(b);
            }
            public int GetHashCode(HashSet<string> p) {
                var l = p.ToList();
                l.Sort();
                StringBuilder b = new StringBuilder();
                foreach (var item in l) {
                    b.AppendJoin(',', item);
                }
                return b.ToString().GetHashCode();
            }
        }

        private static ConcurrentDictionary<HashSet<string>, Channel<Job>> jobqueue = new ConcurrentDictionary<HashSet<string>, Channel<Job>>(new EqualityComparer());
        private static int id = 0;

        // private string Decrypt(byte[] key, byte[] iv, byte[] message) {
        //     using (var aes = Aes.Create())
        //     using (var decryptor = aes.CreateDecryptor(key, iv))
        //     using (var body = new MemoryStream(message))
        //     using (var cryptoStream = new CryptoStream(body, decryptor, CryptoStreamMode.Read))
        //     using (var bodyReader = new StreamReader(cryptoStream, Encoding.UTF8))
        //     {
        //        return bodyReader.ReadToEnd();
        //     }
        // }

        public static ConcurrentDictionary<Session, Session> sessions = new ConcurrentDictionary<Session, Session>();
        public delegate void RepoDownload(long runid, string url, bool submodules, bool nestedSubmodules);

        public static event RepoDownload OnRepoDownload;

        [HttpGet("{poolId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "Agent")]
        public async Task<IActionResult> GetMessage(int poolId, Guid sessionId)
        {
            Session session;
            if(!_cache.TryGetValue(sessionId, out session)) {
                this.HttpContext.Response.StatusCode = 403;
                return await Ok(new WrappedException(new TaskAgentSessionExpiredException("This server has been restarted"), true, new Version(2, 0)));
            }
            try {
                var ts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted, new CancellationTokenSource(TimeSpan.FromSeconds(50)).Token);
                await session.MessageLock.WaitAsync(ts.Token);
                session.Timer.Stop();
                session.Timer.Start();
                session.DropMessage?.Invoke("Called GetMessage without deleting the old Message");
                session.DropMessage = null;
                if(session.Job == null) {
                    if(session.Agent.TaskAgent.Ephemeral == true && session.FirstJobReceived) {
                        try {
                            new AgentController(_cache, _context).Delete(session.Agent.Pool.Id, session.Agent.TaskAgent.Id);
                        } catch {

                        }
                        this.HttpContext.Response.StatusCode = 403;
                        return await Ok(new WrappedException(new TaskAgentSessionExpiredException("This agent has been removed by Ephemeral"), true, new Version(2, 0)));
                    }
                    var labels = session.Agent.TaskAgent.Labels.Select(l => l.Name.ToLowerInvariant()).ToArray();
                    HashSet<HashSet<String>> labelcom = labels.Select(l => new HashSet<string>{l}).ToHashSet(new EqualityComparer());
                    for(long j = 0; j < labels.LongLength; j++) {
                        var it = labelcom.ToArray();
                        for(long i = 0, size = it.LongLength; i < size; i++) {
                            var res = it[i].Append(labels[j]).ToHashSet();
                            labelcom.Add(res);
                        }
                    }
                    foreach(var label in labelcom) {
                        Channel<Job> queue = jobqueue.GetOrAdd(label, (a) => Channel.CreateUnbounded<Job>());
                    }
                    var queues = jobqueue.ToArray().Where(e => e.Key.IsSubsetOf(from l in session.Agent.TaskAgent.Labels select l.Name.ToLowerInvariant())).ToArray();
                    while(!ts.IsCancellationRequested) {
                        var poll = queues.Select(q => q.Value.Reader.WaitToReadAsync(ts.Token).AsTask()).ToArray();
                        await Task.WhenAny(poll);
                        if(ts.IsCancellationRequested) {
                            return NoContent();
                        }
                        for(long i = 0; i < poll.LongLength && !HttpContext.RequestAborted.IsCancellationRequested; i++ ) {
                            if(poll[i].IsCompletedSuccessfully && poll[i].Result)
                            try {
                                if(queues[i].Value.Reader.TryRead(out var req)) {
                                    try {
                                        TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string>{ $"Read Job from Queue: {req.name} for queue {string.Join(",", queues[i].Key)} assigned to Runner Name:{session.Agent.TaskAgent.Name} Labels:{string.Join(",", queues[i].Key)} {sessionId}" }), req.TimeLineId, req.JobId);
                                        req.SessionId = sessionId;
                                        if(req.CancelRequest.IsCancellationRequested) {
                                            TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string>{ $"Cancelled Job: {req.name} for queue {string.Join(",", queues[i].Key)} unassigned to Runner Name:{session.Agent.TaskAgent.Name} Labels:{string.Join(",", queues[i].Key)}" }), req.TimeLineId, req.JobId);
                                            new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = req.JobId, Result = TaskResult.Canceled, RequestId = req.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                                            continue;
                                        }
                                        var q = queues[i].Value;
                                        session.DropMessage = reason => {
                                            TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string>{ $"Requeued Job: {req.name} for queue {string.Join(",", queues[i].Key)} unassigned to Runner Name:{session.Agent.TaskAgent.Name} Labels:{string.Join(",", queues[i].Key)}: {reason}" }), req.TimeLineId, req.JobId);
                                            q.Writer.WriteAsync(req);
                                            session.Job = null;
                                            session.JobTimer?.Stop();
                                        };
                                        try {
                                            if(req.message == null) {
                                                Console.WriteLine("req.message == null in GetMessage of Worker, skip invalid message");
                                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string>{ $"Failed Job: {req.name} for queue {string.Join(",", queues[i].Key)}: req.message == null in GetMessage of Worker, skip invalid message" }), req.TimeLineId, req.JobId);
                                                continue;
                                            }
                                            var res = req.message.Invoke($"{ServerUrl}/Unknown/Unknown/");
                                            if(res == null) {
                                                Console.WriteLine("res == null in GetMessage of Worker, skip internal Error");
                                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string>{ $"Failed Job: {req.name} for queue {string.Join(",", queues[i].Key)}: req.message == null in GetMessage of Worker, skip invalid message" }), req.TimeLineId, req.JobId);
                                                Job job = GetJob(req.JobId);
                                                if(job != null) {
                                                    new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Failed, RequestId = job.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                                                }
                                                continue;
                                            }
                                            HttpContext.RequestAborted.ThrowIfCancellationRequested();
                                            if(session.JobTimer == null) {
                                                session.JobTimer = new System.Timers.Timer();
                                                session.JobTimer.Elapsed += (a,b) => {
                                                    if(session.Job != null) {
                                                        session.Job.CancelRequest.Cancel();
                                                    }
                                                };
                                                session.JobTimer.AutoReset = false;
                                            } else {
                                                session.JobTimer.Stop();
                                            }
                                            session.Job = req;
                                            session.JobTimer.Interval = session.Job.TimeoutMinutes * 60 * 1000;
                                            session.JobTimer.Start();
                                            session.Key.GenerateIV();
                                            using (var encryptor = session.Key.CreateEncryptor(session.Key.Key, session.Key.IV))
                                            using (var body = new MemoryStream())
                                            using (var cryptoStream = new CryptoStream(body, encryptor, CryptoStreamMode.Write)) {
                                                await new ObjectContent<AgentJobRequestMessage>(res, new VssJsonMediaTypeFormatter(true)).CopyToAsync(cryptoStream);
                                                cryptoStream.FlushFinalBlock();
                                                var msg = await Ok(new TaskAgentMessage() {
                                                    Body = Convert.ToBase64String(body.ToArray()),
                                                    MessageId = id++,
                                                    MessageType = JobRequestMessageTypes.PipelineAgentJobRequest,
                                                    IV = session.Key.IV
                                                });
                                                HttpContext.RequestAborted.ThrowIfCancellationRequested();
                                                if(req.CancelRequest.IsCancellationRequested) {
                                                    session.Job = null;
                                                    session.DropMessage = null;
                                                    session.JobTimer.Stop();
                                                    TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string>{ $"Cancelled Job (2): {req.name} for queue {string.Join(",", queues[i].Key)} unassigned to Runner Name:{session.Agent.TaskAgent.Name} Labels:{string.Join(",", queues[i].Key)}" }), req.TimeLineId, req.JobId);
                                                    new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = req.JobId, Result = TaskResult.Canceled, RequestId = req.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                                                    continue;
                                                    //return NoContent();
                                                }
                                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string>{ $"Sent Job to Runner: {req.name} for queue {string.Join(",", queues[i].Key)} assigned to Runner Name:{session.Agent.TaskAgent.Name} Labels:{string.Join(",", queues[i].Key)}" }), req.TimeLineId, req.JobId);
                                                return msg;
                                            }
                                        } catch(Exception ex) {
                                            try {
                                                TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string>{ $"Error while sending message (inner area): {ex.Message}" }), req.TimeLineId, req.JobId);
                                            } catch {
                                                
                                            }
                                            if(session.DropMessage != null) {
                                                session.DropMessage?.Invoke(ex.Message);
                                                session.DropMessage = null;
                                            } else {
                                                await queues[i].Value.Writer.WriteAsync(req);
                                            }
                                        }
                                    } catch(Exception ex) {
                                        try {
                                            TimeLineWebConsoleLogController.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string>{ $"Error while sending message (outer area): {ex.Message}" }), req.TimeLineId, req.JobId);
                                        } catch {

                                        }
                                        await queues[i].Value.Writer.WriteAsync(req);
                                    }
                                }
                            } catch(Exception ex) {
                                session.DropMessage?.Invoke(ex.Message);
                                session.DropMessage = null;
                            }
                        }
                    }
                } else if(!session.Job.Cancelled) {
                    try {
                        await Task.Delay(-1, CancellationTokenSource.CreateLinkedTokenSource(session.JobRunningToken, ts.Token, session.Job.CancelRequest.Token).Token);
                    } catch (TaskCanceledException) {
                        if(!session.JobRunningToken.IsCancellationRequested && session.Job.CancelRequest.IsCancellationRequested) {
                            session.Job.Cancelled = true;
                            session.Key.GenerateIV();
                            // await Task.Delay(2500);
                            using (var encryptor = session.Key.CreateEncryptor(session.Key.Key, session.Key.IV))
                            using (var body = new MemoryStream())
                            using (var cryptoStream = new CryptoStream(body, encryptor, CryptoStreamMode.Write)) {
                                await new ObjectContent<JobCancelMessage>(new JobCancelMessage(session.Job.JobId, TimeSpan.FromMinutes(session.Job.CancelTimeoutMinutes)), new VssJsonMediaTypeFormatter(true)).CopyToAsync(cryptoStream);
                                cryptoStream.FlushFinalBlock();
                                return await Ok(new TaskAgentMessage() {
                                    Body = Convert.ToBase64String(body.ToArray()),
                                    MessageId = id++,
                                    MessageType = JobCancelMessage.MessageType,
                                    IV = session.Key.IV
                                });
                            }
                        }
                        if(session.JobRunningToken.IsCancellationRequested && session.Agent.TaskAgent.Ephemeral == true) {
                            try {
                                new AgentController(_cache, _context).Delete(session.Agent.Pool.Id, session.Agent.TaskAgent.Id);
                            } catch {

                            }
                        }
                        // The official runner ignores the next job if we don't delay here
                        await Task.Delay(1000);
                    }
                } else {
                    try {
                        await Task.Delay(-1, CancellationTokenSource.CreateLinkedTokenSource(session.JobRunningToken, ts.Token).Token);
                    } catch (TaskCanceledException) {
                        if(session.JobRunningToken.IsCancellationRequested && session.Agent.TaskAgent.Ephemeral == true) {
                            try {
                                new AgentController(_cache, _context).Delete(session.Agent.Pool.Id, session.Agent.TaskAgent.Id);
                            } catch {
                                
                            }
                        }
                        // The official runner ignores the next job if we don't delay here
                        await Task.Delay(1000);
                    }
                }
                return NoContent();
            } finally {
                session.MessageLock.Release();
            }
        }

        private void RefreshAgent(int poolId, int agentId = -1)
        {
        }

        private void SendMessage(int poolId, long requestId, TaskAgentMessage message) {
            
        }

        [HttpPost("{poolId}")]
        public void Post(int poolId, long requestId = -1, int agentId = -1, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] TaskAgentMessage message = null) {
            if(message == null && requestId == -1) {
                RefreshAgent(poolId, agentId);
            } else if (agentId == -1) {
                SendMessage(poolId, requestId, message);
            }
        }

        public class GitUser {
            public int Id {get;set;}
            [DataMember]
            public string username {get; set;}
            public string login {get; set;}
        }


        public class Repo {
            [DataMember]
            public string full_name {get; set;}
            public Uri html_url {get; set;}

            public GitUser Owner {get;set;}
            public string default_branch { get; set; }
            public Permissions Permissions { get; set; }
            public bool Fork { get; set; }
        }
        public class Permissions {
            public bool Admin  { get; set; }
            public bool Push  { get; set; }
            public bool Pull  { get; set; }
        }

        public class GitCommit {
            public string Ref {get;set;}
            public string Sha {get;set;}
            public List<string> Added {get;set;}
            public List<string> Removed {get;set;}
            public List<string> Modified {get;set;}
            public Repo Repo {get;set;}
        }

        public class GitPullRequest {
            public GitCommit head {get;set;}
            public GitCommit Base {get;set;}
        }

        public class GiteaHook
        {
            [DataMember]
            public Repo repository {get; set;}
            
            public GitCommit head_commit {get;set;}
            public string Action {get;set;}
            public long? Number {get;set;}
            public string Ref {get;set;}
            public string After {get;set;}
            public string merge_commit_sha {get;set;}
            public GitUser sender {get;set;}
            public GitPullRequest pull_request {get;set;}

            public List<GitCommit> Commits {get;set;}
            public string ref_type { get; set; }
            public string Sha { get; set; }
            public string tag_name { get; set; }
        }

        public class Issue {
            public string id {get;set;}

            public IEnumerable<string> added {get;set;}
            public IEnumerable<string> removed {get;set;}
            public IEnumerable<string> modified {get;set;}
        }

        class UnknownItem {
            public string download_url {get;set;}
            public string path {get;set;}
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> OnWebhook()
        {
            KeyValuePair<GiteaHook, JObject> obj;
            if(WebhookHMACAlgorithmName.Length > 0 && WebhookSecret.Length > 0 && WebhookSignatureHeader.Length > 0) {
                var hmac = HMAC.Create(WebhookHMACAlgorithmName);
                hmac.Key = System.Text.Encoding.UTF8.GetBytes(WebhookSecret);
                string value = HttpContext.Request.Headers[WebhookSignatureHeader];
                if(WebhookSignaturePrefix.Length > 0 && !value.StartsWith(WebhookSignaturePrefix)) {
                    return Forbid();
                }
                obj = await FromBody2<GiteaHook>(hmac, value.Substring(WebhookSignaturePrefix.Length));
            } else {
                obj = await FromBody2<GiteaHook>();
            }
            // Try to fix head_commit == null 
            if(obj.Key.head_commit == null) {
                var val = obj.Value.GetValue("commits");
                if(val != null && val.HasValues) {
                    obj.Value.Remove("head_commit");
                    obj.Value.Add("head_commit", val.First);
                }
            }
            string e = "push";
            StringValues ev;
            if(Request.Headers.TryGetValue("X-GitHub-Event", out ev) && ev.Count == 1 && ev.First().Length > 0) {
                e = ev.First();
            }
            var hook = obj.Key;
            string githubAppToken = null;
            try {
                if(GITHUB_TOKEN == null) {
                    githubAppToken = await CreateGithubAppToken(hook.repository.full_name);
                }
                Dictionary<string, string> evs = new Dictionary<string, string>();
                if(e == "pull_request") {
                    evs.Add("pull_request_target", hook?.pull_request?.Base?.Sha);
                    if(AllowPullRequests) {
                        evs.Add("pull_request", hook?.pull_request?.head?.Sha);
                    }
                } else if(e.StartsWith("pull_request_") && AllowPullRequests) {
                    evs.Add(e, hook?.pull_request?.head?.Sha);
                } else if(e == "create" && hook?.ref_type != null) {
                    evs.Add(e, hook?.Sha);
                } else if(e == "release" && hook?.ref_type != null) {
                    evs.Add(e, hook?.tag_name);
                } else if(hook?.After != null) {
                    evs.Add(e, hook?.After);
                } else {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("runner", string.IsNullOrEmpty(GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version) ? "0.0.0" : GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version));
                    if(!string.IsNullOrEmpty(GITHUB_TOKEN ?? githubAppToken)) {
                        client.DefaultRequestHeaders.Add("Authorization", $"token {GITHUB_TOKEN ?? githubAppToken}");
                    }
                    var urlBuilder = new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{hook.repository.full_name}/commits"));
                    urlBuilder.Query = $"?page=1&limit=1";
                    var res = await client.GetAsync(urlBuilder.ToString());
                    if(res.StatusCode == System.Net.HttpStatusCode.OK) {
                        var content = await res.Content.ReadAsStringAsync();
                        var o = JsonConvert.DeserializeObject<GitCommit[]>(content)[0];
                        hook.After = o.Sha;
                    }
                    evs.Add(e, "");
                }
                foreach(var em in evs) {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("runner", string.IsNullOrEmpty(GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version) ? "0.0.0" : GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version));
                    if(!string.IsNullOrEmpty(GITHUB_TOKEN ?? githubAppToken)) {
                        client.DefaultRequestHeaders.Add("Authorization", $"token {GITHUB_TOKEN ?? githubAppToken}");
                    }
                    var urlBuilder = new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{hook.repository.full_name}/contents/.github%2Fworkflows"));
                    urlBuilder.Query = $"?ref={Uri.EscapeDataString(em.Value)}";
                    var res = await client.GetAsync(urlBuilder.ToString());
                    // {
                    //     "type": "gitea",
                    //     "config": {
                    //     "content_type": "json",
                    //     "url": "http://ubuntu.fritz.box/_apis/v1/Message"
                    //     },
                    //     "events": [
                    //     "create",
                    //     "delete",
                    //     "fork",
                    //     "push",
                    //     "issues",
                    //     "issue_assign",
                    //     "issue_label",
                    //     "issue_milestone",
                    //     "issue_comment",
                    //     "pull_request",
                    //     "pull_request_assign",
                    //     "pull_request_label",
                    //     "pull_request_milestone",
                    //     "pull_request_comment",
                    //     "pull_request_review_approved",
                    //     "pull_request_review_rejected",
                    //     "pull_request_review_comment",
                    //     "pull_request_sync",
                    //     "repository",
                    //     "release"
                    //     ],
                    //     "active": true
                    // }
                    if(res.StatusCode == System.Net.HttpStatusCode.OK) {
                        var content = await res.Content.ReadAsStringAsync();
                        var workflowList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UnknownItem>>(content);
                        List<KeyValuePair<string, string>> workflows = new List<KeyValuePair<string, string>>();
                        foreach (var item in workflowList)
                        {
                            try {
                                if(item.path.EndsWith(".yml") || item.path.EndsWith(".yaml")) {
                                    var fileRes = await client.GetAsync(item.download_url);
                                    var filecontent = await fileRes.Content.ReadAsStringAsync();
                                    workflows.Add(new KeyValuePair<string, string>(item.path, filecontent));
                                }
                            } catch (Exception ex) {
                                await Console.Error.WriteLineAsync(ex.Message);
                                await Console.Error.WriteLineAsync(ex.StackTrace);
                            }
                        }
                        foreach(var workflow in workflows) {
                            ConvertYaml(workflow.Key, workflow.Value, hook.repository.full_name, GitServerUrl, hook, obj.Value, em.Key, workflows: workflows.ToArray());
                        }
                    }
                }
            } catch (Exception ex) {
                await Console.Error.WriteLineAsync(ex.Message);
                await Console.Error.WriteLineAsync(ex.StackTrace);
            } finally {
                if(githubAppToken != null) {
                    await DeleteGithubAppToken(githubAppToken);
                }
            }
            return Ok();
        }

        [HttpPost("schedule")]
        public async Task<ActionResult> OnSchedule([FromQuery] string job, [FromQuery] int? list, [FromQuery] string[] env, [FromQuery] string[] secrets, [FromQuery] string[] matrix, [FromQuery] string[] platform, [FromQuery] bool? localcheckout)
        {
            if(WebhookSecret.Length > 0) {
                return NotFound();
            }
            var form = await Request.ReadFormAsync();
            KeyValuePair<GiteaHook, JObject> obj;
            var eventFile = form.Files.GetFile("event");
            using(var reader = new StreamReader(eventFile.OpenReadStream())) {
                string text = await reader.ReadToEndAsync();
                var obj_ = JObject.Parse(text);
                obj = new KeyValuePair<GiteaHook, JObject>(JsonConvert.DeserializeObject<GiteaHook>(text), obj_);
            }

            var workflow = (from f in form.Files where f.Name != "event" select new KeyValuePair<string, string>(f.FileName, new StreamReader(f.OpenReadStream()).ReadToEnd())).ToArray();

            // Try to fix head_commit == null 
            if(obj.Key.head_commit == null) {
                var val = obj.Value.GetValue("commits");
                if(val != null && val.HasValues) {
                    obj.Value["head_commit"] = val.First;
                }
            }
            string e = "push";
            StringValues ev;
            if(Request.Headers.TryGetValue("X-GitHub-Event", out ev) && ev.Count == 1 && ev.First().Length > 0) {
                e = ev.First();
            }
            var hook = obj.Key;
            if(workflow.Any()) {
                List<HookResponse> responses = new List<HookResponse>();
                foreach (var w in workflow) {
                    responses.Add(ConvertYaml(w.Key, w.Value, hook?.repository?.full_name ?? "Unknown/Unknown", GitServerUrl, hook, obj.Value, e, job, list >= 1, env, secrets, matrix, platform, localcheckout ?? true, workflow));
                }
                return await Ok(responses, true);
            }
            return Ok();
        }

        [HttpPost("schedule2")]
        public async Task<IActionResult> OnSchedule2([FromQuery] string job, [FromQuery] int? list, [FromQuery] string[] env, [FromQuery] string[] secrets, [FromQuery] string[] matrix, [FromQuery] string[] platform, [FromQuery] bool? localcheckout)
        {
            if(WebhookSecret.Length > 0) {
                return NotFound();
            }
            var form = await Request.ReadFormAsync();
            KeyValuePair<GiteaHook, JObject> obj;
            var eventFile = form.Files.GetFile("event");
            using(var reader = new StreamReader(eventFile.OpenReadStream())) {
                string text = await reader.ReadToEndAsync();
                var obj_ = JObject.Parse(text);
                obj = new KeyValuePair<GiteaHook, JObject>(JsonConvert.DeserializeObject<GiteaHook>(text), obj_);
            }

            var workflow = (from f in form.Files where f.Name != "event" select new KeyValuePair<string, string>(f.FileName, new StreamReader(f.OpenReadStream()).ReadToEnd())).ToArray();

            // Try to fix head_commit == null 
            if(obj.Key.head_commit == null) {
                var val = obj.Value.GetValue("commits");
                if(val != null && val.HasValues) {
                    obj.Value["head_commit"] = val.First;
                }
            }
            string e = "push";
            StringValues ev;
            if(Request.Headers.TryGetValue("X-GitHub-Event", out ev) && ev.Count == 1 && ev.First().Length > 0) {
                e = ev.First();
            }
            var hook = obj.Key;
            var requestAborted = HttpContext.RequestAborted;
            return new PushStreamResult(async stream => {
                var wait = requestAborted.WaitHandle;
                var writer = new StreamWriter(stream);
                try
                {
                    List<long> runid = new List<long>();
                    writer.NewLine = "\n";
                    var queue2 = Channel.CreateUnbounded<KeyValuePair<string,string>>(new UnboundedChannelOptions { SingleReader = true });
                    var chwriter = queue2.Writer;
                    TimeLineWebConsoleLogController.LogFeedEvent handler = (sender, timelineId2, recordId, record) => {
                        // (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>) val;
                        // Job job;
                        // if (TimelineController.dict.TryGetValue(timelineId2, out val) && _cache.TryGetValue(val.Item1[0].Id, out job) && runid.Contains(job.runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>("log", JsonConvert.SerializeObject(new { timelineId = timelineId2, recordId, record }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        // }
                    };
                    TimelineController.TimeLineUpdateDelegate handler2 = (timelineId2, timeline) => {
                        // (List<TimelineRecord>, ConcurrentDictionary<Guid, List<TimelineRecordLogLine>>) val;
                        // Job job;
                        // if(TimelineController.dict.TryGetValue(timelineId2, out val) && _cache.TryGetValue(val.Item1[0].Id, out job) && runid.Contains(job.runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>("timeline", JsonConvert.SerializeObject(new { timelineId = timelineId2, timeline }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        // }
                    };
                    MessageController.RepoDownload rd = (_runid, url, submodules, nestedSubmodules) => {
                        if(runid.Contains(_runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>("repodownload", JsonConvert.SerializeObject(new { url, submodules, nestedSubmodules }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        }
                    };

                    FinishJobController.JobCompleted completed = (ev) => {
                        // Job job;
                        // if(_cache.TryGetValue(ev.JobId, out job) && runid.Contains(job.runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>("finish", JsonConvert.SerializeObject(ev, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                        // }
                    };

                    Action<MessageController.WorkflowEventArgs> onworkflow = workflow_ => {
                        lock(runid) {
                            if(runid.Remove(workflow_.runid)) {
                                var empty = runid.Count == 0;
                                Action delay = async () => {
                                    await chwriter.WriteAsync(new KeyValuePair<string, string>("workflow", JsonConvert.SerializeObject(workflow_, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                                    if(empty) {
                                        // await Task.Delay(100);
                                        await chwriter.WriteAsync(new KeyValuePair<string, string>(null, null));
                                    }
                                };
                                delay.Invoke();
                            }
                        }
                    };
                    
                    var chreader = queue2.Reader;
                    var ping = Task.Run(async () => {
                        try {
                            while(!requestAborted.IsCancellationRequested) {
                                KeyValuePair<string, string> p = await chreader.ReadAsync(requestAborted);
                                await writer.WriteLineAsync($"event: {p.Key ?? ""}");
                                await writer.WriteLineAsync($"data: {p.Value ?? ""}");
                                await writer.WriteLineAsync();
                                await writer.FlushAsync();
                                if(p.Key == null && p.Value == null) {
                                    return;
                                }
                            }
                        } catch (OperationCanceledException) {

                        }
                    }, requestAborted);
                    try {
                        TimeLineWebConsoleLogController.logfeed += handler;
                        TimelineController.TimeLineUpdate += handler2;
                        MessageController.OnRepoDownload += rd;
                        FinishJobController.OnJobCompleted += completed;
                        MessageController.workflowevent += onworkflow;
                        List<HookResponse> responses = new List<HookResponse>();
                        lock(runid) {
                            if(workflow.Any()) {
                                foreach (var w in workflow) {
                                    HookResponse response = ConvertYaml(w.Key, w.Value, hook?.repository?.full_name ?? "Unknown/Unknown", GitServerUrl, hook, obj.Value, e, job, list >= 1, env, secrets, matrix, platform, localcheckout ?? true, workflow, run => runid.Add(run));
                                    if(response.skipped || response.failed) {
                                        runid.Remove(response.run_id);
                                        if(response.failed) {
                                            chwriter.WriteAsync(new KeyValuePair<string, string>("workflow", JsonConvert.SerializeObject(new WorkflowEventArgs() { runid = response.run_id, Success = false }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                                        }
                                    }
                                }
                            }
                            if(runid.Count == 0) {
                                chwriter.WriteAsync(new KeyValuePair<string, string>(null, null));
                            }
                        }

                        await ping;
                    } finally {
                        TimeLineWebConsoleLogController.logfeed -= handler;
                        TimelineController.TimeLineUpdate -= handler2;
                        MessageController.OnRepoDownload -= rd;
                        FinishJobController.OnJobCompleted -= completed;
                        MessageController.workflowevent -= onworkflow;
                    }
                } catch (OperationCanceledException) {

                } finally {
                    await writer.DisposeAsync();
                }
            }, "text/event-stream");
        }

        private static ConcurrentDictionary<Guid, Job> initializingJobs = new ConcurrentDictionary<Guid, Job>();

        [HttpGet]
        public async Task<IActionResult> GetJobs([FromQuery] string repo, [FromQuery] long[] runid, [FromQuery] int? depending, [FromQuery] Guid? jobid, [FromQuery] int? page) {
            if(jobid != null) {
                var j = GetJob(jobid.Value);
                if(j != null) {
                    return await Ok(j, true);
                } else if(initializingJobs.TryGetValue(jobid.Value, out j)) {
                    return await Ok(j, true);
                } else {
                    return NotFound("No such job found on this server");
                }
            }
            var query = from j in _context.Jobs where (repo == null || j.repo == repo) && (runid.Length == 0 || runid.Contains(j.runid)) orderby j.runid descending, j.WorkflowRunAttempt descending, j.RequestId descending select j;
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30).Include(j => j.WorkflowRunAttempt) : query.Include(j => j.WorkflowRunAttempt), true);
        }

        [HttpGet("owners")]
        public async Task<IActionResult> GetOwners([FromQuery] int? page) {
            var query = from j in _context.Set<Owner>() orderby j.Id descending select j;
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30) : query, true);
        }

        [HttpGet("repositories")]
        public async Task<IActionResult> GetRepositories([FromQuery] int? page, [FromQuery] string owner) {
            var query = from j in _context.Set<Repository>() where j.Owner.Name == owner orderby j.Id descending select j;
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30) : query, true);
        }

        [HttpGet("workflow/runs")]
        public async Task<IActionResult> GetWorkflows([FromQuery] int? page, [FromQuery] string owner, [FromQuery] string repo) {
            var query = (from j in _context.Set<WorkflowRunAttempt>() where j.Attempt == 1 && j.WorkflowRun.Workflow.Repository.Owner.Name == owner && j.WorkflowRun.Workflow.Repository.Name == repo orderby j.WorkflowRun.Id descending select j).Include(j => j.WorkflowRun).Select(j => new WorkflowRun() { EventName = j.EventName, FileName = j.WorkflowRun.FileName, DisplayName = j.WorkflowRun.DisplayName, Id = j.WorkflowRun.Id});
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30) : query, true);
        }

        [HttpGet("workflow/run/{id}")]
        public async Task<IActionResult> GetWorkflows(long id) {
            return await Ok((from r in _context.Set<WorkflowRun>() where r.Id == id select r).First(), true);
        }

        [HttpGet("workflow/run/{id}/attempts")]
        public async Task<IActionResult> GetWorkflowAttempts(long id, [FromQuery] int? page) {
            var query = from r in _context.Set<WorkflowRunAttempt>() where r.WorkflowRun.Id == id select r;
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30) : query, true);
        }

        [HttpGet("workflow/run/{id}/attempt/{attempt}")]
        public async Task<IActionResult> GetWorkflows(long id, int attempt) {
            var query = from r in _context.Set<WorkflowRunAttempt>() where r.WorkflowRun.Id == id && r.Attempt == attempt select r;
            return await Ok(query.First(), true);
        }

        [HttpGet("workflow/run/{id}/attempt/{attempt}/jobs")]
        public async Task<IActionResult> GetWorkflowAttempts(long id, int attempt, [FromQuery] int? page) {
            var query = from j in _context.Jobs where j.WorkflowRunAttempt.WorkflowRun.Id == id && j.WorkflowRunAttempt.Attempt == attempt select j;
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30) : query, true);
        }

        private void RerunWorkflow(long runid, Dictionary<string, List<Job>> finishedJobs = null) {
            var run = (from r in _context.Set<WorkflowRun>() where r.Id == runid select r).First();
            var lastAttempt = (from a in _context.Entry(run).Collection(r => r.Attempts).Query() orderby a.Attempt descending select a).First();

            long attempt = lastAttempt.Attempt + 1;
            var _attempt = new WorkflowRunAttempt() { Attempt = (int) attempt, WorkflowRun = run, EventPayload = lastAttempt.EventPayload, EventName = lastAttempt.EventName, Workflow = lastAttempt.Workflow };
            _context.Artifacts.Add(new ArtifactContainer() { Attempt = _attempt } );
            _context.SaveChanges();
            var payloadObject = JObject.Parse(lastAttempt.EventPayload);
            var hook = payloadObject.ToObject<GiteaHook>();
            var e = lastAttempt.EventName;
            string repository_name = hook?.repository?.full_name ?? "Unknown/Unknown";
            runid = run.Id;
            long runnumber = run.Id;

            var Ref = hook?.Ref;
            if(Ref == null) {
                if(e == "pull_request_target") {
                    var tmp = hook?.pull_request?.Base?.Ref;
                    if(tmp != null) {
                        Ref = "refs/heads/" + tmp;
                    }
                } else if(e == "pull_request" && hook?.Number != null) {
                    if(hook?.merge_commit_sha != null && HasPullRequestMergePseudoBranch) {
                        Ref = $"refs/pull/{hook.Number}/merge";
                    } else {
                        Ref = $"refs/pull/{hook.Number}/head";
                    }
                }
            } else if(hook?.ref_type != null) {
                if(e == "create") {
                    // Fixup create hooks to have a git ref
                    if(hook?.ref_type == "branch") {
                        Ref = "refs/heads/" + Ref;
                    } else if(hook?.ref_type == "tag") {
                        Ref = "refs/tags/" + Ref;
                    }
                    hook.After = hook?.Sha;
                } else {
                    Ref = null;
                }
            }
            if(Ref == null && hook?.repository?.default_branch != null) {
                Ref = "refs/heads/" + hook?.repository?.default_branch;
            }
            string Sha;
            if(e == "pull_request_target") {
                Sha = hook?.pull_request?.Base?.Sha;
            } else if(e == "pull_request") {
                if(hook?.merge_commit_sha == null || !HasPullRequestMergePseudoBranch) {
                    Sha = hook?.pull_request?.head?.Sha;
                } else {
                    Sha = hook.merge_commit_sha;
                }
            } else {
                Sha = hook.After;
            }
            ConvertYaml2(run.FileName, lastAttempt.Workflow, repository_name, GitServerUrl, hook, payloadObject, e, null, false, null, null, null, null, false, runid, runnumber, Ref, Sha, attempt: _attempt, finishedJobs: finishedJobs, statusSha: e == "pull_request_target" ? hook?.pull_request?.head?.Sha : Sha);
        }

        [HttpPost("rerun/{id}")]
        public void RerunJob(Guid id) {
            Job job = GetJob(id);
            var finishedJobs = new Dictionary<string, List<Job>>();
            foreach(var _job in (from j in _context.Jobs where j.runid == job.runid && (j.WorkflowIdentifier != job.WorkflowIdentifier || (j.Matrix != job.Matrix && job.Matrix != null)) select j).Include(z => z.Outputs).Include(z => z.WorkflowRunAttempt)) {
                if((job.WorkflowIdentifier != _job.WorkflowIdentifier || !TemplateTokenEqual(job.MatrixToken, _job.MatrixToken)) && !finishedJobs.TryAdd(_job.WorkflowIdentifier, new List<Job> { _job })) {
                    bool found = false;
                    for(int i = 0; i < finishedJobs[_job.WorkflowIdentifier].Count; i++) {
                        if(TemplateTokenEqual(finishedJobs[_job.WorkflowIdentifier][i].MatrixToken, _job.MatrixToken)) {
                            found = true;
                            if(finishedJobs[_job.WorkflowIdentifier][i].WorkflowRunAttempt.Attempt < _job.WorkflowRunAttempt.Attempt) {
                                finishedJobs[_job.WorkflowIdentifier][i] = _job;
                            }
                        }
                    }
                    if(!found) {
                        finishedJobs[_job.WorkflowIdentifier].Add(_job);
                    }
                }
            }
            Clone().RerunWorkflow(job.runid, finishedJobs);
        }

        [HttpPost("rerunworkflow/{id}")]
        public void RerunJob(long id) {
            Clone().RerunWorkflow(id);
        }

        [HttpPost("rerunFailed/{id}")]
        public void RerunFailedJobs(long id) {
            var finishedJobs = new Dictionary<string, List<Job>>();
            foreach(var _job in (from j in _context.Jobs where j.runid == id && j.Result == TaskResult.Succeeded select j).Include(z => z.Outputs).Include(z => z.WorkflowRunAttempt)) {
                if(!finishedJobs.TryAdd(_job.WorkflowIdentifier, new List<Job> { _job })) {
                    bool found = false;
                    for(int i = 0; i < finishedJobs[_job.WorkflowIdentifier].Count; i++) {
                        if(TemplateTokenEqual(finishedJobs[_job.WorkflowIdentifier][i].MatrixToken, _job.MatrixToken)) {
                            found = true;
                            if(finishedJobs[_job.WorkflowIdentifier][i].WorkflowRunAttempt.Attempt < _job.WorkflowRunAttempt.Attempt) {
                                finishedJobs[_job.WorkflowIdentifier][i] = _job;
                            }
                        }
                    }
                    if(!found) {
                        finishedJobs[_job.WorkflowIdentifier].Add(_job);
                    }
                }
            }
            Clone().RerunWorkflow(id, finishedJobs);
        }

        [HttpPost("cancel/{id}")]
        public void CancelJob(Guid id) {
            Job job = _cache.Get<Job>(id);
            if(job != null) {
                job.CancelRequest.Cancel();
                if(job.SessionId == Guid.Empty) {
                    new FinishJobController(_cache, _context).InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<String, VariableValue>() });
                }
            }
        }

        [HttpPost("cancelWorkflow/{runid}")]
        public void CancelWorkflow(long runid) {
            if(cancelWorkflows.TryGetValue(runid, out var source)) {
                source.Cancel();
            }
        }

        public class PushStreamResult: IActionResult
        {
            private readonly Func<Stream, Task> _onStreamAvailabe;
            private readonly string _contentType;

            public PushStreamResult(Func<Stream, Task> onStreamAvailabe, string contentType)
            {
                _onStreamAvailabe = onStreamAvailabe;
                _contentType = contentType;
            }

            public async Task ExecuteResultAsync(ActionContext context)
            {
                var stream = context.HttpContext.Response.Body;
                context.HttpContext.Response.GetTypedHeaders().ContentType = new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(_contentType);
                await _onStreamAvailabe(stream);
            }
        }

        private delegate void JobEvent(object sender, string repo, Job job);
        private static event JobEvent jobevent;
        public class WorkflowEventArgs {
            public long runid {get;set;}
            public bool Success {get;set;}
            public Dictionary<string, VariableValue> Outputs {get;set;}
        }
        public static event Action<WorkflowEventArgs> workflowevent;

        [HttpGet("event")]
        public IActionResult Message(string owner, string repo, [FromQuery] string filter, [FromQuery] long? runid)
        {
            var mfilter = new Minimatch.Minimatcher(filter ?? (owner + "/" + repo));
            var requestAborted = HttpContext.RequestAborted;
            return new PushStreamResult(async stream => {
                var wait = requestAborted.WaitHandle;
                var writer = new StreamWriter(stream);
                try
                {
                    writer.NewLine = "\n";
                    var queue2 = Channel.CreateUnbounded<KeyValuePair<string,Job>>(new UnboundedChannelOptions { SingleReader = true });
                    var chwriter = queue2.Writer;
                    JobEvent handler = (sender, crepo, job) => {
                        if (mfilter.IsMatch(crepo) && (runid == null || runid == job.runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, Job>(crepo, job));
                        }
                    };
                    var chreader = queue2.Reader;
                    var ping = Task.Run(async () => {
                        try {
                            while(!requestAborted.IsCancellationRequested) {
                                KeyValuePair<string, Job> p = await chreader.ReadAsync(requestAborted);
                                await writer.WriteLineAsync("event: job");
                                await writer.WriteLineAsync(string.Format("data: {0}", JsonConvert.SerializeObject(new { repo = p.Key, job = p.Value }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                                await writer.WriteLineAsync();
                                await writer.FlushAsync();
                            }
                        } catch (OperationCanceledException) {

                        }
                    }, requestAborted);
                    jobevent += handler;
                    await ping;
                    jobevent -= handler;
                } finally {
                    await writer.DisposeAsync();
                }
            }, "text/event-stream");
        }
    }
}
