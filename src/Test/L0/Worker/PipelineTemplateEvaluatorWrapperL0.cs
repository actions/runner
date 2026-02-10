using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using LegacyContextData = GitHub.DistributedTask.Pipelines.ContextData;
using LegacyExpressions = GitHub.DistributedTask.Expressions2;
using Moq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class PipelineTemplateEvaluatorWrapperL0
    {
        private CancellationTokenSource _ecTokenSource;
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;

        // -------------------------------------------------------------------
        // EvaluateAndCompare core behavior
        // -------------------------------------------------------------------

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateAndCompare_DoesNotRecordMismatch_WhenResultsMatch()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);

                var token = new StringToken(null, null, null, "test-value");
                var contextData = new DictionaryContextData();
                var expressionFunctions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateStepDisplayName(token, contextData, expressionFunctions);

                Assert.Equal("test-value", result);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateAndCompare_SkipsMismatchRecording_WhenCancellationOccursDuringEvaluation()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);

                // Call EvaluateAndCompare directly: the new evaluator cancels the token
                // and returns a different value, forcing hasMismatch = true.
                // Because cancellation flipped during the evaluation window, the
                // mismatch should be skipped.
                var result = wrapper.EvaluateAndCompare<string, string>(
                    "TestCancellationSkip",
                    () => "legacy-value",
                    () =>
                    {
                        _ecTokenSource.Cancel();
                        return "different-value";
                    },
                    (legacy, @new) => string.Equals(legacy, @new, StringComparison.Ordinal));

                Assert.Equal("legacy-value", result);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateAndCompare_RecordsMismatch_WhenResultsDifferWithoutCancellation()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);

                // Different results without cancellation — mismatch SHOULD be recorded.
                var result = wrapper.EvaluateAndCompare<string, string>(
                    "TestMismatchRecorded",
                    () => "legacy-value",
                    () => "different-value",
                    (legacy, @new) => string.Equals(legacy, @new, StringComparison.Ordinal));

                Assert.Equal("legacy-value", result);
                Assert.True(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        // -------------------------------------------------------------------
        // Smoke tests — both parsers agree, no mismatch recorded
        // -------------------------------------------------------------------

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateStepContinueOnError_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var token = new BooleanToken(null, null, null, true);
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateStepContinueOnError(token, contextData, functions);

                Assert.True(result);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateStepEnvironment_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var token = new MappingToken(null, null, null);
                token.Add(new StringToken(null, null, null, "FOO"), new StringToken(null, null, null, "bar"));
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateStepEnvironment(token, contextData, functions, StringComparer.OrdinalIgnoreCase);

                Assert.NotNull(result);
                Assert.Equal("bar", result["FOO"]);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateStepIf_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var token = new BasicExpressionToken(null, null, null, "true");
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();
                var expressionState = new List<KeyValuePair<string, object>>();

                var result = wrapper.EvaluateStepIf(token, contextData, functions, expressionState);

                Assert.True(result);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateStepInputs_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var token = new MappingToken(null, null, null);
                token.Add(new StringToken(null, null, null, "input1"), new StringToken(null, null, null, "val1"));
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateStepInputs(token, contextData, functions);

                Assert.NotNull(result);
                Assert.Equal("val1", result["input1"]);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateStepTimeout_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var token = new NumberToken(null, null, null, 10);
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateStepTimeout(token, contextData, functions);

                Assert.Equal(10, result);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateJobContainer_EmptyImage_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var token = new StringToken(null, null, null, "");
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateJobContainer(token, contextData, functions);

                Assert.Null(result);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateJobOutput_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var token = new MappingToken(null, null, null);
                token.Add(new StringToken(null, null, null, "out1"), new StringToken(null, null, null, "val1"));
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateJobOutput(token, contextData, functions);

                Assert.NotNull(result);
                Assert.Equal("val1", result["out1"]);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateEnvironmentUrl_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var token = new StringToken(null, null, null, "https://example.com");
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateEnvironmentUrl(token, contextData, functions);

                Assert.NotNull(result);
                var stringResult = result as StringToken;
                Assert.NotNull(stringResult);
                Assert.Equal("https://example.com", stringResult.Value);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateJobDefaultsRun_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var token = new MappingToken(null, null, null);
                token.Add(new StringToken(null, null, null, "shell"), new StringToken(null, null, null, "bash"));
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateJobDefaultsRun(token, contextData, functions);

                Assert.NotNull(result);
                Assert.Equal("bash", result["shell"]);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateJobServiceContainers_Null_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateJobServiceContainers(null, contextData, functions);

                Assert.Null(result);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateJobSnapshotRequest_Null_BothParsersAgree()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);
                var contextData = new DictionaryContextData();
                var functions = new List<LegacyExpressions.IFunctionInfo>();

                var result = wrapper.EvaluateJobSnapshotRequest(null, contextData, functions);

                Assert.Null(result);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        // -------------------------------------------------------------------
        // JSON parse error equivalence via EvaluateAndCompare
        // -------------------------------------------------------------------

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateAndCompare_JsonReaderExceptions_TreatedAsEquivalent()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);

                // Both throw JsonReaderException with different messages — should be treated as equivalent
                var legacyEx = new Newtonsoft.Json.JsonReaderException("Error reading JToken from JsonReader. Path '', line 0, position 0.");
                var newEx = new Newtonsoft.Json.JsonReaderException("Error parsing fromJson", new Newtonsoft.Json.JsonReaderException("Unexpected end"));

                Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
                    wrapper.EvaluateAndCompare<string, string>(
                        "TestJsonEquivalence",
                        () => throw legacyEx,
                        () => throw newEx,
                        (a, b) => string.Equals(a, b, StringComparison.Ordinal)));

                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateAndCompare_MixedJsonExceptionTypes_TreatedAsEquivalent()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);

                // Legacy throws Newtonsoft JsonReaderException, new throws System.Text.Json.JsonException
                var legacyEx = new Newtonsoft.Json.JsonReaderException("Error reading JToken");
                var newEx = new System.Text.Json.JsonException("Error parsing fromJson");

                Assert.Throws<Newtonsoft.Json.JsonReaderException>(() =>
                    wrapper.EvaluateAndCompare<string, string>(
                        "TestMixedJsonTypes",
                        () => throw legacyEx,
                        () => throw newEx,
                        (a, b) => string.Equals(a, b, StringComparison.Ordinal)));

                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateAndCompare_NonJsonExceptions_RecordsMismatch()
        {
            try
            {
                Setup();
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);

                // Both throw non-JSON exceptions with different messages — should record mismatch
                var legacyEx = new InvalidOperationException("some error");
                var newEx = new InvalidOperationException("different error");

                Assert.Throws<InvalidOperationException>(() =>
                    wrapper.EvaluateAndCompare<string, string>(
                        "TestNonJsonMismatch",
                        () => throw legacyEx,
                        () => throw newEx,
                        (a, b) => string.Equals(a, b, StringComparison.Ordinal)));

                Assert.True(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private void Setup([CallerMemberName] string name = "")
        {
            _ecTokenSource?.Dispose();
            _ecTokenSource = new CancellationTokenSource();

            _hc = new TestHostContext(this, name);

            var expressionValues = new LegacyContextData.DictionaryContextData();
            var expressionFunctions = new List<LegacyExpressions.IFunctionInfo>();

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Global)
                .Returns(new GlobalContext
                {
                    FileTable = new List<String>(),
                    Variables = new Variables(_hc, new Dictionary<string, VariableValue>()),
                    WriteDebug = true,
                });
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);
            _ec.Setup(x => x.ExpressionValues).Returns(expressionValues);
            _ec.Setup(x => x.ExpressionFunctions).Returns(expressionFunctions);
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { _hc.GetTrace().Info($"{tag}{message}"); });
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<ExecutionContextLogOptions>())).Callback((Issue issue, ExecutionContextLogOptions logOptions) => { _hc.GetTrace().Info($"[{issue.Type}]{logOptions.LogMessageOverride ?? issue.Message}"); });
        }

        private void Teardown()
        {
            _hc?.Dispose();
        }
    }
}
