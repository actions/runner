using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using Runner.Server.Azure.Devops;

namespace Runner.Server.Azure.Devops {
public class Stage {
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string[] DependsOn { get; set; }
    public string Condition { get; set; }
    public List<Job> Jobs { get; set; }
    public Dictionary<string, VariableValue> Variables { get; set; }
    public TemplateToken TemplateContext { get; set; }
    public Dictionary<string, Azure.Devops.Stage> Dependencies { get; set; }
    public Pool Pool { get; set; }

    public Stage Parse(Runner.Server.Azure.Devops.Context context, TemplateToken source) {
        var jobToken = source.AssertMapping("job-root");
        foreach(var kv in jobToken) {
            switch(kv.Key.AssertString("key").Value) {
                case "stage":
                    Name = kv.Value is NullToken ? null : kv.Value.AssertString("name").Value;
                break;
                case "displayName":
                    DisplayName = kv.Value.AssertString("name").Value;
                break;
                case "dependsOn":
                    DependsOn = (from dep in kv.Value.AssertScalarOrSequence("dependsOn") select dep.AssertString("dep").Value).ToArray();
                break;
                case "condition":
                    Condition = kv.Value.AssertLiteralString("condition");
                break;
                case "variables":
                    Variables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                    AzureDevops.ParseVariables(context, Variables, kv.Value);
                break;
                case "jobs":
                    Jobs = new List<Job>();
                    Job.ParseJobs(context, Jobs, kv.Value.AssertSequence(""));
                break;
                case "templateContext":
                    TemplateContext = kv.Value;
                break;
                case "pool":
                    Pool = new Pool().Parse(context, kv.Value);
                break;
            }
        }
        return this;
    }

    public static void ParseStages(Runner.Server.Azure.Devops.Context context, List<Stage> stages, SequenceToken stagesToken) {
        foreach(var job in stagesToken) {
            if(job is MappingToken mstep && mstep.Count > 0) {
                if((mstep[0].Key as StringToken)?.Value == "template") {
                    var path = (mstep[0].Value as StringToken)?.Value;
                    var file = AzureDevops.ReadTemplate(context, path, mstep.Count == 2 ? mstep[1].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null, "stage-template-root");
                    ParseStages(context.ChildContext(file, path), stages, (from e in file where e.Key.AssertString("").Value == "stages" select e.Value).First().AssertSequence(""));
                } else {
                    stages.Add(new Stage().Parse(context, mstep));
                }
            }
        }
    }

    public DictionaryContextData ToContextData() {
        var stage = new DictionaryContextData();
        stage["stage"] = new StringContextData(Name);
        if(DisplayName?.Length > 0) {
            stage["displayName"] = new StringContextData(DisplayName);
        }
        if(Condition?.Length > 0) {
            stage["condition"] = new StringContextData(Condition);
        }
        if(TemplateContext != null) {
            stage["templateContext"] = TemplateContext.ToContextData();
        }
        if(DependsOn != null) {
            var dependsOn = new ArrayContextData();
            foreach(var dep in DependsOn) {
                dependsOn.Add(new StringContextData(dep));
            }
            stage["dependsOn"] = dependsOn;
        }
        if(Variables != null) {
            var vars = new DictionaryContextData();
            foreach(var v in Variables) {
                vars[v.Key] = new StringContextData(v.Value.Value);
            }
            stage["variables"] = vars;
        }
        var jobs = new ArrayContextData();
        foreach(var job in Jobs) {
            jobs.Add(job.ToContextData());
        }
        stage["jobs"] = jobs;
        if(Pool != null) {
            stage["pool"] = Pool.ToContextData();
        }
        return stage;
    }
}
}
