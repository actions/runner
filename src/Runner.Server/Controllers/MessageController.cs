using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Schema;
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
using YamlDotNet.Serialization;
using System.Diagnostics;
using System.Reflection;
using Quartz;
using Quartz.Impl.Matchers;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Writers;
using Sdk.Actions;
using Sdk.Pipelines;
using ExecutionContext = Sdk.Pipelines.ExecutionContext;
using Microsoft.Extensions.FileProviders;
using Runner.Server.Services;
using static Runner.Server.Services.SecretHelper;

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public partial class MessageController : GitHubAppIntegrationBase
    {
        private string GitServerUrl;
        private string GitApiServerUrl;
        private string GitGraphQlServerUrl;
        private IMemoryCache _cache;
        private SqLiteDb _context;
        private IServiceProvider _provider;
        private WebConsoleLogService _webConsoleLogService;
        private ISchedulerFactory factory;
        private string GITHUB_TOKEN;
        private string GITHUB_TOKEN_READ_ONLY;
        private string GITHUB_TOKEN_NONE;
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
        private Dictionary<string, string> GitHubContext { get; }
        private bool AllowPrivateActionAccess { get; }
        private int Verbosity { get; }
        private int MaxWorkflowDepth { get; }
        private int MaxDifferentReferencedWorkflows { get; }
        private int MaxWorkflowFileSize { get; }
        private int MaxConcurrencyGroupNameLength { get; }
        private bool MergedInputs { get; set; }
        public bool ReusableWorkflowObjectType { get; private set; }

        private bool reusableWorkflowInheritEnv;
        private string workflowRootFolder;

        private bool DisableNoCI { get; }
        private string OnQueueJobProgram { get; }
        private string OnQueueJobArgs { get; }
        private MessageController Clone() {
            var nc = new MessageController(Configuration, _cache, new SqLiteDb(_context.Options), _provider, _webConsoleLogService, factory);
            // We have no access to the HttpContext in the clone
            nc.ServerUrl = ServerUrl;
            return nc;
        }

        public class QuartzScheduleWorkflowJob : IJob
        {
            private MessageController controller;
            public QuartzScheduleWorkflowJob(IConfiguration configuration, IMemoryCache memoryCache, SqLiteDb context, IServiceProvider provider, WebConsoleLogService webConsoleLogService, ISchedulerFactory factory = null) {
                controller = new MessageController(configuration, memoryCache, context, provider, webConsoleLogService, factory);
            }

            public async Task Execute(IJobExecutionContext context)
            {
                JobKey key = context.JobDetail.Key;
                var cronGroup = key.Group;
                var workflowFileFilter = cronGroup.Substring(cronGroup.LastIndexOf(controller.workflowRootFolder));
                var payload = context.MergedJobDataMap.GetString("payload");
                var jobj = JObject.Parse(payload);
                await controller.ExecuteWebhook("schedule", new KeyValuePair<GiteaHook, JObject>(jobj.ToObject<GiteaHook>(), jobj), workflowFileFilter);
            }
        }

        public MessageController(IConfiguration configuration, IMemoryCache memoryCache, SqLiteDb context, IServiceProvider provider, WebConsoleLogService webConsoleLogService, ISchedulerFactory factory = null) : base(configuration)
        {
            GitServerUrl = configuration.GetSection("Runner.Server")?.GetValue<string>("GitServerUrl") ?? "";
            GitApiServerUrl = configuration.GetSection("Runner.Server")?.GetValue<string>("GitApiServerUrl") ?? "";
            GitGraphQlServerUrl = configuration.GetSection("Runner.Server")?.GetValue<string>("GitGraphQlServerUrl") ?? "";
            GITHUB_TOKEN = configuration.GetSection("Runner.Server")?.GetValue<string>("GITHUB_TOKEN") ?? "";
            GITHUB_TOKEN_NONE = configuration.GetSection("Runner.Server")?.GetValue<string>("GITHUB_TOKEN_NONE") ?? "";
            GITHUB_TOKEN_READ_ONLY = configuration.GetSection("Runner.Server")?.GetValue<string>("GITHUB_TOKEN_READ_ONLY") ?? "";
            WebhookHMACAlgorithmName = configuration.GetSection("Runner.Server")?.GetValue<string>("WebhookHMACAlgorithmName") ?? "";
            WebhookSignatureHeader = configuration.GetSection("Runner.Server")?.GetValue<string>("WebhookSignatureHeader") ?? "";
            WebhookSignaturePrefix = configuration.GetSection("Runner.Server")?.GetValue<string>("WebhookSignaturePrefix") ?? "";
            WebhookSecret = configuration.GetSection("Runner.Server")?.GetValue<string>("WebhookSecret") ?? "";
            AllowPullRequests = configuration.GetSection("Runner.Server")?.GetValue<bool>("AllowPullRequests") ?? false;
            NoRecursiveNeedsCtx = configuration.GetSection("Runner.Server")?.GetValue<bool>("NoRecursiveNeedsCtx") ?? false;
            QueueJobsWithoutRunner = configuration.GetSection("Runner.Server")?.GetValue<bool>("QueueJobsWithoutRunner") ?? false;
            WriteAccessForPullRequestsFromForks = configuration.GetSection("Runner.Server")?.GetValue<bool>("WriteAccessForPullRequestsFromForks") ?? false;
            AllowJobNameOnJobProperties = configuration.GetSection("Runner.Server")?.GetValue<bool>("AllowJobNameOnJobProperties") ?? false;
            HasPullRequestMergePseudoBranch = configuration.GetSection("Runner.Server")?.GetValue<bool>("HasPullRequestMergePseudoBranch") ?? false;
            GitHubContext = configuration.GetSection("Runner.Server:GitHubContext").Get<Dictionary<string, string>>() ?? new Dictionary<string, string>();
            AllowPrivateActionAccess = configuration.GetSection("Runner.Server").GetValue<bool>("AllowPrivateActionAccess");
            Verbosity = configuration.GetSection("Runner.Server")?.GetValue<int>("Verbosity", 1) ?? 1;
            DisableNoCI = configuration.GetSection("Runner.Server").GetValue<bool>("DisableNoCI");
            OnQueueJobProgram = configuration.GetSection("Runner.Server").GetValue<string>("OnQueueJobProgram");
            OnQueueJobArgs = configuration.GetSection("Runner.Server").GetValue<string>("OnQueueJobArgs");
            MaxWorkflowDepth = configuration.GetSection("Runner.Server").GetValue<int>("MaxWorkflowDepth", 3);
            MaxDifferentReferencedWorkflows = configuration.GetSection("Runner.Server").GetValue<int>("MaxDifferentReferencedWorkflows", 20);
            MaxWorkflowFileSize = configuration.GetSection("Runner.Server").GetValue<int>("MaxWorkflowFileSize", 512 * 1024);
            MaxConcurrencyGroupNameLength = configuration.GetSection("Runner.Server").GetValue<int>("MaxConcurrencyGroupNameLength", 400);
            MergedInputs = configuration.GetSection("Runner.Server").GetValue<bool>("MergedInputs", true);
            ReusableWorkflowObjectType = configuration.GetSection("Runner.Server").GetValue<bool>("ReusableWorkflowObjectType", false);
            reusableWorkflowInheritEnv = configuration.GetSection("Runner.Server").GetValue<bool>("ReusableWorkflowInheritEnv", false);
            workflowRootFolder = configuration.GetSection("Runner.Server").GetValue<string>("WorkflowRootFolder", ".github/workflows");
            _cache = memoryCache;
            _context = context;
            _provider = provider;
            _webConsoleLogService = webConsoleLogService;
            this.factory = factory;
        }

        [HttpDelete("{poolId}/{messageId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "Agent")]
        public IActionResult DeleteMessage(int poolId, long messageId, Guid sessionId)
        {
            Session session;
            if(_cache.TryGetValue(sessionId, out session) && session.TaskAgentSession.SessionId == sessionId) {
                if(session.MessageLock.Wait(50000)) {
                    try {
                        session.Timer.Stop();
                        session.Timer.Start();
                        session.DropMessage = null;
                        if(session.Job != null && !session.JobAccepted) {
                            session.JobAccepted = true;
                        }
                    } finally {
                        session.MessageLock.Release();
                    }
                }
                return Ok();
            } else {
                return NotFound();
            }
        }

        class JobItem : JobItemFacade {
            public JobItem() {
                RequestId = Interlocked.Increment(ref reqId);
                ActionStatusQueue = new System.Threading.Tasks.Dataflow.ActionBlock<Func<Task>>(action => action().Wait());
                Cancel = new CancellationTokenSource();
                RefPrefix = "";
            }
            public CancellationTokenSource Cancel {get;}

            public System.Threading.Tasks.Dataflow.ActionBlock<Func<Task>> ActionStatusQueue {get;}

            public string name {get;set;}

            public string RefPrefix { get; set; }

            public string Stage {get;set;}
            public string DisplayName {get;set;}
            public ISet<string> Needs {get;set;}
            public FinishJobController.JobCompleted OnJobEvaluatable { get;set;}

            public Func<GitHub.DistributedTask.ObjectTemplating.ITraceWriter, bool> EvaluateIf { get;set;}

            public Guid Id { get; set;}
            public long RequestId { get; set;}
            public Guid TimelineId { get; set;}

            public List<JobItem> Childs { get; set; }

            private TaskResult? stat;

            public bool ContinueOnError {get;set;}
            public bool NoFailFast {get;set;}

            public TaskResult? Status { get => stat ?? (Childs?.Any() ?? false ? Childs.Any(c => c.Status == TaskResult.Failed) ? TaskResult.Failed : Childs.All(c => c.Status == TaskResult.Succeeded) ? TaskResult.Succeeded : null : stat); set => stat = ContinueOnError && value == TaskResult.Failed ? TaskResult.Succeeded : value; }
            public Dictionary<string, JobItem> Dependencies { get; set;}

            // Ref: https://docs.microsoft.com/en-us/azure/devops/pipelines/process/expressions?view=azure-devops#job-status-functions
            public bool Success { get => Dependencies?.All(p => p.Value.Status == TaskResult.Succeeded || p.Value.Status == TaskResult.SucceededWithIssues) ?? true; }
            public bool SucceededOrFailed { get => Dependencies?.All(p => p.Value.Status == TaskResult.Succeeded || p.Value.Status == TaskResult.SucceededWithIssues || p.Value.Status == TaskResult.Failed) ?? true; }
            public bool Failure { get => Dependencies?.Any(p => p.Value.Status == TaskResult.Failed) ?? false; }

            public bool Completed { get; set; }
            public bool NoStatusCheck { get; set; }
            public bool CheckRunStarted { get; set; }

            public JobCompletedEvent JobCompletedEvent { get; set; }

            public bool TryGetDependency(string name, out JobItemFacade jobItem) {
                if(Dependencies.TryGetValue(name, out var job)) {
                    jobItem = job;
                    return true;
                }
                jobItem = null;
                return false;
            }
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

        KeyValuePair<string, WorkflowPattern>[] CompilePatterns(SequenceToken sequence) {
            return (from item in sequence select new KeyValuePair<string, WorkflowPattern>(item.AssertString("pattern").Value, new WorkflowPattern(item.AssertString("pattern").Value))).ToArray();
        }

        bool skip(KeyValuePair<string, WorkflowPattern>[] sequence, IEnumerable<string> input, GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter = null) {
            
            return sequence != null && sequence.Length > 0 && !input.Any(file => {
                bool matched = false;
                foreach (var item in sequence) {
                    if(item.Value.Regex.IsMatch(file)) {
                        var pattern = item.Key;
                        if(item.Value.Negative) {
                            matched = false;
                            traceWriter?.Info("{0} excluded by pattern {1}", file, pattern);
                        } else {
                            matched = true;
                            traceWriter?.Info("{0} included by pattern {1}", file, pattern);
                        }
                    }
                }
                return matched;
            });
        }

        bool filter(KeyValuePair<string, WorkflowPattern>[] sequence, IEnumerable<string> input, GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter = null) {
            return sequence != null && sequence.Length > 0 && input.All(file => {
                foreach (var item in sequence)
                {
                    if(item.Value.Regex.IsMatch(file) ^ item.Value.Negative) {
                        var pattern = item.Key;
                        traceWriter?.Info("{0} ignored by pattern {1}", file, pattern);
                        return true;
                    }
                }
                return false;
            });
        }

        private class HookResponse {
            public string repo {get;set;}
            public long run_id {get;set;}
            public bool skipped {get;set;}
            public bool failed {get;set;}
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
                        string envValue = "";
                        if (env.Length > separatorIndex + 1)
                        {
                            envValue = env.Substring(separatorIndex + 1);
                        }
                        kvhandler.Invoke(envKey, envValue);
                    }
                }
            }
        }

        private static IEnumerable<MappingToken> LoadEnv(string[] contents)
        {
            var ret = new List<(ISet<string> keys, MappingToken envToken)>();
            LoadEnvSec(contents, (envKey, envValue) => {
                var entry = new KeyValuePair<ScalarToken, TemplateToken>(new StringToken(null, null, null, envKey), new StringToken(null, null, null, envValue));
                foreach(var l in ret) {
                    if(l.keys.Add(envKey)) {
                        l.envToken.Add(entry);
                        return;
                    }
                }
                var environment = new MappingToken(null, null, null);
                environment.Add(entry);
                ret.Add((new HashSet<string>(new [] { envKey }, StringComparer.OrdinalIgnoreCase), environment));
            });
            return from env in ret select env.envToken;
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

        public static ConcurrentDictionary<long, WorkflowState> WorkflowStates = new ConcurrentDictionary<long, WorkflowState>();
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
        private static ConcurrentDictionary<string, ConcurrencyGroup> concurrencyGroups = new ConcurrentDictionary<string, ConcurrencyGroup>(StringComparer.OrdinalIgnoreCase);

        private HookResponse ConvertYaml(string fileRelativePath, string content, string repository, string giteaUrl, GiteaHook hook, JObject payloadObject, string e = "push", string selectedJob = null, bool list = false, string[] env = null, string[] secrets = null, string[] _matrix = null, string[] platform = null, bool localcheckout = false, KeyValuePair<string, string>[] workflows = null, Action<long> workflowrun = null, string Ref = null, string Sha = null, string StatusCheckSha = null, ISecretsProvider secretsProvider = null, int? rrunid = null, string jobId = null, bool? failed = null, bool? rresetArtifacts = null, bool? refresh = null, string[] taskNames = null, bool azure = false) {
            string owner_name = repository.Split('/', 2)[0];
            string repo_name = repository.Split('/', 2)[1];
            Func<Workflow> getWorkflow = () => (from w in _context.Set<Workflow>() where w.FileName == fileRelativePath && w.Repository.Owner.Name == owner_name && w.Repository.Name == repo_name select w).FirstOrDefault();
            var run = new WorkflowRun { FileName = fileRelativePath, Workflow = getWorkflow() };
            long attempt = 1;
            var _attempt = new WorkflowRunAttempt() { Attempt = (int) attempt++, WorkflowRun = run, EventPayload = payloadObject.ToString(), EventName = e, Workflow = content, Ref = Ref, Sha = Sha, StatusCheckSha = StatusCheckSha };
            Dictionary<string, List<Job>> finishedJobs = null;
            if(rrunid != null) {
                run = (from r in _context.Set<WorkflowRun>() where r.Id == rrunid select r).First();
                var lastAttempt = (from a in _context.Entry(run).Collection(r => r.Attempts).Query() orderby a.Attempt descending select a).First();
                var firstAttempt = (from a in _context.Entry(run).Collection(r => r.Attempts).Query() orderby a.Attempt ascending select a).First();
                attempt = lastAttempt.Attempt + 1;
                _attempt = new WorkflowRunAttempt() { Attempt = (int) attempt, WorkflowRun = run, EventPayload = payloadObject.ToString(), EventName = e, Workflow = content, Ref = Ref, Sha = Sha, StatusCheckSha = StatusCheckSha, TimeLineId = firstAttempt.TimeLineId, ArtifactsMinAttempt = rresetArtifacts == true ? (int) attempt : lastAttempt.ArtifactsMinAttempt };
                if(failed == true) {
                    finishedJobs = getFailedJobs(rrunid.Value);
                } else if(!string.IsNullOrEmpty(jobId)) {
                    finishedJobs = getPreviousJobs(rrunid.Value).Where(kv => !(string.Equals(kv.Key, jobId, StringComparison.OrdinalIgnoreCase) || kv.Key.StartsWith(jobId + "/", StringComparison.OrdinalIgnoreCase))).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                } else if(refresh == true) {
                    finishedJobs = getPreviousJobs(rrunid.Value);
                }
            }
            _context.Artifacts.Add(new ArtifactContainer() { Attempt = _attempt } );
            if(run.Workflow == null && rrunid == null) {
                // Fix creating duplicated repositories
                lock(concurrencyGroups) {
                    if((run.Workflow = getWorkflow()) == null) {
                        Func<Owner> createOwner = () => {
                            var o = new Owner { Name = owner_name };
                            Task.Run(() => ownerevent?.Invoke(o));
                            return o;
                        };
                        Func<Repository> createRepo = () => {
                            var r = new Repository { Name = repo_name, Owner = (from o in _context.Set<Owner>() where o.Name == owner_name select o).FirstOrDefault() ?? createOwner() };
                            Task.Run(() => repoevent?.Invoke(r));
                            return r;
                        };
                        run.Workflow = new Workflow { FileName = fileRelativePath, Repository = (from r in _context.Set<Repository>() where r.Owner.Name == owner_name && r.Name == repo_name select r).FirstOrDefault() ?? createRepo() };
                    }
                    _context.SaveChanges();
                }
            } else {
                _context.SaveChanges();
            }
            run.Result = _attempt.Result;
            run.Ref = _attempt.Ref;
            run.Sha = _attempt.Sha;
            run.EventName = _attempt.EventName;
            run.Owner = owner_name;
            run.Repo = repo_name;
            Task.Run(() => runevent?.Invoke(owner_name, repo_name, run));
            workflowrun?.Invoke(run.Id);
            var runid = run.Id;
            long runnumber = run.Id;
            // Legacy compat of pre 3.6.0
            Ref = LegacyCompatFillRef(hook, e, Ref);
            Sha = LegacyCompatFillSha(hook, e, Sha);
            if(azure) {
                return AzureDevopsMain(fileRelativePath, content, repository, giteaUrl, hook, payloadObject, e, selectedJob, list, env, secrets, _matrix, platform, localcheckout, runid, runnumber, Ref, Sha, workflows: workflows, attempt: _attempt, statusSha: !string.IsNullOrEmpty(StatusCheckSha) ? StatusCheckSha : (e == "pull_request_target" ? hook?.pull_request?.head?.Sha : Sha), secretsProvider: secretsProvider, finishedJobs: finishedJobs, taskNames: taskNames);
            }
            return ConvertYaml2(fileRelativePath, content, repository, giteaUrl, hook, payloadObject, e, selectedJob, list, env, secrets, _matrix, platform, localcheckout, runid, runnumber, Ref, Sha, workflows: workflows, attempt: _attempt, statusSha: !string.IsNullOrEmpty(StatusCheckSha) ? StatusCheckSha : (e == "pull_request_target" ? hook?.pull_request?.head?.Sha : Sha), secretsProvider: secretsProvider, finishedJobs: finishedJobs);
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

        public static void UpdateJob(object sender, Job job) {
            Task.Run(() => jobupdateevent?.Invoke(sender, job.repo, job));
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

        private class CallingJob {
            public string Id {get;set;}
            public Guid TimelineId {get;set;}
            public Guid RecordId {get;set;}
            public string WorkflowName {get;set;}
            public string WorkflowRunName {get;set;}
            public string WorkflowFileName {get;set;}
            public string Name {get;set;}
            public string Event {get;set;}
            public PipelineContextData DispatchInputs {get;set;}
            public PipelineContextData Inputs {get;set;}
            public CancellationToken? CancellationToken {get;set;}
            public CancellationToken? ForceCancellationToken {get;set;}
            public Action<CallingJob, WorkflowEventArgs> Workflowfinish {get;set;}
            // Set by the called workflow to indicate whether to clean cached job dependencies
            public bool RanJob {get;set;}
            public Dictionary<string, string> Permissions { get; set; }
            public ISet<string> ProvidedInputs { get; set; }
            public ISet<string> ProvidedSecrets { get; set; }

            public string WorkflowSha {get;set;}
            public string WorkflowRef {get;set;}
            public string WorkflowRepo {get;set;}
            public string WorkflowPath {get;set;}
            public int Depth {get;set;}
            public TemplateToken JobConcurrency { get; set; }
            public ArrayContextData Matrix { get; set; }

            public static ArrayContextData ChildMatrix(ArrayContextData srcmatrices, PipelineContextData matrix) {
                var matrices = srcmatrices?.Clone() as ArrayContextData ?? new ArrayContextData();
                matrices.Add(matrix);
                return matrices;
            }
        }

        private static bool TryParseJobSelector(string selectedJob, out string jobname, out string matrix, out string child) {
            jobname = null;
            matrix = null;
            child = null;
            if(selectedJob == null) {
                return false;
            }
            var jobidEnd = selectedJob.IndexOf("/");
            var matrixBeg = selectedJob.IndexOf("(");
            if(jobidEnd == -1 && matrixBeg == -1) {
                jobname = selectedJob;
                return true;
            }
            if(matrixBeg != -1 && (matrixBeg < jobidEnd || jobidEnd == -1)) {
                var matrixEndDel = selectedJob.IndexOf("{", matrixBeg + 1);
                if(matrixEndDel == -1) {
                    return false;
                }
                var matrixDel = $"}}{selectedJob.Substring(matrixBeg + 1, matrixEndDel - (matrixBeg + 1))})";
                var matrixEnd = selectedJob.IndexOf(matrixDel, matrixEndDel + 1);
                if(matrixEnd == -1) {
                    return false;
                }
                jobname = selectedJob.Substring(0, matrixBeg);
                matrix = selectedJob.Substring(matrixEndDel, matrixEnd - matrixEndDel + 1);
                child = selectedJob.Length > (matrixEnd + matrixDel.Length + 1) && selectedJob[matrixEnd + matrixDel.Length] == '/' ? selectedJob.Substring(matrixEnd + matrixDel.Length + 1) : null;
                return true;
            }
            jobname = selectedJob.Substring(0, jobidEnd);
            child = selectedJob.Substring(jobidEnd + 1);
            return true;
        }

        private HookResponse ConvertYaml2(string fileRelativePath, string content, string repository, string giteaUrl, GiteaHook hook, JObject payloadObject, string e, string selectedJob, bool list, string[] env, string[] secrets, string[] _matrix, string[] platform, bool localcheckout, long runid, long runnumber, string Ref, string Sha, CallingJob callingJob = null, KeyValuePair<string, string>[] workflows = null, WorkflowRunAttempt attempt = null, string statusSha = null, Dictionary<string, List<Job>> finishedJobs = null, ISecretsProvider secretsProvider = null, List<TemplateToken> parentEnv = null) {
            attempt = _context.Set<WorkflowRunAttempt>().Find(attempt.Id);
            _context.Entry(attempt).Reference(a => a.WorkflowRun).Load();
            secretsProvider ??= new DefaultSecretsProvider(Configuration);
            bool asyncProcessing = false;
            Guid workflowTimelineId = callingJob?.TimelineId ?? attempt.TimeLineId;
            if(workflowTimelineId == Guid.Empty) {
                workflowTimelineId = Guid.NewGuid();
                attempt.TimeLineId = workflowTimelineId;
                _context.SaveChanges();
                _webConsoleLogService.CreateNewRecord(workflowTimelineId, new TimelineRecord{ Id = workflowTimelineId, Name = fileRelativePath, RefName = fileRelativePath, RecordType = "workflow" });
            }
            Guid workflowRecordId = callingJob?.RecordId ?? attempt.TimeLineId;
            if(workflowTimelineId == attempt.TimeLineId) {
                // Add workflow as dummy job, to improve early cancellation of Runner.Client
                initializingJobs.TryAdd(workflowTimelineId, new Job() { JobId = workflowTimelineId, TimeLineId = workflowTimelineId, runid = runid, workflowname = fileRelativePath } );
                if(attempt.Attempt > 1) {
                    workflowRecordId = Guid.NewGuid();
                    UpdateTimeLine(_webConsoleLogService.CreateNewRecord(workflowTimelineId, new TimelineRecord{ Id = workflowRecordId, ParentId = workflowTimelineId, Order = attempt.Attempt, Name = $"Attempt {attempt.Attempt}", RecordType = "workflow" }));
                } else {
                    UpdateTimeLine(workflowTimelineId, _webConsoleLogService.GetTimeLine(workflowTimelineId));
                }
            }
            Action finishWorkflow = () => {
                if(callingJob == null) {
                    SyncLiveLogsToDb(workflowTimelineId);
                }
                // Cleanup dummy job for this workflow
                if(workflowTimelineId == attempt.TimeLineId) {
                    initializingJobs.Remove(workflowTimelineId, out _);
                }
            };
            var workflowTraceWriter = new TraceWriter2(line => {
                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ line }), workflowTimelineId, workflowRecordId);
            }, Verbosity);
            workflowTraceWriter.Info($"Initialize Workflow Run {runid}");
            string event_name = e;
            string repository_name = repository;
            MappingToken workflowOutputs = null;
            var jobsctx = new DictionaryContextData();

            var workflowname = callingJob?.WorkflowName ?? fileRelativePath;
            Func<JobItem, TaskResult?, Task> updateJobStatus = async (next, status) => {
                var effective_event = callingJob?.Event ?? event_name;
                if(!string.IsNullOrEmpty(hook?.repository?.full_name) && !string.IsNullOrEmpty(statusSha) && !next.NoStatusCheck && (effective_event == "push" || ((effective_event == "pull_request" || effective_event == "pull_request_target") && (new [] { "opened", "synchronize", "synchronized", "reopened" }).Any(t => t == hook?.Action))) && !localcheckout) {
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
                            // Skipped jobs don't block required checks: so Skipped => Success https://github.com/github/docs/commit/66b433088115a579b7f1d774aa1ee852fc5ec2b
                            if(status == TaskResult.Succeeded || status == TaskResult.SucceededWithIssues || status == TaskResult.Skipped) {
                                jobstatus = JobStatus.Success;
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
                    } else {
                        var ghAppToken = await CreateGithubAppToken(repository_name, new { Permissions = new { metadata = "read", checks = "write" } });
                        if(ghAppToken != null) {
                            try {
                                var appClient2 = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"), new Uri(GitServerUrl))
                                {
                                    Credentials = new Octokit.Credentials(ghAppToken)
                                };
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
                                await DeleteGithubAppToken(ghAppToken);
                            }
                        }
                        
                    }
                }
            };
            // It seems like the inputs context is never null as of December 2022
            PipelineContextData inputs = new DictionaryContextData();
            DictionaryContextData globalEnv = null;
            DictionaryContextData vars = new DictionaryContextData();
            var globalVars = secretsProvider.GetVariablesForEnvironment("");
            foreach(var kv in globalVars) {
                vars[kv.Key] = new StringContextData(kv.Value);
            }
            var workflowContext = new WorkflowContext() { FileName = callingJob?.WorkflowFileName ?? fileRelativePath, EventPayload = payloadObject };
            workflowContext.FeatureToggles = globalVars;
            foreach(var t in secretsProvider.GetReservedSecrets()) {
                workflowContext.FeatureToggles[t.Key] = t.Value;
            }
            PipelineContextData contextsTemplate = null;
            bool dynamicContextTemplate = workflowContext.HasFeature("system.runner.server.dynamicContextTemplate");
            Func<JobItem, DictionaryContextData> evalContextTemplate = null;
            Func<JobItem, DictionaryContextData> createContext = job => {
                var contextData = (dynamicContextTemplate && evalContextTemplate != null ? evalContextTemplate(job) : contextsTemplate?.Clone() as DictionaryContextData) ?? new DictionaryContextData();
                contextData["inputs"] = inputs;
                contextData["vars"] = vars;
                contextData["env"] = globalEnv;
                contextData["secrets"] = null;
                if(contextData.ContainsKey("github")) {
                    return contextData;
                }
                var githubctx = new DictionaryContextData();
                contextData.Add("github", githubctx);
                githubctx.Add("server_url", new StringContextData(GitServerUrl));
                githubctx.Add("api_url", new StringContextData(GitApiServerUrl));
                githubctx.Add("graphql_url", new StringContextData(GitGraphQlServerUrl));
                githubctx.Add("workflow", new StringContextData(workflowname));
                githubctx.Add("repository", new StringContextData(repository_name));
                githubctx.Add("sha", new StringContextData(Sha ?? "000000000000000000000000000000000"));
                githubctx.Add("repository_owner", new StringContextData(repository_name.Split('/', 2)[0]));
                githubctx.Add("ref", new StringContextData(Ref));
                // TODO check if it is protected
                githubctx.Add("ref_protected", new BooleanContextData(false));
                githubctx.Add("ref_type", new StringContextData(Ref.StartsWith("refs/tags/") ? "tag" : Ref.StartsWith("refs/heads/") ? "branch" : ""));
                githubctx.Add("ref_name", new StringContextData(Ref.StartsWith("refs/tags/") ? Ref.Substring("refs/tags/".Length) : Ref.StartsWith("refs/heads/") ? Ref.Substring("refs/heads/".Length) : ""));
                if(AllowJobNameOnJobProperties) {
                    githubctx.Add("job", new StringContextData(job.name));
                }
                if(workflowContext.HasFeature("system.runner.server.workflowinfo")) {
                    var workflowRef = callingJob?.WorkflowRef ?? callingJob?.WorkflowSha ?? Ref ?? Sha;
                    var workflowRepo = callingJob?.WorkflowRepo ?? repository_name;
                    var job_workflow_ref = $"{workflowRepo}/{(callingJob?.WorkflowPath ?? workflowContext?.FileName ?? "")}@{workflowRef}";
                    githubctx["job_workflow_sha"] = new StringContextData(callingJob?.WorkflowSha ?? Sha);
                    githubctx["job_workflow_ref"] = new StringContextData(job_workflow_ref);
                    githubctx["job_workflow_ref_repository"] = new StringContextData(workflowRepo);
                    githubctx["job_workflow_ref_repository_owner"] = new StringContextData(workflowRepo.Split('/', 2)[0]);
                    githubctx["job_workflow_ref_repository_name"] = new StringContextData(workflowRepo.Split('/', 2)[1]);
                    githubctx["job_workflow_ref_ref"] = new StringContextData(workflowRef);
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
                // The Git URL to the repository. For example, git://github.com/codertocat/hello-world.git.
                githubctx["repositoryUrl"] = new StringContextData(hook?.repository?.CloneUrl ?? $"{GitServerUrl}/{repository_name}.git");
                foreach(var kv in GitHubContext) {
                    githubctx[kv.Key] = new StringContextData(kv.Value);
                }
                return contextData;
            };
            try {
                var workflowBytelen = System.Text.Encoding.UTF8.GetByteCount(content);
                if(workflowBytelen > MaxWorkflowFileSize) {
                    throw new Exception("Workflow size too large {workflowBytelen} exceeds {MaxWorkflowFileSize} bytes");
                }
                List<JobItem> jobgroup = new List<JobItem>();
                List<JobItem> dependentjobgroup = new List<JobItem>();
                var token = default(TemplateToken);
                {
                    workflowContext.FileTable = new List<string> { fileRelativePath };
                    var templateContext = CreateTemplateContext(workflowTraceWriter, workflowContext);
                    // Get the file ID
                    var fileId = templateContext.GetFileId(fileRelativePath);

                    workflowTraceWriter.Trace("Parsing Workflow...");

                    // Read the file
                    var fileContent = content ?? System.IO.File.ReadAllText(fileRelativePath);
                    using (var stringReader = new StringReader(fileContent))
                    {
                        var yamlObjectReader = new YamlObjectReader(fileId, stringReader, workflowContext.HasFeature("system.runner.server.yaml.anchors"), workflowContext.HasFeature("system.runner.server.yaml.fold"), workflowContext.HasFeature("system.runner.server.yaml.merge"));
                        token = TemplateReader.Read(templateContext, workflowContext.HasFeature("system.runner.server.strictValidation") ? "workflow-root-strict" : "workflow-root", yamlObjectReader, fileId, out _);
                    }

                    templateContext.Errors.Check();
                }
                if(token == null) {
                    throw new Exception("token is null after parsing your workflow, this should never happen");
                }
                var actionMapping = token.AssertMapping("workflow root");

                TemplateToken workflowDefaults = null;
                List<TemplateToken> workflowEnvironment = new List<TemplateToken>();
                if(parentEnv?.Count > 0) {
                    workflowEnvironment.AddRange(parentEnv);
                } else if(env?.Length > 0) {
                    workflowEnvironment.AddRange(LoadEnv(env));
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
                workflowname = callingJob?.WorkflowName ?? (from r in actionMapping where r.Key.AssertString("workflow root mapping key").Value == "name" select r).FirstOrDefault().Value?.AssertString("name").Value ?? workflowname;
                TemplateToken globalEnvToken = (from r in actionMapping where r.Key.AssertString("workflow root mapping key").Value == "env" select r).FirstOrDefault().Value;
                if(globalEnvToken != null) {
                    workflowEnvironment.Add(globalEnvToken);
                }
                Action initGlobalEnvAndContexts = () => {
                    if(!workflowContext.HasFeature("system.runner.server.disablecontextTemplate") && (workflowContext.FeatureToggles.TryGetValue("system.runner.server.contextTemplate", out var contextTemplate) || (contextTemplate = PipelineTemplateSchemaFactory.LoadResource("contextTemplate.yml")) != null)) {
                        evalContextTemplate = job => {
                            using(var reader = new StringReader(contextTemplate)) {
                                var objectReader = new YamlObjectReader(null, reader, true, true, true);
                                var contextData = new DictionaryContextData();
                                var serverctx = new DictionaryContextData();
                                contextData["server"] = serverctx;
                                serverctx["event"] = payloadObject.ToPipelineContextData();
                                serverctx.Add("event_name", new StringContextData(callingJob?.Event ?? event_name));
                                serverctx.Add("server_url", new StringContextData(GitServerUrl));
                                serverctx.Add("api_url", new StringContextData(GitApiServerUrl));
                                serverctx.Add("graphql_url", new StringContextData(GitGraphQlServerUrl));
                                serverctx.Add("workflow", new StringContextData(workflowname));
                                serverctx.Add("repository", new StringContextData(repository_name));
                                serverctx.Add("sha", new StringContextData(Sha));
                                serverctx.Add("ref", new StringContextData(Ref));
                                serverctx.Add("run_id", new StringContextData(runid.ToString()));
                                serverctx.Add("run_number", new StringContextData(runnumber.ToString()));
                                serverctx.Add("run_attempt", new StringContextData(attempt.Attempt.ToString()));
                                if(job != null) {
                                    var jobctx = new DictionaryContextData();
                                    jobctx["name"] = new StringContextData(job.name);
                                    if(job.DisplayName != null) {
                                        jobctx["displayName"] = new StringContextData(job.DisplayName);
                                    }
                                    jobctx["instance_id"] = new StringContextData(job.Id.ToString());
                                    var needs = new ArrayContextData();
                                    foreach(var dep in job.Needs) {
                                        needs.Add(new StringContextData(dep));
                                    }
                                    jobctx["needs"] = needs;
                                    serverctx["job"] = jobctx;
                                }
                                serverctx["workflow_file_name"] = new StringContextData(workflowContext?.FileName ?? "");
                                if(workflowContext.WorkflowRunName != null) {
                                    serverctx["workflow_run_name"] = new StringContextData(workflowContext.WorkflowRunName);
                                }
                                serverctx["variables"] = vars;
                                var workflowRef = callingJob?.WorkflowRef ?? callingJob?.WorkflowSha ?? Ref ?? Sha;
                                var workflowRepo = callingJob?.WorkflowRepo ?? repository_name;
                                var job_workflow_ref = $"{workflowRepo}/{(callingJob?.WorkflowPath ?? workflowContext?.FileName ?? "")}@{workflowRef}";
                                serverctx["job_workflow_sha"] = new StringContextData(callingJob?.WorkflowSha ?? Sha);
                                serverctx["job_workflow_ref"] = new StringContextData(job_workflow_ref);
                                serverctx["job_workflow_ref_repository"] = new StringContextData(workflowRepo);
                                serverctx["job_workflow_ref_repository_owner"] = new StringContextData(workflowRepo.Split('/', 2)[0]);
                                serverctx["job_workflow_ref_repository_name"] = new StringContextData(workflowRepo.Split('/', 2)[1]);
                                serverctx["job_workflow_ref_ref"] = new StringContextData(workflowRef);
                                var templateContext = CreateTemplateContext(workflowContext.HasFeature("system.runner.server.debugcontextTemplate") ? workflowTraceWriter : new EmptyTraceWriter(), workflowContext, contextData);
                                templateContext.Schema = PipelineTemplateSchemaFactory.LoadSchema("contextTemplateSchema.json");
                                templateContext.Flags |= ExpressionFlags.ExtendedDirectives | ExpressionFlags.ExtendedFunctions | ExpressionFlags.AllowAnyForInsert;
                                var token = TemplateReader.Read(templateContext, "context-root", objectReader, null, out _);
                                templateContext.Errors.Check();
                                var ret = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "context-root", token, 0, null, true)?.AssertMapping("context-root").ToContextData();
                                templateContext.Errors.Check();
                                return ret as DictionaryContextData;
                            }
                        };
                        contextsTemplate = evalContextTemplate(null);
                    }
                    if(workflowEnvironment.Count > 0) {
                        globalEnv = new DictionaryContextData();
                        foreach(var cEnv in workflowEnvironment) {
                            var contextData = createContext(null);
                            var templateContext = CreateTemplateContext(workflowTraceWriter, workflowContext, contextData);
                            var workflowEnv = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "workflow-env", cEnv, 0, null, true);
                            // Best effort, don't check for errors
                            // templateContext.Errors.Check();
                            // Best effort, make global env available this is not available on github actions
                            if(workflowEnv is MappingToken genvToken) {
                                foreach(var kv in genvToken) {
                                    if(kv.Key is StringToken key && kv.Value is StringToken val) {
                                        globalEnv[key.Value] = new StringContextData(val.Value);
                                    }
                                }
                            }
                        }
                    }
                };
                // Delay initialize the template for workflow_dispatch, since the inputs in the event payload are modified
                if(e != "workflow_dispatch") {
                    initGlobalEnvAndContexts();
                }

                TemplateToken tk = (from r in actionMapping where r.Key.AssertString("workflow root mapping key").Value == "on" select r).FirstOrDefault().Value;
                if(tk == null) {
                    throw new Exception("Your workflow is invalid, missing 'on' property");
                }
                if((e == "push" || e == "schedule") && factory != null && Ref == ("refs/heads/" + (hook?.repository?.default_branch ?? "main"))) {
                    var schedules = tk is MappingToken scheduleMapping ? (from r in tk.AssertMapping("on") where r.Key.AssertString("schedule").Value == "schedule" select r).FirstOrDefault().Value?.AssertSequence("on.schedule.*.cron") : null;
                    var cm = schedules != null ? (from cron in schedules select cron.AssertMapping("cron").FirstOrDefault().Value?.AssertString("cronval")?.Value).ToList() : new List<string>();
                    var groupName = $"workflowscheduler_{repository_name}_{fileRelativePath}";
                    ((Action)(async () => {
                        var scheduler = await factory.GetScheduler();
                        var currentSchedules = await scheduler.GetJobKeys(GroupMatcher<JobKey>.GroupEquals(groupName));
                        foreach(var sched in currentSchedules) {
                            if(cm.Contains(sched.Name)) {
                                cm.Remove(sched.Name);
                            } else {
                                await scheduler.DeleteJob(sched);
                            }
                        }
                        foreach(var sched in cm) {
                            try {
                                var schedArray = sched.Split(" ");
                                var regex = new Regex("[0-9]+");
                                schedArray[2] = regex.Replace(schedArray[2], m => (int.Parse(m.Value)-1).ToString());
                                schedArray[3] = regex.Replace(schedArray[3], m => (int.Parse(m.Value)-1).ToString());
                                // Needs to be delayed, otherwise it ends up with -1/2, if it uses */2
                                schedArray = schedArray.Select(s => s.Replace("*/", "0/")).ToArray();
                                schedArray[4] = regex.Replace(schedArray[4], m => (int.Parse(m.Value)+1).ToString());
                                List<string> scheds = new List<string>();
                                if(schedArray[4] != "*") {
                                    scheds.Add($"0 {schedArray[0]} {schedArray[1]} * ? {schedArray[4]}");
                                }
                                if(schedArray[2] != "*" || schedArray[3] != "*" || !scheds.Any()) {
                                    scheds.Add($"0 {schedArray[0]} {schedArray[1]} {schedArray[2]} {schedArray[3]} ?");
                                }
                                var payload = new JObject();
                                payload["schedule"] = sched;
                                if(payloadObject.TryGetValue("sender", out var v)) {
                                    payload["sender"] = v;
                                }
                                if(payloadObject.TryGetValue("repository", out v)) {
                                    payload["repository"] = v;
                                }
                                if(payloadObject.TryGetValue("organization", out v)) {
                                    payload["organization"] = v;
                                }
                                if(payloadObject.TryGetValue("enterprise", out v)) {
                                    payload["enterprise"] = v;
                                }
                                var details = JobBuilder.Create<QuartzScheduleWorkflowJob>().WithIdentity(sched, groupName).UsingJobData("payload", payload.ToString()).Build();
                                await scheduler.ScheduleJob(details, (from s in scheds select TriggerBuilder.Create().WithCronSchedule(s).Build()).ToList(), true);
                            } catch {

                            }
                        }
                    }))();
                }
                Func<HookResponse> skipWorkflow = () => {
                    if(callingJob != null) {
                        callingJob.Workflowfinish.Invoke(callingJob, new WorkflowEventArgs { runid = runid, Success = false });
                    } else {
                        attempt.Status = Status.Completed;
                        attempt.Result = TaskResult.Skipped;
                        UpdateWorkflowRun(attempt, repository_name);
                        _context.SaveChanges();
                    }
                    return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                };
                MappingToken mappingEvent = null;
                switch(tk.Type) {
                    case TokenType.String:
                        if(tk.AssertString("on").Value != e) {
                            // Skip, not the right event
                            workflowTraceWriter.Info($"Skipping the Workflow, '{tk.AssertString("str").Value}' isn't is the requested event '{e}'");
                            return skipWorkflow();
                        }
                        break;
                    case TokenType.Sequence:
                        if((from r in tk.AssertSequence("on") where r.AssertString(e).Value == e select r).FirstOrDefault() == null) {
                            // Skip, not the right event
                            workflowTraceWriter.Info($"Skipping the Workflow, [{string.Join(',', from r in tk.AssertSequence("seq") select "'" + r.AssertString(e).Value + "'")}] doesn't contain the requested event '{e}'");
                            return skipWorkflow();
                        }
                        break;
                    case TokenType.Mapping:
                        var e2 = (from r in tk.AssertMapping("on") where r.Key.AssertString(e).Value == e select r).FirstOrDefault();
                        var rawEvent = e2.Value;
                        if(rawEvent == null) {
                            workflowTraceWriter.Info($"Skipping the Workflow, [{string.Join(',', from r in tk.AssertMapping("mao") select "'" + r.Key.AssertString(e).Value + "'")}] doesn't contain the requested event '{e}'");
                            return skipWorkflow();
                        }
                        if(e == "schedule") {
                            var crons = e2.Value.AssertSequence("on.schedule.*.cron");
                            var cm = (from cron in crons select cron.AssertMapping("cron")).ToArray();
                            if(cm.Length == 0 || !cm.All(c => c.Count == 1 && c.First().Key.AssertString("cron key").Value == "cron")) {
                                throw new Exception("Only cron is supported!");
                            }
                            var values = (from c in cm select c.First().Value.AssertString("cron value").Value).ToArray();
                            var validator = new Regex("^(((\\d+,)+\\d+|((\\d+|\\*)\\/\\d+|JAN|FEB|MAR|APR|MAY|JUN|JUL|AUG|SEP|OCT|NOV|DEC)|(\\d+-\\d+)|\\d+|\\*|MON|TUE|WED|THU|FRI|SAT|SUN) ?){5,7}$");
                            if(!values.All(s => validator.IsMatch(s))) {
                                var z = 0;
                                var sb = new StringBuilder();
                                foreach (var prop in (from cronpattern in values where !validator.IsMatch(cronpattern) select cronpattern)) {
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
                        var rawTypes = workflowContext.HasFeature("system.runner.server.workflow_dispatch-rawtypes");
                        var correctNumberType = workflowContext.HasFeature("system.runner.server.workflow_dispatch-correct-number-type");
                        allowed.Add("inputs");
                        // Validate inputs and apply defaults
                        var workflowInputs = mappingEvent != null ? (from r in mappingEvent where r.Key.AssertString("inputs").Value == "inputs" select r).FirstOrDefault().Value?.AssertMapping("map") : null;
                        ISet<string> validInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var dispatchInputs = payloadObject["inputs"] as JObject;
                        if(dispatchInputs == null) {
                            dispatchInputs = new JObject();
                            payloadObject["inputs"] = dispatchInputs;
                        }
                        // https://github.com/github/feedback/discussions/9092#discussioncomment-2453678
                        var inputsCtx = new DictionaryContextData();
                        inputs = inputsCtx; // Released https://github.blog/changelog/2022-06-10-github-actions-inputs-unified-across-manual-and-reusable-workflows/
                        if(workflowInputs != null) {
                            foreach(var input in workflowInputs) {
                                var inputName = input.Key.AssertString("on.workflow_dispatch.inputs mapping key").Value;
                                validInputs.Add(inputName);
                                var inputInfo = input.Value?.AssertMapping($"on.workflow_dispatch.inputs.{inputName}");
                                if(inputInfo != null) {
                                    var workflowDispatchMappingKey = $"on.workflow_dispatch.inputs.{inputName} mapping key";
                                    bool required = (from r in inputInfo where r.Key.AssertString(workflowDispatchMappingKey).Value == "required" select r.Value.AssertBoolean($"on.workflow_dispatch.{inputName}.required").Value).FirstOrDefault();
                                    string type = (from r in inputInfo where r.Key.AssertString(workflowDispatchMappingKey).Value == "type" select r.Value.AssertString($"on.workflow_dispatch.{inputName}.type").Value).FirstOrDefault();
                                    SequenceToken options = (from r in inputInfo where r.Key.AssertString(workflowDispatchMappingKey).Value == "options" select r.Value.AssertSequence($"on.workflow_dispatch.{inputName}.options")).FirstOrDefault();
                                    JToken def = (from r in inputInfo where r.Key.AssertString(workflowDispatchMappingKey).Value == "default" select r.Value).FirstOrDefault()?.ToContextData()?.ToJToken();
                                    if(def == null) {
                                        switch(type) {
                                        case "boolean":
                                            def = false;
                                        break;
                                        case "number":
                                            def = 0;
                                        break;
                                        case "choice":
                                            def = options?.FirstOrDefault()?.AssertString($"on.workflow_dispatch.{inputName}.options[0]")?.Value ?? "";
                                        break;
                                        case "string":
                                        case "environment":
                                            def = "";
                                        break;
                                        }
                                    }
                                    
                                    var actualInputName = (from di in dispatchInputs.Properties() where string.Equals(di.Name, inputName, StringComparison.OrdinalIgnoreCase) select di.Name).FirstOrDefault();
                                    if(actualInputName == null) {
                                        if(required) {
                                            throw new Exception($"This workflow requires the input: {inputName}, but no such input were provided");
                                        }
                                        dispatchInputs[inputName] = !rawTypes && def?.Type == JTokenType.Boolean ? ((bool)def ? "true" : "false") : (!rawTypes && (def?.Type == JTokenType.String || def?.Type == JTokenType.Float || def?.Type == JTokenType.Integer) ? def?.ToString() : def);
                                        actualInputName = inputName;
                                    }
                                    inputsCtx[actualInputName] = dispatchInputs[actualInputName]?.ToPipelineContextData();
                                    // Allow boolean and number types with a string webhook payload for GitHub Actions compat
                                    if(dispatchInputs[actualInputName]?.Type == JTokenType.String) {
                                        switch(type) {
                                        case "boolean":
                                            // https://github.com/actions/runner/issues/1483#issuecomment-1091025877
                                            bool result;
                                            var val = dispatchInputs[actualInputName].ToString();
                                            switch(val) {
                                            case "true":
                                                result = true;
                                            break;
                                            case "false":
                                                result = false;
                                            break;
                                            default:
                                                throw new Exception($"on.workflow_dispatch.inputs.{inputName}, expected true or false, unexpected value: {val}");
                                            }
                                            inputsCtx[actualInputName] = new BooleanContextData(result);
                                        break;
                                        case "number":
                                            // Based on manual testing numbers are still strings in inputs ctx 2023/04/24, even if the number type is included in the strict schema file
                                            if(correctNumberType) {
                                                if(Double.TryParse(dispatchInputs[actualInputName].ToString(), out var numbervalue)) {
                                                    inputsCtx[actualInputName] = new NumberContextData(numbervalue);
                                                } else {
                                                    throw new Exception($"on.workflow_dispatch.inputs.{inputName}, expected a number, unexpected value: {dispatchInputs[actualInputName].ToString()}");
                                                }
                                            }
                                        break;
                                        }
                                    }
                                }
                            }
                        }
                        foreach(var providedInput in dispatchInputs) {
                            if(!validInputs.Contains(providedInput.Key)) {
                                throw new Exception($"This workflow doesn't define input {providedInput.Key}");
                            }
                        }
                        // Initialize the custom GitHub context and global env
                        initGlobalEnvAndContexts();
                    }
                    if(e == "workflow_call") {
                        allowed.Add("inputs");
                        allowed.Add("outputs");
                        allowed.Add("secrets");
                        // Validate inputs and apply defaults
                        var workflowInputs = mappingEvent != null ? (from r in mappingEvent where r.Key.AssertString("on.workflow_call mapping key").Value == "inputs" select r).FirstOrDefault().Value?.AssertMapping("on.workflow_call.inputs") : null;
                        workflowOutputs = mappingEvent != null ? (from r in mappingEvent where r.Key.AssertString("on.workflow_call mapping key").Value == "outputs" select r).FirstOrDefault().Value?.AssertMapping("on.workflow_call.outputs") : null;
                        ISet<string> validInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        inputs = callingJob?.Inputs ?? new DictionaryContextData();
                        if(workflowInputs != null) {
                            foreach(var input in workflowInputs) {
                                var inputName = input.Key.AssertString("on.workflow_call.inputs mapping key").Value;
                                validInputs.Add(inputName);
                                var inputInfo = input.Value?.AssertMapping($"on.workflow_call.inputs.{inputName}");
                                if(inputInfo != null) {
                                    var workflowCallInputMappingKey = $"on.workflow_call.inputs.{inputName} mapping key";
                                    bool required = (from r in inputInfo where r.Key.AssertString(workflowCallInputMappingKey).Value == "required" select r.Value.AssertBoolean($"on.workflow_call.inputs.{inputName}.required").Value).FirstOrDefault();
                                    string type = (from r in inputInfo where r.Key.AssertString(workflowCallInputMappingKey).Value == "type" select r.Value.AssertString($"on.workflow_call.inputs.{inputName}.type").Value).FirstOrDefault();
                                    var defassertMessage = $"on.workflow_call.inputs.{inputName}.default";
                                    var rawdef = (from r in inputInfo where r.Key.AssertString(workflowCallInputMappingKey).Value == "default" select r.Value).FirstOrDefault();
                                    if(rawdef != null) {
                                        workflowTraceWriter.Info($"Evaluate {defassertMessage}");
                                        var contextData = createContext(null);
                                        contextData["inputs"] = callingJob?.DispatchInputs;
                                        var templateContext = CreateTemplateContext(workflowTraceWriter, workflowContext, contextData);
                                        rawdef = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, templateContext.Schema.Definitions.ContainsKey("workflow_call-input-context") ? "workflow_call-input-context" : "workflow-call-input-default", rawdef, 0, null, true);
                                        templateContext.Errors.Check();
                                    }
                                    var def = rawdef?.ToContextData();
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
                                        if(!ReusableWorkflowObjectType || type != "object") {
                                            throw new Exception($"on.workflow_call.inputs.{inputName}.type assigned to invalid type: '{type}', expected {(ReusableWorkflowObjectType ? "'object', " : "")}'string', 'number' or 'boolean'");
                                        }
                                        break;
                                    }
                                    var inputsDict = inputs.AssertDictionary("dict");
                                    var assertMessage = $"This workflow requires that the input: {inputName}, to have type {type}";
                                    if(callingJob?.ProvidedInputs?.Contains(inputName) == true && inputsDict.TryGetValue(inputName, out var val)) {
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
                        if(callingJob?.ProvidedInputs != null) {
                            foreach(var name in callingJob.ProvidedInputs) {
                                if(!validInputs.Contains(name)) {
                                    throw new Exception($"This workflow doesn't define input {name}");
                                }
                            }
                        }
                        // Validate secrets, bypass validation for secrets: inherit
                        if(callingJob?.ProvidedSecrets != null) {
                            var workflowSecrets = mappingEvent != null ? (from r in mappingEvent where r.Key.AssertString("on.workflow_call mapping key").Value == "secrets" select r).FirstOrDefault().Value?.AssertMapping("on.workflow_call.secrets") : null;
                            ISet<string> validSecrets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            if(workflowSecrets != null) {
                                foreach(var secret in workflowSecrets) {
                                    var secretName = secret.Key.AssertString("on.workflow_call.secrets mapping key").Value;
                                    if(IsReservedVariable(secretName)) {
                                        throw new Exception($"This workflow defines the reserved secret {secretName}, using it can cause undefined behavior");
                                    }
                                    var secretMapping = secret.Value?.AssertMapping($"on.workflow_call.secrets.{secretName}");
                                    if(secretMapping != null) {
                                        var workflowCallSecretsMappingKey = $"on.workflow_call.secrets.{secretName} mapping key";
                                        validSecrets.Add(secretName);
                                        bool required = (from r in secretMapping where r.Key.AssertString(workflowCallSecretsMappingKey).Value == "required" select r.Value.AssertBoolean($"on.workflow_call.secrets.{secretName}.required").Value).FirstOrDefault();
                                        
                                        if(!callingJob.ProvidedSecrets.Contains(secretName) && required) {
                                            throw new Exception($"This workflow requires the secret: {secretName}, but no such secret were provided");
                                        }
                                    }
                                }
                            }
                            foreach(var name in callingJob.ProvidedSecrets) {
                                if(!validSecrets.Contains(name)) {
                                    throw new Exception($"This workflow doesn't define secret {name}");
                                }
                            }
                        }
                    }

                    if(mappingEvent != null && !mappingEvent.All(p => allowed.Any(s => s == p.Key.AssertString($"on.{event_name} mapping key").Value))) {
                        var z = 0;
                        var sb = new StringBuilder();
                        foreach (var prop in (from p in mappingEvent where !allowed.Any(s => s == p.Key.AssertString($"on.{event_name} mapping key").Value) select p.Key.AssertString($"on.{event_name} mapping key").Value)) {
                            if(z++ != 0) {
                                sb.Append(", ");
                            }
                            sb.Append(prop);
                        }
                        workflowTraceWriter.Info($"The following event properties are invalid: {sb.ToString()}, please remove them from {e}");
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
                    var fworkflows = extractFilter("workflows");

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
                            if(!(from t in types select t.AssertString($"on.{event_name}.type.*").Value).Any(t => t == hook?.Action)) {
                                workflowTraceWriter.Info($"Skipping Workflow, due to types filter. Requested Action was {hook?.Action}, but require {string.Join(',', from t in types select "'" + t.AssertString($"on.{event_name}.type.*").Value + "'")}");
                                return skipWorkflow();
                            }
                        } else if(e == "pull_request" || e == "pull_request_target"){
                            var prdefaults = new [] { "opened", "synchronize", "synchronized", "reopened" };
                            if(!prdefaults.Any(t => t == hook?.Action)) {
                                workflowTraceWriter.Info($"Skipping Workflow, due to default types filter of the {e} trigger. Requested Action was {hook?.Action}, but require {string.Join(',', from t in prdefaults select "'" + t + "'")}");
                                return skipWorkflow();
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

                            if(branchesIgnore != null && filter(CompilePatterns(branchesIgnore), new[] { branch }, workflowTraceWriter)) {
                                workflowTraceWriter.Info($"Skipping Workflow, due to branches-ignore filter. github.ref='{Ref2}'");
                                return skipWorkflow();
                            }
                            if(branches != null && skip(CompilePatterns(branches), new[] { branch }, workflowTraceWriter)) {
                                workflowTraceWriter.Info($"Skipping Workflow, due to branches filter. github.ref='{Ref2}'");
                                return skipWorkflow();
                            }
                            if((tags != null || tagsIgnore != null) && branches == null && branchesIgnore == null) {
                                workflowTraceWriter.Info($"Skipping Workflow, due to existense of tag filter. github.ref='{Ref2}'");
                                return skipWorkflow();
                            }
                        } else if(Ref2.StartsWith(rtags) == true) {
                            var tag = Ref2.Substring(rtags.Length);

                            if(tagsIgnore != null && filter(CompilePatterns(tagsIgnore), new[] { tag }, workflowTraceWriter)) {
                                workflowTraceWriter.Info($"Skipping Workflow, due to tags-ignore filter. github.ref='{Ref2}'");
                                return skipWorkflow();
                            }
                            if(tags != null && skip(CompilePatterns(tags), new[] { tag }, workflowTraceWriter)) {
                                workflowTraceWriter.Info($"Skipping Workflow, due to tags filter. github.ref='{Ref2}'");
                                return skipWorkflow();
                            }
                            if((branches != null || branchesIgnore != null) && tags == null && tagsIgnore == null) {
                                workflowTraceWriter.Info($"Skipping Workflow, due to existense of branch filter. github.ref='{Ref2}'");
                                return skipWorkflow();
                            }
                        }
                    }
                    if(hook.Commits != null) {
                        var changedFiles = hook.Commits.SelectMany(commit => (commit.Added ?? new List<string>()).Concat(commit.Removed ?? new List<string>()).Concat(commit.Modified ?? new List<string>()));
                        if(pathsIgnore != null && filter(CompilePatterns(pathsIgnore), changedFiles, workflowTraceWriter)) {
                            workflowTraceWriter.Info($"Skipping Workflow, due to paths-ignore filter.");
                            return skipWorkflow();
                        }
                        if(paths != null && skip(CompilePatterns(paths), changedFiles, workflowTraceWriter)) {
                            workflowTraceWriter.Info($"Skipping Workflow, due to paths filter.");
                            return skipWorkflow();
                        }
                    }
                    var fworkflowName = hook.Workflow?.Name;
                    if(fworkflowName != null && fworkflows != null && skip(CompilePatterns(fworkflows), new[] { fworkflowName }, workflowTraceWriter)) {
                        workflowTraceWriter.Info($"Skipping Workflow, due to workflows filter.");
                        return skipWorkflow();
                    }
                }
                Func<GitHub.DistributedTask.ObjectTemplating.ITraceWriter, TemplateToken, string> evalRunName = (traceWriter, runName) => {
                    if(runName == null) {
                        return null;
                    }
                    var contextData = createContext(null);
                    var templateContext = CreateTemplateContext(traceWriter, workflowContext, contextData);
                    var workflowEnv = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, templateContext.Schema.Definitions.ContainsKey("workflow-run-name") ? "workflow-run-name" : "run-name", runName, 0, null, true);
                    templateContext.Errors.Check();
                    var ret = workflowEnv?.AssertString("run-name")?.Value;
                    return string.IsNullOrWhiteSpace(ret) ? null : ret;
                };
                workflowContext.WorkflowRunName = callingJob?.WorkflowRunName ?? evalRunName(workflowTraceWriter, (from r in actionMapping where r.Key.AssertString("workflow root mapping key").Value == "run-name" select r).FirstOrDefault().Value) ?? evalRunName(workflowContext.HasFeature("system.runner.server.debugdefrunname") ? workflowTraceWriter : new EmptyTraceWriter(), new BasicExpressionToken(null, null, null, "github.event_name == 'push' && github.event.head_commit.message || startswith(github.event_name, 'pull_request') && github.event.pull_request.title || ''")) ?? workflowname;
                if(callingJob == null) {
                    workflowTraceWriter.Info($"Updated Workflow Name: {workflowContext.WorkflowRunName}");
                    if(attempt.WorkflowRun != null && !string.IsNullOrEmpty(workflowContext.WorkflowRunName)) {
                        attempt.WorkflowRun.DisplayName = workflowContext.WorkflowRunName;
                        UpdateWorkflowRun(attempt, repository_name);
                        _context.SaveChanges();
                    }
                }

                Action<DictionaryContextData, string, JobItem, JobCompletedEvent> updateNeedsCtx = (needsctx, name, job, e) => {
                    IDictionary<string, VariableValue> dependentOutputs = e.Outputs != null ? new Dictionary<string, VariableValue>(e.Outputs, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                    DictionaryContextData jobctx = new DictionaryContextData();
                    needsctx[name] = jobctx;
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
                    jobctx.Add("result", new StringContextData(result.ToString().ToLowerInvariant()));
                };
                FinishJobController.JobCompleted workflowcomplete = null;
                TemplateToken workflowPermissions = null;
                TemplateToken workflowConcurrency = null;
                var jobnamebuilder = new ReferenceNameBuilder();
                CancellationTokenSource finished = null;
                foreach (var actionPair in actionMapping)
                {
                    var propertyName = actionPair.Key.AssertString($"workflow root mapping key");

                    switch (propertyName.Value)
                    {
                        case "jobs":
                        TemplateToken evaluatedJobs = null;
                        {
                            var contextData = createContext(null);
                            var templateContext = CreateTemplateContext(workflowTraceWriter, workflowContext, contextData);
                            evaluatedJobs = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "jobs", actionPair.Value, 0, null, true);
                            templateContext.Errors.Check();
                        }
                        var jobs = evaluatedJobs.AssertMapping("jobs");
                        List<string> errors = new List<string>();
                        foreach (var job in jobs) {
                            var jn = job.Key.AssertString($"jobs mapping key");
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
                            var jn = job.Key.AssertString($"jobs mapping key");
                            var jobname = jn.Value;
                            var run = job.Value.AssertMapping("jobs");
                            var jobitem = new JobItem() { name = jobname, Id = Guid.NewGuid() };
                            dependentjobgroup.Add(jobitem);

                            var skipDependencies = workflowContext.HasFeature("system.runner.server.skipDependencies");
                            var needs = (from r in run where r.Key.AssertString($"jobs.{jobname} mapping key").Value == "needs" select r).FirstOrDefault().Value?.AssertScalarOrSequence("jobs.{jobname}.needs");
                            List<string> neededJobs = new List<string>();
                            if (needs != null) {
                                neededJobs.AddRange(from need in needs select need.AssertString($"jobs.{jobname}.needs.*").Value);
                            }
                            jobitem.Needs = neededJobs.ToHashSet(StringComparer.OrdinalIgnoreCase);
                            var contextData = createContext(jobitem);
                            
                            var needsctx = new DictionaryContextData();

                            if(skipDependencies) {
                                neededJobs.Clear();
                                Func<string, DictionaryContextData> parseDeps = name => {
                                    if(workflowContext.FeatureToggles.TryGetValue(name, out var value)) {
                                        DictionaryContextData ret;
                                        var templateContext = CreateTemplateContext(workflowTraceWriter, workflowContext, contextData);
                                        using (var stringReader = new StringReader(value)) {
                                            var yamlObjectReader = new YamlObjectReader(null, stringReader);
                                            ret = TemplateReader.Read(templateContext, "any", yamlObjectReader, null, out _).ToContextData().AssertDictionary(name);
                                        }
                                        templateContext.Errors.Check();
                                        return ret;
                                    }
                                    return new DictionaryContextData();
                                };
                                needsctx = parseDeps("system.runner.server.needs");
                                foreach(var childneed in parseDeps($"system.runner.server.{jobname.PrefixJobIdIfNotNull(callingJob?.Id)}.needs")) {
                                    needsctx[childneed.Key] = childneed.Value;
                                }
                            }
                            contextData.Add("needs", needsctx);

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
                                            var job = (from j in jobgroup where string.Equals(j.name, name, StringComparison.OrdinalIgnoreCase) && j.Id == e.JobId select j).FirstOrDefault();
                                            if(job != null) {
                                                updateNeedsCtx(needsctx, name, job, e);
                                                return true;
                                            }
                                            return false;
                                        }) == 0 || neededJobs.Count > 0) {
                                            return;
                                        }
                                    }

                                    if(skipDependencies) {
                                        foreach(var kv in jobitem.Dependencies) {
                                            var status = needsctx.TryGetValue(kv.Key, out var jobdata) && jobdata is DictionaryContextData jobdatadict && jobdatadict.TryGetValue("result", out var jobresult) && jobresult is StringContextData jobresultstr ? Enum.Parse<NeedsTaskResult>(jobresultstr.Value, true) : NeedsTaskResult.Success;
                                            switch(status) {
                                            case NeedsTaskResult.Success:
                                                jobitem.Dependencies[kv.Key].Status = TaskResult.Succeeded;
                                            break;
                                            case NeedsTaskResult.Skipped:
                                                jobitem.Dependencies[kv.Key].Status = TaskResult.Skipped;
                                            break;
                                            case NeedsTaskResult.Failure:
                                                jobitem.Dependencies[kv.Key].Status = TaskResult.Failed;
                                            break;
                                            case NeedsTaskResult.Cancelled:
                                                jobitem.Dependencies[kv.Key].Status = TaskResult.Canceled;
                                            break;
                                            }
                                        }
                                    }

                                    dependentjobgroup.Remove(jobitem);
                                    if(!dependentjobgroup.Any()) {
                                        jobgroup.Clear();
                                    } else {
                                        jobgroup.Add(jobitem);
                                    }

                                    var jid = jobitem.Id;
                                    jobitem.TimelineId = Guid.NewGuid();
                                    var jobrecord = new TimelineRecord{ Id = jobitem.Id, Name = jobitem.name, RefName = jobitem.name, RecordType = "job" };
                                    var jobtlUpdate = _webConsoleLogService.CreateNewRecord(jobitem.TimelineId, jobrecord);
                                    var jobTraceWriter = new TraceWriter2(line => _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(jobitem.Id, new List<string>{ line }), jobitem.TimelineId, jobitem.Id));
                                    var jobNameToken = (from r in run where r.Key.AssertString($"jobs.{jobname} mapping key").Value == "name" select r.Value).FirstOrDefault();
                                    var _jobdisplayname = (jobNameToken?.ToString() ?? jobitem.name)?.PrefixJobNameIfNotNull(callingJob?.Name);
                                    jobrecord.Name = _jobdisplayname;
                                    jobitem.DisplayName = _jobdisplayname;
                                    // For Runner.Client to show the workflowname
                                    initializingJobs.TryAdd(jobitem.Id, new Job() { JobId = jobitem.Id, TimeLineId = jobitem.TimelineId, name = jobitem.DisplayName, workflowname = workflowname, runid = runid, RequestId = jobitem.RequestId } );
                                    UpdateTimeLine(jobtlUpdate);
                                    jobTraceWriter.Info("{0}", $"Evaluate if");
                                    var ifexpr = (from r in run where r.Key.AssertString($"jobs.{jobname} mapping key").Value == "if" select r).FirstOrDefault().Value;
                                    var translateConditionCtx = CreateTemplateContext(jobTraceWriter, workflowContext);
                                    var convertedIfExpr = PipelineTemplateConverter.ConvertToIfCondition(translateConditionCtx, ifexpr, true);
                                    var condition = new BasicExpressionToken(null, null, null, convertedIfExpr);
                                    translateConditionCtx.Errors.Check();
                                    var recursiveNeedsctx = needsctx;
                                    if(!NoRecursiveNeedsCtx) {
                                        needsctx = new DictionaryContextData();
                                        contextData["needs"] = needsctx;
                                        foreach(var need in jobitem.Needs) {
                                            if(recursiveNeedsctx.TryGetValue(need, out var val)) {
                                                needsctx[need] = val;
                                            }
                                        }
                                    }
                                    bool matrixIf = false;
                                    {
                                        var templateContext = CreateTemplateContext(jobTraceWriter, workflowContext, contextData, new ExecutionContext() { Cancelled = workflowContext.CancellationToken, JobContext = jobitem });
                                        matrixIf = !condition.CheckHasRequiredContext(contextData, templateContext.ExpressionFunctions);
                                    }
                                    jobitem.EvaluateIf = (traceWriter) => {
                                        if(matrixIf) {
                                            return true;
                                        }
                                        var templateContext = CreateTemplateContext(traceWriter, workflowContext, contextData, new ExecutionContext() { Cancelled = workflowContext.CancellationToken, JobContext = jobitem });
                                        // It seems that the offical actions service does provide a recusive needs ctx, but only for if expressions.
                                        templateContext.ExpressionValues["needs"] = recursiveNeedsctx;
                                        templateContext.ExpressionValues["matrix"] = null;
                                        templateContext.ExpressionValues["strategy"] = null;
                                        var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.JobIfResult, condition, 0, null, true);
                                        templateContext.Errors.Check();
                                        return PipelineTemplateConverter.ConvertToIfResult(templateContext, eval);
                                    };
                                    Action<TaskResult> sendFinishJob = result => {
                                        var _job = new Job() { message = null, repo = repository_name, WorkflowRunAttempt = attempt, WorkflowIdentifier = jobitem.name.PrefixJobIdIfNotNull(callingJob?.Id), name = _jobdisplayname, workflowname = workflowname, runid = runid, JobId = jid, RequestId = jobitem.RequestId, TimeLineId = jobitem.TimelineId};
                                        AddJob(_job);
                                        InvokeJobCompleted(new JobCompletedEvent() { JobId = jobitem.Id, Result = result, RequestId = jobitem.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                    };
                                    try {
                                        var _res = (!workflowContext.ForceCancellationToken?.IsCancellationRequested ?? true) && jobitem.EvaluateIf(jobTraceWriter);
                                        if(!_res) {
                                            sendFinishJob(TaskResult.Skipped);
                                            return;
                                        }
                                        Func<TemplateToken, MappingToken> evaluateStrategy = rawstrategy => {
                                            if(rawstrategy == null) {
                                                return null;
                                            }
                                            jobTraceWriter.Info("{0}", "Evaluate strategy");
                                            var templateContext = CreateTemplateContext(jobTraceWriter, workflowContext, contextData);
                                            var strategy = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.Strategy, rawstrategy, 0, null, true)?.AssertMapping($"jobs.{jobname}.strategy");
                                            templateContext.Errors.Check();
                                            return strategy;
                                        };
                                        // Allow including and excluding via list properties https://github.com/orgs/community/discussions/7835
                                        // https://github.com/actions/runner/issues/857
                                        // Matrix has partial subobject matching reported here https://github.com/rhysd/actionlint/issues/249
                                        // It also reveals that sequences are matched partially, if the left seqence starts with the right sequence they are matched
                                        var matrixexcludeincludelists = workflowContext.HasFeature("system.runner.server.matrixexcludeincludelists");

                                        var rawstrategy = (from r in run where r.Key.AssertString($"jobs.{jobname} mapping key").Value == "strategy" select r).FirstOrDefault().Value;
                                        var result = StrategyUtils.ExpandStrategy(evaluateStrategy(rawstrategy), matrixexcludeincludelists, jobTraceWriter, jobname);
                                        if(result.Result != null) {
                                            sendFinishJob(result.Result.Value);
                                            return;
                                        }

                                        var flatmatrix = result.FlatMatrix;
                                        var includematrix = result.IncludeMatrix;
                                        bool failFast = result.FailFast;
                                        double? max_parallel = result.MaxParallel;
                                        var keys = result.MatrixKeys;

                                        // Filter matrix from cli
                                        if(TryParseJobSelector(selectedJob, out var cjob, out var cmatrix, out var cselector) && string.Equals(jobname, cjob, StringComparison.OrdinalIgnoreCase) && (cmatrix != null || cselector == null && _matrix?.Length > 0)) {
                                            var mdict = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                            if(cmatrix != null) {
                                                var templateContext = CreateTemplateContext(jobTraceWriter, workflowContext, contextData);
                                                TemplateToken cm;
                                                using (var stringReader = new StringReader(cmatrix)) {
                                                    var yamlObjectReader = new YamlObjectReader(null, stringReader);
                                                    cm = TemplateReader.Read(templateContext, "any", yamlObjectReader, null, out _);
                                                }
                                                templateContext.Errors.Check();
                                                foreach(var m_ in cm.AssertMapping("selectedJob matrix")) {
                                                    mdict[m_.Key.ToString()] = m_.Value;
                                                }
                                            } else {
                                                foreach(var m_ in _matrix) {
                                                    var i = m_.IndexOf(":");
                                                    var templateContext = CreateTemplateContext(jobTraceWriter, workflowContext, contextData);
                                                    using (var stringReader = new StringReader(m_.Substring(i + 1))) {
                                                        var yamlObjectReader = new YamlObjectReader(null, stringReader);
                                                        mdict[m_.Substring(0, i)] = TemplateReader.Read(templateContext, "any", yamlObjectReader, null, out _);
                                                    }
                                                    templateContext.Errors.Check();
                                                }
                                            }
                                            Predicate<Dictionary<string, TemplateToken>> match = dict => {
                                                foreach(var kv in mdict) {
                                                    TemplateToken val;
                                                    if(!dict.TryGetValue(kv.Key, out val) || !kv.Value.DeepEquals(val, true)) {
                                                        return true;
                                                    }
                                                }
                                                return false;
                                            };
                                            flatmatrix.RemoveAll(match);
                                            includematrix.RemoveAll(match);
                                            if(flatmatrix.Count + includematrix.Count == 0) {
                                                jobTraceWriter.Info("{0}", $"Your specified matrix filter doesn't match any matrix entries");
                                                sendFinishJob(TaskResult.Skipped);
                                                return;
                                            }
                                        }
                                        var jobTotal = flatmatrix.Count + includematrix.Count;
                                        if(flatmatrix.Count == 1 && keys.Count == 0 && jobTotal > 1) {
                                            jobTotal--;
                                        }
                                        // Enforce job matrix limit of github
                                        if(jobTotal > 256) {
                                            jobTraceWriter.Info("{0}", $"Failure: Matrix contains more than 256 entries after exclude");
                                            sendFinishJob(TaskResult.Failed);
                                            return;
                                        }
                                        bool? canBeCancelled = null;
                                        Func<bool> cancelRequest = () => {
                                            if(workflowContext.ForceCancellationToken?.IsCancellationRequested == true || jobitem.Cancel.IsCancellationRequested) {
                                                jobTraceWriter.Info("{0}", $"Cancellation: workflowContext.ForceCancellationToken?.IsCancellationRequested == true || jobitem.Cancel.IsCancellationRequested");
                                                return true;
                                            }
                                            if(!workflowContext.CancellationToken.IsCancellationRequested) {
                                                return false;
                                            }
                                            jobTraceWriter.Info("{0}", $"Cancellation: workflowContext.CancellationToken.IsCancellationRequested");
                                            if(canBeCancelled != null) {
                                                return canBeCancelled.Value;
                                            }
                                            bool ret = !jobitem.EvaluateIf(jobTraceWriter);
                                            canBeCancelled = ret;
                                            return ret;
                                        };
                                        if(cancelRequest()) {
                                            sendFinishJob(TaskResult.Canceled);
                                            return;
                                        }
                                        var strategyctx = new DictionaryContextData();
                                        contextData["strategy"] = strategyctx;
                                        strategyctx["fail-fast"] = new BooleanContextData(failFast);
                                        // The official actions-service only sets it to > 1 if the matrix isn't empty
                                        // The matrix is empty if you omit matrix in strategy or exclude all entries of the matrix without including new ones
                                        strategyctx["max-parallel"] = new NumberContextData(keys.Count == 0 ? 1 : max_parallel.HasValue ? max_parallel.Value : jobTotal);
                                        strategyctx["job-total"] = new NumberContextData(jobTotal);
                                        if(jobTotal > 1) {
                                            jobitem.Childs = new List<JobItem>();
                                            jobitem.NoStatusCheck = true;
                                            var _job = new Job() { message = null, repo = repository_name, WorkflowRunAttempt = attempt, WorkflowIdentifier = jobitem.name.PrefixJobIdIfNotNull(callingJob?.Id), name = jobitem.DisplayName, workflowname = workflowname, runid = runid, JobId = jid, RequestId = jobitem.RequestId, TimeLineId = jobitem.TimelineId};
                                            var clone = Clone();
                                            Task.Run(async () => {
                                                try {
                                                    await Helper.WaitAnyCancellationToken(finished.Token, _job.CancelRequest.Token, jobitem.Cancel.Token);
                                                    if(!finished.IsCancellationRequested) {
                                                        jobitem.Cancel.Cancel();
                                                        foreach(var ji in jobitem.Childs) {
                                                            ji.Cancel.Cancel();
                                                            Job job = _cache.Get<Job>(ji.Id);
                                                            if(job != null) {
                                                                // cancel normal job
                                                                job.CancelRequest.Cancel();
                                                                if(job.SessionId == Guid.Empty) {
                                                                    clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                                }
                                                            }
                                                        }
                                                    }
                                                } finally {
                                                    clone._context.Dispose();
                                                }
                                            });
                                            AddJob(_job);
                                            // Fix workflow doesn't wait for cancelled matrix jobs to finish, add dummy sessionid
                                            _job.SessionId = Guid.NewGuid();
                                        }
                                        {
                                            int i = 0;
                                            var usesJob = (from r in run where r.Key.AssertString($"jobs.{jobname} mapping key").Value == "uses" select r).FirstOrDefault().Value != null;
                                            if(usesJob) {
                                                if((callingJob?.Depth ?? 0) >= MaxWorkflowDepth && MaxWorkflowDepth >= 0) {
                                                    throw new Exception($"Running jobs.{jobname} exceeds max allowed workflow depth {MaxWorkflowDepth}");
                                                }
                                                if(MaxDifferentReferencedWorkflows >= 0) {
                                                    workflowContext.ReferencedWorkflows.Add((from r in run where r.Key.AssertString($"jobs.{jobname} mapping key").Value == "uses" select r).First().Value.AssertString($"jobs.{jobname}.uses").Value);
                                                    if(workflowContext.ReferencedWorkflows.Count > MaxDifferentReferencedWorkflows) {
                                                        throw new Exception($"Running jobs.{jobname} exceeds max allowed different reusable workflows {MaxDifferentReferencedWorkflows}");
                                                    }
                                                }
                                            }
                                            Func<string, Dictionary<string, TemplateToken>, Func<bool, Job>> act = (displaySuffix, item) => {
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
                                                if(finishedJobs != null) {
                                                    if(usesJob) {
                                                        if(!finishedJobs.ContainsKey(jobname)) {
                                                            // Allow rerunning one reusable workflow
                                                            // If the rerun is requested discard all child results
                                                            Array.ForEach(finishedJobs.ToArray(), fjobs => {
                                                                if(fjobs.Key.StartsWith(jobname + "/", StringComparison.OrdinalIgnoreCase)) {
                                                                    finishedJobs.Remove(fjobs.Key);
                                                                }
                                                            });
                                                        }
                                                    } else {
                                                        if(finishedJobs.TryGetValue(jobname, out var fjobs)) {
                                                            foreach(var fjob in fjobs) {
                                                                if((matrixContext?.ToTemplateToken() ?? new NullToken(null, null, null)).DeepEquals(fjob.MatrixContextData[callingJob?.Depth ?? 0].ToTemplateToken() ?? new NullToken(null, null, null))) {
                                                                    var _next = jobTotal > 1 ? new JobItem() { name = jobitem.name, Id = fjob.JobId, NoFailFast = true } : jobitem;
                                                                    _next.TimelineId = fjob.TimeLineId;
                                                                    _next.NoStatusCheck = true;
                                                                    jobitem.Childs?.Add(_next);
                                                                    return b => {
                                                                        var jevent = new JobCompletedEvent(_next.RequestId, _next.Id, fjob.Result.Value, fjob.Outputs.ToDictionary(o => o.Name, o => new VariableValue(o.Value, false), StringComparer.OrdinalIgnoreCase));
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
                                                                if(djob.Dependencies != null && (string.Equals(fjobs.Key, djob.name, StringComparison.OrdinalIgnoreCase) || fjobs.Key.StartsWith(djob.name + "/", StringComparison.OrdinalIgnoreCase))) {
                                                                    foreach(var dep in djob.Dependencies) {
                                                                        if(string.Equals(dep.Key, jobname, StringComparison.OrdinalIgnoreCase)) {
                                                                            finishedJobs.Remove(fjobs.Key);
                                                                            return;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        });
                                                    }
                                                }
                                                var next = jobTotal > 1 ? new JobItem() { name = jobitem.name, Id = Guid.NewGuid() } : jobitem;
                                                Func<string, string> defJobName = jobname => string.IsNullOrEmpty(displaySuffix) ? jobname : $"{jobname} {displaySuffix}";
                                                var _prejobdisplayname = defJobName(_jobdisplayname);
                                                if(jobTotal > 1) {
                                                    next.TimelineId = Guid.NewGuid();
                                                    // For Runner.Client to show the workflowname
                                                    initializingJobs.TryAdd(next.Id, new Job() { JobId = next.Id, TimeLineId = next.TimelineId, name = _prejobdisplayname, workflowname = workflowname, runid = runid, RequestId = next.RequestId } );
                                                    UpdateTimeLine(_webConsoleLogService.CreateNewRecord(next.TimelineId, new TimelineRecord{ Id = next.Id, Name = _prejobdisplayname }));
                                                }
                                                Func<Func<bool, Job>> failJob = () => {
                                                    var _job = new Job() { JobId = next.Id, TimeLineId = next.TimelineId, name = _prejobdisplayname, workflowname = workflowname, repo = repository_name, WorkflowRunAttempt = attempt, runid = runid, RequestId = next.RequestId };
                                                    AddJob(_job);
                                                    InvokeJobCompleted(new JobCompletedEvent() { JobId = next.Id, Result = TaskResult.Failed, RequestId = next.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                    return cancel => _job;
                                                };
                                                try {
                                                    _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ $"Prepare Job for execution" }), next.TimelineId, next.Id);
                                                    var matrixJobTraceWriter = new TraceWriter2(line => {
                                                        _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ line }), next.TimelineId, next.Id);
                                                    });
                                                    jobitem.Childs?.Add(next);
                                                    if(matrixIf) {
                                                        next.EvaluateIf = (traceWriter) => {
                                                            var templateContext = CreateTemplateContext(traceWriter, workflowContext, contextData, new ExecutionContext() { Cancelled = workflowContext.CancellationToken, JobContext = jobitem });
                                                            // It seems that the offical actions service does provide a recusive needs ctx, but only for if expressions.
                                                            templateContext.ExpressionValues["needs"] = recursiveNeedsctx;
                                                            var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.JobIfResult, condition, 0, null, true);
                                                            templateContext.Errors.Check();
                                                            return PipelineTemplateConverter.ConvertToIfResult(templateContext, eval);
                                                        };
                                                        if(!next.EvaluateIf(matrixJobTraceWriter)) {
                                                            var _job = new Job() { JobId = next.Id, TimeLineId = next.TimelineId, name = _prejobdisplayname, workflowname = workflowname, repo = repository_name, WorkflowRunAttempt = attempt, runid = runid, RequestId = next.RequestId };
                                                            AddJob(_job);
                                                            InvokeJobCompleted(new JobCompletedEvent() { JobId = next.Id, Result = TaskResult.Skipped, RequestId = next.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                            return cancel => _job;
                                                        }
                                                    }
                                                    next.NoStatusCheck = usesJob;
                                                    _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ $"Evaluate job name" }), next.TimelineId, next.Id);
                                                    var templateContext = CreateTemplateContext(matrixJobTraceWriter, workflowContext, contextData);
                                                    var _jobdisplayname = _prejobdisplayname;
                                                    if(jobNameToken != null && !(jobNameToken is StringToken)) {
                                                        _jobdisplayname = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "string-strategy-context", jobNameToken, 0, null, true)?.AssertString($"jobs.{jobname}.name must be a string").Value?.PrefixJobNameIfNotNull(callingJob?.Name);
                                                        templateContext.Errors.Check();
                                                    }
                                                    next.DisplayName = _jobdisplayname;
                                                    next.ActionStatusQueue.Post(() => updateJobStatus(next, null));
                                                    return queueJob(matrixJobTraceWriter, workflowDefaults, workflowEnvironment, _jobdisplayname, run, contextData.Clone() as DictionaryContextData, next.Id, next.TimelineId, repository_name, jobname, workflowname, runid, runnumber, secrets, platform ?? new string[] { }, localcheckout, next.RequestId, Ref, Sha, callingJob?.Event ?? event_name, callingJob?.Event, workflows, statusSha, callingJob?.Id, finishedJobs, attempt, next, workflowPermissions, callingJob, dependentjobgroup, selectedJob, _matrix, workflowContext, secretsProvider);
                                                } catch(Exception ex) {
                                                    _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ $"Exception: {ex?.ToString()}" }), next.TimelineId, next.Id);
                                                    return failJob();
                                                }
                                            };
                                            ConcurrentQueue<Func<bool, Job>> jobs = new ConcurrentQueue<Func<bool, Job>>();
                                            List<Job> scheduled = new List<Job>();
                                            FinishJobController.JobCompleted handler2 = null;
                                            Action cleanupOnFinish = () => {
                                                if (scheduled.Count == 0) {
                                                    localJobCompletedEvents.JobCompleted -= handler2;
                                                    if(jobTotal > 1) {
                                                        InvokeJobCompleted(jobitem.JobCompletedEvent ?? new JobCompletedEvent() { JobId = jobitem.Id, Result = TaskResult.Canceled, RequestId = jobitem.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                    }
                                                }
                                            };
                                            Action<string> cancelAll = message => {
                                                foreach (var _j in scheduled) {
                                                    if(!string.IsNullOrEmpty(message)) {
                                                        _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(_j.JobId, new List<string>{ message }), _j.TimeLineId, _j.JobId);
                                                    }
                                                    _j.CancelRequest?.Cancel();
                                                    if(_j.SessionId == Guid.Empty) {
                                                        InvokeJobCompleted(new JobCompletedEvent() { JobId = _j.JobId, Result = TaskResult.Canceled, RequestId = _j.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                    }
                                                }
                                                scheduled.Clear();
                                                while(jobs.TryDequeue(out var cb)) {
                                                    cb(true);
                                                }
                                                cleanupOnFinish();
                                            };
                                            var cancelreqmsg = "Cancelled via cancel request";
                                            handler2 = e => {
                                                if(scheduled.RemoveAll(j => j.JobId == e.JobId) > 0) {
                                                    var currentItem = jobitem.Childs?.Find(ji => ji.Id == e.JobId) ?? (jobitem.Id == e.JobId ? jobitem : null);
                                                    var conclusion = (currentItem == null || currentItem.ContinueOnError != true) ? e.Result : TaskResult.Succeeded;
                                                    if(jobitem.JobCompletedEvent == null) {
                                                        jobitem.JobCompletedEvent = new JobCompletedEvent() { JobId = jobitem.Id, Result = conclusion, RequestId = jobitem.RequestId, Outputs = e.Outputs != null ? new Dictionary<string, VariableValue>(e.Outputs, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) };
                                                    } else {
                                                        if(jobitem.JobCompletedEvent.Result != conclusion) {
                                                            if(jobitem.JobCompletedEvent.Result == TaskResult.Canceled || jobitem.JobCompletedEvent.Result == TaskResult.Abandoned || conclusion == TaskResult.Failed || conclusion == TaskResult.Canceled || conclusion == TaskResult.Abandoned) {
                                                                jobitem.JobCompletedEvent.Result = TaskResult.Failed;
                                                            } else if((jobitem.JobCompletedEvent.Result == TaskResult.SucceededWithIssues || jobitem.JobCompletedEvent.Result == TaskResult.Skipped) && (conclusion == TaskResult.Succeeded || conclusion == TaskResult.SucceededWithIssues || conclusion == TaskResult.Skipped)) {
                                                                jobitem.JobCompletedEvent.Result = TaskResult.Succeeded;
                                                            }
                                                        }
                                                        if(e.Outputs != null) {
                                                            foreach(var output in e.Outputs) {
                                                                if(!string.IsNullOrEmpty(output.Value.Value)) {
                                                                    jobitem.JobCompletedEvent.Outputs[output.Key] = output.Value;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    if(failFast && (conclusion == TaskResult.Failed || conclusion == TaskResult.Canceled || conclusion == TaskResult.Abandoned) && (currentItem == null || currentItem.NoFailFast != true)) {
                                                        cancelAll("Cancelled via strategy.fail-fast == true");
                                                    } else {
                                                        while((!max_parallel.HasValue || scheduled.Count < max_parallel.Value) && jobs.TryDequeue(out var cb)) {
                                                            if(cancelRequest()) {
                                                                cb(true);
                                                                cancelAll(cancelreqmsg);
                                                                return;
                                                            }
                                                            var jret = cb(false);
                                                            if(jret != null) {
                                                                scheduled.Add(jret);
                                                            }
                                                        }
                                                        cleanupOnFinish();
                                                    }
                                                }
                                            };
                                            localJobCompletedEvents.JobCompleted += handler2;
                                            if(keys.Count != 0 || includematrix.Count == 0) {
                                                foreach (var item in flatmatrix) {
                                                    if(cancelRequest()) {
                                                        cancelAll(cancelreqmsg);
                                                        return;
                                                    }
                                                    var j = act(StrategyUtils.GetDefaultDisplaySuffix(from displayitem in keys.SelectMany(key => item[key].Traverse(true)) where !(displayitem is SequenceToken || displayitem is MappingToken) select displayitem.ToString()), item);
                                                    if(j != null) {
                                                        jobs.Enqueue(j);
                                                    }
                                                }
                                            }
                                            foreach (var item in includematrix) {
                                                if(cancelRequest()) {
                                                    cancelAll(cancelreqmsg);
                                                    return;
                                                }
                                                var j = act(StrategyUtils.GetDefaultDisplaySuffix(from displayitem in item.SelectMany(it => it.Value.Traverse(true)) where !(displayitem is SequenceToken || displayitem is MappingToken) select displayitem.ToString()), item);
                                                if(j != null) {
                                                    jobs.Enqueue(j);
                                                }
                                            }
                                            for (int j = 0; j < (max_parallel.HasValue ? (int)max_parallel.Value : jobTotal) && jobs.TryDequeue(out var cb2); j++) {
                                                if(cancelRequest()) {
                                                    cb2(true);
                                                    cancelAll(cancelreqmsg);
                                                    return;
                                                }
                                                var jret = cb2(false);
                                                if(jret != null) {
                                                    scheduled.Add(jret);
                                                }
                                            }
                                            cleanupOnFinish();
                                        }
                                    } catch(Exception ex) {
                                        jobTraceWriter.Info("{0}", $"Internal Error: {ex.Message}, {ex.StackTrace}");
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
                                                InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Failed, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                            }
                                        }
                                        return true;
                                    }) > 0)) {
                                        InvokeJobCompleted(new JobCompletedEvent() { JobId = jobitem.Id, Result = TaskResult.Failed, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                    }
                                }
                            };
                            jobitem.OnJobEvaluatable = handler;
                        }
                        break;
                        case "defaults":
                        workflowDefaults = actionPair.Value;
                        break;
                        case "permissions":
                        workflowPermissions = actionPair.Value.AssertPermissionsValues("permissions");
                        break;
                        case "concurrency":
                        {
                            workflowTraceWriter.Info("{0}", $"Evaluate workflow concurrency");
                            var contextData = createContext(null);
                            var templateContext = CreateTemplateContext(workflowTraceWriter, workflowContext, contextData);
                            workflowConcurrency = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "workflow-concurrency", actionPair.Value, 0, null, true);
                            templateContext.Errors.Check();
                        }
                        break;
                    }
                }
                if(!dependentjobgroup.Any()) {
                    throw new Exception("Your workflow is invalid, you have to define at least one job");
                }
                dependentjobgroup.ForEach(ji => {
                    if(ji.Needs?.Any() == true) {
                        Func<JobItem, ISet<string>, Dictionary<string, JobItem>> pred = null;
                        pred = (cur, cyclic) => {
                            var ret = new Dictionary<string, JobItem>(StringComparer.OrdinalIgnoreCase);
                            if(cur.Needs?.Any() == true) {
                                // To preserve case of direct dependencies as written in yaml
                                foreach(var need in cur.Needs) {
                                    ret[need] = null;
                                }
                                var pcyclic = cyclic.Append(cur.name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                                ISet<string> missingDeps = cur.Needs.ToHashSet(StringComparer.OrdinalIgnoreCase);
                                dependentjobgroup.ForEach(d => {
                                    if(cur.Needs.Contains(d.name)) {
                                        if(pcyclic.Contains(d.name)) {
                                            throw new Exception($"{cur.name}: Cyclic dependency to {d.name} detected");
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
                                                    throw new Exception($"{cur.name}: Cyclic dependency to {k.Key} detected");
                                                }
                                                ret[k.Key] = k.Value;
                                            }
                                        }
                                        missingDeps.Remove(d.name);
                                    }
                                });
                                if(missingDeps.Any()) {
                                    throw new Exception($"{cur.name}: One or more missing dependencies detected: {string.Join(", ", missingDeps)}");
                                }
                            }
                            return ret;
                        };
                        if(ji.Dependencies == null)
                            ji.Dependencies = pred(ji, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    }
                });
                if(selectedJob != null) {
                    List<JobItem> next = new List<JobItem>();
                    dependentjobgroup.RemoveAll(j => {
                        if(TryParseJobSelector(selectedJob, out var cjob, out _, out _) && string.Equals(j.name, cjob, StringComparison.OrdinalIgnoreCase)) {
                            next.Add(j);
                            return true;
                        }
                        return false;
                    });
                    if(!workflowContext.HasFeature("system.runner.server.skipDependencies")) {
                        while(true) {
                            int oldCount = next.Count;
                            dependentjobgroup.RemoveAll(j => {
                                foreach(var j2 in next.ToArray()) {
                                    foreach(var need in j2.Needs) {
                                        if(string.Equals(j.name, need, StringComparison.OrdinalIgnoreCase)) {
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
                    }
                    dependentjobgroup = next;
                    if(dependentjobgroup.Count == 0) {
                        return skipWorkflow();
                    }
                }
                if(list) {
                    workflowTraceWriter.Info("{0}", $"Found {dependentjobgroup.Count} matching jobs for the requested event {e}");
                    foreach(var j in dependentjobgroup) {
                        if(j.Needs.Any()) {
                            workflowTraceWriter.Info("{0}", $"{j.name} depends on {string.Join(", ", j.Needs)}");
                        } else {
                            workflowTraceWriter.Info("{0}", $"{j.name}");
                        }
                    }
                    return skipWorkflow();
                } else {
                    var jobs = dependentjobgroup.ToArray();
                    finished = new CancellationTokenSource();
                    Action<WorkflowEventArgs> finishAsyncWorkflow = evargs => {
                        finished.Cancel();
                        finishWorkflow();
                        // Cleanup dummy jobs, which allows Runner.Client to display the workflowname
                        foreach(var job in jobs) {
                            if(job.Childs != null) {
                                foreach(var ji in job.Childs) {
                                    initializingJobs.Remove(ji.Id, out _);
                                }
                            }
                            initializingJobs.Remove(job.Id, out _);
                        }
                        if(callingJob != null) {
                            callingJob.Workflowfinish.Invoke(callingJob, evargs);
                        } else {
                            WorkflowStates.Remove(runid, out _);
                            workflowevent?.Invoke(evargs);
                            attempt.Status = Status.Completed;
                            attempt.Result = evargs.Success ? TaskResult.Succeeded : TaskResult.Failed;
                            UpdateWorkflowRun(attempt, repository_name);
                            _context.SaveChanges();
                        }
                        _context.Dispose();
                    };
                    FinishJobController.JobCompleted withoutlock = e => {
                        attempt.Status = Status.Running;
                        _context.SaveChanges();
                        UpdateWorkflowRun(attempt, repository_name);
                        var ja = e != null ? jobs.Where(j => e.JobId == j.Id || (j.Childs?.Where(ji => e.JobId == ji.Id).Any() ?? false)).FirstOrDefault() : null;
                        Action<JobItem> updateStatus = job => {
                            job.Status = e.Result;
                        };
                        if(ja != null) {
                            var ji = ja.Childs?.Where(ji => e.JobId == ji.Id).FirstOrDefault() ?? ja;
                            if(workflowOutputs != null && ja == ji) {
                                updateNeedsCtx(jobsctx, ji.name, ji, e);
                            }
                            ji.ActionStatusQueue.Post(() => {
                                return updateJobStatus(ji, e.Result);
                            });
                            ji.Status = e.Result;
                            ji.Completed = true;
                            updateStatus(ji);
                            workflowTraceWriter.Trace($"{ji.DisplayName} ({ji.name}) completed");
                            if(jobs.All(j => j.Completed)) {
                                workflowTraceWriter.Trace($"All jobs completed");
                                FinishJobController.OnJobCompletedAfter -= workflowcomplete;
                                var workflow = jobs.ToList();
                                var evargs = new WorkflowEventArgs { runid = runid, Success = workflow?.All(job => job.ContinueOnError || job.Status == TaskResult.Succeeded || job.Status == TaskResult.SucceededWithIssues || job.Status == TaskResult.Skipped) ?? false };
                                if(workflowOutputs != null) {
                                    try {
                                        workflowTraceWriter.Info("{0}", $"Evaluate on.workflow_call.outputs outputs");
                                        var contextData = createContext(null);
                                        contextData.Add("jobs", jobsctx);
                                        var templateContext = CreateTemplateContext(workflowTraceWriter, workflowContext, contextData);
                                        var outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                                        foreach(var entry in workflowOutputs) {
                                            var key = entry.Key.AssertString("on.workflow_call.outputs mapping key").Value;
                                            templateContext.TraceWriter.Info("{0}", $"Evaluate on.workflow_call.outputs.{key}.value");
                                            var value = entry.Value != null ? GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, templateContext.Schema.Definitions.ContainsKey("workflow_call-output-context") ? "workflow_call-output-context" : "workflow-output-context", (from kv in entry.Value.AssertMapping($"on.workflow_call.outputs.{key}") where kv.Key.AssertString($"on.workflow_call.outputs.{key} mapping key").Value == "value" select kv.Value).First(), 0, null, true)?.AssertString($"on.workflow_call.outputs.{key}.value").Value : null;
                                            templateContext.Errors.Check();
                                            outputs[key] = new VariableValue(value, false);
                                        }
                                        evargs.Outputs = outputs;
                                    } catch {
                                        evargs.Success = false;
                                    }
                                }
                                finishAsyncWorkflow(evargs);
                                return;
                            }
                        }
                        jobCompleted(e);
                    };
                    var channel = Channel.CreateUnbounded<JobCompletedEvent>();
                    Task.Run(async () => {
                        while(!finished.IsCancellationRequested) {
                            try {
                                var ev = await channel.Reader.ReadAsync(finished.Token);
                                withoutlock(ev);
                            } catch(Exception ex) {
                                workflowTraceWriter.Info("{0}", $"Exception: {ex.Message}, {ex.StackTrace}");
                            }
                        }
                    });
                    workflowcomplete = (e) => {
                        channel.Writer.WriteAsync(e);
                    };
                    CancellationTokenSource cancellationToken = null;
                    if(callingJob != null) {
                        workflowContext.ForceCancellationToken = callingJob.ForceCancellationToken;
                        cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(workflowContext.ForceCancellationToken ?? CancellationToken.None, callingJob.CancellationToken.Value);
                        workflowContext.CancellationToken = cancellationToken.Token;
                    } else {
                        var cforceCancelWorkflow = new CancellationTokenSource();
                        var ctoken = CancellationTokenSource.CreateLinkedTokenSource(cforceCancelWorkflow.Token);
                        if(WorkflowStates.TryAdd(runid, new WorkflowState {
                                Cancel = ctoken,
                                ForceCancel = cforceCancelWorkflow
                            })) {
                            workflowContext.CancellationToken = ctoken.Token;
                            cancellationToken = ctoken;
                            workflowContext.ForceCancellationToken = cforceCancelWorkflow.Token;
                        } else {
                            // Better don't allow to have multiple reruns active
                            throw new Exception("This workflow run is already running, multiple attempts are not supported at the same time");
                        }
                    }
                    asyncProcessing = true;
                    Action runWorkflow = () => {
                        var clone = Clone();
                        Task.Run(async () => {
                            try {
                                for(int i = 0; i < 2; i++) {
                                        if(i == 0) {
                                        await Helper.WaitAnyCancellationToken(workflowContext.CancellationToken, finished.Token, workflowContext.ForceCancellationToken ?? CancellationToken.None);
                                        } else {
                                        await Helper.WaitAnyCancellationToken(finished.Token, workflowContext.ForceCancellationToken ?? CancellationToken.None);
                                    }
                                    if(finished.IsCancellationRequested) {
                                        return;
                                    }
                                    if(workflowContext.ForceCancellationToken?.IsCancellationRequested == true) {
                                        workflowTraceWriter.Info("Workflow Force Cancellation: Requested");
                                        foreach(var job2 in jobs) {
                                            if(job2.Status == null) {
                                                workflowTraceWriter.Info($"Force cancelling {job2.DisplayName ?? job2.name}");
                                                var ji = job2;
                                                // cancel pseudo job e.g. workflow_call
                                                ji.Cancel.Cancel();
                                                Job job = _cache.Get<Job>(ji.Id);
                                                if(job != null) {
                                                    // cancel normal job
                                                    job.CancelRequest.Cancel();
                                                    // No check for sessionid, since we do force cancellation
                                                    clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                }
                                                workflowTraceWriter.Info($"Force cancelled {job2.DisplayName ?? job2.name}");
                                            }
                                        }
                                        workflowTraceWriter.Info("Workflow Force Cancellation: Done");
                                        return;
                                    } else if(workflowContext.CancellationToken.IsCancellationRequested) {
                                        workflowTraceWriter.Info("Workflow Cancellation: Requested");
                                        foreach(var cjob in jobs) {
                                            if(cjob.Status == null) {
                                                foreach(var job2 in cjob.Childs?.Prepend(cjob) ?? new [] { cjob }) {
                                                    if(job2.EvaluateIf == null) {
                                                        continue;
                                                    }
                                                    workflowTraceWriter.Info($"Reevaluate Condition of {job2.DisplayName ?? job2.name}");
                                                    bool ifResult;
                                                    try {
                                                        ifResult = job2.EvaluateIf(workflowTraceWriter);
                                                    } catch(Exception ex) {
                                                        ifResult = false;
                                                        workflowTraceWriter.Info($"Exception while evaluating if expression of {job2.DisplayName ?? job2.name}: {ex.Message}, Stacktrace: {ex.StackTrace}");
                                                    }
                                                    if(!ifResult) {
                                                        workflowTraceWriter.Info($"Cancelling {job2.DisplayName ?? job2.name}");
                                                        var ji = job2;
                                                        // cancel pseudo job e.g. workflow_call
                                                        ji.Cancel.Cancel();
                                                        Job job = _cache.Get<Job>(ji.Id);
                                                        if(job != null) {
                                                            // cancel normal job
                                                            job.CancelRequest.Cancel();
                                                            if(job.SessionId == Guid.Empty) {
                                                                clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                            }
                                                        }
                                                        workflowTraceWriter.Info($"Cancelled {job2.DisplayName ?? job2.name}");
                                                    } else {
                                                        workflowTraceWriter.Info($"Skip Cancellation of {job2.DisplayName ?? job2.name}");
                                                    }
                                                }
                                            }
                                        }
                                        workflowTraceWriter.Info("Workflow Cancellation: Done");
                                    }
                                }
                            } finally {
                                clone._context.Dispose();
                            }
                        });
                        FinishJobController.OnJobCompletedAfter += workflowcomplete;
                        workflowcomplete(null);
                    };
                    var jobConcurrency = callingJob?.JobConcurrency?.ToConcurrency(maxConcurrencyGroupNameLength: MaxConcurrencyGroupNameLength);
                    var concurrency = workflowConcurrency?.ToConcurrency(maxConcurrencyGroupNameLength: MaxConcurrencyGroupNameLength);
                    if(string.Equals(jobConcurrency?.Group, concurrency?.Group, StringComparison.OrdinalIgnoreCase)) {
                        // Seems like if both have the same group, then jobConcurrency is discarded.
                        // Observed by adding cancel-in-progress: true to the resuable workflow, while only providing the group of the caller
                        jobConcurrency = null;
                    }
                    if(string.IsNullOrEmpty(jobConcurrency?.Group) && string.IsNullOrEmpty(concurrency?.Group)) {
                        runWorkflow();
                    } else {
                        Action cancelPendingWorkflow = () => {
                            workflowTraceWriter.Info("{0}", "Workflow was cancelled by another workflow or job, while it was pending in the concurrency group");
                            var evargs = new WorkflowEventArgs { runid = runid, Success = false };
                            finishAsyncWorkflow(evargs);
                        };

                        var myAttempt = _context.Set<WorkflowRunAttempt>().Find(attempt.Id);
                        myAttempt.Status = Status.Waiting;
                        _context.SaveChanges();
                        UpdateWorkflowRun(myAttempt, repository_name);


                        Action<Concurrency, Action<ConcurrencyGroup>> addToConcurrencyGroup = (concurrency, action) => {
                            var key = $"{repository_name}/{concurrency.Group}";
                            while(true) {
                                ConcurrencyGroup cgroup = concurrencyGroups.GetOrAdd(key, name => new ConcurrencyGroup() { Key = name });
                                lock(cgroup) {
                                    if(concurrencyGroups.TryGetValue(key, out var _cgroup) && cgroup != _cgroup) {
                                        continue;
                                    }
                                    action(cgroup);
                                    break;
                                }
                            }
                        };
                        Func<Concurrency, Action, Action<ConcurrencyGroup>> processConcurrency = (c, then) => {
                            return cgroup => {
                                var group = c.Group;
                                var cancelInprogress = c.CancelInProgress;
                                ConcurrencyEntry centry = new ConcurrencyEntry();
                                centry.Run = async () => {
                                    if(workflowContext.CancellationToken.IsCancellationRequested) {
                                        workflowTraceWriter.Info("{0}", $"Workflow was cancelled, while it was pending in the concurrency group: {group}");
                                    } else {
                                        workflowTraceWriter.Info("{0}", $"Starting Workflow run by concurrency group: {group}");
                                        then();
                                        await Helper.WaitAnyCancellationToken(finished.Token);
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
                                workflowTraceWriter.Info("{0}", $"Adding Workflow to the concurrency group: {group}, cancel-in-progress: {cancelInprogress}");
                                cgroup.PushEntry(centry, cancelInprogress);
                            };
                        };
                        // Needed to avoid a deadlock between caller and reusable workflow
                        var prerunCancel = new CancellationTokenSource();
                        Task.Run(async () => {
                            await Helper.WaitAnyCancellationToken(workflowContext.CancellationToken, prerunCancel.Token, finished.Token, workflowContext.ForceCancellationToken ?? CancellationToken.None);
                            if(!prerunCancel.Token.IsCancellationRequested && !finished.Token.IsCancellationRequested) {
                                workflowTraceWriter.Info("{0}", $"Prerun cancellation");
                                cancelPendingWorkflow();
                            }
                        });
                        if(string.IsNullOrEmpty(jobConcurrency?.Group) || string.IsNullOrEmpty(concurrency?.Group)) {
                            var con = string.IsNullOrEmpty(jobConcurrency?.Group) ? concurrency : jobConcurrency;
                            addToConcurrencyGroup(con, processConcurrency(con, () => {
                                prerunCancel.Cancel();
                                runWorkflow();
                            }));
                        } else {
                            addToConcurrencyGroup(jobConcurrency, processConcurrency(jobConcurrency, () => {
                                addToConcurrencyGroup(concurrency, processConcurrency(concurrency, () => {
                                    prerunCancel.Cancel();
                                    runWorkflow();
                                }));
                            }));
                        }
                    }
                }
                if(!asyncProcessing) {
                    if(callingJob != null) {
                        callingJob.Workflowfinish.Invoke(callingJob, new WorkflowEventArgs { runid = runid, Success = true });
                    } else {
                        attempt.Result = TaskResult.Succeeded;
                        UpdateWorkflowRun(attempt, repository_name);
                        _context.SaveChanges();
                    }
                }
            } catch(Exception ex) {
                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, ex.Message.Split('\n').ToList()), workflowTimelineId, workflowRecordId);
                if(callingJob != null) {
                    callingJob.Workflowfinish.Invoke(callingJob, new WorkflowEventArgs { runid = runid, Success = false });
                } else {
                    //updateJobStatus.Invoke(new JobItem() { DisplayName = "Fatal Failure", Status = TaskResult.Failed }, TaskResult.Failed);
                    attempt.Result = TaskResult.Failed;
                    UpdateWorkflowRun(attempt, repository_name);
                    _context.SaveChanges();
                }
                return new HookResponse { repo = repository_name, run_id = runid, skipped = false, failed = true };
            } finally {
                if(!asyncProcessing) {
                    finishWorkflow();
                    _context.Dispose();
                }
            }
            return new HookResponse { repo = repository_name, run_id = runid, skipped = false };
        }

        private static void UpdateWorkflowRun(WorkflowRunAttempt attempt, string repository_name)
        {
            attempt.WorkflowRun.Result = attempt.Result;
            attempt.WorkflowRun.Ref = attempt.Ref;
            attempt.WorkflowRun.Sha = attempt.Sha;
            attempt.WorkflowRun.EventName = attempt.EventName;
            var ownerAndRepo = repository_name.Split("/", 2);
            attempt.WorkflowRun.Owner = ownerAndRepo[0];
            attempt.WorkflowRun.Repo = ownerAndRepo[1];
            Task.Run(() => runupdateevent?.Invoke(ownerAndRepo[0], ownerAndRepo[1], attempt.WorkflowRun));
        }

        private struct NugetVersion {
            public string Version { get; set; }
        }

        private struct NugetPackage {
            public string Id { get; set; }
            public NugetVersion[] Versions { get; set; }
        }

        private struct NugetFeed {
            public NugetPackage[] Data { get; set; }
        }

        private static Mutex taskCacheLock = new Mutex();

        private HookResponse AzureDevopsMain(string fileRelativePath, string content, string repository, string giteaUrl, GiteaHook hook, JObject payloadObject, string e, string selectedJob, bool list, string[] env, string[] secrets, string[] _matrix, string[] platform, bool localcheckout, long runid, long runnumber, string Ref, string Sha, CallingJob callingJob = null, KeyValuePair<string, string>[] workflows = null, WorkflowRunAttempt attempt = null, string statusSha = null, Dictionary<string, List<Job>> finishedJobs = null, ISecretsProvider secretsProvider = null, string[] taskNames = null) {
            attempt = _context.Set<WorkflowRunAttempt>().Find(attempt.Id);
            _context.Entry(attempt).Reference(a => a.WorkflowRun).Load();
            secretsProvider ??= new DefaultSecretsProvider(Configuration);
            bool asyncProcessing = false;
            Guid workflowTimelineId = callingJob?.TimelineId ?? attempt.TimeLineId;
            if(workflowTimelineId == Guid.Empty) {
                workflowTimelineId = Guid.NewGuid();
                attempt.TimeLineId = workflowTimelineId;
                _context.SaveChanges();
                _webConsoleLogService.CreateNewRecord(workflowTimelineId, new TimelineRecord{ Id = workflowTimelineId, Name = fileRelativePath, RefName = fileRelativePath, RecordType = "workflow" });
            }
            Guid workflowRecordId = callingJob?.RecordId ?? attempt.TimeLineId;
            if(workflowTimelineId == attempt.TimeLineId) {
                // Add workflow as dummy job, to improve early cancellation of Runner.Client
                initializingJobs.TryAdd(workflowTimelineId, new Job() { JobId = workflowTimelineId, TimeLineId = workflowTimelineId, runid = runid } );
                if(attempt.Attempt > 1) {
                    workflowRecordId = Guid.NewGuid();
                    UpdateTimeLine(_webConsoleLogService.CreateNewRecord(workflowTimelineId, new TimelineRecord{ Id = workflowRecordId, ParentId = workflowTimelineId, Order = attempt.Attempt, Name = $"Attempt {attempt.Attempt}", RecordType = "workflow" }));
                } else {
                    UpdateTimeLine(workflowTimelineId, _webConsoleLogService.GetTimeLine(workflowTimelineId));
                }
            }
            Action finishWorkflow = () => {
                if(callingJob == null) {
                    SyncLiveLogsToDb(workflowTimelineId);
                }
                // Cleanup dummy job for this workflow
                if(workflowTimelineId == attempt.TimeLineId) {
                    initializingJobs.Remove(workflowTimelineId, out _);
                }
            };
            var workflowTraceWriter = new TraceWriter2(line => {
                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, new List<string>{ line }), workflowTimelineId, workflowRecordId);
            }, Verbosity);
            workflowTraceWriter.Info($"Initialize Workflow Run {runid}");
            string event_name = e;
            string repository_name = repository;
            MappingToken workflowOutputs = null;
            var jobsctx = new DictionaryContextData();

            var workflowname = fileRelativePath;
            Func<JobItem, TaskResult?, Task> updateJobStatus = async (next, status) => {
                var effective_event = callingJob?.Event ?? event_name;
                if(!string.IsNullOrEmpty(hook?.repository?.full_name) && !string.IsNullOrEmpty(statusSha) && !next.NoStatusCheck && (effective_event == "push" || ((effective_event == "pull_request" || effective_event == "pull_request_target") && (new [] { "opened", "synchronize", "synchronized", "reopened" }).Any(t => t == hook?.Action))) && !localcheckout) {
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
                            // Skipped jobs don't block required checks: so Skipped => Success https://github.com/github/docs/commit/66b433088115a579b7f1d774aa1ee852fc5ec2b
                            if(status == TaskResult.Succeeded || status == TaskResult.SucceededWithIssues || status == TaskResult.Skipped) {
                                jobstatus = JobStatus.Success;
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
                    } else {
                        var ghAppToken = await CreateGithubAppToken(repository_name, new { Permissions = new { metadata = "read", checks = "write" } });
                        if(ghAppToken != null) {
                            try {
                                var appClient2 = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("gharun"), new Uri(GitServerUrl))
                                {
                                    Credentials = new Octokit.Credentials(ghAppToken)
                                };
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
                                await DeleteGithubAppToken(ghAppToken);
                            }
                        }
                    }
                }
            };
            Func<string, DictionaryContextData> createContext = jobname => {
                var contextData = new GitHub.DistributedTask.Pipelines.ContextData.DictionaryContextData();
                return contextData;
            };
            try {
                CancellationTokenSource cancellationToken = null;
                var cforceCancelWorkflow = new CancellationTokenSource();
                var ctoken = CancellationTokenSource.CreateLinkedTokenSource(cforceCancelWorkflow.Token);
                var state = new WorkflowState {
                    Cancel = ctoken,
                    ForceCancel = cforceCancelWorkflow
                };
                var workflowContext = new WorkflowContext() { FileName = fileRelativePath, WorkflowState = state };
                if(WorkflowStates.TryAdd(runid, state)) {
                    workflowContext.CancellationToken = ctoken.Token;
                    cancellationToken = ctoken;
                    workflowContext.ForceCancellationToken = cforceCancelWorkflow.Token;
                    var orgfinishWorkflow = finishWorkflow;
                    finishWorkflow = () => {
                        orgfinishWorkflow();
                        WorkflowStates.Remove(runid, out _);
                    };
                } else {
                    // Better don't allow to have multiple reruns active
                    throw new Exception("This workflow run is already running, multiple attempts are not supported at the same time");
                }
                var workflowBytelen = System.Text.Encoding.UTF8.GetByteCount(content);
                if(workflowBytelen > MaxWorkflowFileSize) {
                    throw new Exception("Workflow size too large {workflowBytelen} exceeds {MaxWorkflowFileSize} bytes");
                }
                var globalVars = secretsProvider.GetVariablesForEnvironment("");
                workflowContext.FeatureToggles = globalVars;
                ExpressionFlags flags = ExpressionFlags.DTExpressionsV1 | ExpressionFlags.ExtendedDirectives;
                if(workflowContext.HasFeature("system.runner.server.extendedFunctions")) {
                    flags |= ExpressionFlags.ExtendedFunctions;
                }
                if(workflowContext.HasFeature("system.runner.server.DTExpressionsV2")) {
                    flags &= ~ExpressionFlags.DTExpressionsV1;
                }
                if(workflowContext.HasFeature("system.runner.server.allowAnyForInsert")) {
                    flags |= ExpressionFlags.AllowAnyForInsert;
                }
                workflowContext.Flags = flags;
                List<JobItem> jobgroup = new List<JobItem>();
                List<JobItem> dependentjobgroup = new List<JobItem>();
                var fileProvider = new Azure.Devops.DefaultInMemoryFileProviderFileProvider(workflows, (a, b) => {
                    return TryGetFile(runid, a, out var content, b) ? content : null;
                });
                var rootVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                // Provide all env vars as normal variables
                if(env?.Length > 0) {
                    LoadEnvSec(env, (k, v) => rootVariables[k] = v);
                }
                // Provide normal variables from cli
                foreach(var v in globalVars) {
                    rootVariables[v.Key] = v.Value;
                }
                var context = new Azure.Devops.Context { FileProvider = fileProvider, TraceWriter = workflowTraceWriter, VariablesProvider = new AzurePipelinesVariablesProvider(secretsProvider, rootVariables), Flags = flags };
                workflowContext.AzContext = context;
                Dictionary<string, TemplateToken> workflowParameters = null;
                if(workflowContext.FeatureToggles.TryGetValue("system.runner.server.parameters", out var rawparameters)) {
                    workflowParameters = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                    MappingToken pparameters;
                    var templateContext = CreateTemplateContext(workflowTraceWriter, workflowContext, null);
                    using (var stringReader = new StringReader(rawparameters)) {
                        var yamlObjectReader = new YamlObjectReader(null, stringReader);
                        pparameters = TemplateReader.Read(templateContext, "any", yamlObjectReader, null, out _)?.AssertMapping("parameters");
                    }
                    templateContext.Errors.Check();
                    foreach(var kv in pparameters) {
                        workflowParameters[kv.Key.ToString()] = kv.Value;
                    }
                }
                var evaluatedRoot = Runner.Server.Azure.Devops.AzureDevops.ReadTemplate(context, fileRelativePath, workflowParameters).GetAwaiter().GetResult();
                bool forceTaskCacheUpdate = workflowContext.HasFeature("system.runner.server.forceTaskCacheUpdate");
                bool skipTaskCacheUpdate = workflowContext.HasFeature("system.runner.server.skipTaskCacheUpdate");
                bool taskCacheUpdate = workflowContext.HasFeature("system.runner.server.taskCacheUpdate");
                Func<string, Dictionary<string, TaskMetaData>> getOrCreateTaskCache = name => {
                    var cacheKey = "tasksByNameAndVersion";
                    if(name == null && !forceTaskCacheUpdate && _cache.TryGetValue(cacheKey, out Dictionary<string, TaskMetaData> res)){
                        return res;
                    }
                    var azureTasks = Path.Join(GharunUtil.GetLocalStorage(), "AzureTasks");
                    workflowTraceWriter.Info($"Downloading and update {name ?? "default"} Azure Pipeline Tasks from nuget registry");
                    if(WaitHandle.WaitAny(new [] { taskCacheLock, cancellationToken.Token.WaitHandle } ) != 0) {
                        workflowTraceWriter.Error($"Cancelled downloading or updating the cached Tasks");
                        return null;
                    }
                    try {
                        if(name == null && !forceTaskCacheUpdate && _cache.TryGetValue(cacheKey, out res)){
                            return res;
                        }
                        var ( tasks, tasksByNameAndVersion ) = TaskMetaData.LoadTasks(azureTasks);
                        if(!skipTaskCacheUpdate && (name != null || forceTaskCacheUpdate)) {
                            var client = new HttpClient();
                            var nameprefix = "mseng.ms.tf.distributedtask.tasks.";
                            var filter = name == null ? "" : $"q={nameprefix}{Uri.EscapeDataString(name)}&";
                            var pageSize = 10;
                            for(int j = 0; ; j += pageSize) {
                                var feed = JsonConvert.DeserializeObject<NugetFeed>(client.GetStringAsync($"https://pkgs.dev.azure.com/mseng/c86767d8-af79-4303-a7e6-21da0ba435e2/_packaging/e10d0795-57cd-4d7f-904e-5f39703cb096/nuget/v3/query2/?{filter}skip={j}&take={pageSize}&prerelease=true", cancellationToken.Token).GetAwaiter().GetResult());
                                for(int i = 0; i < feed.Data.Length; i++) {
                                    var lowerId = feed.Data[i].Id.ToLower();
                                    var lowerVersion = feed.Data[i].Versions[0].Version.ToLower();
                                    if(lowerId.StartsWith(nameprefix) && lowerId[lowerId.Length - 2] == 'v' && tasksByNameAndVersion.TryGetValue($"{lowerId.Substring(nameprefix.Length, lowerId.Length  - 2 - nameprefix.Length)}@{lowerVersion}", out _)) {
                                        continue;
                                    }
                                    var lowerIdVersion = $"{lowerId}.{lowerVersion}";
                                    var taskZip = Path.Join(azureTasks, lowerIdVersion, "task.zip");
                                    if(!System.IO.File.Exists(taskZip) || !TaskMetaData.ValidZipFile(taskZip)) {
                                        var packageFile = Path.Join(azureTasks, $"{lowerId}.{lowerVersion}.nuget");
                                        if(!System.IO.File.Exists(packageFile) || !TaskMetaData.ValidZipFile(taskZip)) {
                                            var packageUrl = $"https://pkgs.dev.azure.com/mseng/c86767d8-af79-4303-a7e6-21da0ba435e2/_packaging/e10d0795-57cd-4d7f-904e-5f39703cb096/nuget/v3/flat2/{lowerId}/{lowerVersion}/{lowerId}.{lowerVersion}.nupkg";
                                            workflowTraceWriter.Info($"Downloading {lowerId}@{lowerVersion} from {packageUrl}");
                                            var resp = client.GetAsync(packageUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken.Token).GetAwaiter().GetResult();
                                            Directory.CreateDirectory(azureTasks);
                                            using (FileStream fs = new FileStream(packageFile, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 4096, useAsync: true))
                                            using (var result = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                                            {
                                                result.CopyToAsync(fs, 4096, cancellationToken.Token).GetAwaiter().GetResult();
                                                fs.FlushAsync(cancellationToken.Token).GetAwaiter().GetResult();
                                            }
                                        }
                                        workflowTraceWriter.Info($"Extracting {lowerId}@{lowerVersion}");
                                        Directory.CreateDirectory(Path.Join(azureTasks, lowerIdVersion));
                                        using(var task = System.IO.Compression.ZipFile.Open(packageFile, System.IO.Compression.ZipArchiveMode.Read))
                                        using(var stream = task.GetEntry("content/task.zip")?.Open())
                                        using (FileStream fs = new FileStream(taskZip, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true)) {
                                            stream.CopyToAsync(fs, 4096, cancellationToken.Token).GetAwaiter().GetResult();
                                            fs.FlushAsync(cancellationToken.Token).GetAwaiter().GetResult();
                                        }
                                        workflowTraceWriter.Info($"Extracted {lowerId}@{lowerVersion}");
                                        System.IO.File.Delete(packageFile);
                                        workflowTraceWriter.Info($"Deleted {lowerId}.{lowerVersion}.nuget");
                                    }
                                }
                                if(feed.Data.Length < pageSize) {
                                    break;
                                }
                            }
                            ( tasks, tasksByNameAndVersion ) = TaskMetaData.LoadTasks(azureTasks);
                        }
                        _cache.Set(cacheKey, tasksByNameAndVersion);
                        workflowTraceWriter.Info($"Finished updating the cached Tasks");
                        return tasksByNameAndVersion;
                    } catch(Exception ex) {
                        workflowTraceWriter.Error($"Failed to Download or update the cached Tasks: {ex}");
                        return null;
                    } finally {
                        taskCacheLock.ReleaseMutex();
                    }
                };
                var tasksByNameAndVersion = new Dictionary<string, TaskMetaData>(getOrCreateTaskCache(null), StringComparer.OrdinalIgnoreCase);
                state.TasksByNameAndVersion = tasksByNameAndVersion;
                try {
                    if(!string.IsNullOrWhiteSpace(Assembly.GetEntryAssembly().Location)) {
                        TaskMetaData.Load(tasksByNameAndVersion, Path.Join(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "wwwroot", "localcheckoutazure.zip"));
                    } else {
                        var embeddedFileProvider = new ManifestEmbeddedFileProvider(Assembly.GetAssembly(type: typeof(Program))!, "wwwroot");
                        using var str = embeddedFileProvider.GetFileInfo("localcheckoutazure.zip").CreateReadStream();
                        TaskMetaData.Load(tasksByNameAndVersion, "embedded://localcheckoutazure.zip", str);
                    }
                } catch(Exception ex) {
                    workflowTraceWriter.Error($"Failed to read the localcheckoutazure inbox task: {ex}");
                }
                if(taskNames?.Length > 0) {
                    var config = Configuration;
                    var cache = this._cache;
                    foreach(var tn in taskNames) {
                        if(TryGetFile(runid, "task.json", out var taskMetaData, tn)) {
                            var metaData = JsonConvert.DeserializeObject<TaskMetaData>(taskMetaData);
                            metaData.ArchivePath = $"localtaskzip://{tn}";
                            tasksByNameAndVersion[$"{metaData.Name}@{metaData.Version.Major}"] = metaData;
                            tasksByNameAndVersion[$"{metaData.Name}@{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}"] = metaData;
                            tasksByNameAndVersion[$"{metaData.Id}@{metaData.Version.Major}"] = metaData;
                            tasksByNameAndVersion[$"{metaData.Id}@{metaData.Version.Major}.{metaData.Version.Minor}.{metaData.Version.Patch}"] = metaData;
                        }
                    }
                }

                context.TaskByNameAndVersion = skipTaskCacheUpdate || forceTaskCacheUpdate ? new Azure.Devops.StaticTaskCache { TasksByNameAndVersion = tasksByNameAndVersion } : new LambdaTaskCache { Resolver = name => {
                    if(!taskCacheUpdate && state.TasksByNameAndVersion.TryGetValue(name, out var meta)) {
                        return meta;
                    }
                    var namever = name.Split("@", 2);
                    var endver = namever[1].IndexOf('.');
                    if(endver != -1) {
                        namever[1] = namever[1].Substring(0, endver);
                    }
                    // Do not assign state.TasksByNameAndVersion, because it is possible to loose the localcheckout task
                    foreach(var entry in getOrCreateTaskCache($"{namever[0]}v{namever[1]}")) {
                        state.TasksByNameAndVersion[entry.Key] = entry.Value;
                    }
                    if(state.TasksByNameAndVersion.TryGetValue(name, out meta)) {
                        return meta;
                    }
                    return null;
                } };

                var pipeline = new Azure.Devops.Pipeline().Parse(context.ChildContext(evaluatedRoot, fileRelativePath), evaluatedRoot).GetAwaiter().GetResult();

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

                Func<HookResponse> skipWorkflow = () => {
                    if(callingJob != null) {
                        callingJob.Workflowfinish.Invoke(callingJob, new WorkflowEventArgs { runid = runid, Success = false });
                    } else {
                        attempt.Result = TaskResult.Skipped;
                        UpdateWorkflowRun(attempt, repository_name);
                        _context.SaveChanges();
                    }
                    return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                };
                var startTime = DateTimeOffset.Now;
                if(string.IsNullOrEmpty(pipeline.Name)) {
                    pipeline.Name = "$(Date:yyyyMMdd).$(Rev:r)";
                }
                if(!string.IsNullOrEmpty(pipeline.Name)) {
                    var macroexpr = new Regex("\\$\\(([^)]+)\\)");
                    Func<string, int, string> evalMacro = null;
                    evalMacro = (macro, depth) => {
                        return macroexpr.Replace(macro, v => {
                            var keyFormat = v.Groups[1].Value.Split(":", 2);
                            var format = keyFormat.Length == 1 || string.IsNullOrEmpty(keyFormat[1]) ? null : keyFormat[1];
                            switch(keyFormat[0].ToLower()) {
                                case "date":
                                return startTime.ToString(format == null ? "yyyyMMdd" : keyFormat[1]);
                                case "year":
                                return startTime.ToString(format == null ? "yyyy" : keyFormat[1]);
                                case "seconds":
                                return startTime.ToString(format == null ? "s" : keyFormat[1]);
                                case "minutes":
                                return startTime.ToString(format == null ? "m" : keyFormat[1]);
                                case "hours":
                                return startTime.ToString(format == null ? "H" : keyFormat[1]);
                                case "dayOfmonth":
                                return startTime.ToString(format == null ? "d" : keyFormat[1]);
                                case "dayofyear":
                                return startTime.DayOfYear.ToString();
                                case "rev":
                                // Fake revision
                                // r => 1
                                // rr => 01
                                // rrr => 001
                                return string.Join(string.Empty, Enumerable.Repeat('0', Math.Max((format?.Length ?? 1) - 1, 0))) + "1";
                            }
                            // check for cli/rootVariables variables first
                            if(rootVariables.TryGetValue(keyFormat[0], out var strval)) {
                                return depth <= 10 ? evalMacro(strval, depth + 1) : strval;
                            }
                            return pipeline.Variables != null && pipeline.Variables.TryGetValue(keyFormat[0], out var val) ? (depth <= 10 ? evalMacro(val.Value, depth + 1) : val.Value) : v.Groups[0].Value;
                        });
                    };
                    pipeline.Name = evalMacro(pipeline.Name, 0);
                }

                if((pipeline.AppendCommitMessageToRunName ?? true) && hook?.head_commit?.Message != null) {
                    if(!string.IsNullOrEmpty(pipeline.Name)) {
                        pipeline.Name += " " + hook.head_commit.Message;
                    } else {
                        pipeline.Name = workflowname + " " + hook.head_commit.Message;
                    }
                }
  
                workflowname = pipeline.Name ?? workflowname;
                if(callingJob == null) {
                    workflowTraceWriter.Info($"Updated Workflow Name: {workflowname}");
                    if(attempt.WorkflowRun != null && !string.IsNullOrEmpty(workflowname)) {
                        attempt.WorkflowRun.DisplayName = workflowname;
                        UpdateWorkflowRun(attempt, repository_name);
                        _context.SaveChanges();
                    }
                }

                Action<DictionaryContextData, string, JobItem, JobCompletedEvent> updateNeedsCtx = (needsctx, name, job, e) => {
                    DictionaryContextData jobctx = new DictionaryContextData();
                    needsctx[name] = jobctx;
                    var outputsctx = new DictionaryContextData();
                    jobctx["outputs"] = outputsctx;
                    foreach(var j in (IEnumerable<JobItem>) job.Childs ?? new [] { job }) {
                        foreach(var output in from v in _context.TimelineVariables where v.Record.ParentId == j.Id select new { Name = $"{j.RefPrefix}{v.Record.RefName}.{v.Name}", v.Value }) {
                            outputsctx[output.Name] = new StringContextData(output.Value);
                        }
                    }
                    job.Status = e.Result;
                    jobctx.Add("result", new StringContextData(e.Result.ToString()));
                };
                FinishJobController.JobCompleted workflowcomplete = null;
                TemplateToken workflowPermissions = null;
                TemplateToken workflowConcurrency = null;
                CancellationTokenSource finished = null;
                var stagenamebuilder = new ReferenceNameBuilder();
                {
                    List<string> errors = new List<string>();
                    foreach (var stage in pipeline.Stages) {
                        if(!string.IsNullOrEmpty(stage.Name)) {
                            // Validate StageName
                            if(!stagenamebuilder.TryAddKnownName(stage.Name, out var jnerror)) {
                                errors.Add(jnerror);
                            }
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
                }
                var stagesByName = new Dictionary<string, Azure.Devops.Stage>(StringComparer.OrdinalIgnoreCase);
                var allJobs = new JobItem[0];
                for(int s = 0; s < pipeline.Stages.Count; s++) {
                    var stage = pipeline.Stages[s];
                    // If DependsOn is omitted for a stage, depend on the previous one
                    if(stage.DependsOn == null && s > 0) {
                        stage.DependsOn = new [] { pipeline.Stages[s - 1].Name };
                    }
                    if(string.IsNullOrEmpty(stage.Name)) {
                        stagenamebuilder.AppendSegment("Stage");
                        stage.Name = stagenamebuilder.Build();
                    }
                    List<string> errors = new List<string>();
                    var jobnamebuilder = new ReferenceNameBuilder();
                    foreach (var job in stage.Jobs) {
                        if(!string.IsNullOrEmpty(job.Name)) {
                            // Validate Jobname
                            if(!jobnamebuilder.TryAddKnownName(job.Name, out var jnerror)) {
                                errors.Add(jnerror);
                            }
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
                    foreach (var job in stage.Jobs) {
                        if(string.IsNullOrEmpty(job.Name)) {
                            jobnamebuilder.AppendSegment("Job");
                            job.Name = jobnamebuilder.Build();
                        }
                        if(job.Pool == null) {
                            job.Pool = stage.Pool ?? pipeline.Pool;
                        }
                        var jobname = job.Name;
                        var jobitem = new JobItem() { name = jobname, Id = Guid.NewGuid(), Stage = stage.Name };
                        dependentjobgroup.Add(jobitem);

                        var skipDependencies = workflowContext.HasFeature("system.runner.server.skipDependencies");
                        var needs = job.DependsOn;
                        List<string> neededJobs = new List<string>();
                        if (needs != null) {
                            neededJobs.AddRange(needs);
                        }
                        jobitem.Needs = neededJobs.ToHashSet(StringComparer.OrdinalIgnoreCase);
                        List<string> neededStages = new List<string>();
                        if (stage.DependsOn != null) {
                            neededStages.AddRange(stage.DependsOn);
                        }
                        var contextData = createContext(jobname);
                        
                        var needsctx = new DictionaryContextData();
                        var stageDependencies = new DictionaryContextData();
                        var neededStageJobs = new Dictionary<string, List<string>>();
                        DictionaryContextData stageToStageDependencies = new DictionaryContextData();
                        if(skipDependencies) {
                            neededJobs.Clear();
                            neededStages.Clear();
                            Func<string, DictionaryContextData> parseDeps = name => {
                                if(workflowContext.FeatureToggles.TryGetValue(name, out var value)) {
                                    DictionaryContextData ret;
                                    var templateContext = CreateTemplateContext(workflowTraceWriter, workflowContext, contextData);
                                    using (var stringReader = new StringReader(value)) {
                                        var yamlObjectReader = new YamlObjectReader(null, stringReader);
                                        ret = TemplateReader.Read(templateContext, "any", yamlObjectReader, null, out _).ToContextData().AssertDictionary(name);
                                    }
                                    templateContext.Errors.Check();
                                    return ret;
                                }
                                return new DictionaryContextData();
                            };
                            needsctx = parseDeps("system.runner.server.dependencies");
                            stageDependencies = parseDeps("system.runner.server.stageDependencies");
                            stageToStageDependencies = parseDeps("system.runner.server.stageToStageDependencies");
                        }
                        contextData.Add("dependencies", needsctx);
                        contextData.Add("stageDependencies", stageDependencies);

                        FinishJobController.JobCompleted handler = e => {
                            try {
                                if(neededJobs.Count > 0 || neededStages.Count > 0) {
                                    if(e == null) {
                                        if(!NoRecursiveNeedsCtx && jobitem.Dependencies != null) {
                                            neededJobs = jobitem.Dependencies.Keys.ToList();
                                        }
                                        if(!NoRecursiveNeedsCtx && stage.Dependencies != null) {
                                            neededStages = stage.Dependencies.Keys.ToList();
                                        }
                                        foreach(var neededStage in neededStages) {
                                            neededStageJobs[neededStage] = (from dep in allJobs where string.Equals(dep.Stage, neededStage, StringComparison.OrdinalIgnoreCase) select dep.name).ToList();
                                        }
                                        return;
                                    }
                                    foreach(var stageJ in neededStageJobs.ToArray()) {
                                        var neededStage = stageJ.Value;
                                        if(neededStage.RemoveAll(name => {
                                            var job = (from j in jobgroup where string.Equals(j.name, name, StringComparison.OrdinalIgnoreCase) && string.Equals(j.Stage, stageJ.Key, StringComparison.OrdinalIgnoreCase) && j.Id == e.JobId select j).FirstOrDefault();
                                            if(job != null) {
                                                DictionaryContextData cneedsctx;
                                                if(stageDependencies.TryGetValue(stageJ.Key, out var cneedsctxTmp) && cneedsctxTmp is DictionaryContextData cneedsctxTmp2) {
                                                    cneedsctx = cneedsctxTmp2;
                                                } else {
                                                    cneedsctx = new DictionaryContextData();
                                                    stageDependencies[stageJ.Key] = cneedsctx;
                                                }
                                                updateNeedsCtx(cneedsctx, name, job, e);
                                            
                                                DictionaryContextData coutputctx;
                                                if(stageToStageDependencies.TryGetValue(stageJ.Key, out cneedsctxTmp) && cneedsctxTmp is DictionaryContextData cneedsctxTmp3) {
                                                    cneedsctx = cneedsctxTmp3;
                                                } else {
                                                    cneedsctx = new DictionaryContextData();
                                                    stageToStageDependencies[stageJ.Key] = cneedsctx;
                                                }
                                                if(cneedsctx.TryGetValue("outputs", out var coutputmp) && coutputmp is DictionaryContextData coutputtmp2) {
                                                    coutputctx = coutputtmp2;
                                                } else {
                                                    coutputctx = new DictionaryContextData();
                                                    cneedsctx["outputs"] = coutputctx;
                                                }
                                                foreach(var j in (IEnumerable<JobItem>) job.Childs ?? new [] { job }) {
                                                    foreach(var output in from v in _context.TimelineVariables where v.Record.ParentId == j.Id select new { Name = $"{j.name}.{j.RefPrefix}{v.Record.RefName}.{v.Name}", v.Value }) {
                                                        coutputctx[output.Name] = new StringContextData(output.Value);
                                                    }
                                                }
                                                var result = e.Result;
                                                if(cneedsctx.TryGetValue("result", out var cresultTMp) && cresultTMp is StringContextData cresult) {
                                                    var oresult = Enum.Parse<TaskResult>(cresult.Value, true);
                                                    if(result == TaskResult.Skipped && oresult == TaskResult.Succeeded) {
                                                        result = TaskResult.Succeeded;
                                                    } else if((result == TaskResult.Succeeded || result == TaskResult.Skipped) && oresult == TaskResult.SucceededWithIssues ) {
                                                        result = TaskResult.SucceededWithIssues;
                                                    } else if(oresult == TaskResult.Failed || oresult == TaskResult.Abandoned || oresult == TaskResult.Canceled || result == TaskResult.Failed || result == TaskResult.Abandoned || result == TaskResult.Canceled) {
                                                        result = TaskResult.Failed;
                                                    }
                                                }
                                                cneedsctx["result"] = new StringContextData(result.ToString());
                                                return true;
                                            }
                                            return false;
                                        }) == 0 || neededStage.Count > 0) {
                                            continue;
                                        }
                                        neededStageJobs.Remove(stageJ.Key);
                                    }
                                    if(neededStageJobs.Count > 0) {
                                        return;
                                    }
                                    if(neededJobs.Count > 0 && neededJobs.RemoveAll(name => {
                                        var job = (from j in jobgroup where string.Equals(j.name, name, StringComparison.OrdinalIgnoreCase) && string.Equals(j.Stage, stage.Name, StringComparison.OrdinalIgnoreCase) && j.Id == e.JobId select j).FirstOrDefault();
                                        if(job != null) {
                                            updateNeedsCtx(needsctx, name, job, e);
                                            return true;
                                        }
                                        return false;
                                    }) == 0 || neededJobs.Count > 0) {
                                        return;
                                    }
                                }
                                if(skipDependencies) {
                                    foreach(var kv in jobitem.Dependencies) {
                                        var status = needsctx.TryGetValue(kv.Key, out var jobdata) && jobdata is DictionaryContextData jobdatadict && jobdatadict.TryGetValue("result", out var jobresult) && jobresult is StringContextData jobresultstr ? Enum.Parse<TaskResult>(jobresultstr.Value, true) : TaskResult.Succeeded;
                                        jobitem.Dependencies[kv.Key].Status = status;
                                    }
                                }

                                dependentjobgroup.Remove(jobitem);
                                if(!dependentjobgroup.Any()) {
                                    jobgroup.Clear();
                                } else {
                                    jobgroup.Add(jobitem);
                                }

                                var jid = jobitem.Id;
                                jobitem.TimelineId = Guid.NewGuid();
                                var jobrecord = new TimelineRecord{ Id = jobitem.Id, Name = jobitem.name };
                                var jobtlUpdate = _webConsoleLogService.CreateNewRecord(jobitem.TimelineId, jobrecord);
                                var jobTraceWriter = new TraceWriter2(line => _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(jobitem.Id, new List<string>{ line }), jobitem.TimelineId, jobitem.Id));
                                var _jobdisplayname = (job.DisplayName ?? jobitem.name)?.PrefixJobNameIfNotNull(stage.DisplayName ?? stage.Name);
                                jobrecord.Name = _jobdisplayname;
                                jobitem.DisplayName = _jobdisplayname;
                                // For Runner.Client to show the workflowname
                                initializingJobs.TryAdd(jobitem.Id, new Job() { JobId = jobitem.Id, TimeLineId = jobitem.TimelineId, name = jobitem.DisplayName, workflowname = workflowname, runid = runid, RequestId = jobitem.RequestId } );
                                UpdateTimeLine(jobtlUpdate);
                                jobTraceWriter.Info("{0}", $"Evaluate if");
                                var ifexpr = job.Condition;
                                var condition = new BasicExpressionToken(null, null, null, string.IsNullOrEmpty(ifexpr) ? "succeeded()" : ifexpr);
                                var recursiveNeedsctx = needsctx;
                                if(!NoRecursiveNeedsCtx) {
                                    needsctx = new DictionaryContextData();
                                    contextData["dependencies"] = needsctx;
                                    foreach(var need in jobitem.Needs) {
                                        if(recursiveNeedsctx.TryGetValue(need, out var val)) {
                                            needsctx[need] = val;
                                        }
                                    }
                                }
                                DictionaryContextData workflowVariables = new DictionaryContextData();
                                // Provide all env vars as normal variables
                                if(env?.Length > 0) {
                                    LoadEnvSec(env, (k, v) => workflowVariables[k] = new StringContextData(v));
                                }
                                // Provide normal variables from cli
                                foreach(var secr in secretsProvider.GetVariablesForEnvironment("")) {
                                    workflowVariables[secr.Key] = new StringContextData(secr.Value);
                                }
                                // Provide normal secrets from cli
                                foreach(var secr in secretsProvider.GetSecretsForEnvironment(jobTraceWriter, "")) {
                                    workflowVariables[secr.Key] = new StringContextData(secr.Value);
                                }
                                if(pipeline.Variables != null) {
                                    foreach (var v in pipeline.Variables) {
                                        workflowVariables[v.Key] = new StringContextData(v.Value.Value);
                                    }
                                }
                                if(stage.Variables != null) {
                                    foreach (var v in stage.Variables) {
                                        workflowVariables[v.Key] = new StringContextData(v.Value.Value);
                                    }
                                }
                                DictionaryContextData jobVariables = workflowVariables.Clone() as DictionaryContextData;
                                if(job.Variables != null) {
                                    foreach (var v in job.Variables) {
                                        jobVariables[v.Key] = new StringContextData(v.Value.Value);
                                    }
                                }
                                jobitem.EvaluateIf = (traceWriter) => {
                                    var templateContext = Runner.Server.Azure.Devops.AzureDevops.CreateTemplateContext(traceWriter, workflowContext.FileTable, flags, contextData);
                                    // It seems that the offical actions service does provide a recusive needs ctx, but only for if expressions.
                                    templateContext.ExpressionValues["dependencies"] = stageToStageDependencies;
                                    templateContext.ExpressionValues["variables"] = workflowVariables;
                                    var jobCtx = new JobItem() { Dependencies = new Dictionary<string, JobItem>(StringComparer.OrdinalIgnoreCase) };
                                    var exctx = new ExecutionContext() { Cancelled = workflowContext.CancellationToken, JobContext = jobCtx };
                                    if(stage.Dependencies != null) {
                                        foreach(var kv in stage.Dependencies) {
                                            var status = stageToStageDependencies.TryGetValue(kv.Key, out var jobdata) && jobdata is DictionaryContextData jobdatadict && jobdatadict.TryGetValue("result", out var jobresult) && jobresult is StringContextData jobresultstr ? Enum.Parse<TaskResult>(jobresultstr.Value, true) : TaskResult.Succeeded;
                                            jobCtx.Dependencies[kv.Key] = new JobItem { Status = status };
                                        }
                                    }
                                    templateContext.State[nameof(ExecutionContext)] = exctx;
                                    templateContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
                                    templateContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>("Canceled", 0, 0));
                                    templateContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>("Failed", 0, Int32.MaxValue));
                                    templateContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>("Succeeded", 0, Int32.MaxValue));
                                    templateContext.ExpressionFunctions.Add(new FunctionInfo<SucceededOrFailedFunction>("SucceededOrFailed", 0, Int32.MaxValue));
                                    var stageCondition = stage.Condition;
                                    var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "stage-if-result", new BasicExpressionToken(null, null, null, string.IsNullOrEmpty(stageCondition) ? "succeeded()" : stageCondition), 0, null, true);
                                    templateContext.Errors.Check();
                                    if(!PipelineTemplateConverter.ConvertToIfResult(templateContext, eval)) {
                                        return false;
                                    }
                                    templateContext = Runner.Server.Azure.Devops.AzureDevops.CreateTemplateContext(traceWriter, workflowContext.FileTable, flags, contextData);
                                    // It seems that the offical actions service does provide a recusive needs ctx, but only for if expressions.
                                    templateContext.ExpressionValues["stageDependencies"] = stageDependencies;
                                    templateContext.ExpressionValues["dependencies"] = recursiveNeedsctx;
                                    templateContext.ExpressionValues["variables"] = jobVariables;
                                    // canceled is always false if the stage condition is true, by using CancellationToken.None 
                                    templateContext.State[nameof(ExecutionContext)] = new ExecutionContext() { Cancelled = CancellationToken.None, JobContext = jobitem };
                                    templateContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
                                    templateContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>("Canceled", 0, 0));
                                    templateContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>("Failed", 0, Int32.MaxValue));
                                    templateContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>("Succeeded", 0, Int32.MaxValue));
                                    templateContext.ExpressionFunctions.Add(new FunctionInfo<SucceededOrFailedFunction>("SucceededOrFailed", 0, Int32.MaxValue));
                                    eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.JobIfResult, condition, 0, null, true);
                                    templateContext.Errors.Check();
                                    return PipelineTemplateConverter.ConvertToIfResult(templateContext, eval);
                                };
                                Action<TaskResult> sendFinishJob = result => {
                                    var _job = new Job() { message = null, repo = repository_name, WorkflowRunAttempt = attempt, WorkflowIdentifier = jobitem.name.PrefixJobIdIfNotNull(stage.Name), name = _jobdisplayname, workflowname = workflowname, runid = runid, JobId = jid, RequestId = jobitem.RequestId, TimeLineId = jobitem.TimelineId};
                                    AddJob(_job);
                                    InvokeJobCompleted(new JobCompletedEvent() { JobId = jobitem.Id, Result = result, RequestId = jobitem.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                };
                                try {
                                    var _res = (!workflowContext.ForceCancellationToken?.IsCancellationRequested ?? true) && jobitem.EvaluateIf(jobTraceWriter);
                                    if(!_res) {
                                        sendFinishJob(TaskResult.Skipped);
                                        return;
                                    }
                                    var flatmatrix = new List<Dictionary<string, TemplateToken>> { new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase) };
                                    var includematrix = new List<Dictionary<string, TemplateToken>> { };
                                    bool failFast = true;
                                    
                                    // Enforce job matrix limit of github
                                    if(flatmatrix.Count > 256) {
                                        jobTraceWriter.Info("{0}", $"Failure: Matrix contains more than 256 entries after exclude");
                                        sendFinishJob(TaskResult.Failed);
                                        return;
                                    }
                                    var keys = flatmatrix.First().Keys.ToArray();
                                    var deploymentStrategy = job?.Strategy?.RunOnce ?? (Azure.Devops.Strategy.RunOnceStrategy) job?.Strategy?.Rolling ?? (Azure.Devops.Strategy.RunOnceStrategy) job?.Strategy?.Canary; 
                                
                                    var variables = new Dictionary<string, GitHub.DistributedTask.WebApi.VariableValue>(StringComparer.OrdinalIgnoreCase);
                                    variables.Add("system.runner.lowdiskspacethreshold", new VariableValue("100", false)); // actions/runner warns if free space is less than 100MB
                                    // For actions/upload-artifact@v1, actions/download-artifact@v1
                                    variables.Add(SdkConstants.Variables.Build.BuildId, new VariableValue(runid.ToString(), false));
                                    variables.Add(SdkConstants.Variables.Build.BuildNumber, new VariableValue(pipeline.Name, false));
                                    variables["system"] = new VariableValue("build", false);
                                    variables["System.HostType"] = new VariableValue("build", false);
                                    variables["System.ServerType"] = new VariableValue("OnPremises", false);
                                    variables["system.culture"] = new VariableValue("en-US", false);
                                    variables["System.CollectionId"] = new VariableValue(Guid.Empty.ToString(), false);
                                    variables["system.teamProject"] = new VariableValue("runner.server", false);
                                    // If this is not a non zero guid upload artifact tasks refuse to work
                                    variables["system.teamProjectId"] = new VariableValue("667b63ea-5b23-4619-9431-f2cff4e16a11", false);
                                    variables["System.DefinitionId"] = new VariableValue(Guid.Empty.ToString(), false);
                                    variables["System.planid"] = new VariableValue(Guid.Empty.ToString(), false);
                                    variables["system.definitionName"] = new VariableValue(Guid.Empty.ToString(), false);
                                    variables["Build.Clean"] = new VariableValue("true", false);
                                    variables["Build.SyncSources"] = new VariableValue("true", false);
                                    variables["Build.DefinitionName"] = new VariableValue(Guid.Empty.ToString(), false);
                                    // for azurelocalcheckout
                                    variables["System.RunId"] = new VariableValue(runid.ToString(), false);
                                    // ff for agent to enforce readonly vars
                                    variables["agent.readOnlyVariables"] = "true";
                                    variables["agent.retainDefaultEncoding"] = "true";
                                    variables["agent.taskRestrictionsEnforcementMode"] = "Enabled";
                                    variables["agent.disablelogplugin.TestResultLogPlugin"] = "true";
                                    variables["agent.disablelogplugin.TestFilePublisherPlugin"] = "true";

                                    foreach(var v in variables) {
                                        v.Value.IsReadonly = true;
                                    }
                                    // Provide all env vars as normal variables
                                    if(env?.Length > 0) {
                                        LoadEnvSec(env, (k, v) => variables[k] = v);
                                    }
                                    // Provide normal variables from cli
                                    foreach(var secr in secretsProvider.GetVariablesForEnvironment("")) {
                                        variables[secr.Key] = new VariableValue(secr.Value, false);
                                    }
                                    var pipelinecontext = new PipelineContext(startTime);
                                    {
                                        var resourcesCtx = new DictionaryContextData();
                                        var repositoriesCtx = new DictionaryContextData();
                                        resourcesCtx["repositories"] = repositoriesCtx;
                                        if(workflowContext.AzContext.Repositories != null) {
                                            foreach(var repo in workflowContext.AzContext.Repositories) {
                                                var repoCtx = new DictionaryContextData();
                                                var nref = repo.Value.Split("@", 2);
                                                repoCtx["name"] = new StringContextData(nref[0]);
                                                repoCtx["ref"] = new StringContextData(nref[1]);
                                                repositoriesCtx[repo.Key] = repoCtx;
                                            }
                                        }
                                        var containersCtx = new DictionaryContextData();
                                        resourcesCtx["containers"] = containersCtx;
                                        if(pipeline.ContainerResources != null) {
                                            foreach(var container in pipeline.ContainerResources) {
                                                containersCtx[container.Key] = container.Value.ToContextData();
                                            }
                                        }
                                        contextData["resources"] = resourcesCtx;
                                    }
                                    Func<GitHub.DistributedTask.ObjectTemplating.ITraceWriter, DictionaryContextData, Func<string, string>> createEvalVariable = (traceWriter, contextData) => {
                                        return val => {
                                            if(!(val.StartsWith("$[") && val.EndsWith("]"))) {
                                                return val;
                                            }
                                            var templateContext = Runner.Server.Azure.Devops.AzureDevops.CreateTemplateContext(traceWriter, workflowContext.FileTable, workflowContext.Flags, contextData);
                                            templateContext.ExpressionValues["pipeline"] = pipelinecontext;
                                            templateContext.ExpressionFunctions.Add(new FunctionInfo<CounterFunction>("counter", 0, 2));
                                            var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "variable-result", new BasicExpressionToken(null, null, null, val.Substring(2, val.Length - 3)), 0, null, true);
                                            templateContext.Errors.Check();
                                            return eval.AssertString("variable").Value;
                                        };
                                    };
                                    var evalVariable = createEvalVariable(jobTraceWriter, contextData);
                                    {
                                        contextData["variables"] = jobVariables;
                                        if(pipeline.Variables != null) {
                                            foreach(var v in pipeline.Variables) {
                                                variables[v.Key] = new VariableValue(evalVariable(v.Value.Value), v.Value.IsSecret, v.Value.IsReadonly);
                                            }
                                        }
                                        if(stage.Variables != null) {
                                            foreach(var v in stage.Variables) {
                                                variables[v.Key] = new VariableValue(evalVariable(v.Value.Value), v.Value.IsSecret, v.Value.IsReadonly);
                                            }
                                        }
                                        if(job.Variables != null) {
                                            foreach(var v in job.Variables) {
                                                variables[v.Key] = new VariableValue(evalVariable(v.Value.Value), v.Value.IsSecret, v.Value.IsReadonly);
                                            }
                                        }
                                        var vars = new DictionaryContextData();
                                        foreach(var v in variables) {
                                            vars[v.Key] = new StringContextData(v.Value.Value);
                                        }
                                        contextData["variables"] = vars;
                                    }
                                    double? max_parallel = null;
                                    if(job?.Strategy?.MatrixExpression != null) {
                                        var result = evalVariable(job.Strategy.MatrixExpression);
                                        job.Strategy.Matrix = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(result);
                                        if(job.Strategy.Matrix == null) {
                                            job.Strategy = null;
                                        }
                                    }
                                    if(job?.Strategy?.MaxParallel != null) {
                                        var result = evalVariable(job?.Strategy?.MaxParallel);
                                        max_parallel = Int32.Parse(result);
                                    }
                                    if(job?.Strategy?.Parallel != null) {
                                        var result = evalVariable(job.Strategy.Parallel);
                                        var parallelJobs = Int32.Parse(result);
                                        for(int i = 0; i < parallelJobs; i++) {
                                            var mvars = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                            mvars["System.JobPositionInPhase"] = new StringToken(null, null, null, i.ToString());
                                            mvars["System.TotalJobsInPhase"] = new StringToken(null, null, null, parallelJobs.ToString());
                                            includematrix.Add(mvars);
                                        }
                                    } else if(job?.Strategy?.Matrix != null) {
                                        int i = 0;
                                        foreach(var kv in job.Strategy.Matrix) {
                                            var mvars = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                            foreach(var vars in kv.Value) {
                                                mvars[vars.Key] = new StringToken(null, null, null, vars.Value);
                                            }
                                            mvars["System.JobPositionInPhase"] = new StringToken(null, null, null, i.ToString());
                                            includematrix.Add(mvars);
                                            i++;
                                        }
                                    } else if(deploymentStrategy != null) {
                                        if(deploymentStrategy.PreDeploy != null) {
                                            var mvars = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                            mvars["stage"] = new StringToken(null, null, null, "preDeploy");
                                            mvars["Strategy.CycleName"] = new StringToken(null, null, null, "PreIteration");
                                            includematrix.Add(mvars);
                                        }
                                        if(deploymentStrategy.Deploy != null) {
                                            var mvars = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                            mvars["stage"] = new StringToken(null, null, null, "deploy");
                                            mvars["Strategy.CycleName"] = new StringToken(null, null, null, "Iteration");
                                            includematrix.Add(mvars);
                                        }
                                        if(deploymentStrategy.RouteTraffic != null) {
                                            var mvars = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                            mvars["stage"] = new StringToken(null, null, null, "routeTraffic");
                                            mvars["Strategy.CycleName"] = new StringToken(null, null, null, "Iteration");
                                            includematrix.Add(mvars);
                                        }
                                        if(deploymentStrategy.PostRouteTraffic != null) {
                                            var mvars = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                            mvars["stage"] = new StringToken(null, null, null, "postRouteTraffic");
                                            mvars["Strategy.CycleName"] = new StringToken(null, null, null, "Iteration");
                                            includematrix.Add(mvars);
                                        }
                                        if(deploymentStrategy.OnSuccess != null) {
                                            var mvars = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                            mvars["stage"] = new StringToken(null, null, null, "onSuccess");
                                            mvars["Strategy.CycleName"] = new StringToken(null, null, null, "PostIteration");
                                            includematrix.Add(mvars);
                                        }
                                        if(deploymentStrategy.OnFailure != null) {
                                            var mvars = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                            mvars["stage"] = new StringToken(null, null, null, "onFailure");
                                            mvars["Strategy.CycleName"] = new StringToken(null, null, null, "PostIteration");
                                            includematrix.Add(mvars);
                                        }
                                    }
                                    var rawCount = includematrix.Count;

                                    // Filter matrix from cli
                                    if(selectedJob != null && (string.Equals($"{jobitem.Stage}/{jobitem.name}", selectedJob, StringComparison.OrdinalIgnoreCase) || string.Equals($"{jobitem.Stage}", selectedJob, StringComparison.OrdinalIgnoreCase) || pipeline.Stages.Count == 1 && string.Equals($"{jobitem.name}", selectedJob, StringComparison.OrdinalIgnoreCase)) && _matrix?.Length > 0) {
                                        var mdict = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                        foreach(var m_ in _matrix) {
                                            var i = m_.IndexOf(":");
                                            var templateContext = CreateTemplateContext(jobTraceWriter, workflowContext, contextData);
                                            using (var stringReader = new StringReader(m_.Substring(i + 1))) {
                                                var yamlObjectReader = new YamlObjectReader(null, stringReader);
                                                mdict[m_.Substring(0, i)] = TemplateReader.Read(templateContext, "any", yamlObjectReader, null, out _);
                                            }
                                            templateContext.Errors.Check();
                                        }
                                        Predicate<Dictionary<string, TemplateToken>> match = dict => {
                                            foreach(var kv in mdict) {
                                                TemplateToken val;
                                                if(!dict.TryGetValue(kv.Key, out val) || !kv.Value.DeepEquals(val)) {
                                                    return true;
                                                }
                                            }
                                            return false;
                                        };
                                        flatmatrix.RemoveAll(match);
                                        includematrix.RemoveAll(match);
                                        if(flatmatrix.Count + includematrix.Count == 0) {
                                            jobTraceWriter.Info("{0}", $"Your specified matrix filter doesn't match any matrix entries");
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
                                        jobTraceWriter.Info("{0}", $"Failure: Matrix contains more than 256 entries after exclude");
                                        sendFinishJob(TaskResult.Failed);
                                        return;
                                    }
                                    bool? canBeCancelled = null;
                                    Func<bool> cancelRequest = () => {
                                        if(workflowContext.ForceCancellationToken?.IsCancellationRequested == true || jobitem.Cancel.IsCancellationRequested) {
                                            jobTraceWriter.Info("{0}", $"Cancellation: workflowContext.ForceCancellationToken?.IsCancellationRequested == true || jobitem.Cancel.IsCancellationRequested");
                                            return true;
                                        }
                                        if(!workflowContext.CancellationToken.IsCancellationRequested) {
                                            return false;
                                        }
                                        jobTraceWriter.Info("{0}", $"Cancellation: workflowContext.CancellationToken.IsCancellationRequested");
                                        if(canBeCancelled != null) {
                                            return canBeCancelled.Value;
                                        }
                                        bool ret = !jobitem.EvaluateIf(jobTraceWriter);
                                        canBeCancelled = ret;
                                        return ret;
                                    };
                                    if(cancelRequest()) {
                                        sendFinishJob(TaskResult.Canceled);
                                        return;
                                    }
                                    if(jobTotal > 1) {
                                        jobitem.Childs = new List<JobItem>();
                                        jobitem.NoStatusCheck = true;
                                        var _job = new Job() { message = null, repo = repository_name, WorkflowRunAttempt = attempt, WorkflowIdentifier = jobitem.name.PrefixJobIdIfNotNull(stage.Name), name = jobitem.DisplayName, workflowname = workflowname, runid = runid, JobId = jid, RequestId = jobitem.RequestId, TimeLineId = jobitem.TimelineId};
                                        var clone = Clone();
                                        Task.Run(async () => {
                                            try {
                                                await Helper.WaitAnyCancellationToken(finished.Token, _job.CancelRequest.Token, jobitem.Cancel.Token);
                                                if(!finished.IsCancellationRequested) {
                                                    jobitem.Cancel.Cancel();
                                                    foreach(var ji in jobitem.Childs) {
                                                        ji.Cancel.Cancel();
                                                        Job job = _cache.Get<Job>(ji.Id);
                                                        if(job != null) {
                                                            // cancel normal job
                                                            job.CancelRequest.Cancel();
                                                            if(job.SessionId == Guid.Empty) {
                                                                clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                            }
                                                        }
                                                    }
                                                }
                                            } finally {
                                                clone._context.Dispose();
                                            }
                                        });
                                        AddJob(_job);
                                        // Fix workflow doesn't wait for cancelled matrix jobs to finish, add dummy sessionid
                                        _job.SessionId = Guid.NewGuid();
                                    }
                                    {
                                        int i = 0;
                                        Func<string, Dictionary<string, TemplateToken>, Func<TaskResult?, Job>> act = (displaySuffix, item) => {
                                            var providedVars = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                                            int c = i++;
                                            DictionaryContextData matrixContext = new DictionaryContextData();
                                            if(item.Any()) {
                                                foreach (var mk in item) {
                                                    PipelineContextData data = mk.Value.ToContextData();
                                                    matrixContext[mk.Key] = data;
                                                    providedVars[mk.Key] = new VariableValue((mk.Value as StringToken)?.Value ?? "");
                                                }
                                            }
                                            Action<string, string> addMatrixVar = (name, value) => {
                                                matrixContext[name] = new StringContextData(value ?? "");
                                                providedVars[name] = new VariableValue(value ?? "");
                                            };
                                            if(job?.Strategy?.RunOnce != null) {
                                                addMatrixVar("Strategy.Name", new StringContextData("runOnce"));
                                            } else if(job?.Strategy?.Rolling != null) {
                                                addMatrixVar("Strategy.Name", "rolling");
                                            } else if(job?.Strategy?.Canary != null) {
                                                addMatrixVar("Strategy.Name", "canary");
                                            }
                                            if(job?.EnvironmentName != null) {
                                                addMatrixVar("Environment.Name", job.EnvironmentName);
                                            }
                                            
                                            contextData["matrix"] = null;
                                            if(finishedJobs != null) {
                                                if(finishedJobs.TryGetValue($"{stage.Name}/{jobname}", out var fjobs)) {
                                                    foreach(var fjob in fjobs) {
                                                        if((matrixContext?.ToTemplateToken() ?? new MappingToken(null, null, null)).DeepEquals(fjob.MatrixContextData[callingJob?.Depth ?? 0]?.ToTemplateToken() ?? new MappingToken(null, null, null))) {
                                                            var _next = jobTotal > 1 ? new JobItem() { name = jobitem.name, Id = fjob.JobId, NoFailFast = true, Stage = stage.Name } : jobitem;
                                                            if(!string.IsNullOrEmpty(displaySuffix)) {
                                                                _next.RefPrefix = $"{displaySuffix}.";
                                                            }
                                                            _next.TimelineId = fjob.TimeLineId;
                                                            _next.NoStatusCheck = true;
                                                            jobitem.Childs?.Add(_next);
                                                            return b => {
                                                                var jevent = new JobCompletedEvent(_next.RequestId, _next.Id, fjob.Result.Value, fjob.Outputs.ToDictionary(o => o.Name, o => new VariableValue(o.Value, false), StringComparer.OrdinalIgnoreCase));
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
                                                        if(djob.Dependencies != null && (string.Equals(fjobs.Key, $"{djob.Stage}/{djob.name}", StringComparison.OrdinalIgnoreCase) || fjobs.Key.StartsWith($"{djob.Stage}/{djob.name}" + "/", StringComparison.OrdinalIgnoreCase))) {
                                                            if(string.Equals(djob.Stage, stage.Name, StringComparison.OrdinalIgnoreCase)) {
                                                                foreach(var dep in djob.Dependencies) {
                                                                    if(string.Equals(dep.Key, jobname, StringComparison.OrdinalIgnoreCase)) {
                                                                        finishedJobs.Remove(fjobs.Key);
                                                                        return;
                                                                    }
                                                                }
                                                            } else {
                                                                foreach(var dep in stagesByName[djob.Stage].Dependencies) {
                                                                    if(string.Equals(dep.Key, stage.Name, StringComparison.OrdinalIgnoreCase)) {
                                                                        finishedJobs.Remove(fjobs.Key);
                                                                        return;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                });
                                            }
                                            var next = jobTotal > 1 ? new JobItem() { name = jobitem.name, Id = Guid.NewGuid(), Stage = stage.Name } : jobitem;
                                            if(!string.IsNullOrEmpty(displaySuffix)) {
                                                next.RefPrefix = $"{displaySuffix}.";
                                            }
                                            Func<string, string> defJobName = jobname => string.IsNullOrEmpty(displaySuffix) ? jobname : $"{jobname} {displaySuffix}";
                                            var _prejobdisplayname = defJobName(_jobdisplayname);
                                            if(jobTotal > 1) {
                                                next.TimelineId = Guid.NewGuid();
                                                // For Runner.Client to show the workflowname
                                                initializingJobs.TryAdd(next.Id, new Job() { JobId = next.Id, TimeLineId = next.TimelineId, name = _prejobdisplayname, workflowname = workflowname, runid = runid, RequestId = next.RequestId } );
                                                UpdateTimeLine(_webConsoleLogService.CreateNewRecord(next.TimelineId, new TimelineRecord{ Id = next.Id, Name = _prejobdisplayname }));
                                            }
                                            Func<Func<TaskResult?, Job>> failJob = () => {
                                                var _job = new Job() { JobId = next.Id, TimeLineId = next.TimelineId, name = _prejobdisplayname, workflowname = workflowname, repo = repository_name, WorkflowRunAttempt = attempt, runid = runid, RequestId = next.RequestId };
                                                AddJob(_job);
                                                InvokeJobCompleted(new JobCompletedEvent() { JobId = next.Id, Result = TaskResult.Failed, RequestId = next.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                return cancel => _job;
                                            };
                                            try {
                                                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ $"Prepare Job for execution" }), next.TimelineId, next.Id);
                                                var matrixJobTraceWriter = new TraceWriter2(line => {
                                                    _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ line }), next.TimelineId, next.Id);
                                                });
                                                jobitem.Childs?.Add(next);
                                                var _jobdisplayname = _prejobdisplayname;
                                                next.DisplayName = _jobdisplayname;
                                                next.NoStatusCheck = false;
                                                next.ActionStatusQueue.Post(() => updateJobStatus(next, null));
                                                addMatrixVar("System.StageName", stage.Name);
                                                addMatrixVar("System.StageDisplayName", stage.DisplayName);
                                                addMatrixVar("System.PhaseName", job.Name);
                                                addMatrixVar("System.PhaseDisplayName", next.DisplayName);
                                                addMatrixVar("System.JobName", "__default");
                                                addMatrixVar("System.JobDisplayName", next.DisplayName);
                                                var svariables = new Dictionary<string, GitHub.DistributedTask.WebApi.VariableValue>(variables, StringComparer.OrdinalIgnoreCase);
                                                var userAgentName = "AZURE_HTTP_USER_AGENT";
                                                if(!svariables.ContainsKey(userAgentName)) {
                                                    svariables[userAgentName] = "gharun/1.0.0";
                                                }
                                                if(providedVars != null) {
                                                    foreach(var v in providedVars) {
                                                        svariables[v.Key] = v.Value;
                                                    }
                                                }
                                                var vars = new DictionaryContextData();
                                                foreach(var v in svariables) {
                                                    vars[v.Key] = new StringContextData(v.Value.Value);
                                                }
                                                var jcontextData = contextData.Clone() as DictionaryContextData;
                                                jcontextData["variables"] = vars;
                                                var matrixjobEval = createEvalVariable(matrixJobTraceWriter, jcontextData);
                                                if(job.ContinueOnError != null) {
                                                    var rawContinueOnError = matrixjobEval(job.ContinueOnError);
                                                    if(GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.TryParseAzurePipelinesBoolean(rawContinueOnError, out var continueOnError)) {
                                                        next.ContinueOnError = continueOnError;
                                                    } else {
                                                        throw new Exception($"{stage.Name}.{job.Name}.continueOnError: value true | y | yes | on | false | n | no | off was expected, got {rawContinueOnError}");
                                                    }
                                                }
                                                var timeoutMinutes = 3600;
                                                if(job.TimeoutInMinutes != null) {
                                                    var rawTimeoutInMinutes = matrixjobEval(job.TimeoutInMinutes);
                                                    if(Int32.TryParse(rawTimeoutInMinutes, out int numValue)) {
                                                        timeoutMinutes = numValue;
                                                    } else {
                                                        throw new Exception($"{stage.Name}.{job.Name}.timeoutInMinutes expected integer, got {rawTimeoutInMinutes}");
                                                    }
                                                }
                                                var cancelTimeoutMinutes = 5;
                                                if(job.CancelTimeoutInMinutes != null) {
                                                    var rawCancelTimeoutInMinutes = matrixjobEval(job.CancelTimeoutInMinutes);
                                                    if(Int32.TryParse(rawCancelTimeoutInMinutes, out int numValue)) {
                                                        cancelTimeoutMinutes = numValue;
                                                    } else {
                                                        throw new Exception($"{stage.Name}.{job.Name}.cancelTimeoutInMinutes expected integer, got {rawCancelTimeoutInMinutes}");
                                                    }
                                                }
                                                return queueAzureJob(matrixJobTraceWriter, _jobdisplayname, job, pipeline, svariables, matrixjobEval, env, jcontextData, next.Id, next.TimelineId, repository_name, jobname, workflowname, runid, runnumber, secrets, timeoutMinutes, cancelTimeoutMinutes, next.ContinueOnError, platform ?? new string[] { }, localcheckout, next.RequestId, Ref, Sha, callingJob?.Event ?? event_name, callingJob?.Event, workflows, statusSha, stage.Name, finishedJobs, attempt, next, workflowPermissions, callingJob, dependentjobgroup, selectedJob, _matrix, workflowContext, secretsProvider);
                                            } catch(Exception ex) {
                                                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(next.Id, new List<string>{ $"Exception: {ex?.ToString()}" }), next.TimelineId, next.Id);
                                                return failJob();
                                            }
                                        };
                                        ConcurrentQueue<Func<TaskResult?, Job>> jobs = new ConcurrentQueue<Func<TaskResult?, Job>>();
                                        List<Job> scheduled = new List<Job>();
                                        FinishJobController.JobCompleted handler2 = null;
                                        Action cleanupOnFinish = () => {
                                            if (scheduled.Count == 0) {
                                                localJobCompletedEvents.JobCompleted -= handler2;
                                                if(jobTotal > 1) {
                                                    InvokeJobCompleted(jobitem.JobCompletedEvent ?? new JobCompletedEvent() { JobId = jobitem.Id, Result = TaskResult.Canceled, RequestId = jobitem.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                }
                                            }
                                        };
                                        Action<string> cancelAll = message => {
                                            foreach (var _j in scheduled) {
                                                if(!string.IsNullOrEmpty(message)) {
                                                    _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(_j.JobId, new List<string>{ message }), _j.TimeLineId, _j.JobId);
                                                }
                                                _j.CancelRequest?.Cancel();
                                                if(_j.SessionId == Guid.Empty) {
                                                    InvokeJobCompleted(new JobCompletedEvent() { JobId = _j.JobId, Result = TaskResult.Canceled, RequestId = _j.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                }
                                            }
                                            scheduled.Clear();
                                            while(jobs.TryDequeue(out var cb)) {
                                                cb(TaskResult.Canceled);
                                            }
                                            cleanupOnFinish();
                                        };
                                        var cancelreqmsg = "Cancelled via cancel request";
                                        if(job?.Strategy?.Matrix != null || job?.Strategy?.Parallel != null || job?.Strategy == null) {
                                            handler2 = e => {
                                                if(scheduled.RemoveAll(j => j.JobId == e.JobId) > 0) {
                                                    var currentItem = jobitem.Childs?.Find(ji => ji.Id == e.JobId) ?? (jobitem.Id == e.JobId ? jobitem : null);
                                                    var conclusion = (currentItem == null || currentItem.ContinueOnError != true) ? e.Result : TaskResult.Succeeded;
                                                    if(jobitem.JobCompletedEvent == null) {
                                                        jobitem.JobCompletedEvent = new JobCompletedEvent() { JobId = jobitem.Id, Result = conclusion, RequestId = jobitem.RequestId, Outputs = e.Outputs != null ? new Dictionary<string, VariableValue>(e.Outputs, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) };
                                                    } else {
                                                        if(jobitem.JobCompletedEvent.Result != conclusion) {
                                                            if(jobitem.JobCompletedEvent.Result == TaskResult.Canceled || jobitem.JobCompletedEvent.Result == TaskResult.Abandoned || conclusion == TaskResult.Failed || conclusion == TaskResult.Canceled || conclusion == TaskResult.Abandoned) {
                                                                jobitem.JobCompletedEvent.Result = TaskResult.Failed;
                                                            } else if((jobitem.JobCompletedEvent.Result == TaskResult.SucceededWithIssues || jobitem.JobCompletedEvent.Result == TaskResult.Skipped) && (conclusion == TaskResult.Succeeded || conclusion == TaskResult.SucceededWithIssues || conclusion == TaskResult.Skipped)) {
                                                                jobitem.JobCompletedEvent.Result = TaskResult.Succeeded;
                                                            }
                                                        }
                                                        if(e.Outputs != null) {
                                                            foreach(var output in e.Outputs) {
                                                                if(!string.IsNullOrEmpty(output.Value.Value)) {
                                                                    jobitem.JobCompletedEvent.Outputs[output.Key] = output.Value;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    if(failFast && (conclusion == TaskResult.Failed || conclusion == TaskResult.Canceled || conclusion == TaskResult.Abandoned) && (currentItem == null || currentItem.NoFailFast != true)) {
                                                        cancelAll("Cancelled via strategy.fail-fast == true");
                                                    } else {
                                                        while((!max_parallel.HasValue || scheduled.Count < max_parallel.Value) && jobs.TryDequeue(out var cb)) {
                                                            if(cancelRequest()) {
                                                                cb(TaskResult.Canceled);
                                                                cancelAll(cancelreqmsg);
                                                                return;
                                                            }
                                                            var jret = cb(null);
                                                            if(jret != null) {
                                                                scheduled.Add(jret);
                                                            }
                                                        }
                                                        cleanupOnFinish();
                                                    }
                                                }
                                            };
                                            localJobCompletedEvents.JobCompleted += handler2;
                                            if(keys.Length != 0 || includematrix.Count == 0) {
                                                foreach (var item in flatmatrix) {
                                                    if(cancelRequest()) {
                                                        cancelAll(cancelreqmsg);
                                                        return;
                                                    }
                                                    var j = act(StrategyUtils.GetDefaultDisplaySuffix(from displayitem in keys.SelectMany(key => item[key].Traverse(true)) where !(displayitem is SequenceToken || displayitem is MappingToken) select displayitem.ToString()), item);
                                                    if(j != null) {
                                                        jobs.Enqueue(j);
                                                    }
                                                }
                                            }
                                            int counter = 0;
                                            foreach (var item in includematrix) {
                                                if(cancelRequest()) {
                                                    cancelAll(cancelreqmsg);
                                                    return;
                                                }
                                                var position = item.TryGetValue("System.JobPositionInPhase", out var rawposition) && int.TryParse(rawposition.AssertString("System.JobPositionInPhase").Value, out var rpos) ? rpos : counter;
                                                var j = act(job.Strategy?.Matrix?.Keys?.ElementAtOrDefault(position) ?? $"job{position}", item);
                                                if(j != null) {
                                                    jobs.Enqueue(j);
                                                }
                                                counter++;
                                            }
                                            for (int j = 0; j < (max_parallel.HasValue ? (int)max_parallel.Value : jobTotal) && jobs.TryDequeue(out var cb2); j++) {
                                                if(cancelRequest()) {
                                                    cb2(TaskResult.Canceled);
                                                    cancelAll(cancelreqmsg);
                                                    return;
                                                }
                                                var jret = cb2(null);
                                                if(jret != null) {
                                                    scheduled.Add(jret);
                                                }
                                            }
                                            cleanupOnFinish();
                                        } else {
                                            int runOnceStage = 0;
                                            Func<int, List<GitHub.DistributedTask.Pipelines.TaskStep>> getSteps = i => {
                                                switch(i) {
                                                case 0:
                                                   return deploymentStrategy.PreDeploy?.Steps;
                                                case 1:
                                                   return deploymentStrategy.Deploy?.Steps;
                                                case 2:
                                                   return deploymentStrategy.RouteTraffic?.Steps;
                                                case 3:
                                                   return deploymentStrategy.PostRouteTraffic?.Steps;
                                                case 4:
                                                   return deploymentStrategy.OnSuccess?.Steps;
                                                case 5:
                                                   return deploymentStrategy.OnFailure?.Steps;
                                                default:
                                                    return null;
                                                }
                                            };
                                            var defaultPool = job.Pool;
                                            Func<int, Runner.Server.Azure.Devops.Pool> getPool = i => {
                                                switch(i) {
                                                case 0:
                                                   return deploymentStrategy.PreDeploy?.Pool;
                                                case 1:
                                                   return deploymentStrategy.Deploy?.Pool;
                                                case 2:
                                                   return deploymentStrategy.RouteTraffic?.Pool;
                                                case 3:
                                                   return deploymentStrategy.PostRouteTraffic?.Pool;
                                                case 4:
                                                   return deploymentStrategy.OnSuccess?.Pool;
                                                case 5:
                                                   return deploymentStrategy.OnFailure?.Pool;
                                                default:
                                                    return null;
                                                }
                                            };
                                            Func<int, string> getStageName = i => {
                                                switch(i) {
                                                case 0:
                                                   return "PreDeploy";
                                                case 1:
                                                   return "Deploy";
                                                case 2:
                                                   return "RouteTraffic";
                                                case 3:
                                                   return "PostRouteTraffic";
                                                case 4:
                                                   return "OnSuccess";
                                                case 5:
                                                   return "OnFailure";
                                                default:
                                                    return null;
                                                }
                                            };
                                            
                                            var stages = new int[includematrix.Count + 1];
                                            Action<TaskResult> moveNext = conclusion => {
                                                bool successCalled = stages[runOnceStage] == 5;
                                                if(conclusion == TaskResult.Failed || conclusion == TaskResult.Canceled || conclusion == TaskResult.Abandoned) {
                                                    successCalled = false;
                                                    while(stages[runOnceStage] < 5 && jobs.TryDequeue(out var cb2)) {
                                                        cb2(TaskResult.Skipped);
                                                        ++runOnceStage;
                                                    }
                                                }
                                                job.Steps = getSteps(stages[runOnceStage]);
                                                job.Pool = getPool(stages[runOnceStage]) ?? defaultPool;
                                                if(jobs.TryDequeue(out var cb)) {
                                                    if(successCalled) {
                                                        cb(TaskResult.Skipped);
                                                        cleanupOnFinish();
                                                        return;
                                                    } else if(cancelRequest()) {
                                                        cb(TaskResult.Canceled);
                                                        cleanupOnFinish();
                                                        return;
                                                    }
                                                    var jret = cb(null);
                                                    if(jret != null) {
                                                        scheduled.Add(jret);
                                                    }
                                                }
                                                cleanupOnFinish();
                                            };
                                            handler2 = e => {
                                                if(scheduled.RemoveAll(j => j.JobId == e.JobId) > 0) {
                                                    ++runOnceStage;
                                                    var currentItem = jobitem.Childs?.Find(ji => ji.Id == e.JobId) ?? (jobitem.Id == e.JobId ? jobitem : null);
                                                    var conclusion = e.Result;
                                                    if(jobitem.JobCompletedEvent == null) {
                                                        jobitem.JobCompletedEvent = new JobCompletedEvent() { JobId = jobitem.Id, Result = conclusion, RequestId = jobitem.RequestId, Outputs = e.Outputs != null ? new Dictionary<string, VariableValue>(e.Outputs, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) };
                                                    } else {
                                                        if(jobitem.JobCompletedEvent.Result != conclusion) {
                                                            if(jobitem.JobCompletedEvent.Result == TaskResult.Canceled || jobitem.JobCompletedEvent.Result == TaskResult.Abandoned || conclusion == TaskResult.Failed || conclusion == TaskResult.Canceled || conclusion == TaskResult.Abandoned) {
                                                                jobitem.JobCompletedEvent.Result = TaskResult.Failed;
                                                            } else if((jobitem.JobCompletedEvent.Result == TaskResult.SucceededWithIssues || jobitem.JobCompletedEvent.Result == TaskResult.Skipped) && (conclusion == TaskResult.Succeeded || conclusion == TaskResult.SucceededWithIssues || conclusion == TaskResult.Skipped)) {
                                                                jobitem.JobCompletedEvent.Result = TaskResult.Succeeded;
                                                            }
                                                        }
                                                    }
                                                    moveNext(conclusion);
                                                }
                                            };
                                            localJobCompletedEvents.JobCompleted += handler2;
                                            int counter = 0;
                                            int ct2 = 0;
                                            foreach (var item in includematrix) {
                                                if(cancelRequest()) {
                                                    cancelAll(cancelreqmsg);
                                                    return;
                                                }

                                                while(!string.Equals((item["stage"] as StringToken).Value, getStageName(counter), StringComparison.OrdinalIgnoreCase)) counter++;
                                                if(getSteps(counter) == null) {
                                                    continue;
                                                }
                                                stages[ct2] = counter;


                                                var hasResourceName = workflowContext.FeatureToggles.TryGetValue($"system.runner.server.job.{jobitem.name}.resourcename", out var resourcename) || workflowContext.FeatureToggles.TryGetValue("system.runner.server.resourcename", out resourcename);

                                                // Stage Deploy is renamed to jobitem.name the jobid if it is a runOnce Job https://learn.microsoft.com/en-us/azure/devops/pipelines/process/deployment-jobs?view=azure-devops#support-for-output-variables, but only if it only has a single deploy step
                                                var j = act(!hasResourceName && job?.Strategy?.RunOnce != null && rawCount == 1 ? jobitem.name : hasResourceName ? $"{getStageName(counter)}_{resourcename}" : getStageName(counter), item);
                                                if(j != null) {
                                                    jobs.Enqueue(j);
                                                }
                                                ct2++;
                                            }
                                            stages[ct2] = 6;
                                            moveNext(TaskResult.Succeeded);
                                        }
                                    }
                                } catch(Exception ex) {
                                    jobTraceWriter.Info("{0}", $"Internal Error: {ex.Message}, {ex.StackTrace}");
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
                                            InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Failed, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                        }
                                    }
                                    return true;
                                }) > 0)) {
                                    InvokeJobCompleted(new JobCompletedEvent() { JobId = jobitem.Id, Result = TaskResult.Failed, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                }
                            }
                        };
                        jobitem.OnJobEvaluatable = handler;
                    }
                }
                if(!dependentjobgroup.Any()) {
                    throw new Exception("Your workflow is invalid, you have to define at least one job");
                }
                dependentjobgroup.ForEach(ji => {
                    if(ji.Needs?.Any() == true) {
                        Func<JobItem, ISet<string>, Dictionary<string, JobItem>> pred = null;
                        pred = (cur, cyclic) => {
                            var ret = new Dictionary<string, JobItem>(StringComparer.OrdinalIgnoreCase);
                            if(cur.Needs?.Any() == true) {
                                // To preserve case of direct dependencies as written in yaml
                                foreach(var need in cur.Needs) {
                                    ret[need] = null;
                                }
                                var pcyclic = cyclic.Append(cur.name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                                ISet<string> missingDeps = cur.Needs.ToHashSet(StringComparer.OrdinalIgnoreCase);
                                dependentjobgroup.ForEach(d => {
                                    if(!string.Equals(cur.Stage, d.Stage, StringComparison.OrdinalIgnoreCase)) {
                                        return;
                                    }
                                    if(cur.Needs.Contains(d.name)) {
                                        if(pcyclic.Contains(d.name)) {
                                            throw new Exception($"{cur.name}: Cyclic dependency to {d.name} detected");
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
                                                    throw new Exception($"{cur.name}: Cyclic dependency to {k.Key} detected");
                                                }
                                                ret[k.Key] = k.Value;
                                            }
                                        }
                                        missingDeps.Remove(d.name);
                                    }
                                });
                                if(missingDeps.Any()) {
                                    throw new Exception($"{cur.name}: One or more missing dependencies detected: {string.Join(", ", missingDeps)}");
                                }
                            }
                            return ret;
                        };
                        if(ji.Dependencies == null)
                            ji.Dependencies = pred(ji, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    }
                });
                pipeline.Stages.ForEach(ji => {
                    stagesByName[ji.Name] = ji;
                    if(ji.DependsOn?.Any() == true) {
                        Func<Azure.Devops.Stage, ISet<string>, Dictionary<string, Azure.Devops.Stage>> pred = null;
                        pred = (cur, cyclic) => {
                            var ret = new Dictionary<string, Azure.Devops.Stage>(StringComparer.OrdinalIgnoreCase);
                            if(cur.DependsOn?.Any() == true) {
                                // To preserve case of direct dependencies as written in yaml
                                foreach(var need in cur.DependsOn) {
                                    ret[need] = null;
                                }
                                var pcyclic = cyclic.Append(cur.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                                ISet<string> missingDeps = cur.DependsOn.ToHashSet(StringComparer.OrdinalIgnoreCase);
                                pipeline.Stages.ForEach(d => {
                                    if(cur.DependsOn.Contains(d.Name)) {
                                        if(pcyclic.Contains(d.Name)) {
                                            throw new Exception($"{cur.Name}: Cyclic dependency to {d.Name} detected");
                                        }
                                        ret[d.Name] = d;
                                        if(d.Dependencies == null) {
                                            d.Dependencies = pred?.Invoke(d, pcyclic);
                                            foreach (var k in d.Dependencies) {
                                                ret[k.Key] = k.Value;
                                            }
                                        } else {
                                            foreach (var k in d.Dependencies) {
                                                if(pcyclic.Contains(k.Key)) {
                                                    throw new Exception($"{cur.Name}: Cyclic dependency to {k.Key} detected");
                                                }
                                                ret[k.Key] = k.Value;
                                            }
                                        }
                                        missingDeps.Remove(d.Name);
                                    }
                                });
                                if(missingDeps.Any()) {
                                    throw new Exception($"{cur.Name}: One or more missing dependencies detected: {string.Join(", ", missingDeps)}");
                                }
                            }
                            return ret;
                        };
                        if(ji.Dependencies == null)
                            ji.Dependencies = pred(ji, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    }
                });
                if(selectedJob != null) {
                    List<JobItem> next = new List<JobItem>();
                    dependentjobgroup.RemoveAll(j => {
                        if(string.Equals($"{j.Stage}/{j.name}", selectedJob, StringComparison.OrdinalIgnoreCase) || string.Equals($"{j.Stage}", selectedJob, StringComparison.OrdinalIgnoreCase) || pipeline.Stages.Count == 1 && string.Equals($"{j.name}", selectedJob, StringComparison.OrdinalIgnoreCase)) {
                            next.Add(j);
                            return true;
                        }
                        return false;
                    });
                    if(!workflowContext.HasFeature("system.runner.server.skipDependencies")) {
                        while(true) {
                            int oldCount = next.Count;
                            dependentjobgroup.RemoveAll(j => {
                                foreach(var j2 in next.ToArray()) {
                                    if(string.Equals(j.Stage, j2.Stage, StringComparison.OrdinalIgnoreCase)) {
                                        foreach(var need in j2.Needs) {
                                            if(string.Equals(j.name, need, StringComparison.OrdinalIgnoreCase)) {
                                                next.Add(j);
                                                return true;
                                            }
                                        }
                                    } else if(stagesByName[j2.Stage].Dependencies.ContainsKey(j.Stage)) {
                                        next.Add(j);
                                        return true;
                                    }
                                }
                                return false;
                            });
                            if(oldCount == next.Count) {
                                break;
                            }
                        }
                    }
                    dependentjobgroup = next;
                    if(dependentjobgroup.Count == 0) {
                        return skipWorkflow();
                    }
                }
                allJobs = dependentjobgroup.ToArray();
                if(list) {
                    workflowTraceWriter.Info("{0}", $"Found {dependentjobgroup.Count} matching jobs for the requested event {e}");
                    foreach(var j in dependentjobgroup) {
                        var depMessages = new List<string>();
                        if(j.Needs.Any()) {
                            depMessages.Add($"jobs: {string.Join(", ", j.Needs)}");
                        }
                        if(stagesByName[j.Stage].DependsOn?.Any() ?? false) {
                            depMessages.Add($"stages: {string.Join(", ", stagesByName[j.Stage].DependsOn)}");
                        }
                        if(depMessages.Any()) {
                            workflowTraceWriter.Info("{0}", $"{j.Stage}/{j.name} depends on {string.Join(" and ", depMessages.ToArray())}");
                        } else {
                            workflowTraceWriter.Info("{0}", $"{j.Stage}/{j.name}");
                        }
                    }
                    return skipWorkflow();
                } else {
                    var mjobs = dependentjobgroup.ToArray();
                    finished = new CancellationTokenSource();
                    Action<WorkflowEventArgs> finishAsyncWorkflow = evargs => {
                        finished.Cancel();
                        finishWorkflow();
                        // Cleanup dummy mjobs, which allows Runner.Client to display the workflowname
                        foreach(var job in mjobs) {
                            if(job.Childs != null) {
                                foreach(var ji in job.Childs) {
                                    initializingJobs.Remove(ji.Id, out _);
                                }
                            }
                            initializingJobs.Remove(job.Id, out _);
                        }
                        if(callingJob != null) {
                            callingJob.Workflowfinish.Invoke(callingJob, evargs);
                        } else {
                            WorkflowStates.Remove(runid, out _);
                            workflowevent?.Invoke(evargs);
                            attempt.Status = Status.Completed;
                            attempt.Result = evargs.Success ? TaskResult.Succeeded : TaskResult.Failed;
                            UpdateWorkflowRun(attempt, repository_name);
                            _context.SaveChanges();
                        }
                        _context.Dispose();
                    };
                    FinishJobController.JobCompleted withoutlock = e => {
                        var ja = e != null ? mjobs.Where(j => e.JobId == j.Id || (j.Childs?.Where(ji => e.JobId == ji.Id).Any() ?? false)).FirstOrDefault() : null;
                        Action<JobItem> updateStatus = job => {
                            job.Status = e.Result;
                        };
                        if(ja != null) {
                            var ji = ja.Childs?.Where(ji => e.JobId == ji.Id).FirstOrDefault() ?? ja;
                            if(workflowOutputs != null && ja == ji) {
                                updateNeedsCtx(jobsctx, ji.name, ji, e);
                            }
                            ji.ActionStatusQueue.Post(() => {
                                return updateJobStatus(ji, e.Result);
                            });
                            ji.Status = e.Result;
                            ji.Completed = true;
                            updateStatus(ji);
                            workflowTraceWriter.Trace($"{ji.DisplayName} ({ji.name}) completed");
                            if(mjobs.All(j => j.Completed)) {
                                workflowTraceWriter.Trace($"All mjobs completed");
                                FinishJobController.OnJobCompletedAfter -= workflowcomplete;
                                var workflow = mjobs.ToList();
                                var evargs = new WorkflowEventArgs { runid = runid, Success = workflow?.All(job => job.ContinueOnError || job.Status == TaskResult.Succeeded || job.Status == TaskResult.SucceededWithIssues || job.Status == TaskResult.Skipped) ?? false };
                                finishAsyncWorkflow(evargs);
                                return;
                            }
                        }
                        jobCompleted(e);
                    };
                    var channel = Channel.CreateUnbounded<JobCompletedEvent>();
                    Task.Run(async () => {
                        while(!finished.IsCancellationRequested) {
                            try {
                                var ev = await channel.Reader.ReadAsync(finished.Token);
                                withoutlock(ev);
                            } catch(Exception ex) {
                                workflowTraceWriter.Info("{0}", $"Exception: {ex.Message}, {ex.StackTrace}");
                            }
                        }
                    });
                    workflowcomplete = (e) => {
                        channel.Writer.WriteAsync(e);
                    };
                    asyncProcessing = true;
                    Action runWorkflow = () => {
                        var clone = Clone();
                        Task.Run(async () => {
                            try {
                                for(int i = 0; i < 2; i++) {

                                        if(i == 0) {
                                        await Helper.WaitAnyCancellationToken(workflowContext.CancellationToken, finished.Token, workflowContext.ForceCancellationToken ?? CancellationToken.None);
                                        } else {
                                        await Helper.WaitAnyCancellationToken(finished.Token, workflowContext.ForceCancellationToken ?? CancellationToken.None);
                                    }
                                    if(finished.IsCancellationRequested) {
                                        return;
                                    }
                                    if(workflowContext.ForceCancellationToken?.IsCancellationRequested == true) {
                                        workflowTraceWriter.Info("Workflow Force Cancellation: Requested");
                                        foreach(var job2 in mjobs) {
                                            if(job2.Status == null) {
                                                workflowTraceWriter.Info($"Force cancelling {job2.DisplayName ?? job2.name}");
                                                var ji = job2;
                                                // cancel pseudo job e.g. workflow_call
                                                ji.Cancel.Cancel();
                                                Job job = _cache.Get<Job>(ji.Id);
                                                if(job != null) {
                                                    // cancel normal job
                                                    job.CancelRequest.Cancel();
                                                    // No check for sessionid, since we do force cancellation
                                                    clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                }
                                                workflowTraceWriter.Info($"Force cancelled {job2.DisplayName ?? job2.name}");
                                            }
                                        }
                                        workflowTraceWriter.Info("Workflow Force Cancellation: Done");
                                        return;
                                    } else if(workflowContext.CancellationToken.IsCancellationRequested) {
                                        workflowTraceWriter.Info("Workflow Cancellation: Requested");
                                        foreach(var job2 in mjobs) {
                                            if(job2.Status == null && job2.EvaluateIf != null) {
                                                workflowTraceWriter.Info($"Reevaluate Condition of {job2.DisplayName ?? job2.name}");
                                                bool ifResult;
                                                try {
                                                    ifResult = job2.EvaluateIf(workflowTraceWriter);
                                                } catch(Exception ex) {
                                                    ifResult = false;
                                                    workflowTraceWriter.Info($"Exception while evaluating if expression of {job2.DisplayName ?? job2.name}: {ex.Message}, Stacktrace: {ex.StackTrace}");
                                                }
                                                if(!ifResult) {
                                                    workflowTraceWriter.Info($"Cancelling {job2.DisplayName ?? job2.name}");
                                                    var ji = job2;
                                                    // cancel pseudo job e.g. workflow_call
                                                    ji.Cancel.Cancel();
                                                    Job job = _cache.Get<Job>(ji.Id);
                                                    if(job != null) {
                                                        // cancel normal job
                                                        job.CancelRequest.Cancel();
                                                        if(job.SessionId == Guid.Empty) {
                                                            clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                                        }
                                                    }
                                                    workflowTraceWriter.Info($"Cancelled {job2.DisplayName ?? job2.name}");
                                                } else {
                                                    workflowTraceWriter.Info($"Skip Cancellation of {job2.DisplayName ?? job2.name}");
                                                }
                                            }
                                        }
                                        workflowTraceWriter.Info("Workflow Cancellation: Done");
                                    }
                                }
                            } finally {
                                clone._context.Dispose();
                            }
                        });
                        FinishJobController.OnJobCompletedAfter += workflowcomplete;
                        workflowcomplete(null);
                    };
                    var jobConcurrency = callingJob?.JobConcurrency?.ToConcurrency(maxConcurrencyGroupNameLength: MaxConcurrencyGroupNameLength);
                    var concurrency = workflowConcurrency?.ToConcurrency(maxConcurrencyGroupNameLength: MaxConcurrencyGroupNameLength);
                    if(string.Equals(jobConcurrency?.Group, concurrency?.Group, StringComparison.OrdinalIgnoreCase)) {
                        // Seems like if both have the same group, then jobConcurrency is discarded.
                        // Observed by adding cancel-in-progress: true to the resuable workflow, while only providing the group of the caller
                        jobConcurrency = null;
                    }
                    if(string.IsNullOrEmpty(jobConcurrency?.Group) && string.IsNullOrEmpty(concurrency?.Group)) {
                        runWorkflow();
                    } else {
                        Action cancelPendingWorkflow = () => {
                            workflowTraceWriter.Info("{0}", "Workflow was cancelled by another workflow or job, while it was pending in the concurrency group");
                            var evargs = new WorkflowEventArgs { runid = runid, Success = false };
                            finishAsyncWorkflow(evargs);
                        };

                        Action<Concurrency, Action<ConcurrencyGroup>> addToConcurrencyGroup = (concurrency, action) => {
                            var key = $"{repository_name}/{concurrency.Group}";
                            while(true) {
                                ConcurrencyGroup cgroup = concurrencyGroups.GetOrAdd(key, name => new ConcurrencyGroup() { Key = name });
                                lock(cgroup) {
                                    if(concurrencyGroups.TryGetValue(key, out var _cgroup) && cgroup != _cgroup) {
                                        continue;
                                    }
                                    action(cgroup);
                                    break;
                                }
                            }
                        };
                        Func<Concurrency, Action, Action<ConcurrencyGroup>> processConcurrency = (c, then) => {
                            return cgroup => {
                                var group = c.Group;
                                var cancelInprogress = c.CancelInProgress;
                                ConcurrencyEntry centry = new ConcurrencyEntry();
                                centry.Run = async () => {
                                    if(workflowContext.CancellationToken.IsCancellationRequested) {
                                        workflowTraceWriter.Info("{0}", $"Workflow was cancelled, while it was pending in the concurrency group: {group}");
                                    } else {
                                        workflowTraceWriter.Info("{0}", $"Starting Workflow run by concurrency group: {group}");
                                        then();
                                        await Helper.WaitAnyCancellationToken(finished.Token);
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
                                workflowTraceWriter.Info("{0}", $"Adding Workflow to the concurrency group: {group}, cancel-in-progress: {cancelInprogress}");
                                cgroup.PushEntry(centry, cancelInprogress);
                            };
                        };
                        // Needed to avoid a deadlock between caller and reusable workflow
                        var prerunCancel = new CancellationTokenSource();
                        Task.Run(async () => {
                            await Helper.WaitAnyCancellationToken(workflowContext.CancellationToken, prerunCancel.Token, finished.Token, workflowContext.ForceCancellationToken ?? CancellationToken.None);
                            if(!prerunCancel.Token.IsCancellationRequested && !finished.Token.IsCancellationRequested) {
                                workflowTraceWriter.Info("{0}", $"Prerun cancellation");
                                cancelPendingWorkflow();
                            }
                        });
                        if(string.IsNullOrEmpty(jobConcurrency?.Group) || string.IsNullOrEmpty(concurrency?.Group)) {
                            var con = string.IsNullOrEmpty(jobConcurrency?.Group) ? concurrency : jobConcurrency;
                            addToConcurrencyGroup(con, processConcurrency(con, () => {
                                prerunCancel.Cancel();
                                runWorkflow();
                            }));
                        } else {
                            addToConcurrencyGroup(jobConcurrency, processConcurrency(jobConcurrency, () => {
                                addToConcurrencyGroup(concurrency, processConcurrency(concurrency, () => {
                                    prerunCancel.Cancel();
                                    runWorkflow();
                                }));
                            }));
                        }
                    }
                }
                if(!asyncProcessing) {
                    if(callingJob != null) {
                        callingJob.Workflowfinish.Invoke(callingJob, new WorkflowEventArgs { runid = runid, Success = true });
                    } else {
                        attempt.Result = TaskResult.Succeeded;
                        UpdateWorkflowRun(attempt, repository_name);
                        _context.SaveChanges();
                    }
                }
            } catch(Exception ex) {
                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(workflowRecordId, ex.Message.Split('\n').ToList()), workflowTimelineId, workflowRecordId);
                if(callingJob != null) {
                    callingJob.Workflowfinish.Invoke(callingJob, new WorkflowEventArgs { runid = runid, Success = false });
                } else {
                    //updateJobStatus.Invoke(new JobItem() { DisplayName = "Fatal Failure", Status = TaskResult.Failed }, TaskResult.Failed);
                    attempt.Result = TaskResult.Failed;
                    UpdateWorkflowRun(attempt, repository_name);
                    _context.SaveChanges();
                }
                return new HookResponse { repo = repository_name, run_id = runid, skipped = false, failed = true };
            } finally {
                if(!asyncProcessing) {
                    finishWorkflow();
                    _context.Dispose();
                }
            }
            return new HookResponse { repo = repository_name, run_id = runid, skipped = false };
        }

        private static int reqId = 0;

        private class SharedFileUpload {
            public CancellationTokenSource Token;
            public string Content;
        }

        [HttpDelete("fileup/{id}")]
        [HttpPost("fileup/{id}")]
        public async Task UploadFile(Guid id) {
            var sh = _cache.Get<SharedFileUpload>(id);
            if(string.Equals(Request.Method, "Post", StringComparison.OrdinalIgnoreCase)) {
                using(var reader = new StreamReader(Request.Body, Encoding.UTF8)) {
                    sh.Content = await reader.ReadToEndAsync();
                }
            }
            sh.Token.Cancel();
        }

        internal async Task<string> GetFile(long runid, string path, string repository = null) {
            var sh = new SharedFileUpload();
            sh.Token = new CancellationTokenSource(20 * 1000);
            Guid id = Guid.NewGuid();
            _cache.Set(id, sh);
            OnRepoDownload?.Invoke(runid, "/_apis/v1/Message/fileup/" + id, false, false, repository, "file", path);
            await Task.WhenAny(Task.Delay(-1, sh.Token.Token));
            _cache.Remove(id);
            return sh.Content;
        }

        internal bool TryGetFile(long runid, string path, out string content, string repository = null) {
            content = GetFile(runid, path, repository).GetAwaiter().GetResult();
            return content != null;
        }

        [HttpDelete("exists/{id}")]
        [HttpPost("exists/{id}")]
        public void UpRepoExists(Guid id) {
            var sh = _cache.Get<SharedFileUpload>(id);
            if(string.Equals(Request.Method, "Post", StringComparison.OrdinalIgnoreCase)) {
                sh.Content = "ok";
            }
            sh.Token.Cancel();
        }

        internal async Task<bool> RepoExists(long runid, string repository = null) {
            var sh = new SharedFileUpload();
            sh.Token = new CancellationTokenSource(20 * 1000);
            Guid id = Guid.NewGuid();
            _cache.Set(id, sh);
            OnRepoDownload?.Invoke(runid, "/_apis/v1/Message/exists/" + id, false, false, repository, "RepoExists", null);
            await Task.WhenAny(Task.Delay(-1, sh.Token.Token));
            _cache.Remove(id);
            return sh.Content != null;
        }

        private class SharedRepoCopy {
            public Channel<Task> Channel;
            public HttpResponse response;
        }

        [HttpPost("multipartup/{id}")]
        [HttpDelete("multipartup/{id}")]
        public async Task UploadMulti(Guid id) {
            var sh = _cache.Get<SharedRepoCopy>(id);
            Task task;
            if(string.Equals(Request.Method, "Post", StringComparison.OrdinalIgnoreCase)) {
                var type = Request.Headers["Content-Type"].First();
                var ntype = "multipart/form-data" + type.Substring("application/octet-stream".Length);
                sh.response.Headers["Content-Type"] = new StringValues(ntype);
                task = Request.Body.CopyToAsync(sh.response.Body);
            } else {
                sh.response.StatusCode = 404;
                task = Task.CompletedTask;
            }
            await sh.Channel.Writer.WriteAsync(task, HttpContext.RequestAborted);
            await task;
        }

        [HttpGet("multipart/{runid}")]
        public async Task GetMulti(long runid, [FromQuery] bool submodules, [FromQuery] bool nestedSubmodules, [FromQuery] string repositoryAndRef) {
            var channel = Channel.CreateBounded<Task>(1);
            var sh = new SharedRepoCopy();
            sh.Channel = channel;
            Guid id = Guid.NewGuid();
            _cache.Set(id, sh);
            OnRepoDownload?.Invoke(runid, "/_apis/v1/Message/multipartup/" + id, submodules, nestedSubmodules, repositoryAndRef, null, null);
            sh.response = Response;
            var task = await channel.Reader.ReadAsync(HttpContext.RequestAborted);
            await task;
            _cache.Remove(id);
        }

        [HttpPost("zipup/{id}")]
        [HttpDelete("zipup/{id}")]
        public async Task UploadZip(Guid id) {
            var sh = _cache.Get<SharedRepoCopy>(id);
            Task task;
            if(string.Equals(Request.Method, "Post", StringComparison.OrdinalIgnoreCase)) {
                task = Request.Body.CopyToAsync(sh.response.Body);
            } else {
                sh.response.StatusCode = 404;
                task = Task.CompletedTask;
            }
            await sh.Channel.Writer.WriteAsync(task, HttpContext.RequestAborted);
            await task;
        }

        [HttpGet("zipdown/{runid}")]
        public async Task GetZip(long runid, [FromQuery] bool submodules, [FromQuery] bool nestedSubmodules, [FromQuery] string repositoryAndRef, [FromQuery] bool noChildDir) {
            var channel = Channel.CreateBounded<Task>(1);
            var sh = new SharedRepoCopy();
            sh.Channel = channel;
            Guid id = Guid.NewGuid();
            _cache.Set(id, sh);
            OnRepoDownload?.Invoke(runid, "/_apis/v1/Message/zipup/" + id, false, false, repositoryAndRef, noChildDir ? "taskzip" : "zip", null);
            sh.response = Response;
            var task = await channel.Reader.ReadAsync(HttpContext?.RequestAborted ?? new CancellationTokenSource(20 * 1000).Token);
            await task;
            _cache.Remove(id);
        }

        [HttpPost("tarup/{id}")]
        [HttpDelete("tarup/{id}")]
        public async Task UploadTar(Guid id) {
            var sh = _cache.Get<SharedRepoCopy>(id);
            Task task;
            if(string.Equals(Request.Method, "Post", StringComparison.OrdinalIgnoreCase)) {
                task = Request.Body.CopyToAsync(sh.response.Body);
            } else {
                sh.response.StatusCode = 404;
                task = Task.CompletedTask;
            }
            await sh.Channel.Writer.WriteAsync(task, HttpContext.RequestAborted);
            await task;
        }

        [HttpGet("tardown/{runid}")]
        public async Task GetTar(long runid, [FromQuery] bool submodules, [FromQuery] bool nestedSubmodules, [FromQuery] string repositoryAndRef) {
            var channel = Channel.CreateBounded<Task>(1);
            var sh = new SharedRepoCopy();
            sh.Channel = channel;
            Guid id = Guid.NewGuid();
            _cache.Set(id, sh);
            OnRepoDownload?.Invoke(runid, "/_apis/v1/Message/tarup/" + id, false, false, repositoryAndRef, "tar", null);
            sh.response = Response;
            var task = await channel.Reader.ReadAsync(HttpContext.RequestAborted);
            await task;
            _cache.Remove(id);
        }

        [HttpGet("idtoken")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "AgentJob")]
        public async Task<IActionResult> GenerateIdToken([FromQuery] string sig, [FromQuery] string content, [FromQuery] string audience) {
            var rsa = RSA.Create(Startup.AccessTokenParameter);
            using(var memstr = new MemoryStream()) {
                using(var wr = new StreamWriter(memstr)) {
                    await wr.WriteLineAsync(content);
                    wr.Flush();
                    memstr.Seek(0, SeekOrigin.Begin);
                    if(!rsa.VerifyData(memstr, Base64UrlEncoder.DecodeBytes(sig), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) {
                        return NotFound();
                    }
                }
            }
            var mySecurityKey = new RsaSecurityKey(Startup.AccessTokenParameter);
            mySecurityKey.KeyId = Startup.KeyId;
            var myIssuer = ServerUrl;
            var myAudience = audience ?? new Uri(new Uri(GitServerUrl), User.FindFirstValue("repository").Split('/', 2)[0]).ToString();

            var tokenHandler = new JwtSecurityTokenHandler();
            List<Claim> claims = new List<Claim>
            {
                new Claim("jti", Guid.NewGuid().ToString()),
            };
            Dictionary<string, string> sclaims = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            if(sclaims.TryGetValue("environment", out var environment) && !string.IsNullOrEmpty(environment)) {
                claims.Add(new Claim("sub", $"repo:{User.FindFirstValue("repository")}:environment:{environment}"));
            } else {
                // It seems if we have no environment, the oidc token doesn't has the environment claim and the subject includes ref
                claims.Add(new Claim("sub", $"repo:{User.FindFirstValue("repository")}:ref:{User.FindFirstValue("ref") ?? ""}"));
            }
            foreach(var cl in sclaims) {
                if(cl.Value != null) {
                    claims.Add(new Claim(cl.Key, cl.Value));
                }
            }
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.RsaSha256)
            };

            var resources = new JobResources();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var stoken = tokenHandler.WriteToken(token);
            return await Ok(new { value = stoken });
        }
        private Func<bool, Job> queueJob(GitHub.DistributedTask.ObjectTemplating.ITraceWriter matrixJobTraceWriter, TemplateToken workflowDefaults, List<TemplateToken> workflowEnvironment, string displayname, MappingToken run, DictionaryContextData contextData, Guid jobId, Guid timelineId, string repo, string name, string workflowname, long runid, long runnumber, string[] secrets, string[] platform, bool localcheckout, long requestId, string Ref, string Sha, string wevent, string parentEvent, KeyValuePair<string, string>[] workflows = null, string statusSha = null, string parentId = null, Dictionary<string, List<Job>> finishedJobs = null, WorkflowRunAttempt attempt = null, JobItem ji = null, TemplateToken workflowPermissions = null, CallingJob callingJob = null, List<JobItem> dependentjobgroup = null, string selectedJob = null, string[] _matrix = null, WorkflowContext workflowContext = null, ISecretsProvider secretsProvider = null)
        {
            var workflowRef = callingJob?.WorkflowRef ?? callingJob?.WorkflowSha ?? Ref ?? Sha;
            var workflowSha = callingJob?.WorkflowSha ?? Sha;
            var workflowRepo = callingJob?.WorkflowRepo ?? repo;
            var workflowPath = callingJob?.WorkflowPath ?? workflowContext?.FileName ?? "";
            var job_workflow_full_path = $"{workflowRepo}/{workflowPath}";
            var job_workflow_ref = $"{job_workflow_full_path}@{workflowRef}";
            int fileContainerId = -1;
            Func<Func<bool, Job>> failJob = () => {
                var jid = jobId;
                var _job = new Job() { message = null, repo = repo, WorkflowRunAttempt = attempt, WorkflowIdentifier = name.PrefixJobIdIfNotNull(parentId), name = displayname, workflowname = workflowname, runid = runid, JobId = jid, RequestId = requestId, TimeLineId = timelineId, Matrix = CallingJob.ChildMatrix(callingJob?.Matrix, contextData["matrix"])?.ToJToken()?.ToString() };
                AddJob(_job);
                InvokeJobCompleted(new JobCompletedEvent() { JobId = jobId, Result = TaskResult.Failed, RequestId = requestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                return cancel => _job;
            };
            try {
                // Job permissions
                TemplateToken jobPermissions = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "permissions" select r).FirstOrDefault().Value?.AssertPermissionsValues($"jobs.{name}") ?? workflowPermissions;
                var calculatedPermissions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                bool isFork = false;
                {
                    var _hook = workflowContext.EventPayload;
                    var _ghook = _hook.ToObject<GiteaHook>();
                    isFork = !WriteAccessForPullRequestsFromForks && wevent == "pull_request" && _ghook?.pull_request?.Base?.Repo?.full_name != null && (_ghook?.pull_request?.Base?.Repo?.full_name != _ghook?.pull_request?.head?.Repo?.full_name);
                    calculatedPermissions["metadata"] = "read";
                    var stkn = jobPermissions as StringToken;
                    if(jobPermissions == null && callingJob?.Permissions != null) {
                        calculatedPermissions = callingJob.Permissions;
                    } else if(jobPermissions == null || stkn != null) {
                        if(stkn?.Value != "none") {
                            if(stkn?.Value == "read-all" || (isFork && stkn?.Value == "write-all")) {
                                calculatedPermissions["actions"] = "read";
                                calculatedPermissions["checks"] = "read";
                                calculatedPermissions["contents"] = "read";
                                calculatedPermissions["deployments"] = "read";
                                calculatedPermissions["id_token"] = "read";
                                calculatedPermissions["issues"] = "read";
                                calculatedPermissions["discussions"] = "read";
                                calculatedPermissions["packages"] = "read";
                                calculatedPermissions["pages"] = "read";
                                calculatedPermissions["pull_requests"] = "read";
                                calculatedPermissions["repository_projects"] = "read";
                                calculatedPermissions["security_events"] = "read";
                                calculatedPermissions["statuses"] = "read";
                            } else if(isFork) {
                                calculatedPermissions["contents"] = "read";
                            } else if(jobPermissions == null || stkn?.Value == "write-all") {
                                calculatedPermissions["actions"] = "write";
                                calculatedPermissions["checks"] = "write";
                                calculatedPermissions["contents"] = "write";
                                calculatedPermissions["deployments"] = "write";
                                if(stkn?.Value == "write-all") {
                                    // it seems github doesn't assign this permission by default, only by write-all or id-token: write
                                    calculatedPermissions["id_token"] = "write";
                                }
                                calculatedPermissions["issues"] = "write";
                                calculatedPermissions["discussions"] = "write";
                                calculatedPermissions["packages"] = "write";
                                calculatedPermissions["pages"] = "write";
                                calculatedPermissions["pull_requests"] = "write";
                                calculatedPermissions["repository_projects"] = "write";
                                calculatedPermissions["security_events"] = "write";
                                calculatedPermissions["statuses"] = "write";
                            }
                        }
                    } else {
                        foreach(var kv in jobPermissions.AssertMapping($"jobs.{name}.permissions Only string or Mapping expected")) {
                            var keyname = kv.Key.AssertString($"jobs.{name}.permissions mapping key").Value.Replace("-", "_");
                            var keyvalue = kv.Value.AssertString($"jobs.{name}.permissions mapping value").Value;
                            if(keyvalue == "none") {
                                calculatedPermissions.Remove(keyname);
                            } else {
                                calculatedPermissions[keyname] = isFork ? "read" : keyvalue;
                            }
                        }
                    }
                    if(callingJob?.Permissions != null && jobPermissions != null) {
                        var permissionErrors = new List<string>();
                        foreach(var kv in calculatedPermissions.ToArray()) {
                            if(callingJob.Permissions.TryGetValue(kv.Key, out var mPerm)) {
                                if(mPerm == "read" && kv.Value == "write") {
                                    calculatedPermissions[kv.Key] = "read";
                                    permissionErrors.Add($"{kv.Key} from {mPerm} to {kv.Value}");
                                }
                            } else {
                                calculatedPermissions.Remove(kv.Key);
                                permissionErrors.Add($"{kv.Key} from none to {kv.Value}");
                            }
                        }
                        if(permissionErrors.Any()) {
                            throw new Exception($"Elevating permissions {string.Join(", ", permissionErrors)} not permitted");
                        }
                    }
                }
                var environmentToken = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "env" select r).FirstOrDefault().Value;

                List<TemplateToken> environment = new List<TemplateToken>();
                if(workflowEnvironment != null) {
                    environment.AddRange(workflowEnvironment);
                }
                if (environmentToken != null)
                {
                    environment.Add(environmentToken);
                }
                // Update env to include job env
                {
                    var jobEnvCtx = new DictionaryContextData();
                    contextData["env"] = jobEnvCtx;
                    foreach(var envBlock in environment) {
                        var envTemplateContext = CreateTemplateContext(matrixJobTraceWriter, workflowContext, contextData);
                        var cEnv = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(envTemplateContext, "job-env", envBlock, 0, null, true);
                        // Best effort, don't check for errors
                        // templateContext.Errors.Check();
                        // Best effort, make job env available this is not available on github actions
                        if(cEnv is MappingToken genvToken) {
                            foreach(var kv in genvToken) {
                                if(kv.Key is StringToken key && kv.Value is StringToken val) {
                                    jobEnvCtx[key.Value] = new StringContextData(val.Value);
                                }
                            }
                        }
                    }
                }
                var rawSteps = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "steps" select r).FirstOrDefault().Value?.AssertSequence($"jobs.{name}.steps");
                TemplateContext templateContext;
                // Environment, only supported for regular jobs
                TemplateToken deploymentEnvironment = rawSteps != null ? (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "environment" select r).FirstOrDefault().Value : null;
                GitHub.DistributedTask.WebApi.ActionsEnvironmentReference deploymentEnvironmentValue = null;
                if(deploymentEnvironment != null) {
                    matrixJobTraceWriter.Info("{0}", $"Evaluate environment");
                    templateContext = CreateTemplateContext(matrixJobTraceWriter, workflowContext, contextData);
                    deploymentEnvironment = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, templateContext.Schema.Definitions.ContainsKey("environment") ? "environment" : "job-environment", deploymentEnvironment, 0, null, true);
                    templateContext.Errors.Check();
                    if(deploymentEnvironment != null) {
                        if(deploymentEnvironment is StringToken ename) {
                            deploymentEnvironmentValue = new GitHub.DistributedTask.WebApi.ActionsEnvironmentReference(ename.Value);
                        } else {
                            var mtoken = deploymentEnvironment.AssertMapping($"jobs.{name}.environment must be a mapping or string");
                            deploymentEnvironmentValue = new GitHub.DistributedTask.WebApi.ActionsEnvironmentReference((from r in mtoken where r.Key.AssertString($"jobs.{name}.environment mapping key").Value == "name" select r.Value).First().AssertString("name").Value);
                            deploymentEnvironmentValue.Url = (from r in mtoken where r.Key.AssertString($"jobs.{name}.environment mapping key").Value == "url" select r.Value).FirstOrDefault();
                        }
                    }
                }
                // Update Vars context for the selected environment, only used by old runners
                if(!string.IsNullOrEmpty(deploymentEnvironmentValue?.Name)) {
                    DictionaryContextData vars = new DictionaryContextData();
                    contextData["vars"] = vars;
                    var jobVars = secretsProvider.GetVariablesForEnvironment(deploymentEnvironmentValue.Name);
                    foreach(var kv in jobVars) {
                        vars[kv.Key] = new StringContextData(kv.Value);
                    }
                }
                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(ji.Id, new List<string>{ $"Evaluate job continueOnError" }), ji.TimelineId, ji.Id);
                templateContext = CreateTemplateContext(matrixJobTraceWriter, workflowContext, contextData);
                ji.ContinueOnError = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "continue-on-error" select GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "boolean-strategy-context", r.Value, 0, null, true)?.AssertBoolean($"jobs.{name}.continue-on-error be a boolean").Value).FirstOrDefault() ?? false;
                templateContext.Errors.Check();
                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(ji.Id, new List<string>{ $"Evaluate job timeoutMinutes" }), ji.TimelineId, ji.Id);
                templateContext = CreateTemplateContext(matrixJobTraceWriter, workflowContext, contextData);
                var timeoutMinutes = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "timeout-minutes" select GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "number-strategy-context", r.Value, 0, null, true)?.AssertNumber($"jobs.{name}.timeout-minutes be a number").Value).Append(360).First() ?? 360;
                templateContext.Errors.Check();
                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(ji.Id, new List<string>{ $"Evaluate job cancelTimeoutMinutes" }), ji.TimelineId, ji.Id);
                templateContext = CreateTemplateContext(matrixJobTraceWriter, workflowContext, contextData);
                var cancelTimeoutMinutes = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "cancel-timeout-minutes" select GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "number-strategy-context", r.Value, 0, null, true)?.AssertNumber($"jobs.{name}.cancel-timeout-minutes be a number").Value).Append(5).First() ?? 5;
                templateContext.Errors.Check();
                var jobConcurrency = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "concurrency" select r).FirstOrDefault().Value;
                if(jobConcurrency != null) {
                    matrixJobTraceWriter.Info("{0}", $"Evaluate job concurrency");
                    templateContext = CreateTemplateContext(matrixJobTraceWriter, workflowContext, contextData);
                    jobConcurrency = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "job-concurrency", jobConcurrency, 0, null, true);
                    templateContext.Errors.Check();
                }
                if(rawSteps == null) {
                    var rawUses = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "uses" select r).FirstOrDefault().Value?.AssertString($"jobs.{name}.uses");
                    var rawWith = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "with" select r).FirstOrDefault().Value?.AssertMapping($"jobs.{name}.with");
                    var rawSecrets = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "secrets" select r).FirstOrDefault().Value?.AssertJobSecrets($"jobs.{name}.secrets");
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
                        var gitRef = usesSegments.Length == 2 ? usesSegments[1] : string.Empty;
                        if (usesSegments.Length != 2 ||
                            pathSegments.Length < 2 ||
                            string.IsNullOrEmpty(pathSegments[0]) ||
                            string.IsNullOrEmpty(pathSegments[1]) ||
                            string.IsNullOrEmpty(gitRef))
                        {

                        }
                        else
                        {
                            var repositoryName = $"{pathSegments[0]}/{pathSegments[1]}";
                            var directoryPath = pathSegments.Length > 2 ? string.Join("/", pathSegments.Skip(2)) : string.Empty;

                            reference = new RepositoryPathReference
                            {
                                RepositoryType = RepositoryTypes.GitHub,
                                Name = repositoryName,
                                Ref = gitRef,
                                Path = directoryPath,
                            };
                        }
                    }
                    var _job = new Job() { message = null, repo = repo, WorkflowRunAttempt = attempt, WorkflowIdentifier = name.PrefixJobIdIfNotNull(parentId), name = displayname, workflowname = workflowname, runid = runid, JobId = jobId, RequestId = requestId, TimeLineId = timelineId, Matrix = CallingJob.ChildMatrix(callingJob?.Matrix, contextData["matrix"])?.ToJToken()?.ToString() };
                    AddJob(_job);
                    return cancel => {
                        if(cancel) {
                            matrixJobTraceWriter.Verbose("workflow_call cancelled successfully");
                            InvokeJobCompleted(new JobCompletedEvent() { JobId = jobId, Result = TaskResult.Canceled, RequestId = requestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                            return _job;
                        }
                        // Fix workflow doesn't wait for cancelled called workflows to finish, add dummy sessionid
                        _job.SessionId = Guid.NewGuid();
                        var clone = Clone();
                        Task.Run(async () => {
                            Action<string> failedtoInstantiateWorkflow = message => {
                                matrixJobTraceWriter.Error("Failed to instantiate Workflow: {0}", message);
                                clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = jobId, Result = TaskResult.Failed, RequestId = requestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                clone._context.Dispose();
                            };
                            if(reference == null) {
                                failedtoInstantiateWorkflow($"Invalid reference format: {uses.Value}");
                                return;
                            }
                            var calledWorkflowRepo = reference.RepositoryType == PipelineConstants.SelfAlias ? workflowRepo : reference.Name;
                            var calledWorkflowRef = reference.RepositoryType == PipelineConstants.SelfAlias ? workflowRef : reference.Ref;
                            var calledWorkflowSha = reference.RepositoryType == PipelineConstants.SelfAlias ? workflowSha : null;
                            Action<string, string, string> workflow_call = (filename, filecontent, sha) => {
                                var hook = workflowContext.EventPayload;
                                var ghook = hook.ToObject<GiteaHook>();

                                var templateContext = CreateTemplateContext(matrixJobTraceWriter, workflowContext, contextData);
                                var eval = rawWith != null ? GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, templateContext.Schema.Definitions.ContainsKey("job-with") ? "job-with" : "workflow-job-with", rawWith, 0, null, true) : null;
                                templateContext.Errors.Check();
                                // Inherit secrets: https://github.com/github/docs/blob/5ffcd4d90f2529fbe383b51edb3a39db4a1528de/content/actions/using-workflows/reusing-workflows.md#using-inputs-and-secrets-in-a-reusable-workflow
                                bool inheritSecrets = rawSecrets?.Type == TokenType.String && rawSecrets.AssertString($"jobs.{name}.secrets").Value == "inherit";
                                var reuseableSecretsProvider = inheritSecrets ? secretsProvider : new ReusableWorkflowSecretsProvider(name, secretsProvider, rawSecrets?.AssertMapping($"jobs.{name}.secrets"), contextData, workflowContext, environment);
                                // Based on https://github.com/actions/runner/issues/1976#issuecomment-1172940227, dispatchInputs are merged into the workflow_call inputs context
                                var dispatchInputs = MergedInputs ? callingJob?.DispatchInputs ?? contextData["inputs"]?.AssertDictionary("") ?? new DictionaryContextData() : new DictionaryContextData();
                                var mergedInputs = dispatchInputs.Clone().AssertDictionary("");
                                if(eval != null) {
                                    foreach(var kv in eval.ToContextData().AssertDictionary("")) {
                                        mergedInputs[kv.Key] = kv.Value;
                                    }
                                }
                                var callerJob = new CallingJob() { Name = displayname, Event = wevent, DispatchInputs = dispatchInputs, Inputs = mergedInputs, Workflowfinish = (callerJob, e) => {
                                    if(callerJob.RanJob) {
                                        if(callingJob != null) {
                                            callingJob.RanJob = true;
                                        }
                                        Array.ForEach(finishedJobs.ToArray(), fjobs => {
                                            foreach(var djob in dependentjobgroup) {
                                                if(djob.Dependencies != null && (string.Equals(fjobs.Key, djob.name, StringComparison.OrdinalIgnoreCase) || fjobs.Key.StartsWith(djob.name + "/", StringComparison.OrdinalIgnoreCase))) {
                                                    foreach(var dep in djob.Dependencies) {
                                                        if(string.Equals(dep.Key, name, StringComparison.OrdinalIgnoreCase)) {
                                                            finishedJobs.Remove(fjobs.Key);
                                                            return;
                                                        }
                                                    }
                                                }
                                            }
                                        });
                                    }
                                    clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = jobId, Result = e.Success ? TaskResult.Succeeded : TaskResult.Failed, RequestId = requestId, Outputs = e.Outputs ?? new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                }, Id = name.PrefixJobIdIfNotNull(parentId), ForceCancellationToken = workflowContext.ForceCancellationToken, CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(/* Cancellable even if no pseudo job is created */ ji.Cancel.Token, /* Cancellation of pseudo job */ _job.CancelRequest.Token).Token, TimelineId = ji.TimelineId, RecordId = ji.Id, WorkflowName = workflowname, WorkflowRunName = workflowContext.WorkflowRunName, WorkflowFileName = workflowContext.FileName, Permissions = jobPermissions != null ? calculatedPermissions : null /* If permissions are unspecified by the caller you can elevate id-token: write in a reusabe workflow */, ProvidedInputs = rawWith == null || rawWith.Type == TokenType.Null ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : (from entry in rawWith.AssertMapping($"jobs.{ji.name}.with") select entry.Key.AssertString("jobs.{ji.name}.with mapping key").Value).ToHashSet(StringComparer.OrdinalIgnoreCase), ProvidedSecrets = inheritSecrets ? null : rawSecrets == null || rawSecrets.Type == TokenType.Null ? new HashSet<string>(StringComparer.OrdinalIgnoreCase) : (from entry in rawSecrets.AssertMapping($"jobs.{ji.name}.secrets") select entry.Key.AssertString("jobs.{ji.name}.secrets mapping key").Value).ToHashSet(StringComparer.OrdinalIgnoreCase), WorkflowPath = filename, WorkflowRef = calledWorkflowRef, WorkflowRepo = calledWorkflowRepo, WorkflowSha = sha, Depth = (callingJob?.Depth ?? 0) + 1, JobConcurrency = jobConcurrency, Matrix = CallingJob.ChildMatrix(callingJob?.Matrix, contextData["matrix"])};
                                var fjobs = finishedJobs?.Where(kv => kv.Key.StartsWith(name + "/", StringComparison.OrdinalIgnoreCase))?.ToDictionary(kv => kv.Key.Substring(name.Length + 1), kv => kv.Value.Where(val => (val.MatrixContextData[callingJob?.Depth ?? 0]?.ToTemplateToken() ?? new NullToken(null, null, null)).DeepEquals(contextData["matrix"]?.ToTemplateToken() ?? new NullToken(null, null, null))).ToList(), StringComparer.OrdinalIgnoreCase);
                                var sjob = TryParseJobSelector(selectedJob, out var cjob, out _, out var cselector) && string.Equals(name, cjob, StringComparison.OrdinalIgnoreCase) ? cselector : null;
                                if(reusableWorkflowInheritEnv) {
                                    for(int i = 0; i < environment.Count; i++) {
                                        if(environment[i] is MappingToken menv) {
                                            for(int j = 0; j < menv.Count; j++) {
                                                if(menv[j].Value is BasicExpressionToken menvexpr) {
                                                    menv[j] = new KeyValuePair<ScalarToken, TemplateToken>(menv[j].Key, new BasicExpressionToken(null, null, null, new ExpressionParser().ValidateSyntax(menvexpr.Expression, matrixJobTraceWriter.ToExpressionTraceWriter()).PartiallyEvaluate(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "inputs", contextData["inputs"] }, { "matrix", contextData["matrix"] }, { "strategy", contextData["strategy"] } }).ConvertToExpression()));
                                                }
                                            }
                                        }
                                    }
                                }
                                clone.ConvertYaml2(filename, filecontent, repo, GitServerUrl, ghook, hook, "workflow_call", sjob, false, null, null, _matrix, platform, localcheckout, runid, runnumber, Ref, Sha, callingJob: callerJob, workflows, attempt, statusSha: statusSha, finishedJobs: fjobs, secretsProvider: reuseableSecretsProvider, parentEnv: reusableWorkflowInheritEnv ? environment : null);
                            };
                            if((string.Equals(calledWorkflowRepo, repo, StringComparison.OrdinalIgnoreCase) && (calledWorkflowRef == Ref || ("refs/heads/" + calledWorkflowRef) == Ref || ("refs/tags/" + calledWorkflowRef) == Ref) || calledWorkflowRef == Sha) && (workflows != null && workflows.ToDictionary(v => v.Key, v => v.Value).TryGetValue(reference.Path, out var _content) || localcheckout && TryGetFile(runid, reference.Path, out _content))) {
                                try {
                                    workflow_call(reference.Path, _content, Sha);
                                } catch (Exception ex) {
                                    failedtoInstantiateWorkflow(ex.Message);
                                }
                            } else if(localcheckout && TryGetFile(runid, reference.Path, out _content, $"{calledWorkflowRepo}@{calledWorkflowRef}")) {
                                try {
                                    workflow_call(reference.Path, _content, calledWorkflowRef);
                                } catch (Exception ex) {
                                    failedtoInstantiateWorkflow(ex.Message);
                                }
                            } else {
                                var client = new HttpClient();
                                client.DefaultRequestHeaders.Add("accept", "application/json");
                                client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("runner", string.IsNullOrEmpty(GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version) ? "0.0.0" : GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version));
                                string githubAppToken = null;
                                // Ref is better than nothing
                                string workflow_sha = calledWorkflowSha ?? calledWorkflowRef;
                                try {
                                    string token = null;
                                    if(secretsProvider?.GetReservedSecrets()?.TryGetValue("GITHUB_TOKEN", out var providedToken) == true && !string.IsNullOrEmpty(providedToken)) {
                                        token = providedToken;
                                    } else if(!string.IsNullOrEmpty(GITHUB_TOKEN)) {
                                        token = GITHUB_TOKEN;
                                    } else {
                                        if(AllowPrivateActionAccess) {
                                            githubAppToken = await CreateGithubAppToken(calledWorkflowRepo);
                                        }
                                        if(githubAppToken == null) {
                                            githubAppToken = await CreateGithubAppToken(repo);
                                        }
                                        if(githubAppToken != null) {
                                            token = githubAppToken;
                                        }
                                    }
                                    if(!string.IsNullOrEmpty(token) && calledWorkflowSha == null) {
                                        client.DefaultRequestHeaders.Add("Authorization", $"token {token}");
                                        var urlBuilder = new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{calledWorkflowRepo}/commits"));
                                        urlBuilder.Query = $"?sha={Uri.EscapeDataString(calledWorkflowRef)}&page=1&limit=1&per_page=1";
                                        var resolvedSha = await client.GetAsync(urlBuilder.ToString());
                                        if(resolvedSha.StatusCode == System.Net.HttpStatusCode.OK) {
                                            var content = await resolvedSha.Content.ReadAsStringAsync();
                                            var o = JsonConvert.DeserializeObject<MessageController.GitCommit[]>(content)[0];
                                            if(!string.IsNullOrEmpty(o.Sha)) {
                                                workflow_sha = o.Sha;
                                            }
                                        }
                                    }
                                    var url = new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{calledWorkflowRepo}/contents/{Uri.EscapeDataString(reference.Path)}"));
                                    url.Query = $"ref={Uri.EscapeDataString(workflow_sha)}";
                                    var res = await client.GetAsync(url.ToString());
                                    if(res.StatusCode == System.Net.HttpStatusCode.OK) {
                                        var content = await res.Content.ReadAsStringAsync();
                                        var item = Newtonsoft.Json.JsonConvert.DeserializeObject<UnknownItem>(content);
                                        {
                                            try {
                                                var fileRes = await client.GetAsync(item.download_url);
                                                var filecontent = await fileRes.Content.ReadAsStringAsync();
                                                workflow_call(item.path, filecontent, workflow_sha);
                                            } catch (Exception ex) {
                                                failedtoInstantiateWorkflow(ex.Message);
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
                        });
                        return _job;
                    };
                }
                List<Step> steps;
                {
                    templateContext = CreateTemplateContext(matrixJobTraceWriter, workflowContext, contextData);
                    var evaluatedSteps = TemplateEvaluator.Evaluate(templateContext, "steps", rawSteps, 0, null, true) ?? rawSteps;
                    templateContext.Errors.Check();
                    steps = PipelineTemplateConverter.ConvertToSteps(templateContext, evaluatedSteps);
                    templateContext.Errors.Check();
                }
                var runsOn = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "runs-on" select r).FirstOrDefault().Value;
                HashSet<string> runsOnMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if(runsOn != null) {
                    matrixJobTraceWriter.Info("{0}", "Evaluate runs-on");
                    templateContext = CreateTemplateContext(matrixJobTraceWriter, workflowContext, contextData);
                    var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.RunsOn, runsOn, 0, null, true);
                    templateContext.Errors.Check();
                    var runsOnLabels = eval is MappingToken mt ? (from r in mt where r.Key.AssertString($"jobs.{name}.runs-on mapping key").Value == "labels" select r).FirstOrDefault().Value : eval;
                    if(runsOnLabels != null) {
                        runsOnMap.UnionWith(from t in runsOnLabels.AssertScalarOrSequence($"jobs.{name}.runs-on") select t.AssertString($"jobs.{name}.runs-on.*").Value);
                    }
                } else {
                    throw new Exception($"jobs.{name}.runs-on empty set of runner labels");
                }

                // Jobcontainer
                TemplateToken jobContainer = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "container" select r).FirstOrDefault().Value;

                foreach(var p in platform.Reverse()) {
                    var eq = p.IndexOf('=');
                    var set = p.Substring(0, eq).Split(",").ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if(runsOnMap.IsSubsetOf(set) && p.Length > (eq + 1)) {
                        if(p[eq + 1] == '-') {
                            if(p.Length == (eq + 2)) {
                                runsOnMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { };
                            } else {
                                runsOnMap = p.Substring(eq + 2, p.Length - (eq + 2)).Split(',').ToHashSet(StringComparer.OrdinalIgnoreCase);
                            }
                        } else {
                            runsOnMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "self-hosted", "container-host" };
                            if(jobContainer == null) {
                                // Set just the container property of the workflow, the runner will use it
                                jobContainer = new StringToken(null, null, null, p.Substring(eq + 1, p.Length - (eq + 1)));
                            }
                            // If jobContainer != null, nothing we need to do other than use a special runner
                        }
                        break;
                    }
                }

                foreach (var step in steps)
                {
                    step.Id = Guid.NewGuid();
                    if(!string.IsNullOrEmpty((step as ActionStep)?.ContextName)) {
                        step.Name = (step as ActionStep).ContextName;
                    }
                }

                // Jobservicecontainer
                TemplateToken jobServiceContainer = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "services" select r).FirstOrDefault().Value;
                // Job outputs
                TemplateToken outputs = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "outputs" select r).FirstOrDefault().Value;
                var defaultToken = (from r in run where r.Key.AssertString($"jobs.{name} mapping key").Value == "defaults" select r).FirstOrDefault().Value;

                List<TemplateToken> jobDefaults = new List<TemplateToken>();
                if(workflowDefaults != null) {
                    jobDefaults.Add(workflowDefaults);
                }
                if (defaultToken != null) {
                    jobDefaults.Add(defaultToken);
                }

                if(!string.IsNullOrEmpty(OnQueueJobProgram)) {
                    var startupInfo = new ProcessStartInfo(OnQueueJobProgram, OnQueueJobArgs ?? "") { CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true, StandardErrorEncoding = Encoding.UTF8, StandardOutputEncoding = Encoding.UTF8 };
                    startupInfo.Environment["RUNNER_SERVER_PAYLOAD"] = JsonConvert.SerializeObject(new {
                        ContextData = contextData.ToJToken(),
                        Repository = repo,
                        WorkflowFileName = workflowContext?.FileName ?? workflowname,
                        Job = name,
                        JobDisplayName = displayname,
                        Environment = deploymentEnvironmentValue?.Name ?? "",
                        Labels = runsOnMap.ToArray(),
                    }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}});
                    Task.Run(() => {
                        try {
                            using(var proc = new Process()) {
                                proc.StartInfo = startupInfo;
                                proc.OutputDataReceived += (s, e) => matrixJobTraceWriter.Info("OnQueueJob: {0}", e.Data);
                                proc.ErrorDataReceived += (s, e) => matrixJobTraceWriter.Info("OnQueueJob Error: {0}", e.Data);
                                proc.Start();
                                proc.BeginOutputReadLine();
                                proc.BeginErrorReadLine();
                                proc.WaitForExit();
                                matrixJobTraceWriter.Info("OnQueueJob exited with code: {0}", proc.ExitCode);
                            }
                        } catch(Exception ex) {
                            matrixJobTraceWriter.Info("Failed to start OnQueueJob: {0}", ex.Message);
                        }
                    });
                } else if(!QueueJobsWithoutRunner) {
                    var sessionsfreeze = sessions.ToArray();
                    var x = (from s in sessionsfreeze where s.Value.Agent?.TaskAgent?.Labels != null && runsOnMap.IsSubsetOf(from l in s.Value.Agent.TaskAgent.Labels select l.Name) select s.Key).FirstOrDefault();
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
                            if(s.Value.Agent?.TaskAgent?.Labels == null) {
                                continue;
                            }
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
                        matrixJobTraceWriter.Info("{0}", $"No runner is registered for the requested runs-on labels: [{b.ToString()}], please register and run a self-hosted runner with at least these labels. Available runner: {(i == 0 ? "No Runner available!" : b2.ToString())}");
                        return failJob();
                    }
                }
                string runnerToken = null;
                Job job = null;
                job = new Job() { message = (caller, apiUrl) => {
                    try {
                        var cleanupClone = caller.Clone();
                        job.CleanUp?.Invoke();
                        job.CleanUp = () => {
                            if(!string.IsNullOrEmpty(runnerToken)) {
                                try {
                                    DeleteGithubAppToken(runnerToken);
                                } catch {

                                }
                            }
                            if(fileContainerId != -1) {
                                cleanupClone._context.ArtifactFileContainer.Remove(cleanupClone._context.ArtifactFileContainer.Find(fileContainerId));
                                cleanupClone._context.SaveChanges();
                            }
                            cleanupClone._context.Dispose();
                        };

                        var auth = new GitHub.DistributedTask.WebApi.EndpointAuthorization() { Scheme = GitHub.DistributedTask.WebApi.EndpointAuthorizationSchemes.OAuth };
                        var mySecurityKey = new RsaSecurityKey(Startup.AccessTokenParameter);

                        var myIssuer = "http://githubactionsserver";
                        var myAudience = "http://githubactionsserver";

                        var tokenHandler = new JwtSecurityTokenHandler();
                        var hook = workflowContext.EventPayload;
                        var ghook = hook.ToObject<GiteaHook>();
                        var resources = new JobResources();
                        var systemVssConnection = new GitHub.DistributedTask.WebApi.ServiceEndpoint() { Name = WellKnownServiceEndpointNames.SystemVssConnection, Authorization = auth, Url = new Uri(apiUrl) };
                        systemVssConnection.Data["CacheServerUrl"] = apiUrl;
                        var feedStreamUrl = new UriBuilder(new Uri(new Uri(apiUrl), $"_apis/v1/TimeLineWebConsoleLog/feedstream/{Uri.EscapeDataString(timelineId.ToString())}/ws"));
                        feedStreamUrl.Scheme = feedStreamUrl.Scheme == "http" ? "ws" : "wss";
                        systemVssConnection.Data["FeedStreamUrl"] = feedStreamUrl.ToString();
                        systemVssConnection.Data["ResultsServiceUrl"] = apiUrl;
                        if(calculatedPermissions.TryGetValue("id_token", out var p_id_token) && p_id_token == "write") {
                            var environment = deploymentEnvironmentValue?.Name ?? ("");
                            var claims = new Dictionary<string, string>();
                            claims["repository"] = repo;
                            claims["run_attempt"] = attempt?.Attempt.ToString() ?? "1";
                            claims["ref"] = Ref;
                            claims["run_id"] = attempt?.WorkflowRun?.Id.ToString() ?? "0";
                            claims["run_number"] = attempt?.WorkflowRun?.Id.ToString() ?? "0";
                            if(!string.IsNullOrEmpty(environment)) {
                                claims["environment"] = environment;
                            }
                            claims["head_ref"] = contextData.GetPath("github", "head_ref")?.ToString();
                            claims["base_ref"] = contextData.GetPath("github", "base_ref")?.ToString();
                            claims["actor"] = contextData.GetPath("github", "actor")?.ToString();
                            claims["actor_id"] = contextData.GetPath("github", "actor_id")?.ToString();
                            claims["repository_id"] = contextData.GetPath("github", "repository_id")?.ToString();
                            claims["repository_owner"] = contextData.GetPath("github", "repository_owner")?.ToString();
                            claims["repository_owner_id"] = contextData.GetPath("github", "repository_owner_id")?.ToString();
                            claims["workflow"] = contextData.GetPath("github", "workflow")?.ToString();
                            claims["event_name"] = contextData.GetPath("github", "event_name")?.ToString();
                            claims["ref_type"] = contextData.GetPath("github", "ref_type")?.ToString();
                            claims["job_workflow_ref"] = job_workflow_ref;
                            claims["job_workflow_sha"] = callingJob?.WorkflowSha ?? Sha;
                            claims["sha"] = Sha;
                            claims["repository_visibility"] = contextData.GetPath("github", "repository_visibility")?.ToString();
                            claims["workflow_ref"] = contextData.GetPath("github", "workflow_ref")?.ToString();
                            claims["workflow_sha"] = contextData.GetPath("github", "workflow_sha")?.ToString();
                            var content = JsonConvert.SerializeObject(claims);
                            using(var rsa = RSA.Create(Startup.AccessTokenParameter))
                            using(var memstr = new MemoryStream()) {
                                using(var wr = new StreamWriter(memstr)) {
                                    wr.WriteLineAsync(content);
                                    wr.Flush();
                                    memstr.Seek(0, SeekOrigin.Begin);
                                    systemVssConnection.Data["GenerateIdTokenUrl"] = new Uri(new Uri(apiUrl), $"_apis/v1/Message/idtoken?sig={Uri.EscapeDataString(Base64UrlEncoder.Encode(rsa.SignData(memstr, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)))}&content={Uri.EscapeDataString(content)}").ToString();
                                }
                            }
                        }
                        resources.Endpoints.Add(systemVssConnection);

                        // Ensure secrets.github_token is available in the runner
                        VariableValue github_token = new VariableValue(GITHUB_TOKEN_NONE, true);
                        if(calculatedPermissions.TryGetValue("contents", out var contents)) {
                            switch(contents) {
                                case "read":
                                    github_token = new VariableValue(GITHUB_TOKEN_READ_ONLY, true);
                                    break;                                    
                                case "write":
                                    github_token = new VariableValue(GITHUB_TOKEN, true);
                                    break;
                            }
                        }
                        var variables = new Dictionary<string, GitHub.DistributedTask.WebApi.VariableValue>(StringComparer.OrdinalIgnoreCase);
                        variables.Add("system.github.job", new VariableValue(name, false));
                        // actions/runner throws if ${{ inputs == null }}, due to AssertDictionary without null check
                        if(contextData.GetPath("inputs") != null) {
                            variables.Add("system.workflowFileFullPath", new VariableValue(job_workflow_full_path, false));
                        }
                        variables.Add("system.workflowFilePath", new VariableValue(workflowPath, false));
                        Regex special = new Regex("[^a-zA-Z_\\-]");
                        variables.Add("system.phaseDisplayName", new VariableValue(special.Replace($"{name}", "-"), false));
                        variables.Add("system.runnerGroupName", new VariableValue("Default", false));
                        variables.Add("system.runner.lowdiskspacethreshold", new VariableValue("100", false)); // actions/runner warns if free space is less than 100MB
                        variables.Add("DistributedTask.NewActionMetadata", new VariableValue("true", false));
                        variables.Add("DistributedTask.EnableCompositeActions", new VariableValue("true", false));
                        variables.Add("DistributedTask.EnhancedAnnotations", new VariableValue("true", false));
                        variables.Add("DistributedTask.UploadStepSummary", new VariableValue("true", false));
                        variables.Add("DistributedTask.AddWarningToNode12Action", new VariableValue("true", false));
                        variables.Add("DistributedTask.AllowRunnerContainerHooks", new VariableValue("true", false));
                        variables.Add("DistributedTask.DeprecateStepOutputCommands", new VariableValue("true", false));
                        variables.Add("DistributedTask.ForceGithubJavascriptActionsToNode16", new VariableValue("true", false)); // https://github.blog/changelog/2023-05-04-github-actions-all-actions-will-run-on-node16-instead-of-node12/
                        variables.Add("actions_uses_cache_service_v2", new VariableValue("true", false)); // https://github.com/actions/toolkit/discussions/1890
                        // For actions/upload-artifact@v1, actions/download-artifact@v1
                        variables.Add(SdkConstants.Variables.Build.BuildId, new VariableValue(runid.ToString(), false));
                        variables.Add(SdkConstants.Variables.Build.BuildNumber, new VariableValue(runid.ToString(), false));
                        var resp = cleanupClone.CreateArtifactContainer(runid, attempt.Attempt, new CreateActionsStorageArtifactParameters() { Name = $"Artifact of {displayname}",  }).GetAwaiter().GetResult();
                        fileContainerId = resp.Id;
                        variables.Add(SdkConstants.Variables.Build.ContainerId, new VariableValue(resp.Id.ToString(), false));
                        bool sendUserVariables = !isFork || !workflowContext.HasFeature("system.runner.server.NoVarsForPRFromFork");
                        // Only send referenced action variables
                        var allvars = secretsProvider.GetVariablesForEnvironment(deploymentEnvironmentValue?.Name).ToArray();
                        var varKeys = (from kv in allvars select $"vars.{kv.Key}").ToArray();
                        var referencedVars = (from tmplBlock in (workflowEnvironment?.Append(run)?.ToArray() ?? new [] { run }) select tmplBlock.CheckReferencesContext(varKeys, templateContext.Flags)).ToArray();
                        var varsContext = new DictionaryContextData();
                        for(int i = 0; i < allvars.Length; i++) {
                            // Only send referenced or reserved variables
                            if(IsReservedVariable(allvars[i].Key)) {
                                variables[allvars[i].Key] = new VariableValue(allvars[i].Value, false);
                            } else if(sendUserVariables && referencedVars.Any(rs => rs != null && rs[i]) || IsActionsDebugVariable(allvars[i].Key)) {
                                varsContext[allvars[i].Key] = new StringContextData(allvars[i].Value);
                            }
                        }
                        // Pass action user variables
                        contextData["vars"] = varsContext;
                        bool sendUserSecrets = !isFork;
                        // Only send referenced action secrets
                        var allsecrets = secretsProvider.GetSecretsForEnvironment(matrixJobTraceWriter, deploymentEnvironmentValue?.Name).ToArray();
                        var secretKeys = (from kv in allsecrets select $"secrets.{kv.Key}").ToArray();
                        var referencedSecrets = (from tmplBlock in (workflowEnvironment?.Append(run)?.ToArray() ?? new [] { run }) select tmplBlock.CheckReferencesContext(secretKeys, templateContext.Flags)).ToArray();
                        for(int i = 0; i < allsecrets.Length; i++) {
                            // Only send referenced or reserved secrets
                            if(sendUserSecrets && referencedSecrets.Any(rs => rs != null && rs[i]) || IsReservedVariable(allsecrets[i].Key) || IsActionsDebugVariable(allsecrets[i].Key)) {
                                variables[allsecrets[i].Key] = new VariableValue(allsecrets[i].Value, true);
                            }
                        }
                        if(!string.IsNullOrEmpty(github_token?.Value) || variables.TryGetValue("github_token", out github_token) && !string.IsNullOrEmpty(github_token.Value)) {
                            variables["github_token"] = variables["system.github.token"] = github_token;
                        } else {
                            var ownerAndRepo = repo.Split("/", 2);
                            var ghappPerm = new Dictionary<string, string>(calculatedPermissions, StringComparer.OrdinalIgnoreCase);
                            // id_token is provided by the systemvssconnection, not by github_token
                            ghappPerm.Remove("id_token");
                            runnerToken = CreateGithubAppToken(repo, new { Permissions = ghappPerm }).GetAwaiter().GetResult();
                            if(runnerToken != null) {
                                variables["github_token"] = variables["system.github.token"] = new VariableValue(runnerToken, true);
                                variables["system.github.token.permissions"] = new VariableValue(Newtonsoft.Json.JsonConvert.SerializeObject(calculatedPermissions), false);
                            }
                        }
                        var tokenDescriptor = new SecurityTokenDescriptor
                        {
                            Subject = new ClaimsIdentity(new Claim[]
                            {
                                new Claim("Agent", "job"),
                                new Claim("repository", repo),
                                new Claim("ref", Ref),
                                new Claim("defaultRef", "refs/heads/" + (ghook?.repository?.default_branch ?? "main")),
                                new Claim("attempt", attempt.Attempt.ToString()),
                                new Claim("artifactsMinAttempt", attempt.ArtifactsMinAttempt.ToString()),
                                new Claim("localcheckout", localcheckout ? "actions/checkout" : ""),
                                new Claim("runid", runid.ToString()),
                                new Claim("github_token", variables.TryGetValue("github_token", out var ghtoken) ? ghtoken.Value : ""),
                                new Claim("scp", $"Actions.Results:{runid}:{job.JobId}"),
                                new Claim("ac", "[{\"Scope\": \"\", \"Permission\": 3}]")
                            }),
                            Expires = DateTime.UtcNow.AddMinutes(timeoutMinutes + 10),
                            Issuer = myIssuer,
                            Audience = myAudience,
                            SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.RsaSha256)
                        };

                        var token = tokenHandler.CreateToken(tokenDescriptor);
                        var stoken = tokenHandler.WriteToken(token);
                        auth.Parameters.Add(GitHub.DistributedTask.WebApi.EndpointAuthorizationParameters.AccessToken, stoken);
                        var req = new AgentJobRequestMessage(new GitHub.DistributedTask.WebApi.TaskOrchestrationPlanReference() { PlanType = "free", ContainerId = 0, ScopeIdentifier = Guid.NewGuid(), PlanGroup = "free", PlanId = Guid.NewGuid(), Owner = new GitHub.DistributedTask.WebApi.TaskOrchestrationOwner() { Id = 0, Name = "Community" }, Version = 12 }, new GitHub.DistributedTask.WebApi.TimelineReference() { Id = timelineId, Location = null, ChangeId = 1 }, jobId, displayname, name, jobContainer, jobServiceContainer, environment, variables, new List<GitHub.DistributedTask.WebApi.MaskHint>(), resources, contextData, new WorkspaceOptions(), steps.Cast<JobStep>(), workflowContext.FileTable, outputs, jobDefaults, deploymentEnvironmentValue, null );
                        req.RequestId = requestId;
                        return req;
                    } catch(Exception ex) {
                        matrixJobTraceWriter.Error("{0}", $"Internal Error: {ex.Message}, {ex.StackTrace}");
                        Console.WriteLine($"Internal Error: {ex.Message}, {ex.StackTrace}");
                        return null;
                    }
                }, repo = repo, WorkflowRunAttempt = attempt, WorkflowIdentifier = name.PrefixJobIdIfNotNull(parentId), name = displayname, workflowname = workflowname, runid = runid, /* SessionId = sessionId,  */JobId = jobId, RequestId = requestId, TimeLineId = timelineId, TimeoutMinutes = timeoutMinutes, CancelTimeoutMinutes = cancelTimeoutMinutes, ContinueOnError = ji.ContinueOnError, Matrix = CallingJob.ChildMatrix(callingJob?.Matrix, contextData["matrix"])?.ToJToken()?.ToString() };
                AddJob(job);
                //ConcurrencyGroup
                string group = null;
                bool cancelInprogress = false;
                if(jobConcurrency != null) {
                    if(jobConcurrency is StringToken stkn) {
                        group = stkn.Value;
                    } else {
                        var cmapping = jobConcurrency.AssertMapping($"jobs.{name}.concurrency must be a string or mapping");
                        group = (from r in cmapping where r.Key.AssertString($"jobs.{name}.concurrency mapping key").Value == "group" select r).FirstOrDefault().Value?.AssertString($"jobs.{name}.concurrency.group")?.Value;
                        cancelInprogress = (from r in cmapping where r.Key.AssertString($"jobs.{name}.concurrency mapping key").Value == "cancel-in-progress" select r).FirstOrDefault().Value?.AssertBoolean($"jobs.{name}.concurrency.cancel-in-progress")?.Value ?? cancelInprogress;
                    }
                    var concurrencyGroupNameLength = System.Text.Encoding.UTF8.GetByteCount(group ?? "");
                    if(MaxConcurrencyGroupNameLength >= 0 && concurrencyGroupNameLength > MaxConcurrencyGroupNameLength) {
                        throw new Exception($"jobs.{name}.concurrency: The specified concurrency group name with length {concurrencyGroupNameLength} exceeds the maximum allowed length of {MaxConcurrencyGroupNameLength}");
                    }
                }
                return cancel => {
                    if(cancel || job.CancelRequest.IsCancellationRequested) {
                        job.CancelRequest.Cancel();
                        InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                    } else {
                        Action _queueJob = () => {
                            try {
                                using(var scope = _provider.CreateScope()) {
                                    var queueService = scope.ServiceProvider.GetService<IQueueService>();

                                    if(queueService != null) {
                                        job.SessionId = Guid.NewGuid();
                                        queueService.PickJob(job.message.Invoke(this, this.ServerUrl + "/"), job.CancelRequest.Token, new string[0]);
                                    } else
                                    {
                                        queueJob(runsOnMap, job);
                                    }
                                }
                            } catch(ObjectDisposedException) {
                                // The scope has been disposed, might happen due to rerunning jobs
                                queueJob(runsOnMap, job);
                            }

                            void queueJob(HashSet<string> runsOnMap, Job job)
                            {
                                Channel<Job> queue = jobqueue.GetOrAdd(runsOnMap, (a) => Channel.CreateUnbounded<Job>());

                                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Queued Job: {job.name} for queue {string.Join(",", runsOnMap)}" }), job.TimeLineId, job.JobId);
                                queue.Writer.WriteAsync(job);
                            }
                        };
                        if(string.IsNullOrEmpty(group)) {
                            _queueJob();
                        } else {
                            var clone = Clone();
                            var key = $"{repo}/{group}";
                            while(true) {
                                ConcurrencyGroup cgroup = concurrencyGroups.GetOrAdd(key, name => new ConcurrencyGroup() { Key = name });
                                lock(cgroup) {
                                    if(concurrencyGroups.TryGetValue(key, out var _cgroup) && cgroup != _cgroup) {
                                        continue;
                                    }
                                    ConcurrencyEntry centry = new ConcurrencyEntry();
                                    Action finish = () => {
                                        cgroup.FinishRunning(centry);
                                        clone._context.Dispose();
                                    };
                                    centry.Run = () => {
                                        if(job.CancelRequest.IsCancellationRequested) {
                                            _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Job was cancelled, while it was pending in the concurrency group" }), job.TimeLineId, job.JobId);
                                            finish();
                                        } else {
                                            FinishJobController.OnJobCompleted += evdata => {
                                                if(evdata.JobId == job.JobId) {
                                                    finish();
                                                }
                                            };
                                            _queueJob();
                                        }
                                    };
                                    centry.CancelPending = () => {
                                        _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Job was cancelled by another workflow or job, while it was pending in the concurrency group" }), job.TimeLineId, job.JobId);
                                        job.CancelRequest.Cancel();
                                        clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                        clone._context.Dispose();
                                    };
                                    centry.CancelRunning = cancelInProgress => {
                                        if(job.SessionId != Guid.Empty) {
                                            if(cancelInProgress) {
                                                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Job was cancelled by another workflow or job, while it was in progress in the concurrency group: {group}" }), job.TimeLineId, job.JobId);
                                                job.CancelRequest.Cancel();
                                            }
                                            // Keep Job running, since the cancelInProgress is false
                                        } else {
                                            job.CancelRequest.Cancel();
                                            clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                        }
                                    };
                                    _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Adding Job to the concurrency group: {group}, cancel-in-progress: {cancelInprogress}" }), job.TimeLineId, job.JobId);
                                    cgroup.PushEntry(centry, cancelInprogress);
                                }
                                break;
                            }
                        }
                    }
                    return job;
                };
            } catch(Exception ex) {
                matrixJobTraceWriter.Info("Exception: {0}", ex?.ToString());
                return failJob();
            }
        }

        private Func<TaskResult?, Job> queueAzureJob(GitHub.DistributedTask.ObjectTemplating.ITraceWriter matrixJobTraceWriter, string displayname, Runner.Server.Azure.Devops.Job rjob, Runner.Server.Azure.Devops.Pipeline pipeline, Dictionary<string, GitHub.DistributedTask.WebApi.VariableValue> variables, Func<string, string> evalVariable, string[] env, DictionaryContextData contextData, Guid jobId, Guid timelineId, string repo, string name, string workflowname, long runid, long runnumber, string[] secrets, double timeoutMinutes, double cancelTimeoutMinutes, bool continueOnError, string[] platform, bool localcheckout, long requestId, string Ref, string Sha, string wevent, string parentEvent, KeyValuePair<string, string>[] workflows = null, string statusSha = null, string parentId = null, Dictionary<string, List<Job>> finishedJobs = null, WorkflowRunAttempt attempt = null, JobItem ji = null, TemplateToken workflowPermissions = null, CallingJob callingJob = null, List<JobItem> dependentjobgroup = null, string selectedJob = null, string[] _matrix = null, WorkflowContext workflowContext = null, ISecretsProvider secretsProvider = null)
        {
            int fileContainerId = -1;
            Func<Func<TaskResult?, Job>> failJob = () => {
                var jid = jobId;
                var _job = new Job() { message = null, repo = repo, WorkflowRunAttempt = attempt, WorkflowIdentifier = name.PrefixJobIdIfNotNull(parentId), name = displayname, workflowname = workflowname, runid = runid, JobId = jid, RequestId = requestId, TimeLineId = timelineId, Matrix = CallingJob.ChildMatrix(callingJob?.Matrix, contextData["matrix"])?.ToJToken()?.ToString() };
                AddJob(_job);
                InvokeJobCompleted(new JobCompletedEvent() { JobId = jobId, Result = TaskResult.Failed, RequestId = requestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                return cancel => _job;
            };
            try {
                var macroexpr = new Regex("\\$\\(([^)]+)\\)");
                Func<Dictionary<string, GitHub.DistributedTask.WebApi.VariableValue>, string, int, string> evalMacro = null;
                evalMacro = (vars, macro, depth) => {
                    return macroexpr.Replace(macro, v => {
                        var keyFormat = v.Groups[1].Value.Split(":", 2);
                        return vars.TryGetValue(keyFormat[0], out var val) ? (depth <= 10 ? evalMacro(vars, val.Value, depth + 1) : val.Value) : v.Groups[0].Value;
                    });
                };
                var runsOnMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { };
                Func<string, string> demandToLabel = demand => {
                    var sd = demand.Split("-equals", 2);
                    if(sd.Length == 1) {
                        return sd[0].Trim();
                    }
                    return $"{sd[0].Trim()}={sd[1].Trim()}";
                };
                // Add capabilities to the map
                if(rjob.Pool != null) {
                    string[] demands = null;
                    string vmImage = null;
                    string poolName = null;
                    if(rjob.Pool.Demands != null) {
                        demands = (from d in rjob.Pool.Demands select evalMacro(variables, d, 0)).ToArray();
                    }
                    if(rjob.Pool.VmImage != null) {
                        vmImage = evalMacro(variables, rjob.Pool.VmImage, 0);
                    }
                    if(rjob.Pool.Name != null) {
                        poolName = evalMacro(variables, rjob.Pool.Name, 0);
                    }
                    if(!string.IsNullOrEmpty(vmImage)) {
                        runsOnMap.Add(vmImage);
                    } if(demands != null) {
                        foreach(var demand in demands) {
                            if(!string.IsNullOrEmpty(demand)) {
                                runsOnMap.Add(demandToLabel(demand));
                            }
                        }
                    }
                }
                var jobcontainerRef = rjob.Container;
                foreach(var p in platform.Reverse()) {
                    var eq = p.IndexOf('=');
                    var set = p.Substring(0, eq).Split(",").Select(demandToLabel).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if(runsOnMap.IsSubsetOf(set) && p.Length > (eq + 1)) {
                        if(p[eq + 1] == '-') {
                            if(p.Length == (eq + 2)) {
                                runsOnMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { };
                            } else {
                                runsOnMap = p.Substring(eq + 2, p.Length - (eq + 2)).Split(',').Select(demandToLabel).ToHashSet(StringComparer.OrdinalIgnoreCase);
                            }
                        } else {
                            runsOnMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { };
                            if(jobcontainerRef == null) {
                                // Set just the container property of the workflow, the runner will use it
                                jobcontainerRef = new Azure.Devops.Container().Parse(new StringToken(null, null, null, p.Substring(eq + 1, p.Length - (eq + 1))));
                            }
                            // If jobContainer != null, ignore the container
                        }
                        break;
                    }
                }
                if(!QueueJobsWithoutRunner) {
                    var sessionsfreeze = sessions.ToArray();
                    var x = (from s in sessionsfreeze where s.Value.Agent.TaskAgent.SystemCapabilities?.Count > 0 && runsOnMap.IsSubsetOf(s.Value.Agent.TaskAgent.SystemCapabilities.Concat(s.Value.Agent.TaskAgent.UserCapabilities ?? new Dictionary<string, string>()).SelectMany(kv => new [] { kv.Key, $"{kv.Key}={kv.Value}" } )) select s.Key).FirstOrDefault();
                    if(x == null) {
                        StringBuilder b = new StringBuilder();
                        int i = 0;
                        foreach(var e in runsOnMap) {
                            if(i++ != 0) {
                                b.Append(", ");
                            }
                            b.Append(e);
                        }
                        matrixJobTraceWriter.Info("{0}", $"No agent is registered for the requested capabilities: [{b.ToString()}], please register and run a self-hosted runner with at least these capabilities");
                        return failJob();
                    }
                }
                string runnerToken = null;
                Job job = null;
                job = new Job() { message = (caller, apiUrl) => {
                    try {
                        var cleanupClone = caller.Clone();
                        job.CleanUp?.Invoke();
                        job.CleanUp = () => {
                            if(!string.IsNullOrEmpty(runnerToken)) {
                                try {
                                    DeleteGithubAppToken(runnerToken);
                                } catch {

                                }
                            }
                            if(fileContainerId != -1) {
                                cleanupClone._context.ArtifactFileContainer.Remove(cleanupClone._context.ArtifactFileContainer.Find(fileContainerId));
                                cleanupClone._context.SaveChanges();
                            }
                            cleanupClone._context.Dispose();
                        };
                        var auth = new GitHub.DistributedTask.WebApi.EndpointAuthorization() { Scheme = GitHub.DistributedTask.WebApi.EndpointAuthorizationSchemes.OAuth };
                        var mySecurityKey = new RsaSecurityKey(Startup.AccessTokenParameter);

                        var myIssuer = "http://githubactionsserver";
                        var myAudience = "http://githubactionsserver";

                        var resp = cleanupClone.CreateArtifactContainer(runid, attempt.Attempt, new CreateActionsStorageArtifactParameters() { Name = $"Artifact of {displayname}",  }).GetAwaiter().GetResult();
                        fileContainerId = resp.Id;

                        var svariables = new Dictionary<string, GitHub.DistributedTask.WebApi.VariableValue>(variables, StringComparer.OrdinalIgnoreCase);
                        // Provide normal secrets from cli
                        foreach(var secr in secretsProvider.GetSecretsForEnvironment(matrixJobTraceWriter, "")) {
                            svariables[secr.Key] = new VariableValue(secr.Value, true);
                        }
                        svariables[SdkConstants.Variables.Build.ContainerId] = fileContainerId.ToString();
                        svariables["system.collectionUri"] = new VariableValue(apiUrl, false, true);
                        svariables["system.teamFoundationCollectionUri"] = new VariableValue(apiUrl, false, true);
                        svariables["system.taskDefinitionsUri"] = new VariableValue(apiUrl, false, true);
                        svariables["system.jobid"] = new VariableValue(jobId.ToString(), false, true);
                        svariables["system.timelineid"] = new VariableValue(timelineId.ToString(), false, true);

                        var tokenHandler = new JwtSecurityTokenHandler();
                        var tokenDescriptor = new SecurityTokenDescriptor
                        {
                            Subject = new ClaimsIdentity(new Claim[]
                            {
                                new Claim("Agent", "job"),
                                new Claim("repository", repo),
                                new Claim("ref", Ref),
                                new Claim("defaultRef", "refs/heads/" + ("main")),
                                new Claim("attempt", attempt.Attempt.ToString()),
                                new Claim("artifactsMinAttempt", attempt.ArtifactsMinAttempt.ToString()),
                                new Claim("localcheckout", localcheckout ? "actions/checkout" : ""),
                                new Claim("containerid", fileContainerId.ToString()),
                                new Claim("runid", runid.ToString()),
                            }),
                            Expires = DateTime.UtcNow.AddMinutes(timeoutMinutes + 10),
                            Issuer = myIssuer,
                            Audience = myAudience,
                            SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.RsaSha256)
                        };

                        var resources = new JobResources();
                        var token = tokenHandler.CreateToken(tokenDescriptor);
                        var stoken = tokenHandler.WriteToken(token);
                        auth.Parameters.Add(GitHub.DistributedTask.WebApi.EndpointAuthorizationParameters.AccessToken, stoken);
                        var systemVssConnection = new GitHub.DistributedTask.WebApi.ServiceEndpoint() { Name = WellKnownServiceEndpointNames.SystemVssConnection, Authorization = auth, Url = new Uri(apiUrl) };
                        resources.Endpoints.Add(systemVssConnection);

                        string jobcontainer = null;
                        var containerResources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        Func<string, string, Azure.Devops.Container, string> addContainer = (alias, image, container) => {
                            var cr = new ContainerResource {
                                Alias = alias,
                                Image = image,
                                Options = container.Options,
                                Endpoint = container.Endpoint != null ? new ServiceEndpointReference() { Name = container.Endpoint } : null
                            };
                            if(container.Env != null) {
                                var env = new Dictionary<String, String>();
                                foreach(var kv in container.Env) {
                                    env[kv.Key] = kv.Value;
                                }
                                cr.Environment = env;
                            }
                            if(container.Volumes != null) {
                                var volumes = new List<String>();
                                foreach(var v in container.Volumes) {
                                    volumes.Add(v);
                                }
                                cr.Volumes = volumes;
                            }
                            if(container.Ports != null) {
                                var ports = new List<String>();
                                foreach(var v in container.Ports) {
                                    ports.Add(v);
                                }
                                cr.Ports = ports;
                            }
                            if(container.MountReadonly != null) {
                                var readOnlyMounts = new List<string>();
                                if(container.MountReadonly.Externals == true) {
                                    readOnlyMounts.Add("externals");
                                }
                                if(container.MountReadonly.Work == true) {
                                    readOnlyMounts.Add("work");
                                }
                                if(container.MountReadonly.Tasks == true) {
                                    readOnlyMounts.Add("tasks");
                                }
                                if(container.MountReadonly.Tools == true) {
                                    readOnlyMounts.Add("tools");
                                }
                                cr.Properties.Set("readOnlyMounts", readOnlyMounts);
                            }
                            if(container.MapDockerSocket != null) {
                                cr.Properties.Set("mapDockerSocket", container.MapDockerSocket.Value);
                            }
                            if(containerResources.Add(cr.Alias)) {
                                resources.Containers.Add(cr);
                            }
                            return cr.Alias;
                        };
                        Func<Azure.Devops.Container, String> getContainerAlias = c => {
                            if(c.Alias != null) {
                                var alias = evalVariable(c.Alias);
                                if(pipeline.ContainerResources != null && pipeline.ContainerResources.TryGetValue(alias, out var cresource)) {
                                    return addContainer(alias, cresource.Image, cresource);
                                } else if(alias.Contains("{") || alias.Contains("}")) {
                                    cresource = JsonConvert.DeserializeObject<Azure.Devops.Container>(alias);
                                    return addContainer(Guid.NewGuid().ToString(), cresource.Image, cresource);
                                }
                                return addContainer(Guid.NewGuid().ToString(), alias, c);
                            }
                            var image = evalVariable(c.Image);
                            return addContainer(Guid.NewGuid().ToString(), image, c);
                        };
                        if(jobcontainerRef != null) {
                            jobcontainer = getContainerAlias(jobcontainerRef);
                        }
                        var jobSidecarContainers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        if(rjob.Services != null) {
                            foreach(var service in rjob.Services) {
                                jobSidecarContainers[service.Key] = getContainerAlias(service.Value);
                            }
                        }

                        var endpointPrefix = "system.runner.server.ServiceEndpoint.";
                        var repositoryPrefix = "system.runner.server.RepositoryResource.";
                        foreach(var variable in svariables) {
                            if(variable.Key.StartsWith(endpointPrefix, StringComparison.OrdinalIgnoreCase)) {
                                var endp = JsonConvert.DeserializeObject<ServiceEndpoint>(variable.Value.Value);
                                if(string.IsNullOrEmpty(endp.Name)) {
                                    endp.Name = variable.Key.Substring(endpointPrefix.Length);
                                }
                                if(endp.Id == Guid.Empty) {
                                    endp.Id = Guid.NewGuid();
                                }
                                resources.Endpoints.Add(endp);
                            } else if(variable.Key.StartsWith(repositoryPrefix, StringComparison.OrdinalIgnoreCase)) {
                                var repo = JsonConvert.DeserializeObject<RepositoryResource>(variable.Value.Value);
                                if(string.IsNullOrEmpty(repo.Alias)) {
                                    repo.Alias = variable.Key.Substring(repositoryPrefix.Length);
                                }
                                if(string.IsNullOrEmpty(repo.Id)) {
                                    repo.Id = Guid.NewGuid().ToString();
                                }
                                resources.Repositories.Add(repo);
                            }
                        }

                        if(!resources.Repositories.Any(repo => string.Equals(repo.Alias, "self", StringComparison.OrdinalIgnoreCase))) {
                            var dummyEndpoint = new ServiceEndpoint() {
                                Name = Guid.NewGuid().ToString(),
                                Url = new Uri("https://localhost/"),
                                Id = Guid.NewGuid(),
                                Type = "GithubEnterprise",
                                Owner = "test",
                                Description = "Some weird thingy",
                                IsReady = true,
                                IsShared = true,
                                GroupScopeId = Guid.NewGuid(),
                                OperationStatus = null,
                                Data = null
                            };
                            resources.Endpoints.Add(dummyEndpoint);
                            var repoResource = new RepositoryResource() {
                                Alias = "self",
                                Id = Guid.NewGuid().ToString(),
                                Endpoint = new ServiceEndpointReference() { Id = dummyEndpoint.Id, Name = dummyEndpoint.Name },
                                Url = new Uri("https://localhost/Unknown/Unknown"),
                                Type = "GithubEnterprise",
                                Version = "0000000000000000000000000000000000000000"
                            };
                            repoResource.Properties.Set("name", "repo1");
                            repoResource.Properties.Set("ref", "main");
                            resources.Repositories.Add(repoResource);
                        }

                        var stepNames = new ReferenceNameBuilder(true);

                        var steps = rjob.Steps/*.Prepend(checkoutTask)*/.Where(s => s.Enabled).ToList();
                        var checkoutGuid = Guid.Parse("6d15af64-176c-496d-b583-fd2ae21d4df4");
                        var tasksByNameAndVersion = workflowContext?.WorkflowState?.TasksByNameAndVersion;

                        var localcheckoutRef = localcheckout && tasksByNameAndVersion != null && 
                                            tasksByNameAndVersion.TryGetValue("localcheckoutazure@0", out var localcheckoutazure)
                            ? new TaskStepDefinitionReference {
                                Id = localcheckoutazure.Id,
                                Name = localcheckoutazure.Name,
                                Version = $"{localcheckoutazure.Version.Major}.{localcheckoutazure.Version.Minor}.{localcheckoutazure.Version.Patch}"
                            } : null;

                        // checkout: none triggers an error in the builtin checkout task
                        var removedCheckoutNone = steps.RemoveAll(step => step.Reference?.Id == checkoutGuid && 
                                                            step.Inputs.TryGetValue("repository", out var repo) && 
                                                            repo == "none") > 0;

                        var removedDownloadStep = steps.RemoveAll(step => step.Reference?.Name != null && 
                                (string.Equals(step.Reference.Name, "DownloadPipelineArtifact", StringComparison.OrdinalIgnoreCase) || 
                                string.Equals(step.Reference.Name, "DownloadBuildArtifacts", StringComparison.OrdinalIgnoreCase)) && 
                                (step.Inputs.TryGetValue("buildType", out var buildType) || step.Inputs.TryGetValue("source", out buildType)) && 
                                buildType == "none");

                        // Replace checkout with localcheckout if applicable
                        var checkoutSteps = (from step in steps where step.Reference?.Id == checkoutGuid select step).ToArray();
                        foreach (var step in checkoutSteps) {
                            step.Reference = localcheckoutRef;
                            if(step.Inputs.TryGetValue("repository", out var repo)) {
                                if(checkoutSteps.Length > 1 && (!step.Inputs.TryGetValue("path", out var path) || path == "")) {
                                    step.Inputs["path"] = repo == "self" ? "CurrentRepo" : repo;
                                }
                                if(repo == "self") {
                                    step.Inputs["repository"] = "";
                                    step.Inputs["ref"] = "";
                                } else if(workflowContext.AzContext.Repositories.TryGetValue(repo, out var nameAndRef)) {
                                    var nref = nameAndRef.Split("@", 2);
                                    step.Inputs["repository"] = nref[0];
                                    step.Inputs["ref"] = nref[1];
                                }
                            }
                        }

                        if (rjob.DeploymentJob) {
                            // TODO Inject Download step for deployment jobs
                        } else {
                            var injectCheckout = !removedCheckoutNone && !steps.Any(step => step.Reference?.Id == checkoutGuid);
                            if (localcheckoutRef != null) {
                                if (!removedCheckoutNone && !steps.Any(step => step.Reference?.Id == checkoutGuid)) {
                                    steps.Insert(0, new TaskStep {
                                        DisplayName = "Default Checkout Task",
                                        Reference = localcheckoutRef
                                    });
                                }
                            } else {
                                if (injectCheckout) {
                                    var checkoutTask = new TaskStep {
                                        DisplayName = "Default Checkout Task",
                                        Reference = new TaskStepDefinitionReference {
                                            Id = checkoutGuid,
                                            Name = "Checkout",
                                            Version = "1.0.0"
                                        }
                                    };
                                    checkoutTask.Inputs["repository"] = "self";
                                    checkoutTask.Inputs["clean"] = "true";
                                    checkoutTask.Inputs["fetchDepth"] = "0";
                                    checkoutTask.Inputs["lfs"] = "false";
                                    steps.Insert(0, checkoutTask);
                                }
                            }
                        }
                        var seNameToId = resources.Endpoints.ToOrdinalIgnoreCaseDictionary(s => s.Name, s => s.Id.ToString());
                        List<string> errors = new List<string>();
                        for(int i = 0; i < steps.Count; i++) {
                            var clone = new TaskStep();
                            var org = steps[i] as TaskStep;
                            var metaData = org.Reference.Id == checkoutGuid ? null : tasksByNameAndVersion[$"{org.Reference.Id}@{org.Reference.Version}"];
                            var inputs = metaData?.Inputs?.ToOrdinalIgnoreCaseDictionary(inp => inp.Name, inp => inp);
                            if(org.Inputs != null) {
                                foreach(var kv in org.Inputs) {
                                    if(inputs?.TryGetValue(kv.Key, out var inp) ?? false) {
                                        if(inp.Type.StartsWith("connectedService:", StringComparison.OrdinalIgnoreCase) && seNameToId.TryGetValue(kv.Value, out var seId)) {
                                            clone.Inputs[kv.Key] = seId;
                                            continue;
                                        }
                                    }
                                    clone.Inputs[kv.Key] = kv.Value;
                                }
                            }
                            if(org.Environment != null) {
                                foreach(var kv in org.Environment) {
                                    clone.Environment[kv.Key] = kv.Value;
                                }
                            }
                            clone.Id = Guid.NewGuid();
                            clone.Name = org.Name;
                            string stepNameError;
                            if(!string.IsNullOrEmpty(clone.Name) && !stepNames.TryAddKnownName(clone.Name, out stepNameError)) {
                                errors.Add(stepNameError);
                            }
                            clone.DisplayName = org.DisplayName;
                            if(string.IsNullOrEmpty(clone.DisplayName)) {
                                clone.DisplayName = string.IsNullOrEmpty(metaData?.InstanceNameFormat) ? org.Reference.Name : metaData.InstanceNameFormat;
                            }
                            if(!string.IsNullOrEmpty(clone.DisplayName)) {
                                var displayNameVars = new Dictionary<string, GitHub.DistributedTask.WebApi.VariableValue>(variables, StringComparer.OrdinalIgnoreCase);
                                if(clone.Inputs != null) {
                                    foreach(var inp in clone.Inputs) {
                                        displayNameVars[inp.Key] = inp.Value;
                                    }
                                    if(inputs != null) {
                                        foreach(var inp in inputs) {
                                            if(!clone.Inputs.ContainsKey(inp.Key)) {
                                                displayNameVars[inp.Key] = inp.Value.DefaultValue ?? "";
                                            }
                                        }
                                    }
                                }
                                clone.DisplayName = evalMacro(displayNameVars, clone.DisplayName, 0);
                            }
                            clone.Enabled = org.Enabled;
                            clone.Condition = org.Condition;
                            clone.Reference = org.Reference;
                            clone.Target = org.Target;
                            if(pipeline.ContainerResources != null && !string.IsNullOrEmpty(clone.Target?.Target) && !string.Equals(clone.Target.Target, "host", StringComparison.OrdinalIgnoreCase) && pipeline.ContainerResources.TryGetValue(clone.Target.Target, out var cresource)) {
                                addContainer(clone.Target.Target, cresource.Image, cresource);
                            }
                            clone.ContinueOnError = org.ContinueOnError;
                            clone.RetryCountOnTaskFailure = org.RetryCountOnTaskFailure;
                            clone.TimeoutInMinutes = org.TimeoutInMinutes;
                            
                            steps[i] = clone;
                        }

                        foreach(var step in steps) {
                            if(string.IsNullOrEmpty(step.Name)) {
                                stepNames.AppendSegment((step as TaskStep).Reference.Name);
                                step.Name = stepNames.Build();
                            }
                        }
                        
                        var req = new AgentJobRequestMessage(
                            new GitHub.DistributedTask.WebApi.TaskOrchestrationPlanReference{ 
                                PlanGroup = "free", PlanId = Guid.NewGuid(), PlanType = "free", ScopeIdentifier = Guid.Empty, Version = 14
                            }, 
                            new GitHub.DistributedTask.WebApi.TimelineReference {
                                Id = timelineId
                            }, jobId, displayname, name, new StringToken(null, null, null, jobcontainer ?? ""), null, null,
                            svariables,
                            new List<MaskHint>(),
                            resources, null,
                            new WorkspaceOptions() {
                                Clean = rjob.WorkspaceClean
                            },
                            steps,
                            null,
                            null,
                            null,
                            null,
                            null);
                        req.RequestId = requestId;
                        req.SetJobSidecarContainers(jobSidecarContainers);
                        var json = JsonConvert.SerializeObject(req, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}});
                        return req;
                    } catch(Exception ex) {
                        matrixJobTraceWriter.Error("{0}", $"Internal Error: {ex.Message}, {ex.StackTrace}");
                        Console.WriteLine($"Internal Error: {ex.Message}, {ex.StackTrace}");
                        return null;
                    }
                }, repo = repo, WorkflowRunAttempt = attempt, WorkflowIdentifier = name.PrefixJobIdIfNotNull(parentId), name = displayname, workflowname = workflowname, runid = runid, /* SessionId = sessionId,  */JobId = jobId, RequestId = requestId, TimeLineId = timelineId, TimeoutMinutes = timeoutMinutes, CancelTimeoutMinutes = cancelTimeoutMinutes, ContinueOnError = continueOnError, Matrix = CallingJob.ChildMatrix(callingJob?.Matrix, contextData["matrix"])?.ToJToken()?.ToString() };
                AddJob(job);
                //ConcurrencyGroup
                string group = null;
                bool cancelInprogress = false;
                return result => {
                    if(result != null || job.CancelRequest.IsCancellationRequested) {
                        job.CancelRequest.Cancel();
                        InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = result ?? TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                    } else {
                        Action _queueJob = () => {
                            try {
                                using(var scope = _provider.CreateScope()) {
                                    var queueService = scope.ServiceProvider.GetService<IQueueService>();

                                    if(queueService != null) {
                                        job.SessionId = Guid.NewGuid();
                                        queueService.PickJob(job.message.Invoke(this, this.ServerUrl + "/"), job.CancelRequest.Token, new string[0]);
                                    } else
                                    {
                                        queueJob(runsOnMap, job);
                                    }
                                }
                            } catch(ObjectDisposedException) {
                                // The scope has been disposed, might happen due to rerunning jobs
                                queueJob(runsOnMap, job);
                            }

                            void queueJob(HashSet<string> runsOnMap, Job job)
                            {
                                Channel<Job> queue = jobqueueAzure.GetOrAdd(runsOnMap, (a) => Channel.CreateUnbounded<Job>());

                                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string> { $"Queued Job: {job.name} for queue {string.Join(",", runsOnMap)}" }), job.TimeLineId, job.JobId);
                                queue.Writer.WriteAsync(job);
                            }
                        };
                        if(string.IsNullOrEmpty(group)) {
                            _queueJob();
                        } else {
                            var clone = Clone();
                            var key = $"{repo}/{group}";
                            while(true) {
                                ConcurrencyGroup cgroup = concurrencyGroups.GetOrAdd(key, name => new ConcurrencyGroup() { Key = name });
                                lock(cgroup) {
                                    if(concurrencyGroups.TryGetValue(key, out var _cgroup) && cgroup != _cgroup) {
                                        continue;
                                    }
                                    ConcurrencyEntry centry = new ConcurrencyEntry();
                                    Action finish = () => {
                                        cgroup.FinishRunning(centry);
                                        clone._context.Dispose();
                                    };
                                    centry.Run = () => {
                                        if(job.CancelRequest.IsCancellationRequested) {
                                            _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Job was cancelled, while it was pending in the concurrency group" }), job.TimeLineId, job.JobId);
                                            finish();
                                        } else {
                                            FinishJobController.OnJobCompleted += evdata => {
                                                if(evdata.JobId == job.JobId) {
                                                    finish();
                                                }
                                            };
                                            _queueJob();
                                        }
                                    };
                                    centry.CancelPending = () => {
                                        _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Job was cancelled by another workflow or job, while it was pending in the concurrency group" }), job.TimeLineId, job.JobId);
                                        job.CancelRequest.Cancel();
                                        clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                        clone._context.Dispose();
                                    };
                                    centry.CancelRunning = cancelInProgress => {
                                        if(job.SessionId != Guid.Empty) {
                                            if(cancelInProgress) {
                                                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Job was cancelled by another workflow or job, while it was in progress in the concurrency group: {group}" }), job.TimeLineId, job.JobId);
                                                job.CancelRequest.Cancel();
                                            }
                                            // Keep Job running, since the cancelInProgress is false
                                        } else {
                                            job.CancelRequest.Cancel();
                                            clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                        }
                                    };
                                    _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(job.JobId, new List<string>{ $"Adding Job to the concurrency group: {group}, cancel-in-progress: {cancelInprogress}" }), job.TimeLineId, job.JobId);
                                    cgroup.PushEntry(centry, cancelInprogress);
                                }
                                break;
                            }
                        }
                    }
                    return job;
                };
            } catch(Exception ex) {
                matrixJobTraceWriter.Info("Exception: {0}", ex?.ToString());
                return failJob();
            }
        }

        public delegate AgentJobRequestMessage MessageFactory(MessageController self, string apiUrl);

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
                return b.ToString().ToLowerInvariant().GetHashCode();
            }
        }

        private static ConcurrentDictionary<HashSet<string>, Channel<Job>> jobqueue = new ConcurrentDictionary<HashSet<string>, Channel<Job>>(new EqualityComparer());
        private static ConcurrentDictionary<HashSet<string>, Channel<Job>> jobqueueAzure = new ConcurrentDictionary<HashSet<string>, Channel<Job>>(new EqualityComparer());
        private static int id = 0;

        public static ConcurrentDictionary<Session, Session> sessions = new ConcurrentDictionary<Session, Session>();
        public delegate void RepoDownload(long runid, string url, bool submodules, bool nestedSubmodules, string repository, string format, string path);

        public static event RepoDownload OnRepoDownload;

        [HttpGet("isagentonline")]
        public async Task<IActionResult> IsAgentOnline([FromQuery] string name)
        {
            var sessionsfreeze = sessions.ToArray();
            var online = sessionsfreeze.Any(s => name == s.Value.Agent.TaskAgent.Name);
            if(!online) {
                this.HttpContext.Response.StatusCode = 404;
            }
            return await Ok(new { online });
        }

        [HttpGet("{poolId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Policy = "Agent")]
        [SwaggerResponse(200, type: typeof(TaskAgentMessage))]
        [SwaggerResponse(204, "No message for 50s")]
        [SwaggerResponse(403, type: typeof(WrappedException))]
        public async Task<IActionResult> GetMessage(int poolId, Guid sessionId)
        {
            Session session;
            if(!_cache.TryGetValue(sessionId, out session)) {
                this.HttpContext.Response.StatusCode = 403;
                Exception except = Request.Headers.UserAgent.FirstOrDefault()?.Contains("Vsts") == true ? new Microsoft.TeamFoundation.DistributedTask.WebApi.TaskAgentSessionExpiredException("This server has been restarted AZP") : new TaskAgentSessionExpiredException("This server has been restarted");
                return await Ok(new WrappedException(except, true, new Version(2, 0)));
            }
            var ts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted, new CancellationTokenSource(TimeSpan.FromSeconds(50)).Token);
            await session.MessageLock.WaitAsync(ts.Token);
            try {
                session.Timer.Stop();
                session.Timer.Start();
                if(session.DropMessage != null && session.Job != null && !session.JobAccepted) {
                    return await SendJob(sessionId, session, null, 0, session.Job);
                }
                session.DropMessage?.Invoke("Called GetMessage without deleting the old Message");
                session.DropMessage = null;
                var job = session.Job;
                var jobRunningToken = session.JobRunningToken;
                if(job == null) {
                    if(session.Agent.TaskAgent.Ephemeral == true && session.FirstJobReceived) {
                        try {
                            DeleteAgent(session.Agent.Pool.Id, session.Agent.TaskAgent.Id);
                        } catch {

                        }
                        this.HttpContext.Response.StatusCode = 403;
                        return await Ok(new WrappedException(new TaskAgentSessionExpiredException("This agent has been removed by Ephemeral"), true, new Version(2, 0)));
                    }
                    var isAzureAgent = session.Agent.TaskAgent.SystemCapabilities?.Count > 0;
                    var labels = isAzureAgent ? session.Agent.TaskAgent.SystemCapabilities.Concat(session.Agent.TaskAgent.UserCapabilities ?? new Dictionary<string, string>()).SelectMany(kv => new [] { kv.Key, $"{kv.Key}={kv.Value}" } ).ToArray() : session.Agent.TaskAgent.Labels.Select(l => l.Name).ToArray();
                    var agentJobQueue = isAzureAgent ? jobqueueAzure : jobqueue;
                    var defLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { };
                    var defChannel = agentJobQueue.GetOrAdd(defLabels, (a) => Channel.CreateUnbounded<Job>());
                    if(isAzureAgent) {
                    //     var caps = session.Agent.TaskAgent.SystemCapabilities.Concat(session.Agent.TaskAgent.UserCapabilities ?? new Dictionary<string, string>()).ToArray();
                    //     foreach(var cap in caps) {
                    //         foreach(var cap2 in caps) {
                    //             agentJobQueue.TryAdd(label, (a) => Channel.CreateUnbounded<Job>());
                    //         }
                    //     }
                    } else {
                        HashSet<HashSet<string>> labelcom = labels.Select(l => new HashSet<string>(StringComparer.OrdinalIgnoreCase){l}).ToHashSet(new EqualityComparer());
                        for(long j = 0; j < labels.LongLength; j++) {
                            var it = labelcom.ToArray();
                            for(long i = 0, size = it.LongLength; i < size; i++) {
                                var res = it[i].Append(labels[j]).ToHashSet(StringComparer.OrdinalIgnoreCase);
                                labelcom.Add(res);
                            }
                        }
                        foreach(var label in labelcom) {
                            Channel<Job> queue = agentJobQueue.GetOrAdd(label, (a) => Channel.CreateUnbounded<Job>());
                        }
                    }
                    var queues = agentJobQueue.ToArray().Where(e => e.Key.IsSubsetOf(labels)).Append(new KeyValuePair<HashSet<string>, Channel<Job>>(defLabels, defChannel)).ToArray();
                    while(!ts.IsCancellationRequested) {
                        var poll = queues.Select(q => q.Value.Reader.WaitToReadAsync(ts.Token).AsTask()).ToArray();
                        await Task.WhenAny(poll);
                        if(ts.IsCancellationRequested) {
                            return NoContent();
                        }
                        for(long i = 0; i < poll.LongLength && !HttpContext.RequestAborted.IsCancellationRequested; i++ ) {
                            if(poll[i].IsCompletedSuccessfully && poll[i].Result)
                            try {
                                if(queues[i].Value.Reader.TryRead(out var req))
                                {
                                    var res = await SendJob(sessionId, session, queues, i, req);
                                    if(res is NoContentResult) {
                                        continue;
                                    }
                                    return res;
                                }
                            } catch(Exception ex) {
                                session.DropMessage?.Invoke(ex.Message);
                                session.DropMessage = null;
                            }
                        }
                    }
                } else if(!job.Cancelled) {
                    try {
                        var now = DateTime.UtcNow;
                        if(session.DoNotCancelBefore != null && session.DoNotCancelBefore > now) {
                            // Attempt to mitigate an actions/runner bug, where the runner doesn't send a jobcompleted event if we cancel to early
                            await Task.Delay(session.DoNotCancelBefore.Value - now, CancellationTokenSource.CreateLinkedTokenSource(jobRunningToken, ts.Token).Token);
                        }
                        await Helper.WaitAnyCancellationToken(jobRunningToken, ts.Token, job.CancelRequest.Token);
                    } catch (TaskCanceledException) {
                        // Connection Reset
                        if(ts.Token.IsCancellationRequested)
                        {
                            return NoContent();
                        }
                        if(!jobRunningToken.IsCancellationRequested && job.CancelRequest.IsCancellationRequested) {
                            try {
                                session.DropMessage = (reason) => {
                                    job.Cancelled = false;
                                };
                                job.Cancelled = true;
                                session.Key.GenerateIV();
                                // await Task.Delay(2500);
                                using (var encryptor = session.Key.CreateEncryptor(session.Key.Key, session.Key.IV))
                                using (var body = new MemoryStream())
                                using (var cryptoStream = new CryptoStream(body, encryptor, CryptoStreamMode.Write)) {
                                    await new ObjectContent<JobCancelMessage>(new JobCancelMessage(job.JobId, TimeSpan.FromMinutes(job.CancelTimeoutMinutes)), new VssJsonMediaTypeFormatter(true)).CopyToAsync(cryptoStream);
                                    cryptoStream.FlushFinalBlock();
                                    return await Ok(new TaskAgentMessage() {
                                        Body = Convert.ToBase64String(body.ToArray()),
                                        MessageId = id++,
                                        MessageType = JobCancelMessage.MessageType,
                                        IV = session.Key.IV
                                    });
                                }
                            } finally {
                                // Catch possible desynchronized runner is stale forever
                                var clone = Clone();
                                Task.Run(async () => {
                                    await Task.Delay(TimeSpan.FromMinutes(job.CancelTimeoutMinutes + 2));
                                    clone.InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                    clone._context.Dispose();
                                });
                            }
                        }
                        if(jobRunningToken.IsCancellationRequested && session.Agent.TaskAgent.Ephemeral == true) {
                            try {
                                DeleteAgent(session.Agent.Pool.Id, session.Agent.TaskAgent.Id);
                            } catch {

                            }
                        }
                        // The official runner ignores the next job if we don't delay here
                        await Task.Delay(1000);
                    }
                } else {
                    try {
                        await Helper.WaitAnyCancellationToken(jobRunningToken, ts.Token);
                    } catch (TaskCanceledException) {
                        // Connection Reset
                        if(ts.Token.IsCancellationRequested)
                        {
                            return NoContent();
                        }
                        if(jobRunningToken.IsCancellationRequested && session.Agent.TaskAgent.Ephemeral == true) {
                            try {
                                DeleteAgent(session.Agent.Pool.Id, session.Agent.TaskAgent.Id);
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

            async Task<IActionResult> SendJob(Guid sessionId, Session session, KeyValuePair<HashSet<string>, Channel<Job>>[] queues, long i, Job req)
            {
                try
                {
                    HashSet<string> agentlabels = null;
                    if(queues != null)
                    {
                        session.JobAccepted = false;
                        agentlabels = queues[i].Key;
                        _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string> { $"Read Job from Queue: {req.name} assigned to Runner Name:{session.Agent.TaskAgent.Name} Labels:{string.Join(",", agentlabels)}" }), req.TimeLineId, req.JobId);
                        _context.Attach(req);
                        req.SessionId = sessionId;
                        _context.SaveChanges();
                        if (req.CancelRequest.IsCancellationRequested)
                        {
                            _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string> { $"Cancelled Job: {req.name} unassigned from Runner Name:{session.Agent.TaskAgent.Name} Labels:{string.Join(",", agentlabels)}" }), req.TimeLineId, req.JobId);
                            InvokeJobCompleted(new JobCompletedEvent() { JobId = req.JobId, Result = TaskResult.Canceled, RequestId = req.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                            return NoContent();
                        }
                        var q = queues[i].Value;
                        session.DropMessage = reason =>
                        {
                            _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string> { $"Requeued Job: {req.name} unassigned from Runner Name:{session.Agent.TaskAgent.Name} Labels:{string.Join(",", agentlabels)}: {reason}" }), req.TimeLineId, req.JobId);
                            q.Writer.WriteAsync(req);
                            session.Job = null;
                            session.JobTimer?.Stop();
                        };
                    }
                    agentlabels ??= new HashSet<string>();
                    try
                    {
                        if (req.message == null)
                        {
                            Console.WriteLine("req.message == null in GetMessage of Worker, skip invalid message");
                            _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string> { $"Failed Job: {req.name} for queue {string.Join(",", agentlabels)}: req.message == null in GetMessage of Worker, skip invalid message" }), req.TimeLineId, req.JobId);
                            return NoContent();
                        }
                        // Use Uri to ensure that a host only ServerUrl has a leading `/`, the actions cache api assumes the it ends with a slash
                        var res = req.message.Invoke(this, new Uri(new Uri(ServerUrl), "./").ToString());
                        if (res == null)
                        {
                            Console.WriteLine("res == null in GetMessage of Worker, skip internal Error");
                            _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string> { $"Failed Job: {req.name} for queue {string.Join(",", agentlabels)}: req.message == null in GetMessage of Worker, skip invalid message" }), req.TimeLineId, req.JobId);
                            InvokeJobCompleted(new JobCompletedEvent() { JobId = req.JobId, Result = TaskResult.Failed, RequestId = req.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                            return NoContent();
                        }
                        HttpContext.RequestAborted.ThrowIfCancellationRequested();
                        if (session.JobTimer == null)
                        {
                            session.JobTimer = new System.Timers.Timer();
                            session.JobTimer.Elapsed += (a, b) =>
                            {
                                if (session.Job != null)
                                {
                                    session.Job.CancelRequest.Cancel();
                                }
                            };
                            session.JobTimer.AutoReset = false;
                        }
                        else
                        {
                            session.JobTimer.Stop();
                        }
                        session.Job = req;
                        session.JobTimer.Interval = session.Job.TimeoutMinutes * 60 * 1000;
                        session.JobTimer.Start();
                        session.Key.GenerateIV();
                        using (var encryptor = session.Key.CreateEncryptor(session.Key.Key, session.Key.IV))
                        using (var body = new MemoryStream())
                        using (var cryptoStream = new CryptoStream(body, encryptor, CryptoStreamMode.Write))
                        {
                            await new ObjectContent<AgentJobRequestMessage>(res, new VssJsonMediaTypeFormatter(true)).CopyToAsync(cryptoStream);
                            cryptoStream.FlushFinalBlock();
                            var msg = await Ok(new TaskAgentMessage()
                            {
                                Body = Convert.ToBase64String(body.ToArray()),
                                MessageId = id++,
                                MessageType = JobRequestMessageTypes.PipelineAgentJobRequest,
                                IV = session.Key.IV
                            });
                            if (req.CancelRequest.IsCancellationRequested)
                            {
                                session.Job = null;
                                session.DropMessage = null;
                                session.JobTimer.Stop();
                                _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string> { $"Cancelled Job (2): {req.name} for queue {string.Join(",", agentlabels)} unassigned from Runner Name:{session.Agent.TaskAgent.Name} Labels:{string.Join(",", agentlabels)}" }), req.TimeLineId, req.JobId);
                                InvokeJobCompleted(new JobCompletedEvent() { JobId = req.JobId, Result = TaskResult.Canceled, RequestId = req.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                                return NoContent();
                                //return NoContent();
                            }
                            HttpContext.RequestAborted.ThrowIfCancellationRequested();
                            _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string> { $"Send Job to Runner: {req.name} for queue {string.Join(",", agentlabels)} assigned to Runner Name:{session.Agent.TaskAgent.Name} Labels:{string.Join(",", agentlabels)}" }), req.TimeLineId, req.JobId);
                            // Attempt to mitigate an actions/runner bug, where the runner doesn't send a jobcompleted event if we cancel to early
                            // Reduced from 5 to 2 seconds
                            session.DoNotCancelBefore = DateTime.UtcNow.AddSeconds(2);
                            return msg;
                        }
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string> { $"Error while sending message (inner area): {ex.Message}" }), req.TimeLineId, req.JobId);
                        }
                        catch
                        {

                        }
                        if (session.DropMessage != null)
                        {
                            session.DropMessage?.Invoke(ex.Message);
                            session.DropMessage = null;
                        }
                        else
                        {
                            await queues[i].Value.Writer.WriteAsync(req);
                        }
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        _webConsoleLogService.AppendTimelineRecordFeed(new TimelineRecordFeedLinesWrapper(req.JobId, new List<string> { $"Error while sending message (outer area): {ex.Message}" }), req.TimeLineId, req.JobId);
                    }
                    catch
                    {

                    }
                    await queues[i].Value.Writer.WriteAsync(req);
                }
                return NoContent();
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

            [DataMember(Name = "clone_url")]
            public string CloneUrl { get; set; }
        }
        public class Permissions {
            public bool Admin  { get; set; }
            public bool Push  { get; set; }
            public bool Pull  { get; set; }
        }

        public class GitCommit {
            public string Message {get;set;}
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
            public string merge_commit_sha {get;set;}
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
            public GitUser sender {get;set;}
            public GitPullRequest pull_request {get;set;}

            public List<GitCommit> Commits {get;set;}
            public string ref_type { get; set; }
            public string Sha { get; set; }
            public Release Release { get; set; }
            public WebhookWorkflow Workflow  { get; set; }
        }
        public class WebhookWorkflow {
            public string Name { get; set; }
        }
        public class Release {
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
            public string Sha {get;set;}
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
            return await ExecuteWebhook(e, obj);
        }

        private async Task<ActionResult> ExecuteWebhook(string e, KeyValuePair<GiteaHook, JObject> obj, string workflowFileFilter = null) {
            var hook = obj.Key;
            string githubAppToken = null;
            if(!string.IsNullOrEmpty(hook?.repository?.full_name)) {
                try {
                    if(string.IsNullOrEmpty(GITHUB_TOKEN)) {
                        githubAppToken = await CreateGithubAppToken(hook.repository.full_name);
                    }
                    var skipCILabels = new [] { "[skip ci]", "[ci skip]", "[no ci]", "[skip actions]", "[actions skip]" };
                    var evs = new Dictionary<string,  (string,string,string)>();
                    if(e == "pull_request") {
                        evs.Add("pull_request_target", ($"refs/heads/{hook?.pull_request?.Base?.Ref}", hook?.pull_request?.Base?.Sha, hook?.pull_request?.head?.Sha));
                        if(AllowPullRequests || (!hook?.pull_request?.head?.Repo?.Fork ?? false)) {
                            if(HasPullRequestMergePseudoBranch) {
                                evs.Add("pull_request", ($"refs/pull/{hook.Number}/merge",  hook?.pull_request?.merge_commit_sha, hook?.pull_request?.head?.Sha));
                            } else {
                                evs.Add("pull_request", ($"refs/pull/{hook.Number}/head",  hook?.pull_request?.head?.Sha, hook?.pull_request?.head?.Sha));
                            }
                        }
                    } else if(e.StartsWith("pull_request_") && AllowPullRequests) {
                        if(HasPullRequestMergePseudoBranch) {
                            evs.Add(e, ($"refs/pull/{hook.Number}/merge",  hook?.pull_request?.merge_commit_sha, hook?.pull_request?.head?.Sha));
                        } else {
                            evs.Add(e, ($"refs/pull/{hook.Number}/head", hook?.pull_request?.head?.Sha, hook?.pull_request?.head?.Sha));
                        }
                    } else if(e == "create") {
                        if(!string.IsNullOrEmpty(hook?.ref_type) && !string.IsNullOrEmpty(hook?.Ref)) {
                            evs.Add(e, ((hook.ref_type == "branch" ? "refs/heads/" : "refs/tags/") + hook.Ref, null, null));
                        }
                    } else if(e == "release") {
                        if(!string.IsNullOrEmpty(hook?.Release?.tag_name)) {
                            evs.Add(e, ($"refs/tags/{hook.Release.tag_name}", null, null));
                        }
                    } else if(e == "push") {
                        if(DisableNoCI || (!hook?.Commits?.Any(c => skipCILabels.Any(l => c.Message?.Contains(l) ?? false)) ?? true)) {
                            evs.Add(e, (hook?.Ref, hook?.After, hook?.After));
                        }
                    } else {
                        evs.Add(e, (hook?.repository?.default_branch != null ? $"refs/heads/{hook.repository.default_branch}" : null, null, null));
                    }
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("runner", string.IsNullOrEmpty(GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version) ? "0.0.0" : GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version));
                    if(!string.IsNullOrEmpty(!string.IsNullOrEmpty(GITHUB_TOKEN) ? GITHUB_TOKEN : githubAppToken)) {
                        client.DefaultRequestHeaders.Add("Authorization", $"token {(!string.IsNullOrEmpty(GITHUB_TOKEN) ? GITHUB_TOKEN : githubAppToken)}");
                    }
                    foreach(var em in evs) {
                        var (Ref, Sha, StatusCheckSha) = em.Value;
                        if(Sha == null) {
                            var cres = await client.GetAsync(new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{hook.repository.full_name}/commits")) { Query = $"?sha={Uri.EscapeDataString(Ref ?? "")}&page=1&limit=1&per_page=1" }.ToString());
                            if(cres.StatusCode == System.Net.HttpStatusCode.OK) {
                                var content = await cres.Content.ReadAsStringAsync();
                                var o = JsonConvert.DeserializeObject<GitCommit[]>(content)[0];
                                Sha = o.Sha;
                            }
                        }
                        var urlBuilder = new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{hook.repository.full_name}/contents/{Uri.EscapeDataString(workflowRootFolder)}"));
                        urlBuilder.Query = $"?ref={Uri.EscapeDataString(Sha ?? Ref)}";
                        var res = await client.GetAsync(urlBuilder.ToString());
                        if(res.StatusCode == System.Net.HttpStatusCode.OK) {
                            var content = await res.Content.ReadAsStringAsync();
                            var workflowList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UnknownItem>>(content);
                            List<KeyValuePair<string, string>> workflows = new List<KeyValuePair<string, string>>();
                            foreach (var item in workflowList)
                            {
                                try {
                                    if((workflowFileFilter == null || workflowFileFilter == item.path) && (item.path.EndsWith(".yml") || item.path.EndsWith(".yaml"))) {
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
                                var clone = Clone();
                                Task.Run(() => clone.ConvertYaml(workflow.Key, workflow.Value, hook.repository.full_name, GitServerUrl, hook, obj.Value, em.Key, workflows: workflows.ToArray(), Ref: Ref, Sha: Sha, StatusCheckSha: StatusCheckSha));
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
            }
            return Ok();
        }

        [HttpPost("schedule2")]
        public async Task<IActionResult> OnSchedule2([FromQuery] string job, [FromQuery] int? list, [FromQuery] string[] env, [FromQuery] string[] secrets, [FromQuery] string[] matrix, [FromQuery] string[] platform, [FromQuery] bool? localcheckout, [FromQuery] string Ref, [FromQuery] string Sha, [FromQuery] string Repository, [FromQuery] int? runid, [FromQuery] string jobId, [FromQuery] bool? failed, [FromQuery] bool? resetArtifacts, [FromQuery] bool? refresh, [FromQuery] string[] taskNames)
        {
            if(WebhookSecret.Length > 0) {
                return NotFound();
            }
            // Prevent register schedule events, if a workflow is triggered manually
            this.factory = null;
            var rrunid = runid;
            var form = await Request.ReadFormAsync();
            KeyValuePair<GiteaHook, JObject> obj;
            var eventFile = form.Files.GetFile("event");
            using(var reader = new StreamReader(eventFile.OpenReadStream())) {
                string text = await reader.ReadToEndAsync();
                var obj_ = JObject.Parse(text);
                obj = new KeyValuePair<GiteaHook, JObject>(obj_.ToObject<GiteaHook>(), obj_);
            }

            var workflow = (from f in form.Files where f.Name != "event" && !(f.FileName.EndsWith(".secrets") && f.Name == "actions-environment-secrets" || f.FileName.EndsWith(".vars") && f.Name == "actions-environment-variables") select new KeyValuePair<string, string>(f.FileName, new StreamReader(f.OpenReadStream()).ReadToEnd())).ToArray();
            var des = new DeserializerBuilder().Build();
            var secretsEnvironments = (from f in form.Files where f.FileName.EndsWith(".secrets") && f.Name == "actions-environment-secrets" select (f.FileName.Substring(0, f.FileName.Length - 8), des.Deserialize<IDictionary<string, string>>(new StreamReader(f.OpenReadStream())))).ToOrdinalIgnoreCaseDictionary(kv => kv.Item1, kv => (IDictionary<string, string>) kv.Item2.ToOrdinalIgnoreCaseDictionary());
            if(!secretsEnvironments.ContainsKey("") && secrets?.Length > 0) {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                LoadEnvSec(secrets, (k, v) => dict[k] = v);
                secretsEnvironments[""] = dict;
            }
            var varEnvironments = (from f in form.Files where f.FileName.EndsWith(".vars") && f.Name == "actions-environment-variables" select (f.FileName.Substring(0, f.FileName.Length - 5), des.Deserialize<IDictionary<string, string>>(new StreamReader(f.OpenReadStream())))).ToOrdinalIgnoreCaseDictionary(kv => kv.Item1, kv => (IDictionary<string, string>) kv.Item2.ToOrdinalIgnoreCaseDictionary());

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
            // For debugging purposes of missing logs in Runner.Client
            bool sendLostLogEvents = varEnvironments.TryGetValue("", out var fflags) && fflags.TryGetValue("system.runner.server.sendlostevents", out var fflagvalue) ? string.Equals(fflagvalue, "true", StringComparison.OrdinalIgnoreCase) : false;
            return new PushStreamResult(async stream => {
                var wait = requestAborted.WaitHandle;
                ConcurrentDictionary<Guid, Job> jobCache = new ConcurrentDictionary<Guid, Job>();
                await using(var writer = new StreamWriter(stream) { NewLine = "\n" } ) {
                    List<long> runid = new List<long>();
                    var queue2 = Channel.CreateUnbounded<KeyValuePair<string,string>>(new UnboundedChannelOptions { SingleReader = true });
                    var chwriter = queue2.Writer;
                    var serializerSettings = new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}};
                    Func<Job, Task> updateJob = async jobInstance => {
                        if(jobCache.TryAdd(jobInstance.JobId, jobInstance)) {
                            await chwriter.WriteAsync(new KeyValuePair<string, string>("job", JsonConvert.SerializeObject(jobInstance, serializerSettings)));
                        } else {
                            await chwriter.WriteAsync(new KeyValuePair<string, string>("job_update", JsonConvert.SerializeObject(jobInstance, serializerSettings)));
                        }
                    };
                    WebConsoleLogService.LogFeedEvent handler = async (sender, timelineId2, recordId, record) => {
                        var timeline = _webConsoleLogService.GetTimeLine(timelineId2);
                        if (timeline?.Any() == true && (_cache.TryGetValue(timeline[0].Id, out Job job) || initializingJobs.TryGetValue(timeline[0].Id, out job)) && runid.Contains(job.runid)) {
                            await updateJob(job);
                            await chwriter.WriteAsync(new KeyValuePair<string, string>("log", JsonConvert.SerializeObject(new { timelineId = timelineId2, recordId, record }, serializerSettings)));
                        } else if(sendLostLogEvents) {
                            // For debugging purposes of missing logs in Runner.Client
                            await chwriter.WriteAsync(new KeyValuePair<string, string>("log_lost", JsonConvert.SerializeObject(new { timelineId = timelineId2, recordId, record }, serializerSettings)));
                        }
                    };
                    TimelineController.TimeLineUpdateDelegate handler2 = async (timelineId2, timeline) => {
                        var timeline2 = _webConsoleLogService.GetTimeLine(timelineId2);
                        if(timeline2?.Any() == true && (_cache.TryGetValue(timeline2[0].Id, out Job job) || initializingJobs.TryGetValue(timeline2[0].Id, out job)) && runid.Contains(job.runid)) {
                            await updateJob(job);
                            await chwriter.WriteAsync(new KeyValuePair<string, string>("timeline", JsonConvert.SerializeObject(new { timelineId = timelineId2, timeline }, serializerSettings)));
                        } else if(sendLostLogEvents) {
                            // For debugging purposes of missing logs in Runner.Client
                            await chwriter.WriteAsync(new KeyValuePair<string, string>("timeline_lost", JsonConvert.SerializeObject(new { timelineId = timelineId2, timeline }, serializerSettings)));
                        }
                    };
                    MessageController.RepoDownload rd = (_runid, url, submodules, nestedSubmodules, repository, format, path) => {
                        if(runid.Contains(_runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>("repodownload", JsonConvert.SerializeObject(new { url, submodules, nestedSubmodules, repository, format, path }, serializerSettings)));
                        }
                    };

                    FinishJobController.JobCompleted completed = async (ev) => {
                        if((_cache.TryGetValue(ev.JobId, out Job job) || initializingJobs.TryGetValue(ev.JobId, out job)) && runid.Contains(job.runid)) {
                            await updateJob(job);
                            await chwriter.WriteAsync(new KeyValuePair<string, string>("finish", JsonConvert.SerializeObject(ev, serializerSettings)));
                        }
                    };

                    Action<MessageController.WorkflowEventArgs> onworkflow = workflow_ => {
                        lock(runid) {
                            if(runid.Remove(workflow_.runid)) {
                                var empty = runid.Count == 0;
                                Action delay = async () => {
                                    await chwriter.WriteAsync(new KeyValuePair<string, string>("workflow", JsonConvert.SerializeObject(workflow_, serializerSettings)));
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
                    _webConsoleLogService.LogFeed += handler;
                    TimelineController.TimeLineUpdate += handler2;
                    MessageController.OnRepoDownload += rd;
                    FinishJobController.OnJobCompleted += completed;
                    MessageController.workflowevent += onworkflow;
                    List<HookResponse> responses = new List<HookResponse>();
                    bool azpipelines = string.Equals(e, "azpipelines", StringComparison.OrdinalIgnoreCase);
                    lock(runid) {
                        if(workflow.Any()) {
                            foreach (var w in workflow) {
                                HookResponse response = Clone().ConvertYaml(w.Key, w.Value, string.IsNullOrEmpty(Repository) ? hook?.repository?.full_name ?? "Unknown/Unknown" : Repository, GitServerUrl, hook, obj.Value, e, job, list >= 1, env, secrets, matrix, platform, localcheckout ?? true, workflow, run => runid.Add(run), Ref: Ref, Sha: Sha, secretsProvider: new ScheduleSecretsProvider{ SecretsEnvironments = secretsEnvironments, VarEnvironments = varEnvironments }, rrunid: rrunid, jobId: jobId, failed: failed, rresetArtifacts: resetArtifacts, refresh: refresh, taskNames: taskNames, azure: azpipelines);
                                if(response.skipped || response.failed) {
                                    runid.Remove(response.run_id);
                                    if(response.failed) {
                                        chwriter.WriteAsync(new KeyValuePair<string, string>("workflow", JsonConvert.SerializeObject(new WorkflowEventArgs() { runid = response.run_id, Success = false }, serializerSettings)));
                                    }
                                }
                            }
                        }
                        if(runid.Count == 0) {
                            chwriter.WriteAsync(new KeyValuePair<string, string>(null, null));
                        }
                    }

                    try {
                        await ping;
                    } catch(OperationCanceledException) {

                    } finally {
                        _webConsoleLogService.LogFeed -= handler;
                        TimelineController.TimeLineUpdate -= handler2;
                        MessageController.OnRepoDownload -= rd;
                        FinishJobController.OnJobCompleted -= completed;
                        MessageController.workflowevent -= onworkflow;
                    }
                }
            }, "text/event-stream");
        }

        private static ConcurrentDictionary<Guid, Job> initializingJobs = new ConcurrentDictionary<Guid, Job>();

        [HttpGet]
        [SwaggerResponse(200, type: typeof(Job[]))]
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
            var query = from j in _context.Jobs where (repo == null || j.repo.ToLower() == repo.ToLower()) && (runid.Length == 0 || runid.Contains(j.runid)) orderby j.runid descending, j.WorkflowRunAttempt descending, j.RequestId descending select j;
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30).Include(j => j.WorkflowRunAttempt) : query.Include(j => j.WorkflowRunAttempt), true);
        }

        [HttpGet("owners")]
        [SwaggerResponse(200, type: typeof(Owner[]))]
        public async Task<IActionResult> GetOwners([FromQuery] int? page) {
            var query = from j in _context.Set<Owner>() orderby j.Id descending select j;
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30) : query, true);
        }

        [HttpGet("repositories")]
        [SwaggerResponse(200, type: typeof(Repository[]))]
        public async Task<IActionResult> GetRepositories([FromQuery] int? page, [FromQuery] string owner) {
            var query = from j in _context.Set<Repository>() where j.Owner.Name.ToLower() == owner.ToLower() orderby j.Id descending select j;
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30) : query, true);
        }

        [HttpGet("workflow/runs")]
        [SwaggerResponse(200, type: typeof(WorkflowRun[]))]
        public async Task<IActionResult> GetWorkflows([FromQuery] int? page, [FromQuery] string owner, [FromQuery] string repo) {
            if(!string.IsNullOrEmpty(owner)) {
                var own = await _context.Set<Owner>().Where(o => o.Name.ToLower() == owner.ToLower()).FirstOrDefaultAsync();
                if(own != null && !string.IsNullOrEmpty(repo)) {
                    var rep = await _context.Set<Repository>().Where(r => r.Owner == own && r.Name.ToLower() == repo.ToLower()).FirstOrDefaultAsync();
                    if(rep != null) {
                        var altquery = from run in _context.Set<WorkflowRun>() from attempt in _context.Set<WorkflowRunAttempt>() where run.Id == attempt.WorkflowRun.Id && attempt.Attempt == (from a in _context.Set<WorkflowRunAttempt>() where run.Id == a.WorkflowRun.Id orderby a.Attempt descending select a.Attempt).First() && attempt.WorkflowRun.Workflow.Repository == rep orderby run.Id descending select new WorkflowRun() { EventName = attempt.EventName, Ref = attempt.Ref, Sha = attempt.Sha, Result = attempt.Result, Status = attempt.Status, FileName = run.FileName, DisplayName = run.DisplayName, Id = run.Id, Owner = run.Workflow.Repository.Owner.Name, Repo = run.Workflow.Repository.Name};
                        return await Ok(page.HasValue ? altquery.Skip(page.Value * 30).Take(30) : altquery, true);
                    }
                }
            }

            var query = (from run in _context.Set<WorkflowRun>() from attempt in _context.Set<WorkflowRunAttempt>() where run.Id == attempt.WorkflowRun.Id && attempt.Attempt == (from a in _context.Set<WorkflowRunAttempt>() where run.Id == a.WorkflowRun.Id orderby a.Attempt descending select a.Attempt).First() && (string.IsNullOrEmpty(owner) || run.Workflow.Repository.Owner.Name.ToLower() == owner.ToLower()) && (string.IsNullOrEmpty(repo) || run.Workflow.Repository.Name.ToLower() == repo.ToLower()) orderby run.Id descending select new WorkflowRun() { EventName = attempt.EventName, Ref = attempt.Ref, Sha = attempt.Sha, Result = attempt.Result, Status = attempt.Status, FileName = run.FileName, DisplayName = run.DisplayName, Id = run.Id, Owner = run.Workflow.Repository.Owner.Name, Repo = run.Workflow.Repository.Name});
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30) : query, true);
        }

        [HttpGet("workflow/run/{id}")]
        [SwaggerResponse(200, type: typeof(WorkflowRun))]
        public async Task<IActionResult> GetWorkflows(long id) {
            return await Ok((from r in _context.Set<WorkflowRun>() where r.Id == id select r).First(), true);
        }

        [HttpGet("workflow/run/{id}/attempts")]
        [SwaggerResponse(200, type: typeof(WorkflowRunAttempt[]))]
        public async Task<IActionResult> GetWorkflowAttempts(long id, [FromQuery] int? page) {
            var query = from r in _context.Set<WorkflowRunAttempt>() where r.WorkflowRun.Id == id select r;
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30) : query, true);
        }

        [HttpGet("workflow/run/{id}/attempt/{attempt}")]
        [SwaggerResponse(200, type: typeof(WorkflowRunAttempt))]
        public async Task<IActionResult> GetWorkflows(long id, int attempt) {
            var query = from r in _context.Set<WorkflowRunAttempt>() where r.WorkflowRun.Id == id && r.Attempt == attempt select r;
            return await Ok(query.First(), true);
        }

        [HttpGet("workflow/run/{id}/attempt/{attempt}/jobs")]
        [SwaggerResponse(200, type: typeof(Job[]))]
        public async Task<IActionResult> GetWorkflowAttempts(long id, int attempt, [FromQuery] int? page) {
            var query = from j in _context.Jobs where j.WorkflowRunAttempt.WorkflowRun.Id == id && j.WorkflowRunAttempt.Attempt == attempt select j;
            return await Ok(page.HasValue ? query.Skip(page.Value * 30).Take(30) : query, true);
        }

        private void RerunWorkflow(long runid, Dictionary<string, List<Job>> finishedJobs = null, bool onLatestCommit = false, bool resetArtifacts = true) {
            var clone = Clone();
            Task.Run(() => clone.RerunWorkflow2(runid, finishedJobs, onLatestCommit, resetArtifacts));
        }
        private void RerunWorkflow2(long runid, Dictionary<string, List<Job>> finishedJobs = null, bool onLatestCommit = false, bool resetArtifacts = true) {
            string latestWorkflow = null;
            var run = (from r in _context.Set<WorkflowRun>() where r.Id == runid select r).First();
            var lastAttempt = (from a in _context.Entry(run).Collection(r => r.Attempts).Query() orderby a.Attempt descending select a).First();
            string latestSha = lastAttempt.Sha;
            var payloadObject = JObject.Parse(lastAttempt.EventPayload);
            var hook = payloadObject.ToObject<GiteaHook>();
            string repository_name = hook?.repository?.full_name ?? "Unknown/Unknown";
            if(onLatestCommit) {
                string githubAppToken = null;
                try {
                    if(string.IsNullOrEmpty(GITHUB_TOKEN)) {
                        githubAppToken = CreateGithubAppToken(repository_name).GetAwaiter().GetResult();
                    }
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("runner", string.IsNullOrEmpty(GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version) ? "0.0.0" : GitHub.Runner.Sdk.BuildConstants.RunnerPackage.Version));
                    if(!string.IsNullOrEmpty(!string.IsNullOrEmpty(GITHUB_TOKEN) ? GITHUB_TOKEN : githubAppToken)) {
                        client.DefaultRequestHeaders.Add("Authorization", $"token {(!string.IsNullOrEmpty(GITHUB_TOKEN) ? GITHUB_TOKEN : githubAppToken)}");
                    }
                    var cres = client.GetAsync(new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{repository_name}/commits")) { Query = $"?sha={Uri.EscapeDataString(lastAttempt.Ref ?? "")}&page=1&limit=1&per_page=1" }.ToString()).GetAwaiter().GetResult();
                    if(cres.IsSuccessStatusCode) {
                        var content = cres.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        var o = JsonConvert.DeserializeObject<GitCommit[]>(content)[0];
                        latestSha = o.Sha;
                    }

                    var url = new UriBuilder(new Uri(new Uri(GitApiServerUrl + "/"), $"repos/{repository_name}/contents/{Uri.EscapeDataString(run.FileName)}"));
                    url.Query = $"ref={Uri.EscapeDataString(latestSha)}";
                    var res = client.GetAsync(url.ToString()).GetAwaiter().GetResult();
                    if(res.IsSuccessStatusCode) {
                        var content = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        var item = Newtonsoft.Json.JsonConvert.DeserializeObject<UnknownItem>(content);
                        var fileRes = client.GetAsync(item.download_url).GetAwaiter().GetResult();
                        if(fileRes.IsSuccessStatusCode) {
                            latestWorkflow = fileRes.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        }
                    }
                } finally {
                    if(githubAppToken != null) {
                        DeleteGithubAppToken(githubAppToken).GetAwaiter().GetResult();
                    }
                }
            }
            var firstAttempt = (from a in _context.Entry(run).Collection(r => r.Attempts).Query() orderby a.Attempt ascending select a).First();

            int attempt = lastAttempt.Attempt + 1;
            var _attempt = new WorkflowRunAttempt() { Attempt = attempt, WorkflowRun = run, EventPayload = lastAttempt.EventPayload, EventName = lastAttempt.EventName, Workflow = latestWorkflow ?? lastAttempt.Workflow, Ref = lastAttempt.Ref, Sha = latestSha ?? lastAttempt.Sha, StatusCheckSha = lastAttempt.StatusCheckSha, TimeLineId = firstAttempt.TimeLineId, ArtifactsMinAttempt = resetArtifacts ? attempt : lastAttempt.ArtifactsMinAttempt };
            _context.Artifacts.Add(new ArtifactContainer() { Attempt = _attempt } );
            _context.SaveChanges();
            var e = lastAttempt.EventName;
            // Try to load the repository_name from our database
            var repository = (from w in _context.Entry(run).Reference(r => r.Workflow).Query().Include(w => w.Repository.Owner) select w.Repository).FirstOrDefault();

            if(!string.IsNullOrEmpty(repository?.Name) && !string.IsNullOrEmpty(repository?.Owner?.Name)) {
                repository_name = $"{repository.Owner.Name}/{repository.Name}";
            }
            runid = run.Id;
            long runnumber = run.Id;
            var Ref = _attempt.Ref;
            // Legacy compat of pre 3.6.0
            Ref = LegacyCompatFillRef(hook, e, Ref);
            string Sha = _attempt.Sha;
            Sha = LegacyCompatFillSha(hook, e, Sha);
            var xres = ConvertYaml2(run.FileName, _attempt.Workflow, repository_name, GitServerUrl, hook, payloadObject, e, null, false, null, null, null, null, false, runid, runnumber, Ref, Sha, attempt: _attempt, finishedJobs: finishedJobs, statusSha: !string.IsNullOrEmpty(_attempt.StatusCheckSha) ? _attempt.StatusCheckSha : (e == "pull_request_target" ? hook?.pull_request?.head?.Sha : Sha));
        }

        private string LegacyCompatFillRef(GiteaHook hook, string e, string Ref)
        {
            if (string.IsNullOrEmpty(Ref))
            {
                Ref = hook?.Ref;
                if (Ref == null)
                {
                    if (e == "pull_request_target")
                    {
                        var tmp = hook?.pull_request?.Base?.Ref;
                        if (tmp != null)
                        {
                            Ref = "refs/heads/" + tmp;
                        }
                    }
                    else if (e == "pull_request" && hook?.Number != null)
                    {
                        if (HasPullRequestMergePseudoBranch)
                        {
                            Ref = $"refs/pull/{hook.Number}/merge";
                        }
                        else
                        {
                            Ref = $"refs/pull/{hook.Number}/head";
                        }
                    }
                }
                else if (hook?.ref_type != null)
                {
                    if (e == "create")
                    {
                        // Fixup create hooks to have a git ref
                        if (hook?.ref_type == "branch")
                        {
                            Ref = "refs/heads/" + Ref;
                        }
                        else if (hook?.ref_type == "tag")
                        {
                            Ref = "refs/tags/" + Ref;
                        }
                        hook.After = hook?.Sha;
                    }
                    else
                    {
                        Ref = null;
                    }
                }
                if (Ref == null && hook?.repository?.default_branch != null)
                {
                    Ref = "refs/heads/" + hook?.repository?.default_branch;
                }
            }

            return Ref;
        }

        private string LegacyCompatFillSha(GiteaHook hook, string e, string Sha)
        {
            if (string.IsNullOrEmpty(Sha))
            {
                if (e == "pull_request_target")
                {
                    Sha = hook?.pull_request?.Base?.Sha;
                }
                else if (e == "pull_request")
                {
                    if (!HasPullRequestMergePseudoBranch)
                    {
                        Sha = hook?.pull_request?.head?.Sha;
                    }
                    else
                    {
                        Sha = hook?.pull_request?.merge_commit_sha;
                    }
                }
                else
                {
                    Sha = hook.After;
                }
            }

            return Sha;
        }

        private Dictionary<string, List<Job>> getJobsWithout(Job job) {
            var finishedJobs = new Dictionary<string, List<Job>>(StringComparer.OrdinalIgnoreCase);
            foreach(var _job in (from j in _context.Jobs where j.runid == job.runid && j.WorkflowIdentifier != null && (j.WorkflowIdentifier != job.WorkflowIdentifier || (j.Matrix != job.Matrix && job.Matrix != null)) select j).Include(z => z.Outputs).Include(z => z.WorkflowRunAttempt)) {
                if((job.WorkflowIdentifier != _job.WorkflowIdentifier || !job.MatrixToken.DeepEquals(_job.MatrixToken)) && !finishedJobs.TryAdd(_job.WorkflowIdentifier, new List<Job> { _job })) {
                    bool found = false;
                    for(int i = 0; i < finishedJobs[_job.WorkflowIdentifier].Count; i++) {
                        if(finishedJobs[_job.WorkflowIdentifier][i].MatrixToken.DeepEquals(_job.MatrixToken)) {
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
            return finishedJobs;
        }

        [HttpPost("rerun/{id}")]
        public void RerunJob(Guid id, [FromQuery] bool onLatestCommit) {
            Job job = GetJob(id);
            RerunWorkflow(job.runid, getJobsWithout(job), onLatestCommit, false);
        }

        [HttpPost("rerunworkflow/{id}")]
        public void PostRerunWorkflow(long id, [FromQuery] bool onLatestCommit) {
            RerunWorkflow(id, onLatestCommit: onLatestCommit);
        }

        private Dictionary<string, List<Job>> getPreviousJobs(long id) {
            var finishedJobs = new Dictionary<string, List<Job>>(StringComparer.OrdinalIgnoreCase);
            foreach(var _job in (from j in _context.Jobs where j.runid == id && j.WorkflowIdentifier != null select j).Include(z => z.Outputs).Include(z => z.WorkflowRunAttempt)) {
                if(!finishedJobs.TryAdd(_job.WorkflowIdentifier, new List<Job> { _job })) {
                    bool found = false;
                    for(int i = 0; i < finishedJobs[_job.WorkflowIdentifier].Count; i++) {
                        if(finishedJobs[_job.WorkflowIdentifier][i].MatrixToken.DeepEquals(_job.MatrixToken)) {
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
            return finishedJobs;
        }

        private Dictionary<string, List<Job>> getFailedJobs(long id) {
            return getPreviousJobs(id).ToDictionary(kv => kv.Key, kv => kv.Value.Where(j => j.Result == TaskResult.Succeeded).ToList(), StringComparer.OrdinalIgnoreCase);
        }

        [HttpPost("rerunFailed/{id}")]
        public void RerunFailedJobs(long id, [FromQuery] bool onLatestCommit) {
            RerunWorkflow(id, getFailedJobs(id), onLatestCommit, false);
        }

        [HttpPost("cancel/{id}")]
        public void CancelJob(Guid id, [FromQuery] bool force) {
            Job job = _cache.Get<Job>(id);
            if(job != null) {
                job.CancelRequest.Cancel();
                if(job.SessionId == Guid.Empty || force) {
                    InvokeJobCompleted(new JobCompletedEvent() { JobId = job.JobId, Result = TaskResult.Canceled, RequestId = job.RequestId, Outputs = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase) });
                }
            }
        }

        [HttpPost("cancelWorkflow/{runid}")]
        public void CancelWorkflow(long runid) {
            if(WorkflowStates.TryGetValue(runid, out var source)) {
                source.Cancel.Cancel();
            }
        }

        [HttpPost("forceCancelWorkflow/{runid}")]
        public void ForceCancelWorkflow(long runid) {
            if(WorkflowStates.TryGetValue(runid, out var source)) {
                source.ForceCancel.Cancel();
                source.Cancel.Cancel();
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
                try {
                    var stream = context.HttpContext.Response.Body;
                    context.HttpContext.Response.GetTypedHeaders().ContentType = new Microsoft.Net.Http.Headers.MediaTypeHeaderValue(_contentType);
                    await _onStreamAvailabe(stream);
                } catch(Exception ex) {
                    Console.Error.WriteLine($"{ex.Message}\nStacktrace: {ex.StackTrace}");
                }
            }
        }

        private delegate void JobEvent(object sender, string repo, Job job);
        private static event JobEvent jobevent;
        private static event JobEvent jobupdateevent;
        private static event Action<Owner> ownerevent;
        private static event Action<Repository> repoevent;
        private static event Action<string, string, WorkflowRun> runevent;
        private static event Action<string, string, WorkflowRun> runupdateevent;
        public class WorkflowEventArgs {
            public long runid {get;set;}
            public bool Success {get;set;}
            public Dictionary<string, VariableValue> Outputs {get;set;}
        }
        public static event Action<WorkflowEventArgs> workflowevent;

        [HttpGet("event")]
        [SwaggerResponse(200, contentTypes: new[]{"text/event-stream"})]
        public IActionResult Message(string owner, string repo, [FromQuery] string filter, [FromQuery] long? runid)
        {
            var mfilter = new WorkflowPattern(filter ?? (owner + "/" + repo), RegexOptions.IgnoreCase);
            var requestAborted = HttpContext.RequestAborted;
            return new PushStreamResult(async stream => {
                var wait = requestAborted.WaitHandle;
                await using(var writer = new StreamWriter(stream) { NewLine = "\n" } ) {
                    var queue = Channel.CreateUnbounded<KeyValuePair<string,Job>>(new UnboundedChannelOptions { SingleReader = true });
                    var chwriter = queue.Writer;
                    JobEvent handler = (sender, crepo, job) => {
                        if (mfilter.Regex.IsMatch(crepo) && (runid == null || runid == job.runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, Job>(crepo, job));
                        }
                    };
                    var chreader = queue.Reader;
                    var ping = Task.Run(async () => {
                        try {
                            while(!requestAborted.IsCancellationRequested) {
                                KeyValuePair<string, Job> p = await chreader.ReadAsync(requestAborted);
                                await writer.WriteLineAsync("event: job");
                                await writer.WriteLineAsync(string.Format("data: {0}", JsonConvert.SerializeObject(new { repo = p.Key, job = p.Value }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                                await writer.WriteLineAsync();
                                await writer.FlushAsync();
                            }
                        } catch(OperationCanceledException) {
                            
                        }
                    }, requestAborted);
                    jobevent += handler;
                    try {
                        await ping;
                    } catch(OperationCanceledException) {

                    } finally {
                        jobevent -= handler;
                    }
                }
            }, "text/event-stream");
        }

        [HttpGet("event2")]
        [SwaggerResponse(200, contentTypes: new[]{"text/event-stream"})]
        public IActionResult LiveUpdateEvents([FromQuery] string owner, [FromQuery] string repo, [FromQuery] long? runid)
        {
            var requestAborted = HttpContext.RequestAborted;
            return new PushStreamResult(async stream => {
                var wait = requestAborted.WaitHandle;
                await using(var writer = new StreamWriter(stream) { NewLine = "\n" } ) {
                    var queue2 = Channel.CreateUnbounded<KeyValuePair<string,object>>(new UnboundedChannelOptions { SingleReader = true });
                    var chwriter = queue2.Writer;
                    JobEvent handler = (sender, crepo, job) => {
                        var repoowner = crepo.Split("/", 2);
                        if((string.IsNullOrEmpty(owner) || owner.ToLower() == repoowner[0].ToLower()) && (string.IsNullOrEmpty(repo) || repo.ToLower() == repoowner[1].ToLower()) && (runid == null || runid == job.runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, object>("job", job));
                        }
                    };
                    JobEvent updatehandler = (sender, crepo, job) => {
                        var repoowner = crepo.Split("/", 2);
                        if((string.IsNullOrEmpty(owner) || owner.ToLower() == repoowner[0].ToLower()) && (string.IsNullOrEmpty(repo) || repo.ToLower() == repoowner[1].ToLower()) && (runid == null || runid == job.runid)) {
                            chwriter.WriteAsync(new KeyValuePair<string, object>("jobupdate", job));
                        }
                    };
                    Action<Owner> ownerh = cowner => {
                        if(string.IsNullOrEmpty(owner)) {
                            chwriter.WriteAsync(new KeyValuePair<string, object>("owner", cowner));
                        }
                    };
                    Action<Repository> repoh = crepo => {
                        if((string.IsNullOrEmpty(owner) || owner.ToLower() == crepo.Owner.Name.ToLower()) && string.IsNullOrEmpty(repo)) {
                            chwriter.WriteAsync(new KeyValuePair<string, object>("repo", crepo));
                        }
                    };
                    Action<string, string, WorkflowRun> runh = (cowner, crepo, crun) => {
                        if((string.IsNullOrEmpty(repo) || repo.ToLower() == crepo.ToLower()) && (string.IsNullOrEmpty(owner) || owner.ToLower() == cowner.ToLower()) && runid == null) {
                            chwriter.WriteAsync(new KeyValuePair<string, object>("workflowrun", crun));
                        }
                    };
                    Action<string, string, WorkflowRun> runupdateh = (cowner, crepo, crun) => {
                        if((string.IsNullOrEmpty(repo) || repo.ToLower() == crepo.ToLower()) && (string.IsNullOrEmpty(owner) || owner.ToLower() == cowner.ToLower()) && runid == null) {
                            chwriter.WriteAsync(new KeyValuePair<string, object>("workflowrunupdate", crun));
                        }
                    };
                    var chreader = queue2.Reader;
                    var ping = Task.Run(async () => {
                        try {
                            while(!requestAborted.IsCancellationRequested) {
                                KeyValuePair<string, object> p = await chreader.ReadAsync(requestAborted);
                                string value = "";
                                try {
                                    value = JsonConvert.SerializeObject(p.Value, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}});
                                } catch (Exception ex) {
                                    p = new KeyValuePair<string, object>("error", null);
                                    value = ex.ToString();
                                }
                                await writer.WriteLineAsync($"event: {p.Key}");
                                await writer.WriteLineAsync($"data: {value}");
                                await writer.WriteLineAsync();
                                await writer.FlushAsync();
                            }
                        } catch {

                        }
                    }, requestAborted);
                    jobevent += handler;
                    jobupdateevent += updatehandler;
                    ownerevent += ownerh;
                    repoevent += repoh;
                    runevent += runh;
                    runupdateevent += runupdateh;
                    try {
                        await ping;
                    } catch(OperationCanceledException) {

                    } finally {
                        runupdateevent -= runupdateh;
                        runevent -= runh;
                        repoevent -= repoh;
                        ownerevent -= ownerh;
                        jobupdateevent -= updatehandler;
                        jobevent -= handler;
                    }
                }
            }, "text/event-stream");
        }

        [AllowAnonymous]
        [HttpGet("gitserverurl")]
        public string GetGitServerUrl() {
            return GitServerUrl;
        }

        private void InvokeJobCompleted(JobCompletedEvent ev) {
            new FinishJobController(_cache, _context, Configuration, _webConsoleLogService).InvokeJobCompleted(ev);
        }

        private Task<List<TimelineRecord>> UpdateTimeLine(Guid timelineId, List<TimelineRecord> patch, bool outOfSyncTimeLineUpdate = false) {
            return new TimelineController(_context, Configuration).UpdateTimeLine(timelineId, patch, outOfSyncTimeLineUpdate);
        }

        private Task<List<TimelineRecord>> UpdateTimeLine((Guid, List<TimelineRecord>) pair, bool outOfSyncTimeLineUpdate = false) {
            return new TimelineController(_context, Configuration).UpdateTimeLine(pair.Item1, pair.Item2, outOfSyncTimeLineUpdate);
        }

        private void SyncLiveLogsToDb(Guid timelineId) {
            _webConsoleLogService.SyncLiveLogsToDb(timelineId);
        }

        private void DeleteAgent(int poolId, ulong agentId) {
            new AgentController(_cache, _context, Configuration).Delete(poolId, agentId);
        }

        private Task<ArtifactFileContainer> CreateArtifactContainer(long run, long attempt, CreateActionsStorageArtifactParameters req, long artifactsMinAttempt = -1) {
            return new ArtifactController(_context, Configuration).CreateContainer(run, attempt, req, artifactsMinAttempt);
        }
    }
}
