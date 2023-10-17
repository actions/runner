
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    public class TaskStep : JobStep, IContextDataProvider
    {
        [JsonConstructor]
        public TaskStep() {
            Environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        public override StepType Type => StepType.Task;

        [DataMember]
        public TaskStepDefinitionReference Reference { get; set; }
        [DataMember]
        public IDictionary<string, string> Environment { get; }
        [DataMember]
        public IDictionary<string, string> Inputs { get; }

        [DataMember(EmitDefaultValue = false)]
        public StepTarget Target { get; set; }

        [DataMember]
        public int RetryCountOnTaskFailure { get; set; }

        public override Step Clone()
        {
            throw new System.NotImplementedException();
        }

        public DictionaryContextData ToContextData() {
            var step = new DictionaryContextData();
            step["task"] = new StringContextData(Reference.RawNameAndVersion);
            if(Name?.Length > 0) {
                step["name"] = new StringContextData(Name);
            }
            if(DisplayName?.Length > 0) {
                step["displayName"] = new StringContextData(DisplayName);
            }
            if(Condition?.Length > 0) {
                step["condition"] = new StringContextData(Condition);
            }
            if(ContinueOnError is BooleanToken b && b.Value) {
                step["continueOnError"] = new StringContextData(b.Value.ToString());
            }
            if(!Enabled) {
                step["enabled"] = new StringContextData(Enabled.ToString());
            }
            if(RetryCountOnTaskFailure > 0) {
                step["retryCountOnTaskFailure"] = new StringContextData(RetryCountOnTaskFailure.ToString());
            }
            if(TimeoutInMinutes is NumberToken n && n.Value > 0) {
                step["timeoutInMinutes"] = new StringContextData(n.Value.ToString());
            }
            if(Target != null) {
                var target = new DictionaryContextData();
                if(Target.Target != null) {
                    target["container"] = new StringContextData(Target.Target);
                }
                if(Target.SettableVariables != null) {
                    if(Target.SettableVariables.Allowed.Count > 0) {
                        var allowed = new ArrayContextData();
                        foreach(var a in Target.SettableVariables.Allowed) {
                            allowed.Add(new StringContextData(a));
                        }
                        target["settableVariables"] = allowed;
                    } else {
                        target["settableVariables"] = new StringContextData("none");
                    }
                }
                if(Target.Commands != null) {
                    target["commands"] = new StringContextData(Target.Commands);
                }
                step["target"] = target;
            }
            if(Inputs?.Count > 0) {
                var inputs = new DictionaryContextData();
                foreach(var inp in Inputs) {
                    inputs[inp.Key] = new StringContextData(inp.Value);
                }
                step["inputs"] = inputs;
            }
            if(Environment?.Count > 0) {
                var env = new DictionaryContextData();
                foreach(var inp in Environment) {
                    env[inp.Key] = new StringContextData(inp.Value);
                }
                step["env"] = env;
            }
            return step;
        }
    }
}
