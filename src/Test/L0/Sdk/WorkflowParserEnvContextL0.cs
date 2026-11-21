using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Actions.Expressions;
using GitHub.Actions.Expressions.Data;
using GitHub.Actions.WorkflowParser;
using GitHub.Actions.WorkflowParser.Conversion;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using Xunit;
using ActionsExpressionParser = GitHub.Actions.Expressions.ExpressionParser;

namespace GitHub.Runner.Common.Tests.Sdk
{
    /// <summary>
    /// Regression tests for https://github.com/actions/runner/issues/2372
    ///
    /// Expressions that reference the "env" named value (ex. <c>${{ env.SOMETHING }}</c>),
    /// including inside larger expressions, used to fail validation with
    /// "Unrecognized named-value: 'env'. Located at position 1 within expression"
    /// when used within <c>jobs.&lt;job_id&gt;.with.&lt;with_id&gt;</c> or
    /// <c>jobs.&lt;job_id&gt;.secrets.&lt;secret_id&gt;</c> of a reusable workflow job.
    /// </summary>
    public sealed class WorkflowParserEnvContextL0
    {
        /// <summary>
        /// Reads a workflow document (JSON is a subset of YAML) against the workflow schema,
        /// exactly like the YAML loader does. Returns the parse errors.
        /// </summary>
        private static List<String> ReadWorkflowErrors(String workflowJson)
        {
            var context = new TemplateContext
            {
                Memory = new TemplateMemory(maxDepth: 50, maxEvents: 1000000, maxBytes: 10 * 1024 * 1024),
                Schema = WorkflowSchemaFactory.GetSchema(WorkflowFeatures.GetDefaults()),
                TraceWriter = new EmptyTraceWriter(),
            };
            var fileId = context.GetFileId("workflow.yml");
            var objectReader = new JsonObjectReader(fileId, workflowJson);
            TemplateReader.Read(context, "workflow-root", objectReader, fileId, out _);
            return context.Errors.Select(x => x.Message ?? String.Empty).ToList();
        }

        // The workflow from issue #2372 (JSON form of the YAML in the bug report)
        private const String Issue2372Workflow = @"{
  ""on"": { ""push"": {} },
  ""env"": { ""SOMETHING"": ""1000"" },
  ""jobs"": {
    ""test"": {
      ""name"": ""call workflow"",
      ""uses"": ""./.github/workflows/callee.yml"",
      ""secrets"": ""inherit"",
      ""with"": {
        ""run_url"": ""{run_url}"",
        ""message"": ""${{ env.SOMETHING }}""
      }
    }
  }
}";

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void ReadWorkflow_ReusableJobWith_AllowsEnvNamedValue()
        {
            var errors = ReadWorkflowErrors(Issue2372Workflow);

            Assert.DoesNotContain(errors, x => x.Contains("Unrecognized named-value"));
            Assert.Empty(errors);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void ReadWorkflow_ReusableJobWith_AllowsEnvInsideLargerExpressions()
        {
            var workflow = @"{
  ""on"": { ""push"": {} },
  ""env"": { ""SOMETHING"": ""1000"" },
  ""jobs"": {
    ""test"": {
      ""uses"": ""./.github/workflows/callee.yml"",
      ""with"": {
        ""prefixed"": ""run-${{ env.SOMETHING }}-end"",
        ""formatted"": ""${{ format('value: {0}', env.SOMETHING) }}"",
        ""combined"": ""${{ env.SOMETHING == '1000' && github.event_name == 'push' }}"",
        ""serialized"": ""${{ toJson(env) }}""
      }
    }
  }
}";
            var errors = ReadWorkflowErrors(workflow);

            Assert.DoesNotContain(errors, x => x.Contains("Unrecognized named-value"));
            Assert.Empty(errors);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void ReadWorkflow_ReusableJobSecretsMapping_AllowsEnvNamedValue()
        {
            var workflow = @"{
  ""on"": { ""push"": {} },
  ""env"": { ""MY_TOKEN"": ""abc123"" },
  ""jobs"": {
    ""test"": {
      ""uses"": ""./.github/workflows/callee.yml"",
      ""secrets"": {
        ""TOKEN"": ""${{ env.MY_TOKEN }}""
      }
    }
  }
}";
            var errors = ReadWorkflowErrors(workflow);

            Assert.DoesNotContain(errors, x => x.Contains("Unrecognized named-value"));
            Assert.Empty(errors);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void ReadWorkflow_ReusableJobWith_StillRejectsStepLevelContexts()
        {
            // "steps", "job", "runner", etc. are step-level contexts and must remain
            // rejected at the reusable workflow job level, even though "env" is now allowed.
            var workflow = @"{
  ""on"": { ""push"": {} },
  ""jobs"": {
    ""test"": {
      ""uses"": ""./.github/workflows/callee.yml"",
      ""with"": {
        ""bad"": ""${{ steps.some-step.outputs.value }}""
      }
    }
  }
}";
            var errors = ReadWorkflowErrors(workflow);

            Assert.Contains(errors, x => x.Contains("Unrecognized named-value: 'steps'"));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Schema_WorkflowJobWith_AllowsEnvContext()
        {
            // "workflow-job-with" (jobs.<job_id>.with.<with_id>) uses "scalar-needs-context"
            // as its loose value type. The env context must be allowed there.
            var schema = WorkflowSchemaFactory.GetSchema(WorkflowFeatures.GetDefaults());
            var definition = schema.GetDefinition("workflow-job-with");

            var withValueType = schema.Get<GitHub.Actions.WorkflowParser.ObjectTemplating.Schema.MappingDefinition>(definition).Single().LooseValueType;
            var scalarDefinition = schema.GetDefinition(withValueType);

            Assert.Equal("scalar-needs-context", withValueType);
            Assert.Contains("env", scalarDefinition.ReaderContext, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("env", scalarDefinition.EvaluatorContext, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Schema_WorkflowJobSecretsMapping_AllowsEnvContext()
        {
            // "workflow-job-secrets-mapping" (jobs.<job_id>.secrets.<secret_id>) uses
            // "scalar-needs-context-with-secrets". The env context must be allowed there too.
            var schema = WorkflowSchemaFactory.GetSchema(WorkflowFeatures.GetDefaults());
            var definition = schema.GetDefinition("scalar-needs-context-with-secrets");

            Assert.Contains("env", definition.ReaderContext, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("env", definition.EvaluatorContext, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("secrets", definition.ReaderContext, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Expression_EnvNamedValue_EvaluatesInsideLargerExpression_WhenRegistered()
        {
            // Mirrors how the workflow-job-with allowed context is turned into named values
            // during validation (ExpressionToken.IsValidExpression) and during evaluation.
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("github"),
                new NamedValueInfo<ContextValueNode>("inputs"),
                new NamedValueInfo<ContextValueNode>("vars"),
                new NamedValueInfo<ContextValueNode>("needs"),
                new NamedValueInfo<ContextValueNode>("strategy"),
                new NamedValueInfo<ContextValueNode>("matrix"),
                new NamedValueInfo<ContextValueNode>("env"),
            };
            var parser = new ActionsExpressionParser();

            var env = new DictionaryExpressionData();
            env.Add("SOMETHING", new StringExpressionData("1000"));
            var github = new DictionaryExpressionData();
            github.Add("event_name", new StringExpressionData("push"));
            var state = new TemplateContext();
            state.ExpressionValues["env"] = env;
            state.ExpressionValues["github"] = github;

            // env.SOMETHING on its own
            var tree1 = parser.CreateTree("env.SOMETHING", null, namedValues, null);
            Assert.Equal("1000", tree1.Evaluate(null, null, state, null).Value);

            // env.SOMETHING inside a larger expression (function call)
            var tree2 = parser.CreateTree("format('value: {0}', env.SOMETHING)", null, namedValues, null);
            Assert.Equal("value: 1000", tree2.Evaluate(null, null, state, null).Value);

            // env.SOMETHING inside a larger boolean expression
            var tree3 = parser.CreateTree("env.SOMETHING == '1000' && github.event_name == 'push'", null, namedValues, null);
            Assert.Equal(true, tree3.Evaluate(null, null, state, null).Value);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Sdk")]
        public void Expression_EnvNamedValue_RejectedWithExactIssueMessage_WhenNotRegistered()
        {
            // Reproduces the exact error from issue #2372: when the allowed context list
            // does not include "env", the parser rejects the named value.
            var namedValues = new List<INamedValueInfo>
            {
                new NamedValueInfo<ContextValueNode>("github"),
                new NamedValueInfo<ContextValueNode>("inputs"),
                new NamedValueInfo<ContextValueNode>("vars"),
                new NamedValueInfo<ContextValueNode>("needs"),
                new NamedValueInfo<ContextValueNode>("strategy"),
                new NamedValueInfo<ContextValueNode>("matrix"),
            };
            var parser = new ActionsExpressionParser();

            var ex = Assert.Throws<ParseException>(() =>
                parser.CreateTree("env.SOMETHING", null, namedValues, null));

            Assert.Contains("Unrecognized named-value: 'env'", ex.Message);
            Assert.Contains("position 1", ex.Message);

            // Also fails when env is referenced inside a larger expression
            var ex2 = Assert.Throws<ParseException>(() =>
                parser.CreateTree("format('value: {0}', env.SOMETHING)", null, namedValues, null));
            Assert.Contains("Unrecognized named-value: 'env'", ex2.Message);
        }
    }
}
