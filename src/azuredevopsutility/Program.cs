// See https://aka.ms/new-console-template for more information
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

// var templateContext = AzureDevops.CreateTemplateContext(new EmptyTraceWriter(), new List<string>());

String[] arguments = Environment.GetCommandLineArgs();

// var fileId = templateContext.GetFileId(arguments[1]);

// // Read the file
// var fileContent = System.IO.File.ReadAllText(arguments[1]);

// TemplateToken token;
// using (var stringReader = new StringReader(fileContent))
// {
//     var yamlObjectReader = new YamlObjectReader(fileId, stringReader);
//     token = TemplateReader.Read(templateContext, "workflow-root", yamlObjectReader, fileId, out _);
// }

// templateContext.Errors.Check();

// var pipelineroot = token.AssertMapping("root");

// TemplateToken? parameters = null;
// foreach(var kv in pipelineroot) {
//     if(kv.Key.Type != TokenType.String) {
//         continue;
//     }
//     switch((kv.Key as StringToken)?.Value) {
//         case "parameters":
//         parameters = kv.Value;
//         break;
//     }
// }

// var contextData = new DictionaryContextData();
// var parametersData = new DictionaryContextData();
// contextData["parameters"] = parametersData;
// contextData["variables"] = null;
// if(parameters?.Type == TokenType.Mapping) {
//     foreach(var kv in parameters as MappingToken) {
//         if(kv.Key.Type != TokenType.String) {
//             continue;
//         }
//         parametersData[(kv.Key as StringToken)?.Value] = kv.Value.ToContextData();
//     }
// }

// templateContext = AzureDevops.CreateTemplateContext(new EmptyTraceWriter(), templateContext.GetFileTable().ToArray(), contextData);

// var evaluatedRoot = TemplateEvaluator.Evaluate(templateContext, "workflow-root", pipelineroot, 0, fileId);
var evaluatedRoot = AzureDevops.ReadTemplate(new DefaultFileProvider(), arguments[1], null);
if(evaluatedRoot != null) {
    Console.WriteLine(evaluatedRoot.ToContextData().ToJToken().ToString());
}

TemplateToken? steps = null;
TemplateToken? jobs = null;
TemplateToken? stages = null;
foreach(var kv in evaluatedRoot as MappingToken) {
    if(kv.Key.Type != TokenType.String) {
        continue;
    }
    switch((kv.Key as StringToken)?.Value) {
        case "steps":
        steps = kv.Value;
        break;
        case "jobs":
        jobs = kv.Value;
        break;
        case "stages":
        stages = kv.Value;
        break;
    }
}

var ( tasks, tasksByNameAndVersion ) = TaskMetaData.LoadTasks("C:\\Program Files\\Azure DevOps Server 2020\\Tools\\Deploy\\TfsServicingFiles\\Tasks\\Individual");
Console.WriteLine("==========================");
Console.WriteLine(JsonConvert.SerializeObject(tasks));

if(steps != null && steps.Type == TokenType.Sequence) {
    var jsteps = new List<JobStep>();
    foreach(var step in steps as SequenceToken) {
        AzureDevops.ParseSteps(jsteps, step, tasksByNameAndVersion);
        // if(step?.Type == TokenType.Mapping && step is MappingToken mstep && mstep.Count > 0 && (mstep[0].Key as StringToken)?.Value == "template") {
        //     var path = (mstep[0].Value as StringToken)?.Value;
        // }
    }

    var message = new AgentJobRequestMessage(
        new GitHub.DistributedTask.WebApi.TaskOrchestrationPlanReference{ 
            PlanGroup = "free", PlanId = Guid.NewGuid(), PlanType = "free", ScopeIdentifier = Guid.Empty, Version = 14
            }, 
            new GitHub.DistributedTask.WebApi.TimelineReference {
                Id = Guid.NewGuid()
            }, Guid.NewGuid(), "Job1", "job1", null, null, null,
            new Dictionary<string, VariableValue> { 
                { "System.Debug", "true" },
                { "System.CollectionId", Guid.NewGuid().ToString() },
                { "System.DefinitionId", Guid.NewGuid().ToString() },
                { "Build.Clean", "true" },
                { "Build.SyncSources", "false" },
                { "Build.DefinitionName", Guid.NewGuid().ToString() },
            },
            new List<MaskHint>(),
            new JobResources() {
            }, null,
            new WorkspaceOptions() {
                Clean = "true"
            },
            jsteps,
            null,
            null,
            null,
            null);
    message.Resources.Endpoints.Add(new ServiceEndpoint() {
        Id = Guid.NewGuid(),
        Authorization = new EndpointAuthorization() {
            Scheme = "OAuth"
        },
        Url = new Uri("http://localhost:5000"),
        Name = "SystemVssConnection"
    });
    message.Resources.Endpoints[0].Authorization.Parameters.Add("AccessToken", "***");
    message.Resources.Repositories.Add(new RepositoryResource() {
        Alias = "self",
        Endpoint = new ServiceEndpointReference() {
            Id = message.Resources.Endpoints[0].Id,
            Name = message.Resources.Endpoints[0].Name
        },
        Id = Guid.NewGuid().ToString(),
        Url = new Uri("http://localhost:5000/test"),
        Type = "Git",
        Version = "000000000000000000000000000000"
    });
    message.RequestId = 24564;
    var json = JsonConvert.SerializeObject(message, new JsonSerializerSettings{ ContractResolver = new CamelCasePropertyNamesContractResolver(), Converters = new List<JsonConverter>{new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() }}});
    Console.WriteLine("==========================");
    Console.WriteLine(json);


} else if(jobs != null){
    var pjobs = new List<Job>();
    Job.ParseJobs(pjobs, jobs.AssertSequence(""), new DefaultFileProvider(), tasksByNameAndVersion);
}else if(stages != null){
    var pstages = new List<Stage>();
    Stage.ParseStages(pstages, jobs.AssertSequence(""), new DefaultFileProvider(), tasksByNameAndVersion);
}