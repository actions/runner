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

namespace Runner.Host.Controllers
{
    [ApiController]
    [Route("_apis/v1/[controller]")]
    public class MessageController : ControllerBase
    {

        private readonly ILogger<MessageController> _logger;

        static Dictionary<Guid, object> dict = new Dictionary<Guid, object>();

        public MessageController(ILogger<MessageController> logger)
        {
            _logger = logger;
        }

        [HttpDelete("{poolId}/{messageId}")]
        public void DeleteMessage(int poolId, long messageId, Guid sessionId)
        {
        }

        private string ConvertYaml(string fileRelativePath, string content = null) {
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
            // AgentJobRequestMessage requestMessage = new AgentJobRequestMessage(new GitHub.DistributedTask.WebApi.TaskOrchestrationPlanReference(), new GitHub.DistributedTask.WebApi.TimelineReference(), Guid.NewGuid(), "name", "name", null, null, null, null, null, null, null, new WorkspaceOptions(), null, templateContext.GetFileTable().ToList(), null, workflowDefaults, new GitHub.DistributedTask.WebApi.ActionsEnvironmentReference("Test"));

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

                        // switch (jn.Value)
                        // {
                        //     case "build":
                            // var eval = new PipelineTemplateEvaluator(new EmptyTraceWriter(), templateContext.Schema, templateContext.GetFileTable().ToList());
                            //GitHub.DistributedTask.Expressions2.Sdk.ExpressionUtility.
                            var run = job.Value.AssertMapping("jobs");

                            var strategy = (from r in run where r.Key.AssertString("strategy").Value == "strategy" select r).FirstOrDefault().Value?.AssertMapping("strategy");
                            if(strategy != null) {
                                var matrix = (from r in strategy where r.Key.AssertString("matrix").Value == "matrix" select r).FirstOrDefault().Value?.AssertMapping("matrix");
                                // var include = new List<Dictionary<string, TemplateToken>>();
                                // var exclude = new List<Dictionary<string, TemplateToken>>();
                                SequenceToken include = null;
                                SequenceToken exclude = null;
                                var flatmatrix = new List<Dictionary<string, TemplateToken>>{ new Dictionary<string, TemplateToken>() };

                                foreach (var item in matrix) {
                                    var key = item.Key.AssertString("Key").Value;
                                    switch(key) {
                                        case "include":
                                        include = item.Value?.AssertSequence("include");
                                        break;
                                        case "exclude":
                                        exclude = item.Value?.AssertSequence("exclude");
                                        break;
                                        default:
                                        var val = item.Value.AssertSequence("seq");
                                        var next = new List<Dictionary<string, TemplateToken>>();
                                        foreach (var mel in flatmatrix) {
                                            foreach (var n in val) {
                                                var ndict = new Dictionary<string, TemplateToken>(mel);
                                                ndict.Add(key, val);
                                                next.Add(ndict);
                                            }
                                        }
                                        flatmatrix = next;
                                        break;
                                    }
                                }
                                if(exclude != null) {
                                    foreach (var item in exclude) {
                                        var map = item.AssertMapping("exclude item").ToDictionary(k => k.Key.AssertString("key").Value, k => k.Value);
                                        flatmatrix.RemoveAll(dict => {
                                            foreach (var item in map) {
                                                TemplateToken val;
                                                if(dict.TryGetValue(item.Key, out val)) {
                                                    if(val != item.Value) {
                                                        return false;
                                                    }
                                                } else {
                                                    return false;
                                                }
                                            }
                                            return true;
                                        });
                                    }
                                }
                                if(include != null) {
                                    foreach (var item in include) {
                                        var map = item.AssertMapping("include item").ToDictionary(k => k.Key.AssertString("key").Value, k => k.Value);
                                        bool matched = false;
                                        flatmatrix.ForEach(dict => {
                                            foreach (var item in map) {
                                                TemplateToken val;
                                                if(dict.TryGetValue(item.Key, out val)) {
                                                    if(val != item.Value) {
                                                        return;
                                                    }
                                                }
                                            }
                                            matched = true;
                                            // Add missing keys
                                            foreach (var item in map) {
                                                dict.TryAdd(item.Key, item.Value);
                                            }
                                        });
                                        if(!matched) {
                                            flatmatrix.Add(map);
                                        }
                                    }
                                }
                                foreach (var item in flatmatrix) {
                                    var contextData2 = new GitHub.DistributedTask.Pipelines.ContextData.DictionaryContextData();
                                    var matrixContext = new DictionaryContextData();
                                    foreach (var mk in item) {
                                        PipelineContextData data = null;
                                        switch(mk.Value.Type) {
                                            case TokenType.Boolean:
                                            data = new BooleanContextData(mk.Value.AssertBoolean("bool").Value);
                                            break;
                                            case TokenType.Number:
                                            data = new NumberContextData(mk.Value.AssertNumber("number").Value);
                                            break;
                                            case TokenType.String:
                                            data = new StringContextData(mk.Value.AssertString("string").Value);
                                            break;
                                            case TokenType.Null:
                                            break;
                                            default:
                                            throw new Exception("Invalid matrix content");
                                        }
                                        matrixContext.Add(mk.Key, data);
                                    }
                                    contextData2.Add("matrix", matrixContext);
                                }
                            }

                            var _token = (from r in run where r.Key.AssertString("str").Value == "if" select r).FirstOrDefault().Value?.AssertString("if")?.Value;
                            if(_token != null) {
                                // string cond = PipelineTemplateConverter.ConvertToIfCondition(templateContext, _token, true);
                                var node = new ExpressionParser().CreateTree(_token, new ExpressionTraceWriter(new EmptyTraceWriter()), new List<INamedValueInfo>(), ExpressionConstants.WellKnownFunctions.Values.ToList());
                                var noderes = node.Evaluate(null, new SecretMasker(), null, new EvaluationOptions());
                            }
                            // var condition = new BasicExpressionToken(null, null, null, "false");
                            // var eval = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, PipelineTemplateConstants.JobIfResult, condition, 0, fileId, true);
                            // bool _res = PipelineTemplateConverter.ConvertToIfResult(templateContext, eval);
                            // // bool _if = eval.EvaluateStepIf((from r in run where r.Key.AssertString("str").Value == "if" select r).FirstOrDefault().Value, new GitHub.DistributedTask.Pipelines.ContextData.DictionaryContextData(), ExpressionConstants.WellKnownFunctions.Values.ToList(), new List<KeyValuePair<string, object>>());
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
                            Console.WriteLine("Test");

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
                            if(environmentToken != null) {
                                environment.AddRange(environmentToken);
                            }
                            
                            // Jobcontainer
                            TemplateToken jobContainer = (from r in run where r.Key.AssertString("container").Value == "container" select r).FirstOrDefault().Value;
                            // Jobservicecontainer
                            TemplateToken jobServiceContainer = (from r in run where r.Key.AssertString("services").Value == "services" select r).FirstOrDefault().Value;
                            // Job outputs
                            TemplateToken outputs = (from r in run where r.Key.AssertString("outputs").Value == "outputs" select r).FirstOrDefault().Value;
                            var contextData = new GitHub.DistributedTask.Pipelines.ContextData.DictionaryContextData();
                            var resources = new JobResources();
                            var auth = new GitHub.DistributedTask.WebApi.EndpointAuthorization() { Scheme = GitHub.DistributedTask.WebApi.EndpointAuthorizationSchemes.OAuth};
                
                            var token_ = GitHub.Services.WebApi.Jwt.JsonWebToken.Create("free", "free", DateTime.UtcNow, DateTime.UtcNow.AddDays(7), VssSigningCredentials.Create(() => RSA.Create(4096), false));
                            auth.Parameters.Add(GitHub.DistributedTask.WebApi.EndpointAuthorizationParameters.AccessToken, token_.EncodedToken);
                            resources.Endpoints.Add(new GitHub.DistributedTask.WebApi.ServiceEndpoint() {Id = Guid.NewGuid(), Name = WellKnownServiceEndpointNames.SystemVssConnection, Authorization = auth, Url = new Uri("https://localhost:5001")});
                            var variables = new Dictionary<String, GitHub.DistributedTask.WebApi.VariableValue>();
                            variables.Add("system.github.token", new VariableValue("48c0ad6b5e5311ba38e8cce918e2602f16240087", true));
                            variables.Add("github_token", new VariableValue("48c0ad6b5e5311ba38e8cce918e2602f16240087", true));
                            variables.Add("DistributedTask.NewActionMetadata", new VariableValue("true", false));
                            
                            var githubctx = new DictionaryContextData();
                            contextData.Add("github", githubctx);
                            githubctx.Add("repository", new StringContextData("ChristopherHX/test"));
                            githubctx.Add("server_url", new StringContextData("http://ubuntu:3042/"));
                            // githubctx.Add("token", new StringContextData("48c0ad6b5e5311ba38e8cce918e2602f16240087"));
                            contextData.Add("needs", new DictionaryContextData());
                            contextData.Add("matrix", null);
                            contextData.Add("strategy", new DictionaryContextData());
                            return JsonConvert.SerializeObject(new AgentJobRequestMessage(new GitHub.DistributedTask.WebApi.TaskOrchestrationPlanReference(){PlanType = "free", ContainerId = 0, ScopeIdentifier = Guid.NewGuid(), PlanGroup = "free", PlanId = Guid.NewGuid(), Owner = new GitHub.DistributedTask.WebApi.TaskOrchestrationOwner(){Id = 0, Name = "Community" }, Version = 12}, new GitHub.DistributedTask.WebApi.TimelineReference(){Id = Guid.NewGuid(), Location = new Uri("https://localhost:5001/timeline/unused"), ChangeId = 1}, Guid.NewGuid(), "Display name", "name", jobContainer, jobServiceContainer, environment, variables, new List<GitHub.DistributedTask.WebApi.MaskHint>(), resources, contextData, new WorkspaceOptions(), steps.Cast<JobStep>(), templateContext.GetFileTable().ToList(), outputs, workflowDefaults, new GitHub.DistributedTask.WebApi.ActionsEnvironmentReference("Test")));
                        // }
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

        // private static bool sent = false;

        [HttpGet("{poolId}")]
        public async Task<TaskAgentMessage> GetMessage(int poolId, Guid sessionId)
        {
            if(!dict.TryGetValue(sessionId, out _)) {
                string res = await  System.IO.File.ReadAllTextAsync("C:/Users/Christopher/Documents/Webserver/build/WebserverConsole/Debug/job.json"); res = ConvertYaml("C:/Users/Christopher/runner/src/Runner.Host/test.yml");
                System.IO.File.WriteAllText("C:/Users/Christopher/Documents/Webserver/build/WebserverConsole/Debug/job.json", res);
                dict.Add(sessionId, res);
                // sent = true;
                return new TaskAgentMessage() {
                    Body = res,
                    MessageId = 2,
                    MessageType = "PipelineAgentJobRequest"
                };
            }
            return new TaskAgentMessage();
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
            var res = await client.GetAsync(string.Format("http://ubuntu:3500/api/v1/repos/{0}/contents/.github%2Fworkflows", hook.repository.full_name));
            // var content = await res.Content.ReadAsAsync<List<Dictionary<string, string>>>();
            var content = await res.Content.ReadAsStringAsync();
            foreach (var item in Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(content))
            {
                var fileUrl = new UriBuilder(item.download_url.ToString());
                fileUrl.Host = "ubuntu";
                var fileRes = await client.GetAsync(fileUrl.Uri);
                var filecontent = await fileRes.Content.ReadAsStringAsync();
                string jobRequest = ConvertYaml(item.path.ToString(), filecontent);
            }
        }
    }
}
