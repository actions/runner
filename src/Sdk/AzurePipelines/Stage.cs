using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Runner.Server.Azure.Devops
{
    public class Stage : IContextDataProvider {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string[] DependsOn { get; set; }
        public string Condition { get; set; }
        public List<Job> Jobs { get; set; }
        public Dictionary<string, VariableValue> Variables { get; set; }
        private Dictionary<string, VariableValue> variablesMetaData;
        public TemplateToken TemplateContext { get; set; }
        public Dictionary<string, Azure.Devops.Stage> Dependencies { get; set; }
        public Pool Pool { get; set; }
        public String LockBehavior { get; set; }

        public async Task<Stage> Parse(Context context, TemplateToken source, bool skipRootCheck = false) {
            var jobToken = source.AssertMapping("job-root");
            if(!skipRootCheck && (jobToken.Count == 0 || jobToken[0].Key.ToString() != "stage")) {
                throw new TemplateValidationException(new [] {new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(jobToken)}Expected stage")});
            }
            var errors = new List<TemplateValidationError>();
            foreach(var kv in jobToken) {
                switch(kv.Key.AssertString("key").Value) {
                    case "stage":
                        Name = kv.Value.AssertLiteralString("name");
                    break;
                    case "displayName":
                        DisplayName = kv.Value.AssertLiteralString("name");
                    break;
                    case "dependsOn":
                        DependsOn = (from dep in kv.Value.AssertScalarOrSequence("dependsOn") select dep.AssertLiteralString("dep")).ToArray();
                    break;
                    case "condition":
                        Condition = kv.Value.AssertLiteralString("condition");
                    break;
                    case "variables":
                        variablesMetaData = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                        await AzureDevops.ParseVariables(context, variablesMetaData, kv.Value);
                        Variables = variablesMetaData.Where(metaData => !metaData.Value.IsGroup).ToDictionary(metaData => metaData.Key, metaData => metaData.Value, StringComparer.OrdinalIgnoreCase);
                        variablesMetaData = variablesMetaData.Where(metaData => !metaData.Value.IsGroupMember).ToDictionary(metaData => metaData.Key, metaData => metaData.Value, StringComparer.OrdinalIgnoreCase);
                    break;
                    case "jobs":
                        Jobs = new List<Job>();
                        await Job.ParseJobs(context, Jobs, kv.Value.AssertSequence(""));
                    break;
                    case "templateContext":
                        TemplateContext = AzureDevops.ConvertAllScalarsToString(kv.Value);
                    break;
                    case "pool":
                        Pool = new Pool().Parse(context, kv.Value);
                    break;
                    case "lockBehavior":
                        LockBehavior = kv.Value.AssertLiteralString("lockBehavior have to be of type string");
                    break;
                    default:
                        errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(kv.Key)}Unexpected Key {kv.Key}"));
                    break;
                }
            }
            if(errors.Any()) {
                throw new TemplateValidationException(errors);
            }
            return this;
        }

        public static async Task ParseStages(Context context, List<Stage> stages, SequenceToken stagesToken) {
            var errors = new List<TemplateValidationError>();
            foreach(var job in stagesToken) {
                try {
                    if(job is MappingToken mstep && mstep.Count > 0) {
                        if((mstep[0].Key as StringToken)?.Value == "template") {
                            var path = (mstep[0].Value as LiteralToken)?.ToString();
                            if(mstep.Count == 2 && (mstep[1].Key as StringToken)?.Value != "parameters") {
                                errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[1].Key)}Unexpected yaml key {(mstep[1].Key as StringToken)?.Value} expected parameters"));
                                continue;
                            }
                            if(mstep.Count > 2) {
                                errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[2].Key)}Unexpected yaml keys {(mstep[2].Key as StringToken)?.Value} after template reference"));
                                continue;
                            }
                            MappingToken file;
                            try {
                                try {
                                    file = await AzureDevops.ReadTemplate(context, path, mstep.Count == 2 ? mstep[1].Value.AssertMapping("param").ToDictionary(kv => kv.Key.AssertString("").Value, kv => kv.Value) : null, "stage-template-root");
                                } catch(Exception ex) when (!(ex is TemplateValidationException)) {
                                    errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[0].Key)}{ex.Message}"));
                                    continue;
                                }
                                await ParseStages(context.ChildContext(file, path), stages, (from e in file where e.Key.AssertString("").Value == "stages" select e.Value).First().AssertSequence(""));
                            } catch(TemplateValidationException ex) {
                                throw new TemplateValidationException(ex.Errors.Prepend(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(mstep[0].Key)}Found Errors inside Template Reference: {ex.Message}")));
                            }
                        } else {
                            stages.Add(await new Stage().Parse(context, mstep));
                        }
                    }
                } catch(TemplateValidationException ex) {
                    errors.AddRange(ex.Errors);
                } catch(Exception ex) {
                    errors.Add(new TemplateValidationError($"{GitHub.DistributedTask.ObjectTemplating.Tokens.TemplateTokenExtensions.GetAssertPrefix(job)}{ex.Message}"));
                }
            }
            if(errors.Any()) {
                throw new TemplateValidationException(errors);
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
                stage["variables"] = vars;
            }
            // Azure Pipelines seem to ignore missing jobs as long as it is only template parameter
            if(Jobs != null) {
                var jobs = new ArrayContextData();
                foreach(var job in Jobs) {
                    jobs.Add(job.ToContextData());
                }
                stage["jobs"] = jobs;
            }
            if(Pool != null) {
                stage["pool"] = Pool.ToContextData();
            }
            if(LockBehavior != null) {
                stage["lockBehavior"] = new StringContextData(LockBehavior);
            }
            return stage;
        }
    }
}
