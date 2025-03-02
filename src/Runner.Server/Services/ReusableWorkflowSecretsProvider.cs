using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GitHub.DistributedTask.Logging;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace Runner.Server.Services
{
    public class ReusableWorkflowSecretsProvider : ISecretsProvider
    {
        private ISecretsProvider parent;
        private TemplateToken secretsMapping;
        private DictionaryContextData contextData;
        private string jobName;
        private WorkflowContext workflowContext;
        private List<TemplateToken> environment;

        public ReusableWorkflowSecretsProvider(string jobName, ISecretsProvider parent, TemplateToken secretsMapping, DictionaryContextData contextData, WorkflowContext workflowContext, List<TemplateToken> environment){
            this.parent = parent;
            this.secretsMapping = secretsMapping;
            this.contextData = contextData;
            this.jobName = jobName;
            this.workflowContext = workflowContext;
            this.environment = environment;
        }
        public IDictionary<string, string> GetSecretsForEnvironment(GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter, string name = null)
        {
            var parentSecrets = parent.GetSecretsForEnvironment(traceWriter, name);
            traceWriter.Info("{0}", $"Evaluating Secrets of {jobName} for environment '{name?.Replace("'","''")}'");
            SecretMasker masker = new SecretMasker();
            var linesplitter = new Regex("\r?\n");
            foreach(var variable in parentSecrets) {
                if(!string.IsNullOrEmpty(variable.Value)) {
                    masker.AddValue(variable.Value);
                    if(variable.Value.Contains('\r') || variable.Value.Contains('\n')) {
                        foreach(var line in linesplitter.Split(variable.Value)) {
                            masker.AddValue(line);
                        }
                    }
                }
            }
            masker.AddValueEncoder(ValueEncoders.Base64StringEscape);
            masker.AddValueEncoder(ValueEncoders.Base64StringEscapeShift1);
            masker.AddValueEncoder(ValueEncoders.Base64StringEscapeShift2);
            masker.AddValueEncoder(ValueEncoders.CommandLineArgumentEscape);
            masker.AddValueEncoder(ValueEncoders.ExpressionStringEscape);
            masker.AddValueEncoder(ValueEncoders.JsonStringEscape);
            masker.AddValueEncoder(ValueEncoders.UriDataEscape);
            masker.AddValueEncoder(ValueEncoders.XmlDataEscape);
            masker.AddValueEncoder(ValueEncoders.TrimDoubleQuotes);
            masker.AddValueEncoder(ValueEncoders.PowerShellPreAmpersandEscape);
            masker.AddValueEncoder(ValueEncoders.PowerShellPostAmpersandEscape);
            var secureTraceWriter = new TraceWriter2(line => {
                traceWriter.Info("{0}", masker.MaskSecrets(line));
            });
            var result = new DictionaryContextData();
            foreach (var variable in parentSecrets) {
                result[variable.Key] = new StringContextData(variable.Value);
            }
            var jobEnvCtx = new DictionaryContextData();
            foreach(var envBlock in environment) {
                var envTemplateContext = SecretHelper.CreateTemplateContext(secureTraceWriter, workflowContext, contextData);
                envTemplateContext.ExpressionValues["env"] = jobEnvCtx;
                envTemplateContext.ExpressionValues["secrets"] = result;
                var cEnv = GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(envTemplateContext, "job-env", envBlock, 0, null, true);
                // Best effort, don't check for errors
                // templateContext.Errors.Check();
                // Best effort, make global env available this is not available on github actions
                if(cEnv is MappingToken genvToken) {
                    foreach(var kv in genvToken) {
                        if(kv.Key is StringToken key && kv.Value is StringToken val) {
                            jobEnvCtx[key.Value] = new StringContextData(val.Value);
                        }
                    }
                }
            }
            var templateContext = SecretHelper.CreateTemplateContext(secureTraceWriter, workflowContext, contextData);
            templateContext.ExpressionValues["env"] = jobEnvCtx;
            templateContext.ExpressionValues["secrets"] = result;
            var evalSec = secretsMapping != null ? GitHub.DistributedTask.ObjectTemplating.TemplateEvaluator.Evaluate(templateContext, templateContext.Schema.Definitions.ContainsKey("job-secrets") ? "job-secrets" : "workflow-job-secrets-mapping", secretsMapping, 0, null, true)?.AssertMapping($"jobs.{name}.secrets") : null;
            templateContext.Errors.Check();
            IDictionary<string, string> ret = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if(evalSec != null) {
                foreach(var entry in evalSec) {
                    ret[entry.Key.AssertString($"jobs.{jobName}.secrets mapping key").Value] = entry.Value.AssertString($"jobs.{jobName}.secrets mapping value").Value;
                }
            }
            return SecretHelper.WithReservedSecrets(ret, parentSecrets);
        }

        public IDictionary<string, string> GetReservedSecrets()
        {
            return parent.GetReservedSecrets();
        }

        public IDictionary<string, string> GetVariablesForEnvironment(string name = null)
        {
            return parent.GetVariablesForEnvironment(name);
        }
    }
}
