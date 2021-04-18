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

namespace Runner.Server.Controllers
{
    [ApiController]
    [Route("{owner}/{repo}/_apis/v1/[controller]")]
    public class MessageController : VssControllerBase
    {
        private string GitServerUrl;
        private string GitApiServerUrl;
        private IMemoryCache _cache;
        private string GITHUB_TOKEN;
        private List<Secret> secrets;

        private class Secret {
            public string Name {get;set;}
            public string Value {get;set;}
        }

        public MessageController(IConfiguration configuration, IMemoryCache memoryCache)
        {
            GitServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitServerUrl") ?? "";
            GitApiServerUrl = configuration.GetSection("Runner.Server")?.GetValue<String>("GitApiServerUrl") ?? "";
            GITHUB_TOKEN = configuration.GetSection("Runner.Server")?.GetValue<String>("GITHUB_TOKEN") ?? "";
            secrets = configuration.GetSection("Runner.Server:Secrets")?.Get<List<Secret>>() ?? new List<Secret>();
            _cache = memoryCache;
        }

        [HttpDelete("{poolId}/{messageId}")]
        public IActionResult DeleteMessage(int poolId, long messageId, Guid sessionId)
        {
            Session session;
            if(_cache.TryGetValue(sessionId, out session) && session.TaskAgentSession.SessionId == sessionId) {
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
                            if(dictionary.TryGetValue(key, out otherv)) {
                                Equals(value, otherv);
                            } else {
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

        enum Stat {
            Success,
            Failure,
            Cancelled
        }

        interface IJobCtx {
            Stat? Status {get;}
        }

        interface IExecutionContext {
            IJobCtx JobContext {get; }
        }

        public sealed class SuccessFunction : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var templateContext = evaluationContext.State as TemplateContext;
                // ArgUtil.NotNull(templateContext, nameof(templateContext));
                var executionContext = templateContext.State[nameof(IExecutionContext)] as IExecutionContext;
                // ArgUtil.NotNull(executionContext, nameof(executionContext));
                Stat jobStatus = executionContext.JobContext.Status ?? Stat.Success;
                return jobStatus == Stat.Success;
            }
        }

        public sealed class FailureFunction : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var templateContext = evaluationContext.State as TemplateContext;
                // ArgUtil.NotNull(templateContext, nameof(templateContext));
                var executionContext = templateContext.State[nameof(IExecutionContext)] as IExecutionContext;
                // ArgUtil.NotNull(executionContext, nameof(executionContext));
                Stat jobStatus = executionContext.JobContext.Status ?? Stat.Success;
                return jobStatus == Stat.Failure;
            }
        }
        public sealed class CancelledFunction : Function
        {
            protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
            {
                resultMemory = null;
                var templateContext = evaluationContext.State as TemplateContext;
                // ArgUtil.NotNull(templateContext, nameof(templateContext));
                var executionContext = templateContext.State[nameof(IExecutionContext)] as IExecutionContext;
                // ArgUtil.NotNull(executionContext, nameof(executionContext));
                Stat jobStatus = executionContext.JobContext.Status ?? Stat.Success;
                return jobStatus == Stat.Cancelled;
            }
        }

        class JobCtx : IJobCtx
        {
            public Stat? Status { get; set;}
        }

        class ExecutionContext : IExecutionContext
        {
            public IJobCtx JobContext { get; set; }
        }

        class JobItem {
            public string name {get;set;}
            public string[] Needs {get;set;}
            public AgentJobRequestMessage message {get;set;}

            public FinishJobController.JobCompleted OnJobEvaluatable { get;set;}

            public Guid Id { get; set;}
            public Guid TimelineId { get; set;}

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
                Console.Error.WriteLine(format, args);
            }

            public void Info(string format, params object[] args)
            {
                Console.Out.WriteLine(format, args);
            }

            public void Info(string message)
            {
                Console.Out.WriteLine(message);
            }

            public void Verbose(string format, params object[] args)
            {
                Console.Out.WriteLine(format, args);
            }

            public void Verbose(string message)
            {
                Console.Out.WriteLine(message);
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
            public string repo;
            public long run_id;
            public bool skipped;
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

        private static ConcurrentDictionary<long, List<JobItem>> dependentjobgroups = new ConcurrentDictionary<long, List<JobItem>>();

        private HookResponse ConvertYaml(string fileRelativePath, string content, string repository, string giteaUrl, GiteaHook hook, JObject payloadObject, string e = "push", string selectedJob = null, bool list = false, string[] env = null, string[] secrets = null, string[] _matrix = null) {
            string repository_name = hook?.repository?.full_name ?? "Unknown/Unknown";
            var runid = _cache.GetOrCreate(repository_name, e => new Int64());
            _cache.Set(repository_name, runid + 1);
            var runnumberkey = $"{repository_name}:/{fileRelativePath}";
            long runnumber = _cache.GetOrCreate(runnumberkey, e => new Int64());
            _cache.Set(runnumberkey, runnumber + 1);
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
                    TraceWriter = new TraceWriter()
                };
                ExecutionContext exctx = new ExecutionContext();
                exctx.JobContext = new JobCtx() {};
                templateContext.State[nameof(IExecutionContext)] = exctx;
                templateContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
                templateContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
                templateContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, 0));
                templateContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, 0));
                foreach (var func in ExpressionConstants.WellKnownFunctions.Values)
                {
                    templateContext.ExpressionFunctions.Add(func);
                }
                // TemplateConstants.False
                templateContext.Schema = PipelineTemplateSchemaFactory.GetSchema();

                var token = default(TemplateToken);
                // Get the file ID
                var fileId = templateContext.GetFileId(fileRelativePath);

                // Read the file
                var fileContent = content ?? System.IO.File.ReadAllText(fileRelativePath);
                using (var stringReader = new StringReader(fileContent))
                {
                    var yamlObjectReader = new YamlObjectReader(fileId, stringReader);
                    token = TemplateReader.Read(templateContext, "workflow-root", yamlObjectReader, fileId, out _);
                }

                TemplateToken workflowDefaults = null;
                List<TemplateToken> workflowEnvironment = new List<TemplateToken>();
                if(env?.Length > 0) {
                    workflowEnvironment.Add(LoadEnv(env));
                }

                templateContext.Errors.Check();
                if(token == null) {
                    throw new Exception("token is null after parsing your workflow, this should never happen");
                }
                var actionMapping = token.AssertMapping("root");

                Action<JobCompletedEvent> jobCompleted = e => {
                    foreach (var item in dependentjobgroup.ToArray()) {
                        item.OnJobEvaluatable(e);
                    }
                };

                var Ref = hook?.Ref;
                if(Ref == null) {
                    if(e == "pull_request_target") {
                        var tmp = hook?.pull_request?.Base?.Ref;
                        if(tmp != null) {
                            Ref = "refs/heads/" + tmp;
                        }
                    } else if(e == "pull_request" && hook?.Number != null) {
                        if(hook?.merge_commit_sha != null) {
                            Ref = $"refs/pull/{hook.Number}/merge";
                        } else {
                            Ref = $"refs/pull/{hook.Number}/head";
                        }
                    }
                }
                var Sha = hook.After;
                if(Sha == null || Sha.Length == 0) {
                    if(e == "pull_request_target" && hook?.pull_request?.Base?.Sha != null) {
                        Sha = hook?.pull_request?.Base?.Sha;
                    } else if(e == "pull_request" && hook?.pull_request?.head?.Sha != null) {
                        if(hook?.merge_commit_sha == null) {
                            Sha = hook?.pull_request?.head?.Sha;
                        } else {
                            Sha = hook.merge_commit_sha;
                        }
                    }
                }

                TemplateToken tk = (from r in actionMapping where r.Key.AssertString("on").Value == "on" select r).FirstOrDefault().Value;
                switch(tk.Type) {
                    case TokenType.String:
                        if(tk.AssertString("str").Value != e) {
                            // Skip, not the right event
                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                        }
                        break;
                    case TokenType.Sequence:
                        if((from r in tk.AssertSequence("seq") where r.AssertString(e).Value == e select r).FirstOrDefault() == null) {
                            // Skip, not the right event
                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                        }
                        break;
                    case TokenType.Mapping:
                        var e2 = (from r in tk.AssertMapping("seq") where r.Key.AssertString(e).Value == e select r).FirstOrDefault();
                        if(e == "schedule") {
                            var crons = e2.Value?.AssertSequence("cron");
                            if(crons == null) {
                                return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                            }
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
                            
                            //TODO validate cron and handle it
                        } else {
                            var rawEvent = e2.Value;
                            if(rawEvent == null) {
                                // Skip, not the right event
                                return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                            }
                            if(rawEvent.Type != TokenType.Null) {
                                var push = rawEvent.AssertMapping($"expected mapping for event '{e}'");
                                List<string> allowed = new List<string>();
                                allowed.Add("types");

                                if(e == "push" || e == "pull_request" || e == "workflow_run") {
                                    allowed.Add("branches");
                                    allowed.Add("branches-ignore");
                                }
                                if(e == "push" || e == "pull_request") {
                                    allowed.Add("tags");
                                    allowed.Add("tags-ignore");
                                }

                                if(!push.All(p => allowed.Any(s => s == p.Key.AssertString("Key").Value))) {
                                    var z = 0;
                                    var sb = new StringBuilder();
                                    foreach (var prop in (from p in push where !allowed.Any(s => s == p.Key.AssertString("Key").Value) select p.Key.AssertString("Key").Value)) {
                                        if(z++ != 0) {
                                            sb.Append(", ");
                                        }
                                        sb.Append(prop);
                                    }
                                    throw new Exception($"The following event properties are invalid: {sb.ToString()}, please remove from {e}");
                                }

                                // Offical github action server ignores the filter on non push / pull_request (workflow_run) events
                                var branches = (from r in push where r.Key.AssertString("branches").Value == "branches" select r).FirstOrDefault().Value?.AssertSequence("seq");
                                var branchesIgnore = (from r in push where r.Key.AssertString("branches-ignore").Value == "branches-ignore" select r).FirstOrDefault().Value?.AssertSequence("seq");
                                var tags = (from r in push where r.Key.AssertString("tags").Value == "tags" select r).FirstOrDefault().Value?.AssertSequence("seq");
                                var tagsIgnore = (from r in push where r.Key.AssertString("tags-ignore").Value == "tags-ignore" select r).FirstOrDefault().Value?.AssertSequence("seq");
                                var paths = (from r in push where r.Key.AssertString("paths").Value == "paths" select r).FirstOrDefault().Value?.AssertSequence("seq");
                                var pathsIgnore = (from r in push where r.Key.AssertString("paths-ignore").Value == "paths-ignore" select r).FirstOrDefault().Value?.AssertSequence("seq");
                                var types = (from r in push where r.Key.AssertString("types").Value == "types" select r).FirstOrDefault().Value?.AssertSequence("seq");

                                if(branches != null && branchesIgnore != null) {
                                    throw new Exception("branches and branches-ignore shall not be used at the same time");
                                }
                                if(tags != null && tagsIgnore != null) {
                                    throw new Exception("tags and tags-ignore shall not be used at the same time");
                                }
                                if(paths != null && pathsIgnore != null) {
                                    throw new Exception("paths and paths-ignore shall not be used at the same time");
                                }

                                
                                if(types != null && hook?.Action != null) {
                                    if(!(from t in types select t.AssertString("type").Value).Any(t => t == hook?.Action)) {
                                        return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                                    }
                                }

                                var heads = "refs/heads/";
                                var rtags = "refs/tags/";

                                var Ref2 = hook?.Ref;
                                if(Ref2 == null) {
                                    if(e == "pull_request_target" || e == "pull_request") {
                                        var tmp = hook?.pull_request?.Base?.Ref;
                                        if(tmp != null) {
                                            Ref2 = "refs/heads/" + tmp;
                                        }
                                    }
                                }
                                if(Ref2 != null) {
                                    if(Ref2.StartsWith(heads) == true) {
                                        var branch = Ref2.Substring(heads.Length);

                                        if(branchesIgnore != null && filter(CompileMinimatch(branchesIgnore), new[] { branch })) {
                                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                                        }
                                        if(branches != null && skip(CompileMinimatch(branches), new[] { branch })) {
                                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                                        }
                                    } else if(Ref2.StartsWith(rtags) == true) {
                                        var tag = Ref2.Substring(rtags.Length);

                                        if(tagsIgnore != null && filter(CompileMinimatch(tagsIgnore), new[] { tag })) {
                                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                                        }
                                        if(tags != null && skip(CompileMinimatch(tags), new[] { tag })) {
                                            return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                                        }
                                    }
                                }
                                if(hook.Commits != null) {
                                    var changedFiles = hook.Commits.SelectMany(commit => (commit.Added ?? new List<string>()).Concat(commit.Removed ?? new List<string>()).Concat(commit.Modified ?? new List<string>()));
                                    if(pathsIgnore != null && filter(CompileMinimatch(pathsIgnore), changedFiles)) {
                                        return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                                    }
                                    if(paths != null && skip(CompileMinimatch(paths), changedFiles)) {
                                        return new HookResponse { repo = repository_name, run_id = runid, skipped = true };
                                    }
                                }
                            }
                        }
                        break;
                }

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
                            var ctx = new JobCtx() { Status = null };
                            exctx.JobContext = ctx;
                            var jn = job.Key.AssertString($"action.yml property key");
                            var jobname = jn.Value;
                            var run = job.Value.AssertMapping("jobs");
                            var jobitem = new JobItem() { name = jobname };
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
                            Dictionary<Guid, JobItem> guids = new Dictionary<Guid, JobItem>();
                            var contextData = new GitHub.DistributedTask.Pipelines.ContextData.DictionaryContextData();
                            var githubctx = new DictionaryContextData();
                            contextData.Add("github", githubctx);
                            githubctx.Add("server_url", new StringContextData(GitServerUrl));
                            githubctx.Add("api_url", new StringContextData(GitApiServerUrl));
                            var workflowname = (from r in actionMapping where r.Key.AssertString("name").Value == "name" select r).FirstOrDefault().Value?.AssertString("val").Value ?? fileRelativePath;
                            githubctx.Add("workflow", new StringContextData(workflowname));
                            githubctx.Add("repository", new StringContextData(repository_name));
                            githubctx.Add("sha", new StringContextData(Sha ?? "000000000000000000000000000000000"));
                            githubctx.Add("repository_owner", new StringContextData(hook?.repository?.Owner?.login ?? "Unknown"));
                            githubctx.Add("ref", new StringContextData(Ref));
                            githubctx.Add("job", new StringContextData(jobname));
                            githubctx.Add("head_ref", new StringContextData(hook?.pull_request?.head?.Ref ?? ""));// only for PR
                            githubctx.Add("base_ref", new StringContextData(hook?.pull_request?.Base?.Ref ?? ""));// only for PR
                            // event_path is filled by event
                            githubctx.Add("event", payloadObject.ToPipelineContextData());
                            githubctx.Add("event_name", new StringContextData(e));
                            githubctx.Add("actor", new StringContextData(hook?.sender?.login));
                            githubctx.Add("run_id", new StringContextData(runid.ToString()));
                            githubctx.Add("run_number", new StringContextData(runnumber.ToString()));
                            var needsctx = new DictionaryContextData();
                            contextData.Add("needs", needsctx);
                            var strategyctx = new DictionaryContextData();
                            contextData.Add("strategy", strategyctx);

                            FinishJobController.JobCompleted handler = e => {
                                if(neededJobs.Count > 0) {
                                    neededJobs.RemoveAll(name => {
                                        bool ret = false;
                                        foreach(var njb in from j in jobgroup where j.name == name select j) {
                                            ret = true;
                                            guids[njb.Id] = njb;
                                        }
                                        return ret;
                                    });
                                }
                                JobItem job;
                                if(e != null && guids.TryGetValue(e.JobId, out job) && job != null) {
                                    NeedsTaskResult? oldstatus = null;
                                    PipelineContextData oldjobctx;
                                    if(needsctx.TryGetValue(job.name, out oldjobctx) && oldjobctx is DictionaryContextData _ctx && _ctx.ContainsKey("result") && _ctx["result"] is StringContextData res) {
                                        oldstatus = Enum.Parse<NeedsTaskResult>(res, true);
                                    }
                                    DictionaryContextData jobctx = new DictionaryContextData();
                                    needsctx[job.name] = jobctx;
                                    var outputsctx = new DictionaryContextData();
                                    jobctx["outputs"] = outputsctx;
                                    foreach (var item in e.Outputs) {
                                        outputsctx.Add(item.Key, new StringContextData(item.Value.Value));
                                    }
                                    NeedsTaskResult result = NeedsTaskResult.Failure;
                                    switch(e.Result) {
                                        case TaskResult.Failed:
                                        case TaskResult.Abandoned:
                                            result = NeedsTaskResult.Failure;
                                            break;
                                        case TaskResult.Canceled:
                                            result = NeedsTaskResult.Cancelled;
                                            break;
                                        case TaskResult.Succeeded:
                                        case TaskResult.SucceededWithIssues:
                                            result = NeedsTaskResult.Success;
                                            break;
                                        case TaskResult.Skipped:
                                            result = NeedsTaskResult.Skipped;
                                            break;
                                    }
                                    jobctx.Add("result", new StringContextData(result.ToString().ToLower()));
                                    if(e.Result != TaskResult.Succeeded && e.Result != TaskResult.SucceededWithIssues) {
                                        ctx.Status = Stat.Failure;
                                    }
                                    guids.Remove(job.Id);
                                    if(guids.Count == 0 && neededJobs.Count == 0) {
                                        FinishJobController.OnJobCompleted -= jobitem.OnJobEvaluatable;
                                    }
                                }
                                if(guids.Count > 0 || neededJobs.Count > 0) {
                                    return;
                                }
                                dependentjobgroup.Remove(jobitem);
                                if(!dependentjobgroup.Any()) {
                                    jobgroup.Clear();
                                    dependentjobgroups.TryRemove(runid, out _);
                                }

                                exctx.JobContext = ctx;
                                var ifexpr = (from r in run where r.Key.AssertString("str").Value == "if" select r).FirstOrDefault().Value;//?.AssertString("if")?.Value;
                                var condition = new BasicExpressionToken(null, null, null, PipelineTemplateConverter.ConvertToIfCondition(templateContext, ifexpr, true));
                                templateContext.ExpressionValues.Clear();
                                foreach (var pair in contextData) {
                                    templateContext.ExpressionValues[pair.Key] = pair.Value;
                                }

                                var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.JobIfResult, condition, 0, fileId, true);
                                bool _res = PipelineTemplateConverter.ConvertToIfResult(templateContext, eval);
                                if(!_res) {
                                    jobCompleted(new JobCompletedEvent() { JobId = jobitem.Id, Result = TaskResult.Skipped });
                                    return;
                                }
                                
                                var rawstrategy = (from r in run where r.Key.AssertString("strategy").Value == "strategy" select r).FirstOrDefault().Value;
                                
                                if (rawstrategy != null)
                                {
                                    var strategy = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.Strategy, rawstrategy, 0, fileId, true)?.AssertMapping("strategy");
                                    strategyctx.Add("fail-fast", new BooleanContextData((from r in strategy where r.Key.AssertString("fail-fast").Value == "fail-fast" select r).FirstOrDefault().Value?.AssertBoolean("fail-fast")?.Value ?? true));
                                    var max_parallel = (from r in strategy where r.Key.AssertString("max-parallel").Value == "max-parallel" select r).FirstOrDefault().Value?.AssertNumber("max-parallel")?.Value;
                                    strategyctx.Add("max-parallel", max_parallel.HasValue ? new NumberContextData(max_parallel.Value) : null);
                                    var matrix = (from r in strategy where r.Key.AssertString("matrix").Value == "matrix" select r).FirstOrDefault().Value?.AssertMapping("matrix");
                                    if(matrix != null) {
                                        SequenceToken include = null;
                                        SequenceToken exclude = null;
                                        var flatmatrix = new List<Dictionary<string, TemplateToken>> { new Dictionary<string, TemplateToken>() };
                                        var includematrix = new List<Dictionary<string, TemplateToken>> { };
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
                                                    return true;
                                                });
                                            }
                                        }
                                        if(flatmatrix.Count == 0) {
                                            // Fix empty matrix after exclude
                                            flatmatrix.Add(new Dictionary<string, TemplateToken>());
                                        }
                                        var keys = flatmatrix.First().Keys.ToArray();
                                        if (include != null) {
                                            foreach (var item in include) {
                                                var map = item.AssertMapping("include item").ToDictionary(k => k.Key.AssertString("key").Value, k => k.Value);
                                                bool matched = false;
                                                if(keys.Length > 0) {
                                                    flatmatrix.ForEach(dict => {
                                                        foreach (var item in keys) {
                                                            TemplateToken val;
                                                            if (dict.TryGetValue(item, out val) && !TemplateTokenEqual(dict[item], val)) {
                                                                return;
                                                            }
                                                        }
                                                        matched = true;
                                                        // Add missing keys
                                                        foreach (var item in map) {
                                                            dict[item.Key] = item.Value;
                                                        }
                                                    });
                                                }
                                                if (!matched) {
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
                                            
                                            flatmatrix.RemoveAll(dict => {
                                                foreach(var kv in mdict) {
                                                    TemplateToken val;
                                                    if (dict.TryGetValue(kv.Key, out val) && TemplateTokenEqual(kv.Value, val)) {
                                                        return false;
                                                    }
                                                }
                                                return true;
                                            });
                                        }
                                        var jobTotal = flatmatrix.Count + includematrix.Count;
                                        if(keys.Length == 0 && jobTotal > 1) {
                                            jobTotal--;
                                        }
                                        strategyctx.Add("job-total", new NumberContextData( jobTotal ));
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
                                            Action<string, Dictionary<string, TemplateToken>> act = (displayname, item) => {
                                                int c = i++;
                                                strategyctx["job-index"] = new NumberContextData((double)(c));
                                                var matrixContext = new DictionaryContextData();
                                                foreach (var mk in item) {
                                                    PipelineContextData data = mk.Value.ToContextData();
                                                    matrixContext.Add(mk.Key, data);
                                                }
                                                contextData["matrix"] = matrixContext;
                                                var next = new JobItem() { name = jobitem.name, Id = Guid.NewGuid(), TimelineId = Guid.NewGuid() };
                                                if(dependentjobgroup.Any()) {
                                                    jobgroup.Add(next);
                                                }
                                                templateContext.ExpressionValues.Clear();
                                                foreach (var pair in contextData) {
                                                    templateContext.ExpressionValues[pair.Key] = pair.Value;
                                                }
                                                var _jobdisplayname = (from r in run where r.Key.AssertString("name").Value == "name" select GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "string-strategy-context", r.Value, 0, fileId, true).AssertString("job name must be a string").Value).FirstOrDefault() ?? displayname;
                                                
                                                queueJob(templateContext, workflowDefaults, workflowEnvironment, _jobdisplayname, run, contextData.Clone() as DictionaryContextData, next.Id, next.TimelineId, repository_name, $"{jobname}_{c}", workflowname, runid, secrets);
                                            };
                                            if(keys.Length != 0 || includematrix.Count == 0) {
                                                foreach (var item in flatmatrix) {
                                                    act(defaultDisplayName(from key in keys select item[key].ToString()), item);
                                                }
                                            }
                                            foreach (var item in includematrix) {
                                                act(defaultDisplayName(from it in item select it.Value.ToString()), item);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    strategyctx.Add("fail-fast", new BooleanContextData(true));
                                    strategyctx.Add("job-index", new NumberContextData(0));
                                    strategyctx.Add("job-total", new NumberContextData(1));
                                    jobitem.Id = Guid.NewGuid();
                                    jobitem.TimelineId = Guid.NewGuid();
                                    if(dependentjobgroup.Any()) {
                                        jobgroup.Add(jobitem);
                                    }
                                    contextData.Add("matrix", null);
                                    templateContext.ExpressionValues.Clear();
                                    foreach (var pair in contextData) {
                                        templateContext.ExpressionValues[pair.Key] = pair.Value;
                                    }
                                    var jobdisplayname = (from r in run where r.Key.AssertString("name").Value == "name" select GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, "string-strategy-context", r.Value, 0, fileId, true).AssertString("job name must be a string").Value).FirstOrDefault() ?? jobname;
                                    queueJob(templateContext, workflowDefaults, workflowEnvironment, jobdisplayname, run, contextData.Clone() as DictionaryContextData, jobitem.Id, jobitem.TimelineId, repository_name, jobname, workflowname, runid, secrets);
                                }
                            };
                            jobitem.OnJobEvaluatable = handler;
                            jobitem.Needs = neededJobs.ToArray();
                            if(neededJobs.Count > 0) {
                                if(selectedJob == null && !list) {
                                    FinishJobController.OnJobCompleted += jobitem.OnJobEvaluatable;
                                }
                            }
                        }
                        break;
                        case "defaults":
                        workflowDefaults = actionPair.Value;
                        break;
                        case "env":
                        workflowEnvironment.Add(actionPair.Value);
                        break;
                    }
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
                    dependentjobgroup.ForEach(ji => {
                        if(!list && ji.Needs?.Length > 0) {
                            FinishJobController.OnJobCompleted += ji.OnJobEvaluatable;
                        }
                    });
                }
                dependentjobgroups[runid] = dependentjobgroup;
                if(list) {
                    return new HookResponse { repo = repository_name, run_id = runid, skipped = false, jobList = (from ji in dependentjobgroup select new JobListItem{Name= ji.name, Needs = ji.Needs}).ToList()};
                } else {
                    jobCompleted(null);
                }
            } catch (Exception ex) {
                List<string> _errors = new List<string>{ex.Message};
                var RequestId = Interlocked.Increment(ref reqId);
                var jid = Guid.NewGuid();
                var job = jobs.AddOrUpdate(jid, new Job() { errors = _errors, message = null, repo = repository_name, name = fileRelativePath, workflowname = fileRelativePath, runid = runid, /* SessionId = sessionId,  */JobId = jid, RequestId = RequestId }, (id, job) => job);
                jobevent?.Invoke(this, job.repo, job);
            }
            return new HookResponse { repo = repository_name, run_id = runid, skipped = false };
        }

        private static int reqId = 0;
        private void queueJob(TemplateContext templateContext, TemplateToken workflowDefaults, List<TemplateToken> workflowEnvironment, string displayname, MappingToken run, DictionaryContextData contextData, Guid jobId, Guid timelineId, string repo, string name, string workflowname, long runid, string[] secrets)
        {
            TimelineController.dict[timelineId] = ( new List<TimelineRecord>{ new TimelineRecord{ Id = jobId } }, new System.Collections.Concurrent.ConcurrentDictionary<System.Guid, System.Collections.Generic.List<GitHub.DistributedTask.WebApi.TimelineRecordLogLine>>() );
            var runsOn = (from r in run where r.Key.AssertString("str").Value == "runs-on" select r).FirstOrDefault().Value;
            HashSet<string> runsOnMap = new HashSet<string>();
            if (runsOn != null) {
                foreach (var pair in contextData)
                {
                    templateContext.ExpressionValues[pair.Key] = pair.Value;
                }
                var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.RunsOn, runsOn, 0, null, true);
                runsOn = eval;

                if(runsOn is SequenceToken seq2) {
                    foreach(var t in seq2) {
                        runsOnMap.Add(t.AssertString("runs-on member must be a str").Value);
                    }
                } else {
                    runsOnMap.Add(runsOn.AssertString("runs-on must be a str or array of string").Value);
                }
            }

            var sessionsfreeze = sessions.ToArray();
            var x = (from s in sessionsfreeze where runsOnMap.IsSubsetOf(from l in s.Value.Agent.TaskAgent.Labels select l.Name) select s.Key).FirstOrDefault();
            if(x == null) {
                List<string> errors = new List<string>();
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
                errors.Add($"No runner is registered for the requested runs-on labels: [{b.ToString()}], please register and run a self-hosted runner with at least these labels. Available runner: {(i == 0 ? "No Runner available!" : b2.ToString())}");

                var _RequestId = Interlocked.Increment(ref reqId);
                var jid = jobId;
                var _job = jobs.AddOrUpdate(jid, new Job() { errors = errors, message = null, repo = repo, name = displayname, workflowname = workflowname, runid = runid, JobId = jid, RequestId = _RequestId }, (id, job) => job);
                jobevent?.Invoke(this, _job.repo, _job);
                return;
            }
            var res = (from r in run where r.Key.AssertString("str").Value == "steps" select r).FirstOrDefault();
            var seq = res.Value.AssertSequence("seq");

            var steps = PipelineTemplateConverter.ConvertToSteps(templateContext, seq);

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

            // Jobcontainer
            TemplateToken jobContainer = (from r in run where r.Key.AssertString("container").Value == "container" select r).FirstOrDefault().Value;
            // Jobservicecontainer
            TemplateToken jobServiceContainer = (from r in run where r.Key.AssertString("services").Value == "services" select r).FirstOrDefault().Value;
            // Job outputs
            TemplateToken outputs = (from r in run where r.Key.AssertString("outputs").Value == "outputs" select r).FirstOrDefault().Value;
            var resources = new JobResources();
            

            var variables = new Dictionary<String, GitHub.DistributedTask.WebApi.VariableValue>();
            variables.Add("system.github.token", new VariableValue(GITHUB_TOKEN, true));
            variables.Add("github_token", new VariableValue(GITHUB_TOKEN, true));
            variables.Add("DistributedTask.NewActionMetadata", new VariableValue("true", false));
            foreach (var secret in this.secrets) {
                variables[secret.Name] = new VariableValue(secret.Value, true);
            }
            LoadEnvSec(secrets, (name, value) => variables[name] = new VariableValue(value, true));

            var defaultToken = (from r in run where r.Key.AssertString("defaults").Value == "defaults" select r).FirstOrDefault().Value;

            List<TemplateToken> jobDefaults = new List<TemplateToken>();
            if(workflowDefaults != null) {
                jobDefaults.Add(workflowDefaults);
            }
            if (defaultToken != null)
            {
                jobDefaults.Add(defaultToken);
            }

            var RequestId = Interlocked.Increment(ref reqId);
            ConcurrentQueue<Job> queue = jobqueue.GetOrAdd(runsOnMap, (a) => new ConcurrentQueue<Job>());
            var job = jobs.AddOrUpdate(jobId, new Job() { message = (apiUrl) => {
                var auth = new GitHub.DistributedTask.WebApi.EndpointAuthorization() { Scheme = GitHub.DistributedTask.WebApi.EndpointAuthorizationSchemes.OAuth };
                var tokenHandler = new JwtSecurityTokenHandler();
                var token = tokenHandler.CreateJwtSecurityToken(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor() {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Dns, "Runner.Server"),
                    }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    Issuer = "free",
                    Audience = "free",
                    SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new SymmetricSecurityKey(Startup.Key.Key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha512Signature)  //new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.RsaSecurityKey(Startup.Key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha512)
                    
                });
                var stoken = tokenHandler.WriteToken(token);
                auth.Parameters.Add(GitHub.DistributedTask.WebApi.EndpointAuthorizationParameters.AccessToken, stoken);
                var systemVssConnection = new GitHub.DistributedTask.WebApi.ServiceEndpoint() { Id = Guid.NewGuid(), Name = WellKnownServiceEndpointNames.SystemVssConnection, Authorization = auth, Url = new Uri(apiUrl ?? "http://192.168.178.20:5000") };
                systemVssConnection.Data["CacheServerUrl"] = apiUrl;
                resources.Endpoints.Add(systemVssConnection);
                
                var req = new AgentJobRequestMessage(new GitHub.DistributedTask.WebApi.TaskOrchestrationPlanReference() { PlanType = "free", ContainerId = 0, ScopeIdentifier = Guid.NewGuid(), PlanGroup = "free", PlanId = Guid.NewGuid(), Owner = new GitHub.DistributedTask.WebApi.TaskOrchestrationOwner() { Id = 0, Name = "Community" }, Version = 12 }, new GitHub.DistributedTask.WebApi.TimelineReference() { Id = timelineId, Location = null, ChangeId = 1 }, jobId, displayname, name, jobContainer, jobServiceContainer, environment, variables, new List<GitHub.DistributedTask.WebApi.MaskHint>(), resources, contextData, new WorkspaceOptions(), steps.Cast<JobStep>(), templateContext.GetFileTable().ToList(), outputs, jobDefaults, new GitHub.DistributedTask.WebApi.ActionsEnvironmentReference("Test"));
                req.RequestId = RequestId;
                return req;
            }, repo = repo, name = displayname, workflowname = workflowname, runid = runid, /* SessionId = sessionId,  */JobId = jobId, RequestId = RequestId, TimeLineId = timelineId }, (id, job) => job);
            queue.Enqueue(job);
            jobevent?.Invoke(this, job.repo, job);
        }

        public delegate AgentJobRequestMessage MessageFactory(string apiUrl);

        public class Job {
            public Guid JobId { get; set; }
            public long RequestId { get; set; }
            public Guid TimeLineId { get; set; }
            public Guid SessionId { get; set; }
            [IgnoreDataMember]
            public MessageFactory message;

            public string repo { get; set; }
            public string name { get; set; }
            public string workflowname { get; set; }
            public long runid { get; set; }
            public bool CancelRequest { get; internal set; }
            public bool Cancelled { get; internal set; }

            public List<string> errors;
        }
        static ConcurrentDictionary<Guid, Job> jobs = new ConcurrentDictionary<Guid, Job>();


        private class EqualityComparer : IEqualityComparer<HashSet<string>> {
            public bool Equals(HashSet<string> a, HashSet<string> b) {
                return a.SetEquals(b);
            }
            public int GetHashCode(HashSet<string> p) {
                var l = p.ToList();
                l.Sort();
                StringBuilder b = new StringBuilder();
                foreach (var item in l) {
                    b.AppendJoin('|', item);
                }
                return b.ToString().GetHashCode();
            }
        }

        private static ConcurrentDictionary<HashSet<string>, ConcurrentQueue<Job>> jobqueue = new ConcurrentDictionary<HashSet<string>, ConcurrentQueue<Job>>(new EqualityComparer());
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

        private static ConcurrentDictionary<Session, Session> sessions = new ConcurrentDictionary<Session, Session>();

        [HttpGet("{poolId}")]
        public async Task<IActionResult> GetMessage(int poolId, Guid sessionId)
        {
            Session session;
            if(!_cache.TryGetValue(sessionId, out session)) {
                this.HttpContext.Response.StatusCode = 403;
                return await Ok(new WrappedException(new TaskAgentSessionExpiredException("This server has been restarted"), true, new Version(2, 0)));
            }
            sessions.AddOrUpdate(session, s => {
                if(s.Timer == null) {
                    s.Timer = new System.Timers.Timer();
                }
                s.Timer.AutoReset = false;
                s.Timer.Interval = 60000;
                s.Timer.Elapsed += (a,b) => {
                    Session s2;
                    sessions.TryRemove(session, out s2);
                };
                s.Timer.Start();
                return s;
            } , (s, v) => {
                s.Timer.Stop();
                s.Timer.Start();
                return v;
            });
            for (int i = 0; i < 10; i++)
            {
                if(session.Job == null) {
                    Job req;
                    foreach(var queue in jobqueue.ToArray().Where(e => e.Key.IsSubsetOf(from l in session.Agent.TaskAgent.Labels select l.Name))) {
                        if(queue.Value.TryDequeue(out req)) {
                            var apiUrl = $"{Request.Scheme}://{Request.Host.Host ?? (HttpContext.Connection.RemoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 ? ("[" + HttpContext.Connection.LocalIpAddress.ToString() + "]") : HttpContext.Connection.LocalIpAddress.ToString())}:{Request.Host.Port ?? (Request.Host.Host != null ? 80 : HttpContext.Connection.LocalPort)}/runner/host/";
                            
                            var res = req.message.Invoke(apiUrl);
                            req.message = null;
                            
                            session.Job = jobs.AddOrUpdate(res.JobId, new Job() { SessionId = sessionId, JobId = res.JobId, RequestId = res.RequestId, TimeLineId = res.Timeline.Id }, (id, job) => {
                                job.SessionId = sessionId;
                                return job;
                            });
                            _cache.Set("Job_" + res.JobId, session.Job);
                            session.Key.GenerateIV();
                            using (var encryptor = session.Key.CreateEncryptor(session.Key.Key, session.Key.IV))
                            using (var body = new MemoryStream())
                            using (var cryptoStream = new CryptoStream(body, encryptor, CryptoStreamMode.Write)) {
                                using (var bodyWriter = new StreamWriter(cryptoStream, Encoding.UTF8))
                                    bodyWriter.Write(JsonConvert.SerializeObject(res));
                                return await Ok(new TaskAgentMessage() {
                                    Body = Convert.ToBase64String(body.ToArray()),
                                    MessageId = id++,
                                    MessageType = JobRequestMessageTypes.PipelineAgentJobRequest,
                                    IV = session.Key.IV
                                });
                            }
                        }
                    }
                } else if(!session.Job.Cancelled) {
                    if(session.Job.CancelRequest) {
                        session.Job.Cancelled = true;
                        session.Key.GenerateIV();
                        using (var encryptor = session.Key.CreateEncryptor(session.Key.Key, session.Key.IV))
                        using (var body = new MemoryStream())
                        using (var cryptoStream = new CryptoStream(body, encryptor, CryptoStreamMode.Write)) {
                            using (var bodyWriter = new StreamWriter(cryptoStream, Encoding.UTF8))
                                bodyWriter.Write(JsonConvert.SerializeObject(new JobCancelMessage(session.Job.JobId, TimeSpan.FromMinutes(5))));
                            return await Ok(new TaskAgentMessage() {
                                Body = Convert.ToBase64String(body.ToArray()),
                                MessageId = id++,
                                MessageType = JobCancelMessage.MessageType,
                                IV = session.Key.IV
                            });
                        }
                    }
                }
                await Task.Delay(5000);
            }
            return NoContent();
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
        }

        public class GitCommit {
            public string Ref {get;set;}
            public string Sha {get;set;}
            public List<string> Added {get;set;}
            public List<string> Removed {get;set;}
            public List<string> Modified {get;set;}
        }

        public class GitPullRequest {
            public GitCommit head {get;set;}
            public GitCommit Base {get;set;}
        }

        public class GiteaHook
        {
            [DataMember]
            public Repo repository {get; set;}
            
            public string head_commit {get;set;}
            public string Action {get;set;}
            public long? Number {get;set;}
            public string Ref {get;set;}
            public string After {get;set;}
            public string merge_commit_sha {get;set;}
            public GitUser sender {get;set;}
            public GitPullRequest pull_request {get;set;}

            public List<GitCommit> Commits {get;set;}
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
        public async Task<ActionResult> OnWebhook([FromQuery] string workflow, [FromQuery] string job, [FromQuery] int? list, [FromQuery] string[] env, [FromQuery] string[] secrets, [FromQuery] string[] matrix)
        {
            var obj = await FromBody2<GiteaHook>();
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
            if(workflow != null && workflow.Length > 0) {
                var resp = ConvertYaml("workflow.yml", workflow, hook?.repository?.full_name ?? "Unknown/Unknown", GitServerUrl, hook, obj.Value, e, job, list >= 1, env, secrets, matrix);
                return await Ok(resp);
            } else {
                try {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", $"token {GITHUB_TOKEN}");
                    var giteaUrl = $"{hook.repository.html_url.Scheme}://{hook.repository.html_url.Host}:{hook.repository.html_url.Port}";
                    var res = await client.GetAsync($"{giteaUrl}/api/v1/repos/{hook.repository.full_name}/contents/.github%2Fworkflows?ref={hook.After}");
                    // {
                    //     "type": "gitea",
                    //     "config": {
                    //     "content_type": "json",
                    //     "url": "http://ubuntu.fritz.box/runner/host/_apis/v1/Message"
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
                        foreach (var item in Newtonsoft.Json.JsonConvert.DeserializeObject<List<UnknownItem>>(content))
                        {
                            try {
                                var fileRes = await client.GetAsync(item.download_url);
                                var filecontent = await fileRes.Content.ReadAsStringAsync();
                                ConvertYaml(item.path, filecontent, hook.repository.full_name, giteaUrl, hook, obj.Value, e);
                            } catch (Exception ex) {
                                await Console.Error.WriteLineAsync(ex.Message);
                                await Console.Error.WriteLineAsync(ex.StackTrace);
                            }
                        }
                    }
                } catch (Exception ex) {
                    await Console.Error.WriteLineAsync(ex.Message);
                    await Console.Error.WriteLineAsync(ex.StackTrace);
                }
            }
            return Ok();
        }

        [HttpGet]
        public Task<FileStreamResult> GetJobs([FromQuery] string repo, [FromQuery] long? runid, [FromQuery] int? depending) {
            if(runid != null && depending >= 1) {
                List<JobItem> value;
                if(dependentjobgroups.TryGetValue(runid.Value, out value)) {
                    return Ok(from v in value select new Job { JobId = v.Id, TimeLineId = v.TimelineId, name = v.name }, true);
                }
                return Ok(new Job[0], true);
            }
            return Ok(from j in jobs.Values where (repo == null || j.repo == repo) && (runid == null || j.runid == runid) select j, true);
        }

        [HttpPost("cancel/{id}")]
        public void CancelJob(Guid id) {
            Job job;
            if(jobs.TryGetValue(id, out job)) {
                job.CancelRequest = true;
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

        [HttpGet("event")]
        public IActionResult Message(string owner, string repo, [FromQuery] string filter)
        {
            var mfilter = new Minimatch.Minimatcher(filter ?? (owner + "/" + repo));
            var requestAborted = HttpContext.RequestAborted;
            return new PushStreamResult(async stream => {
                var wait = requestAborted.WaitHandle;
                var writer = new StreamWriter(stream);
                try
                {
                    writer.NewLine = "\n";
                    ConcurrentQueue<KeyValuePair<string,Job>> queue = new ConcurrentQueue<KeyValuePair<string, Job>>();
                    JobEvent handler = (sender, crepo, job) => {
                        if (mfilter.IsMatch(crepo)) {
                            queue.Enqueue(new KeyValuePair<string, Job>(crepo, job));
                        }
                    };
                    var ping = Task.Run(async () => {
                        try {
                            while(!requestAborted.IsCancellationRequested) {
                                KeyValuePair<string, Job> p;
                                if(queue.TryDequeue(out p)) {
                                    await writer.WriteLineAsync("event: job");
                                    await writer.WriteLineAsync(string.Format("data: {0}", JsonConvert.SerializeObject(new { repo = p.Key, job = p.Value }, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}})));
                                    await writer.WriteLineAsync();
                                    await writer.FlushAsync();
                                } else {
                                    await writer.WriteLineAsync("event: ping");
                                    await writer.WriteLineAsync("data: {}");
                                    await writer.WriteLineAsync();
                                    await writer.FlushAsync();
                                    await Task.Delay(5000);
                                }
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
