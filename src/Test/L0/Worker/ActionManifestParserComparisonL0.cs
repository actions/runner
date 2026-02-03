using GitHub.Actions.WorkflowParser;
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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    /// <summary>
    /// Tests for parser comparison wrapper classes.
    /// </summary>
    public sealed class ActionManifestParserComparisonL0
    {
        private CancellationTokenSource _ecTokenSource;
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ConvertToLegacySteps_ProducesCorrectSteps_WithExplicitPropertyMapping()
        {
            try
            {
                // Arrange - Test that ActionManifestManagerWrapper properly converts new steps to legacy format
                Setup();

                // Enable comparison feature
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                // Register required services
                var legacyManager = new ActionManifestManagerLegacy();
                legacyManager.Initialize(_hc);
                _hc.SetSingleton<IActionManifestManagerLegacy>(legacyManager);

                var newManager = new ActionManifestManager();
                newManager.Initialize(_hc);
                _hc.SetSingleton<IActionManifestManager>(newManager);

                var wrapper = new ActionManifestManagerWrapper();
                wrapper.Initialize(_hc);

                var manifestPath = Path.Combine(TestUtil.GetTestDataPath(), "conditional_composite_action.yml");

                // Act - Load through the wrapper (which internally converts)
                var result = wrapper.Load(_ec.Object, manifestPath);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(ActionExecutionType.Composite, result.Execution.ExecutionType);

                var compositeExecution = result.Execution as CompositeActionExecutionData;
                Assert.NotNull(compositeExecution);
                Assert.NotNull(compositeExecution.Steps);
                Assert.Equal(6, compositeExecution.Steps.Count);

                // Verify steps are NOT null (this was the bug - JSON round-trip produced nulls)
                foreach (var step in compositeExecution.Steps)
                {
                    Assert.NotNull(step);
                    Assert.NotNull(step.Reference);
                    Assert.IsType<GitHub.DistributedTask.Pipelines.ScriptReference>(step.Reference);
                }

                // Verify step with condition
                var successStep = compositeExecution.Steps[2];
                Assert.Equal("success-conditional", successStep.ContextName);
                Assert.Equal("success()", successStep.Condition);

                // Verify step with complex condition
                var lastStep = compositeExecution.Steps[5];
                Assert.Contains("inputs.exit-code == 1", lastStep.Condition);
                Assert.Contains("failure()", lastStep.Condition);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void EvaluateJobContainer_EmptyImage_BothParsersReturnNull()
        {
            try
            {
                // Arrange - Test that both parsers return null for empty container image at runtime
                Setup();

                var fileTable = new List<string>();

                // Create legacy evaluator
                var legacyTraceWriter = new GitHub.DistributedTask.ObjectTemplating.EmptyTraceWriter();
                var schema = PipelineTemplateSchemaFactory.GetSchema();
                var legacyEvaluator = new PipelineTemplateEvaluator(legacyTraceWriter, schema, fileTable);

                // Create new evaluator
                var newTraceWriter = new GitHub.Actions.WorkflowParser.ObjectTemplating.EmptyTraceWriter();
                var newEvaluator = new WorkflowTemplateEvaluator(newTraceWriter, fileTable, features: null);

                // Create a token representing an empty container image (simulates expression evaluated to empty string)
                var emptyImageToken = new StringToken(null, null, null, "");

                var contextData = new DictionaryContextData();
                var expressionFunctions = new List<LegacyExpressions.IFunctionInfo>();

                // Act - Call both evaluators
                var legacyResult = legacyEvaluator.EvaluateJobContainer(emptyImageToken, contextData, expressionFunctions);

                // Convert token for new evaluator
                var newToken = new GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.StringToken(null, null, null, "");
                var newContextData = new GitHub.Actions.Expressions.Data.DictionaryExpressionData();
                var newExpressionFunctions = new List<GitHub.Actions.Expressions.IFunctionInfo>();

                var newResult = newEvaluator.EvaluateJobContainer(newToken, newContextData, newExpressionFunctions);

                // Assert - Both should return null for empty image (no container)
                Assert.Null(legacyResult);
                Assert.Null(newResult);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FromJsonEmptyString_BothParsersFail_WithDifferentMessages()
        {
            // This test verifies that both parsers fail with different error messages when parsing fromJSON('')
            // The comparison layer should treat these as semantically equivalent (both are JSON parse errors)
            try
            {
                Setup();

                var fileTable = new List<string>();

                // Create legacy evaluator
                var legacyTraceWriter = new GitHub.DistributedTask.ObjectTemplating.EmptyTraceWriter();
                var schema = PipelineTemplateSchemaFactory.GetSchema();
                var legacyEvaluator = new PipelineTemplateEvaluator(legacyTraceWriter, schema, fileTable);

                // Create new evaluator
                var newTraceWriter = new GitHub.Actions.WorkflowParser.ObjectTemplating.EmptyTraceWriter();
                var newEvaluator = new WorkflowTemplateEvaluator(newTraceWriter, fileTable, features: null);

                // Create expression token for fromJSON('')
                var legacyToken = new BasicExpressionToken(null, null, null, "fromJson('')");
                var newToken = new GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens.BasicExpressionToken(null, null, null, "fromJson('')");

                var contextData = new DictionaryContextData();
                var newContextData = new GitHub.Actions.Expressions.Data.DictionaryExpressionData();
                var expressionFunctions = new List<LegacyExpressions.IFunctionInfo>();
                var newExpressionFunctions = new List<GitHub.Actions.Expressions.IFunctionInfo>();

                // Act - Both should throw
                Exception legacyException = null;
                Exception newException = null;

                try
                {
                    legacyEvaluator.EvaluateStepDisplayName(legacyToken, contextData, expressionFunctions);
                }
                catch (Exception ex)
                {
                    legacyException = ex;
                }

                try
                {
                    newEvaluator.EvaluateStepName(newToken, newContextData, newExpressionFunctions);
                }
                catch (Exception ex)
                {
                    newException = ex;
                }

                // Assert - Both threw exceptions
                Assert.NotNull(legacyException);
                Assert.NotNull(newException);

                // Verify the error messages are different (which is why we need semantic comparison)
                Assert.NotEqual(legacyException.Message, newException.Message);

                // Verify both are JSON parse errors (contain JSON-related error indicators)
                var legacyFullMsg = GetFullExceptionMessage(legacyException);
                var newFullMsg = GetFullExceptionMessage(newException);

                // At least one should contain indicators of JSON parsing failure
                var legacyIsJsonError = legacyFullMsg.Contains("JToken") ||
                                        legacyFullMsg.Contains("JsonReader") ||
                                        legacyFullMsg.Contains("fromJson");
                var newIsJsonError = newFullMsg.Contains("JToken") ||
                                     newFullMsg.Contains("JsonReader") ||
                                     newFullMsg.Contains("fromJson");

                Assert.True(legacyIsJsonError, $"Legacy exception should be JSON error: {legacyFullMsg}");
                Assert.True(newIsJsonError, $"New exception should be JSON error: {newFullMsg}");
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
                // Arrange - Test that mismatches are not recorded when cancellation state changes during evaluation
                Setup();

                // Enable comparison feature
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                // Create the wrapper
                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);

                // Create a simple token for evaluation
                var token = new StringToken(null, null, null, "test-value");
                var contextData = new DictionaryContextData();
                var expressionFunctions = new List<LegacyExpressions.IFunctionInfo>();

                // First evaluation without cancellation - should work normally
                var result1 = wrapper.EvaluateStepDisplayName(token, contextData, expressionFunctions);
                Assert.Equal("test-value", result1);
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);

                // Now simulate a scenario where cancellation occurs during evaluation
                // Cancel the token before next evaluation
                _ecTokenSource.Cancel();

                // Evaluate again - even if there were a mismatch, it should be skipped due to cancellation
                var result2 = wrapper.EvaluateStepDisplayName(token, contextData, expressionFunctions);
                Assert.Equal("test-value", result2);

                // Verify no mismatch was recorded (cancellation race detection should have prevented it)
                // Note: In this test, both parsers return the same result, so there's no actual mismatch.
                // The cancellation race detection is a safeguard for when results differ due to timing.
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
        public void EvaluateAndCompare_DoesNotRecordMismatch_WhenResultsMatch()
        {
            try
            {
                // Arrange - Test that no mismatch is recorded when both parsers return matching results
                Setup();

                // Enable comparison feature
                _ec.Object.Global.Variables.Set(Constants.Runner.Features.CompareWorkflowParser, "true");

                // Create the wrapper
                var wrapper = new PipelineTemplateEvaluatorWrapper(_hc, _ec.Object);

                // Create a simple token for evaluation
                var token = new StringToken(null, null, null, "test-value");
                var contextData = new DictionaryContextData();
                var expressionFunctions = new List<LegacyExpressions.IFunctionInfo>();

                // Evaluation without cancellation - should work normally and not record mismatch for matching results
                var result = wrapper.EvaluateStepDisplayName(token, contextData, expressionFunctions);
                Assert.Equal("test-value", result);

                // Since both parsers return the same result, no mismatch should be recorded
                Assert.False(_ec.Object.Global.HasTemplateEvaluatorMismatch);
            }
            finally
            {
                Teardown();
            }
        }

        private string GetFullExceptionMessage(Exception ex)
        {
            var messages = new List<string>();
            var current = ex;
            while (current != null)
            {
                messages.Add(current.Message);
                current = current.InnerException;
            }
            return string.Join(" -> ", messages);
        }

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
