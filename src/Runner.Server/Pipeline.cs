using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;

namespace Runner.Server.Azure.Devops {

public class Pipeline {
    public string Name { get; set; }
    public List<Stage> Stages { get; set; }
    public Dictionary<string, VariableValue> Variables { get; set; }
    public Dictionary<string, Container> ContainerResources { get; set; }
    public Pool Pool { get; set; }

    public Pipeline Parse(Runner.Server.Azure.Devops.Context context, TemplateToken source, Dictionary<string, TaskMetaData> tasksByNameAndVersion) {
        var pipelineRootToken = source.AssertMapping("pipeline-root");
        Pipeline parent = null;
        foreach(var kv in pipelineRootToken) {
            switch(kv.Key.AssertString("key").Value) {
                case "name":
                    Name = kv.Value.AssertString("name").Value;
                break;
                case "variables":
                    Variables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                    AzureDevops.ParseVariables(context, Variables, kv.Value);
                break;
                case "extends":
                    var ext = kv.Value.AssertMapping("extends");
                    string template = null;
                    Dictionary<string, TemplateToken> parameters = null;
                    foreach(var ev in ext) {
                        switch(ev.Key.AssertString("").Value) {
                            case "template":
                                template = ev.Value.AssertString("").Value;
                            break;
                            case "parameters":
                                parameters = ev.Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value);
                            break;
                        }
                    }
                    var templ = AzureDevops.ReadTemplate(context, template, parameters);
                    parent = new Pipeline().Parse(context.ChildContext(templ, template), templ, tasksByNameAndVersion);
                break;
                case "stages":
                    Stages = new List<Stage>();
                    Stage.ParseStages(context, Stages, kv.Value.AssertSequence("stages"), tasksByNameAndVersion);
                break;
                case "steps":
                    var implicitJob = new Job().Parse(context, source, tasksByNameAndVersion);
                    implicitJob.Name = null;
                    Stages = new List<Stage>{ new Stage {
                        Jobs = new List<Job>{ implicitJob }
                    } };
                break;
                case "jobs":
                    var implicitStage = new Stage().Parse(context, source, tasksByNameAndVersion);
                    implicitStage.Name = null;
                    Stages = new List<Stage>{ implicitStage };
                break;
                case "resources":
                    foreach(var resource in kv.Value.AssertMapping("resources")) {
                        switch(resource.Key.AssertString("").Value) {
                            case "containers":
                                ContainerResources = new Dictionary<string, Container>(StringComparer.OrdinalIgnoreCase);
                                foreach(var rawcontainer in resource.Value.AssertSequence("cres")) {
                                    var container = rawcontainer.AssertMapping("");
                                    ContainerResources[container[0].Value.AssertString("").Value] = new Container().Parse(container);
                                }
                            break;
                        } 
                    }
                break;
                case "pool":
                    Pool = new Pool().Parse(context, kv.Value);
                break;
            }
        }
        if(parent != null) {
            Stages = parent.Stages;
            if(parent.ContainerResources != null) {
                foreach(var cr in parent.ContainerResources) {
                    ContainerResources[cr.Key] = cr.Value;
                }
            }
            if(parent.Variables != null) {
                foreach(var cr in parent.Variables) {
                    Variables[cr.Key] = cr.Value;
                }
            }
        }
        return this;
    }
}
}