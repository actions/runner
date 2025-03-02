using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using Sdk.Pipelines;

namespace Runner.Server.Services
{
    public class SecretHelper
    {
        public static bool IsReservedVariable(string v) {
            var pattern = new Regex("^[a-zA-Z_][a-zA-Z_0-9]*$");
            return !pattern.IsMatch(v) || v.StartsWith("GITHUB_", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsActionsDebugVariable(string v) {
            return string.Equals(v, "ACTIONS_STEP_DEBUG", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "ACTIONS_RUNNER_DEBUG", StringComparison.OrdinalIgnoreCase);
        }

        public static IDictionary<string, string> WithReservedSecrets(IDictionary<string, string> dict, IDictionary<string, string> reservedsecrets) {
            var ret = dict == null ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
            if(reservedsecrets != null) {
                foreach(var kv in reservedsecrets) {
                    if(IsReservedVariable(kv.Key) || /* Allow overriding them while calling reusable workflows */ !ret.ContainsKey(kv.Key) && IsActionsDebugVariable(kv.Key)) {
                        ret[kv.Key] = kv.Value;
                    }
                }
            }
            return ret;
        }

        public static TemplateContext CreateTemplateContext(GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter, WorkflowContext context, DictionaryContextData contextData = null, ExecutionContext exctx = null) {
            ExpressionFlags flags = ExpressionFlags.None;
            if(context.HasFeature("system.runner.server.extendedFunctions")) {
                flags |= ExpressionFlags.ExtendedFunctions;
            }
            if(context.HasFeature("system.runner.server.extendedDirectives")) {
                flags |= ExpressionFlags.ExtendedDirectives;
            }
            if(context.HasFeature("system.runner.server.allowAnyForInsert")) {
                flags |= ExpressionFlags.AllowAnyForInsert;
            }
            if(context.HasFeature("system.runner.server.FixInvalidActionsIfExpression")) {
                flags |= ExpressionFlags.FixInvalidActionsIfExpression;
            }
            if(context.HasFeature("system.runner.server.FailInvalidActionsIfExpression")) {
                flags |= ExpressionFlags.FailInvalidActionsIfExpression;
            }
            // For Gitea Actions
            var absoluteActions = context.HasFeature("system.runner.server.absolute_actions");
            var templateContext = new TemplateContext() {
                Flags = flags,
                CancellationToken = System.Threading.CancellationToken.None,
                Errors = new TemplateValidationErrors(10, 500),
                Memory = new TemplateMemory(
                    maxDepth: 100,
                    maxEvents: 1000000,
                    maxBytes: 10 * 1024 * 1024),
                TraceWriter = traceWriter,
                Schema = PipelineTemplateSchemaFactory.GetSchema(),
                AbsoluteActions = absoluteActions,
            };
            if(context.FeatureToggles.TryGetValue("system.runner.server.workflow_schema", out var workflow_schema)) {
                var objectReader = new JsonObjectReader(null, workflow_schema);
                templateContext.Schema = TemplateSchema.Load(objectReader);
            }
            if(exctx != null) {
                templateContext.State[nameof(ExecutionContext)] = exctx;
                templateContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
                templateContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
                templateContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, Int32.MaxValue));
                templateContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, Int32.MaxValue));
            }
            if(contextData != null) {
                foreach (var pair in contextData) {
                    templateContext.ExpressionValues[pair.Key] = pair.Value;
                }
            }
            if(context.FileTable != null) {
                foreach(var fileName in context.FileTable) {
                    templateContext.GetFileId(fileName);
                }
            }
            return templateContext;
        }

    }
}
