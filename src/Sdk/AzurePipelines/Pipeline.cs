using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;

namespace Runner.Server.Azure.Devops {

public class Pipeline {
    public string Name { get; set; }
    public List<Stage> Stages { get; set; }
    public Dictionary<string, VariableValue> Variables { get; set; }
    private Dictionary<string, VariableValue> variablesMetaData;
    public Dictionary<string, Container> ContainerResources { get; set; }
    public Dictionary<string, TemplateToken> OtherResources { get; set; }
    public TemplateToken Trigger { get; set; }
    public TemplateToken Pr { get; set; }
    public TemplateToken Schedules { get; set; }
    public Pool Pool { get; set; }
    public bool? AppendCommitMessageToRunName { get; set; }
    public String LockBehavior { get; set; }

    public Pipeline Parse(Runner.Server.Azure.Devops.Context context, TemplateToken source) {
        var pipelineRootToken = source.AssertMapping("pipeline-root");
        Pipeline parent = null;
        foreach(var kv in pipelineRootToken) {
            switch(kv.Key.AssertString("key").Value) {
                case "name":
                    Name = kv.Value.AssertLiteralString("name");
                break;
                case "variables":
                    variablesMetaData = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                    AzureDevops.ParseVariables(context, variablesMetaData, kv.Value);
                    Variables = variablesMetaData.Where(metaData => !metaData.Value.IsGroup).ToDictionary(metaData => metaData.Key, metaData => metaData.Value, StringComparer.OrdinalIgnoreCase);
                    variablesMetaData = variablesMetaData.Where(metaData => !metaData.Value.IsGroupMember).ToDictionary(metaData => metaData.Key, metaData => metaData.Value, StringComparer.OrdinalIgnoreCase);
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
                                parameters = ev.Value.AssertMapping("param").ToDictionary(pv => pv.Key.AssertString("").Value, pv => pv.Value);
                            break;
                        }
                    }
                    var templ = AzureDevops.ReadTemplate(context, template, parameters);
                    parent = new Pipeline().Parse(context.ChildContext(templ, template), templ);
                break;
                case "stages":
                    Stages = new List<Stage>();
                    Stage.ParseStages(context, Stages, kv.Value.AssertSequence("stages"));
                break;
                case "steps":
                    var implicitJob = new Job().Parse(context, source);
                    implicitJob.Name = null;
                    Stages = new List<Stage>{ new Stage {
                        Jobs = new List<Job>{ implicitJob }
                    } };
                break;
                case "jobs":
                    var implicitStage = new Stage().Parse(context, source);
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
                            default:
                                if(OtherResources == null) {
                                    OtherResources = new Dictionary<string, TemplateToken>(StringComparer.OrdinalIgnoreCase);
                                }
                                OtherResources[resource.Key.ToString()] = resource.Value;
                            break;
                        } 
                    }
                break;
                case "pool":
                    Pool = new Pool().Parse(context, kv.Value);
                break;
                case "appendCommitMessageToRunName":
                    AppendCommitMessageToRunName = kv.Value.AssertAzurePipelinesBoolean("appendCommitMessageToRunName have to be of type bool");
                break;
                case "lockBehavior":
                    LockBehavior = kv.Value.AssertLiteralString("lockBehavior have to be of type string");
                break;
                case "trigger":
                    Trigger = kv.Value;
                break;
                case "pr":
                    Pr = kv.Value;
                break;
                case "schedules":
                    Schedules = kv.Value;
                break;
            }
        }
        if(parent != null) {
            Stages = parent.Stages;
            if(parent.ContainerResources != null) {
                if(ContainerResources == null) {
                    ContainerResources = new Dictionary<string, Container>(StringComparer.OrdinalIgnoreCase);
                }
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

    public DictionaryContextData ToContextData() {
        var pipeline = new DictionaryContextData();
        if(!string.IsNullOrEmpty(Name)) {
            pipeline["name"] = new StringContextData(Name);
        }
        if(variablesMetaData != null) {
            var vars = new ArrayContextData();
            foreach(var v in variablesMetaData) {
                var varmap = new DictionaryContextData();
                vars.Add(varmap);
                if(v.Value.IsGroup) {
                    varmap["group"] = new StringContextData(v.Value.Value);
                } else {
                    varmap["name"] = new StringContextData(v.Key);
                    varmap["value"] = new StringContextData(v.Value.Value);
                    if(v.Value.IsReadonly) {
                        varmap["readonly"] = new StringContextData("true");
                    }
                }
            }
            pipeline["variables"] = vars;
        }
        if(AppendCommitMessageToRunName != null) {
            pipeline["appendCommitMessageToRunName"] = new StringContextData(AppendCommitMessageToRunName.Value.ToString());
        }
        if(Trigger != null) {
            pipeline["trigger"] = Trigger.ToContextData();
        }
        if(Pr != null) {
            pipeline["pr"] = Pr.ToContextData();
        }
        if(Schedules != null) {
            pipeline["schedules"] = Schedules.ToContextData();
        }
        if(ContainerResources != null || OtherResources != null) {
            var resources = new DictionaryContextData();
            pipeline["resources"] = resources;
            if(ContainerResources != null) {
                var containers = new ArrayContextData();
                resources["containers"] = containers;
                foreach(var cr in ContainerResources) {
                    containers.Add(cr.Value.ToContextData(cr.Key));
                }
            }
            if(OtherResources != null) {
                foreach(var ores in OtherResources) {
                    resources[ores.Key] = ores.Value.ToContextData();
                }
            }
        }
        if(Stages != null) {
            var stageList = new ArrayContextData();
            foreach(var stage in Stages) {
                stageList.Add(stage.ToContextData());
            }
            pipeline["stages"] = stageList;
        }
        if(Pool != null) {
            pipeline["pool"] = Pool.ToContextData();
        }
        if(LockBehavior != null) {
            pipeline["lockBehavior"] = new StringContextData(LockBehavior);
        }
        return pipeline;
    }
}
}