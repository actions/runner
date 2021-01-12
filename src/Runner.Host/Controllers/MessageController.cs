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

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    public class MessageController : ControllerBase
    {

        private readonly ILogger<MessageController> _logger;

        public static Dictionary<Guid, object> dict = new Dictionary<Guid, object>();

        public MessageController(ILogger<MessageController> logger)
        {
            _logger = logger;
        }

        [HttpDelete("{poolId}/{messageId}")]
        public void DeleteMessage(int poolId, long messageId, Guid sessionId)
        {
        }

        public static Dictionary<Guid, Guid> jobIdToSessionId = new Dictionary<Guid, Guid>();

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
            public Stat? Status => null;
        }

        class ExecutionContext : IExecutionContext
        {
            public IJobCtx JobContext => new JobCtx();
        }

        public static AgentJobRequestMessage ConvertYaml(string fileRelativePath, string content = null, string repository = null) {
            var templateContext = new TemplateContext(){
                CancellationToken = CancellationToken.None,
                Errors = new TemplateValidationErrors(10, 500),
                Memory = new TemplateMemory(
                    maxDepth: 100,
                    maxEvents: 1000000,
                    maxBytes: 10 * 1024 * 1024),
                TraceWriter = new EmptyTraceWriter()
            };
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
            Console.WriteLine("Hello World!");
            Console.WriteLine(JsonConvert.SerializeObject(token));

            List<TemplateToken> workflowDefaults = new List<TemplateToken>();
            List<TemplateToken> workflowEnvironment = new List<TemplateToken>();

            var actionMapping = token.AssertMapping("root");
            foreach (var actionPair in actionMapping)
            {
                var propertyName = actionPair.Key.AssertString($"action.yml property key");

                switch (propertyName.Value)
                {
                    case "jobs":
                    var jobs = actionPair.Value.AssertMapping("jobs");
                    foreach (var job in jobs)
                        {
                            var jn = job.Key.AssertString($"action.yml property key");
                            var run = job.Value.AssertMapping("jobs");

                            var needs = (from r in run where r.Key.AssertString("needs").Value == "needs" select r).FirstOrDefault().Value?.AssertSequence("needs");
                            List<string> neededJobs = new List<string>();
                            if (needs != null) {
                                neededJobs.AddRange(from need in needs select need.AssertString("list of strings").Value);
                            }

                            var contextData = new GitHub.DistributedTask.Pipelines.ContextData.DictionaryContextData();
                            var githubctx = new DictionaryContextData();
                            contextData.Add("github", githubctx);
                            githubctx.Add("repository", new StringContextData(repository ?? "ChristopherHX/test"));
                            githubctx.Add("server_url", new StringContextData("http://ubuntu:3042/"));
                            // githubctx.Add("token", new StringContextData("48c0ad6b5e5311ba38e8cce918e2602f16240087"));
                            contextData.Add("needs", new DictionaryContextData());
                            contextData.Add("strategy", new DictionaryContextData());

                            var ifexpr = (from r in run where r.Key.AssertString("str").Value == "if" select r).FirstOrDefault().Value;//?.AssertString("if")?.Value;
                            var condition = new BasicExpressionToken(null, null, null, PipelineTemplateConverter.ConvertToIfCondition(templateContext, ifexpr, true));

                            foreach (var pair in contextData)
                            {
                                templateContext.ExpressionValues[pair.Key] = pair.Value;
                            }
                            templateContext.State[nameof(IExecutionContext)] = new ExecutionContext();
                            templateContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
                            templateContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
                            templateContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, 0));
                            templateContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, 0));
                            var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.JobIfResult, condition, 0, fileId, true);
                            bool _res = PipelineTemplateConverter.ConvertToIfResult(templateContext, eval);
                            if(!_res) {
                                continue;
                            }
                            
                            var strategy = (from r in run where r.Key.AssertString("strategy").Value == "strategy" select r).FirstOrDefault().Value?.AssertMapping("strategy");
                            if (strategy != null)
                            {
                                var matrix = (from r in strategy where r.Key.AssertString("matrix").Value == "matrix" select r).FirstOrDefault().Value?.AssertMapping("matrix");
                                SequenceToken include = null;
                                SequenceToken exclude = null;
                                var flatmatrix = new List<Dictionary<string, TemplateToken>> { new Dictionary<string, TemplateToken>() };
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
                                if (include != null)
                                {
                                    foreach (var item in include)
                                    {
                                        var map = item.AssertMapping("include item").ToDictionary(k => k.Key.AssertString("key").Value, k => k.Value);
                                        bool matched = false;
                                        flatmatrix.ForEach(dict =>
                                        {
                                            foreach (var item in map)
                                            {
                                                TemplateToken val;
                                                if (dict.TryGetValue(item.Key, out val) && !TemplateTokenEqual(item.Value, val)) {
                                                    return;
                                                }
                                            }
                                            matched = true;
                                            // Add missing keys
                                            foreach (var item in map)
                                            {
                                                dict.TryAdd(item.Key, item.Value);
                                            }
                                        });
                                        if (!matched)
                                        {
                                            flatmatrix.Add(map);
                                        }
                                    }
                                }
                                foreach (var item in flatmatrix)
                                {
                                    var matrixContext = new DictionaryContextData();
                                    foreach (var mk in item)
                                    {
                                        PipelineContextData data = mk.Value.ToContextData();
                                        matrixContext.Add(mk.Key, data);
                                    }
                                    contextData["matrix"] = matrixContext;
                                    queueJob(templateContext, workflowDefaults, workflowEnvironment, jn, run, contextData);
                                }
                            }
                            else
                            {
                                contextData.Add("matrix", null);
                                queueJob(templateContext, workflowDefaults, workflowEnvironment, jn, run, contextData);
                            }
                        }
                        break;
                    case "defaults":
                    workflowDefaults.Add(actionPair.Value);
                    break;
                    case "env":
                    workflowEnvironment.AddRange(actionPair.Value.AssertSequence("env is a seq"));
                    break;
                }
            }
            return null;
        }

        private static void queueJob(TemplateContext templateContext, List<TemplateToken> workflowDefaults, List<TemplateToken> workflowEnvironment, StringToken jn, MappingToken run, DictionaryContextData contextData)
        {
            var runsOn = (from r in run where r.Key.AssertString("str").Value == "runs-on" select r).FirstOrDefault().Value;
            if (runsOn != null) {
                foreach (var pair in contextData)
                {
                    templateContext.ExpressionValues[pair.Key] = pair.Value;
                }
                var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.JobIfResult, runsOn, 0, null, true);
                runsOn = eval;
            }
            
            var res = (from r in run where r.Key.AssertString("str").Value == "steps" select r).FirstOrDefault();
            var seq = res.Value.AssertSequence("seq");
            // // foreach(var s in seq) {

            // // }
            // templateContext.ExpressionValues.FirstOrDefault();
            var steps = PipelineTemplateConverter.ConvertToSteps(templateContext, seq);

            foreach (var step in steps)
            {
                step.Id = Guid.NewGuid();
                Console.WriteLine(JsonConvert.SerializeObject(step));
            }

            // Merge environment
            var environmentToken = (from r in run where r.Key.AssertString("env").Value == "env" select r).FirstOrDefault().Value?.AssertSequence("env is a seq");
            // SequenceToken environment = workflowEnvironment?.Clone().AssertSequence("env is a seq");
            // if(environment == null) {
            //     environment = environmentToken;
            // } else if (environmentToken != null) {
            //     foreach(var entk in environmentToken) {
            //         environment.Add(entk);
            //     }
            // }
            List<TemplateToken> environment = new List<TemplateToken>();
            environment.AddRange(workflowEnvironment);
            if (environmentToken != null)
            {
                environment.AddRange(environmentToken);
            }

            // Jobcontainer
            TemplateToken jobContainer = (from r in run where r.Key.AssertString("container").Value == "container" select r).FirstOrDefault().Value;
            // Jobservicecontainer
            TemplateToken jobServiceContainer = (from r in run where r.Key.AssertString("services").Value == "services" select r).FirstOrDefault().Value;
            // Job outputs
            TemplateToken outputs = (from r in run where r.Key.AssertString("outputs").Value == "outputs" select r).FirstOrDefault().Value;
            var resources = new JobResources();
            var auth = new GitHub.DistributedTask.WebApi.EndpointAuthorization() { Scheme = GitHub.DistributedTask.WebApi.EndpointAuthorizationSchemes.OAuth };
            // var token_ = GitHub.Services.WebApi.Jwt.JsonWebToken.Create("free", "free", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), VssSigningCredentials.Create(() => Startup.ORGRSA, false));
            // VssSigningCredentials.Create(() => RSA.Create(4096), false);
            // var token = new JwtSecurityToken("free", "free", signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.RsaSecurityKey(Startup.Key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha512), notBefore: DateTime.UtcNow, expires: DateTime.UtcNow.AddDays(7));
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateJwtSecurityToken(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor() {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Dns, "Runner.Host"),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = "free",
                Audience = "free",
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new SymmetricSecurityKey(Startup.Key.Key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha512Signature)  //new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.RsaSecurityKey(Startup.Key), Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha512)
                
            });
            var stoken = tokenHandler.WriteToken(token);
            auth.Parameters.Add(GitHub.DistributedTask.WebApi.EndpointAuthorizationParameters.AccessToken, stoken);
            resources.Endpoints.Add(new GitHub.DistributedTask.WebApi.ServiceEndpoint() { Id = Guid.NewGuid(), Name = WellKnownServiceEndpointNames.SystemVssConnection, Authorization = auth, Url = new Uri("http://ubuntu.fritz.box") });
            var variables = new Dictionary<String, GitHub.DistributedTask.WebApi.VariableValue>();
            variables.Add("system.github.token", new VariableValue("48c0ad6b5e5311ba38e8cce918e2602f16240087", true));
            variables.Add("github_token", new VariableValue("48c0ad6b5e5311ba38e8cce918e2602f16240087", true));
            variables.Add("DistributedTask.NewActionMetadata", new VariableValue("true", false));

            queueLock.WaitOne();
            try {
                jobqueue.Enqueue(new AgentJobRequestMessage(new GitHub.DistributedTask.WebApi.TaskOrchestrationPlanReference() { PlanType = "free", ContainerId = 0, ScopeIdentifier = Guid.NewGuid(), PlanGroup = "free", PlanId = Guid.NewGuid(), Owner = new GitHub.DistributedTask.WebApi.TaskOrchestrationOwner() { Id = 0, Name = "Community" }, Version = 12 }, new GitHub.DistributedTask.WebApi.TimelineReference() { Id = Guid.NewGuid(), Location = new Uri("http://ubuntu.fritz.box/timeline/unused"), ChangeId = 1 }, Guid.NewGuid(), jn.Value, "name", jobContainer, jobServiceContainer, environment, variables, new List<GitHub.DistributedTask.WebApi.MaskHint>(), resources, contextData, new WorkspaceOptions(), steps.Cast<JobStep>(), templateContext.GetFileTable().ToList(), outputs, workflowDefaults, new GitHub.DistributedTask.WebApi.ActionsEnvironmentReference("Test")));
            } finally {
                queueLock.ReleaseMutex();
            }
        }

        public class Job {
            [DataMember]
            public Guid JobId { get; set; }
            [DataMember]
            public long RequestId { get; set; }
            [DataMember]
            public Guid TimeLineId { get; set; }
        }
        static ConcurrentDictionary<Guid, Job> jobs = new ConcurrentDictionary<Guid, Job>();

        private static Queue<AgentJobRequestMessage> jobqueue = new Queue<AgentJobRequestMessage>();
        private static int id = 0;
        public static Mutex queueLock = new Mutex();
        [HttpGet("{poolId}")]
        public Task<TaskAgentMessage> GetMessage(int poolId, Guid sessionId)
        {
            return Task.Factory.StartNew(() => {
                queueLock.WaitOne(5000);
                try {
                    AgentJobRequestMessage res;
                    if(!dict.TryGetValue(sessionId, out _) && jobqueue.TryDequeue(out res)) {
                        dict.Add(sessionId, res);
                        jobIdToSessionId.Add(res.JobId, sessionId);
                        res.RequestId = ++id;
                        jobevent?.Invoke(this, "ChristopherHX/test" , jobs.AddOrUpdate(res.JobId, new Job() { JobId = res.JobId, RequestId = res.RequestId, TimeLineId = res.Timeline.Id }, (id, job) => job));
                        
                        return new TaskAgentMessage() {
                            Body = JsonConvert.SerializeObject(res),
                            MessageId = ++id,
                            MessageType = "PipelineAgentJobRequest",
                        };
                    }
                } finally {
                    queueLock.ReleaseMutex();
                }
                Task.Delay(10000).Wait();
                return null;
            });
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

        public class Repo {
            [DataMember]
            public string full_name {get; set;}
            public Uri html_url {get; set;}
        }

        public class GiteaHook
        {
            [DataMember]
            public Repo repository {get; set;}
            
        }

        [HttpPost]
        public async void OnWebhook([FromBody] /* Dictionary<string, string> dict */GiteaHook hook)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", "token 7a2bf20dced683aea59189278a33691f67d5af55");
            
            var res = await client.GetAsync($"{hook.repository.html_url.Scheme}://{hook.repository.html_url.Host}:{hook.repository.html_url.Port}/api/v1/repos/{hook.repository.full_name}/contents/.github%2Fworkflows");
            // var content = await res.Content.ReadAsAsync<List<Dictionary<string, string>>>();
            if(res.StatusCode == System.Net.HttpStatusCode.OK) {
                var content = await res.Content.ReadAsStringAsync();
                foreach (var item in Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(content))
                {
                    var fileUrl = new UriBuilder(item.download_url.ToString());
                    // fileUrl.Host = "ubuntu";
                    var fileRes = await client.GetAsync(fileUrl.Uri);
                    var filecontent = await fileRes.Content.ReadAsStringAsync();
                    var req = ConvertYaml(item.path.ToString(), filecontent);
                    // jobqueue.Enqueue(req);
                }
            }
        }

        [HttpGet]
        public IEnumerable<Job> GetJobs() {
            return jobs.Values;
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

        [HttpGet("{user}/{repo}")]
        public IActionResult Message(string user, string repo)
        {
            repo = user + "/" + repo;
            var requestAborted = HttpContext.RequestAborted;
            return new PushStreamResult(async stream => {
                var wait = requestAborted.WaitHandle;
                var writer = new StreamWriter(stream);
                try
                {
                    writer.NewLine = "\n";
                    ConcurrentQueue<KeyValuePair<string,Job>> queue = new ConcurrentQueue<KeyValuePair<string, Job>>();
                    JobEvent handler = (sender, crepo, job) => {
                        if (crepo == repo) {
                            queue.Enqueue(new KeyValuePair<string, Job>(repo, job));
                            // await writer.WriteLineAsync("event: job");
                            // await writer.WriteLineAsync(string.Format("data: {0}", JsonConvert.SerializeObject(new { repo, job })));
                            // await writer.WriteLineAsync();
                            // await writer.FlushAsync();
                        }
                    };
                    var ping = Task.Run(async () => {
                        try {
                            while(!requestAborted.IsCancellationRequested) {
                                KeyValuePair<string, Job> p;
                                if(queue.TryDequeue(out p)) {
                                    await writer.WriteLineAsync("event: job");
                                    await writer.WriteLineAsync(string.Format("data: {0}", JsonConvert.SerializeObject(new { repo = p.Key, job = p.Value })));
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
