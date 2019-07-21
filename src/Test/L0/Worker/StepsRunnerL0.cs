﻿// using GitHub.DistributedTask.WebApi;
// using GitHub.Runner.Worker;
// using Moq;
// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using System.Linq;
// using System.Runtime.CompilerServices;
// using System.Threading.Tasks;
// using Xunit;
// using GitHub.DistributedTask.Expressions2;
// using GitHub.DistributedTask.Pipelines.ContextData;

// namespace GitHub.Runner.Common.Tests.Worker
// {
//     public sealed class StepsRunnerL0
//     {
//         private Mock<IExecutionContext> _ec;
//         private StepsRunner _stepsRunner;
//         private Variables _variables;
//         private Dictionary<string, PipelineContextData> _contexts;
//         private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
//         {
//             var hc = new TestHostContext(this, testName);
//             var expressionManager = new ExpressionManager();
//             expressionManager.Initialize(hc);
//             hc.SetSingleton<IExpressionManager>(expressionManager);
//             Dictionary<string, VariableValue> variablesToCopy = new Dictionary<string, VariableValue>();
//             variablesToCopy.Add(Constants.Variables.Agent.RetainDefaultEncoding, new VariableValue("true", false));
//             _variables = new Variables(
//                 hostContext: hc,
//                 copy: variablesToCopy);
//             _ec = new Mock<IExecutionContext>();
//             _ec.SetupAllProperties();
//             _ec.Setup(x => x.Variables).Returns(_variables);

//             _contexts = new Dictionary<string, PipelineContextData>();
//             _contexts["github"] = new DictionaryContextData();
//             _contexts["runner"] = new DictionaryContextData();
//             _contexts["actions"] = new DictionaryContextData();
//             _ec.Setup(x => x.ExpressionValues).Returns(_contexts);

//             var _stepContext = new StepsContext();
//             _ec.Setup(x => x.StepsContext).Returns(_stepContext);
//             _stepsRunner = new StepsRunner();
//             _stepsRunner.Initialize(hc);
//             return hc;
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public async Task RunNormalStepsAllStepPass()
//         {
//             using (TestHostContext hc = CreateTestContext())
//             {
//                 // Arrange.
//                 var variableSets = new[]
//                 {
//                     new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded)  },
//                     new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.SucceededOrFailed) },
//                     new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) }
//                 };
//                 foreach (var variableSet in variableSets)
//                 {
//                     _ec.Object.Result = null;

//                     // Act.
//                     await _stepsRunner.RunAsync(
//                         jobContext: _ec.Object,
//                         steps: variableSet.Select(x => x.Object).ToList());

//                     // Assert.
//                     Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
//                     Assert.Equal(2, variableSet.Length);
//                     variableSet[0].Verify(x => x.RunAsync());
//                     variableSet[1].Verify(x => x.RunAsync());
//                 }
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public async Task RunNormalStepsContinueOnError()
//         {
//             using (TestHostContext hc = CreateTestContext())
//             {
//                 // Arrange.
//                 var variableSets = new[]
//                 {
//                     new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, true), CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded)  },
//                     new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, true), CreateStep(TaskResult.Succeeded, ExpressionManager.SucceededOrFailed) },
//                     new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, true), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) },
//                     new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, true), CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, true)  },
//                     new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, true), CreateStep(TaskResult.Failed, ExpressionManager.SucceededOrFailed, true) },
//                     new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, true), CreateStep(TaskResult.Failed, ExpressionManager.Always, true) }
//                 };
//                 foreach (var variableSet in variableSets)
//                 {
//                     _ec.Object.Result = null;

//                     // Act.
//                     await _stepsRunner.RunAsync(
//                         jobContext: _ec.Object,
//                         steps: variableSet.Select(x => x.Object).ToList());

//                     // Assert.
//                     Assert.Equal(TaskResult.SucceededWithIssues, _ec.Object.Result);
//                     Assert.Equal(2, variableSet.Length);
//                     variableSet[0].Verify(x => x.RunAsync());
//                     variableSet[1].Verify(x => x.RunAsync());
//                 }
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public async Task RunsAfterFailureBasedOnCondition()
//         {
//             using (TestHostContext hc = CreateTestContext())
//             {
//                 // Arrange.
//                 var variableSets = new[]
//                 {
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded) },
//                         Expected = false,
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.SucceededOrFailed) },
//                         Expected = true,
//                     },
//                 };
//                 foreach (var variableSet in variableSets)
//                 {
//                     _ec.Object.Result = null;

//                     // Act.
//                     await _stepsRunner.RunAsync(
//                         jobContext: _ec.Object,
//                         steps: variableSet.Steps.Select(x => x.Object).ToList());

//                     // Assert.
//                     Assert.Equal(TaskResult.Failed, _ec.Object.Result ?? TaskResult.Succeeded);
//                     Assert.Equal(2, variableSet.Steps.Length);
//                     variableSet.Steps[0].Verify(x => x.RunAsync());
//                     variableSet.Steps[1].Verify(x => x.RunAsync(), variableSet.Expected ? Times.Once() : Times.Never());
//                 }
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public async Task RunsAlwaysSteps()
//         {
//             using (TestHostContext hc = CreateTestContext())
//             {
//                 // Arrange.
//                 var variableSets = new[]
//                 {
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) },
//                         Expected = TaskResult.Succeeded,
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) },
//                         Expected = TaskResult.Failed,
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) },
//                         Expected = TaskResult.Failed,
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded), CreateStep(TaskResult.Failed, ExpressionManager.Always) },
//                         Expected = TaskResult.Failed,
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded), CreateStep(TaskResult.Failed, ExpressionManager.Always, true) },
//                         Expected = TaskResult.SucceededWithIssues,
//                     },
//                 };
//                 foreach (var variableSet in variableSets)
//                 {
//                     _ec.Object.Result = null;

//                     // Act.
//                     await _stepsRunner.RunAsync(
//                         jobContext: _ec.Object,
//                         steps: variableSet.Steps.Select(x => x.Object).ToList());

//                     // Assert.
//                     Assert.Equal(variableSet.Expected, _ec.Object.Result ?? TaskResult.Succeeded);
//                     Assert.Equal(2, variableSet.Steps.Length);
//                     variableSet.Steps[0].Verify(x => x.RunAsync());
//                     variableSet.Steps[1].Verify(x => x.RunAsync());
//                 }
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public async Task SetsJobResultCorrectly()
//         {
//             using (TestHostContext hc = CreateTestContext())
//             {
//                 // Arrange.
//                 var variableSets = new[]
//                 {
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded) },
//                         Expected = TaskResult.Failed
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.SucceededOrFailed) },
//                         Expected = TaskResult.Failed
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) },
//                         Expected = TaskResult.Failed
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, continueOnError: true), CreateStep(TaskResult.Failed, ExpressionManager.Succeeded) },
//                         Expected = TaskResult.Failed
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, continueOnError: true), CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded) },
//                         Expected = TaskResult.SucceededWithIssues
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, continueOnError: true), CreateStep(TaskResult.Failed, ExpressionManager.Succeeded, continueOnError: true) },
//                         Expected = TaskResult.SucceededWithIssues
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.SucceededOrFailed) },
//                         Expected = TaskResult.Succeeded
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded), CreateStep(TaskResult.Failed, ExpressionManager.Succeeded) },
//                         Expected = TaskResult.Failed
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded), CreateStep(TaskResult.SucceededWithIssues, ExpressionManager.Succeeded) },
//                         Expected = TaskResult.SucceededWithIssues
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.SucceededWithIssues, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded) },
//                         Expected = TaskResult.SucceededWithIssues
//                     },
//                     new
//                     {
//                         Steps = new[] { CreateStep(TaskResult.SucceededWithIssues, ExpressionManager.Succeeded), CreateStep(TaskResult.Failed, ExpressionManager.Succeeded) },
//                         Expected = TaskResult.Failed
//                     },
//                 //  Abandoned
//                 //  Canceled
//                 //  Failed
//                 //  Skipped
//                 //  Succeeded
//                 //  SucceededWithIssues
//                 };
//                 foreach (var variableSet in variableSets)
//                 {
//                     _ec.Object.Result = null;

//                     // Act.
//                     await _stepsRunner.RunAsync(
//                         jobContext: _ec.Object,
//                         steps: variableSet.Steps.Select(x => x.Object).ToList());

//                     // Assert.
//                     Assert.True(
//                         variableSet.Expected == (_ec.Object.Result ?? TaskResult.Succeeded),
//                         $"Expected '{variableSet.Expected}'. Actual '{_ec.Object.Result}'. Steps: {FormatSteps(variableSet.Steps)}");
//                 }
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public async Task SkipsAfterFailureOnlyBaseOnCondition()
//         {
//             using (TestHostContext hc = CreateTestContext())
//             {
//                 // Arrange.
//                 var variableSets = new[]
//                 {
//                     new
//                     {
//                         Step = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded) },
//                         Expected = false
//                     },
//                     new
//                     {
//                         Step = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.SucceededOrFailed) },
//                         Expected = true
//                     },
//                     new
//                     {
//                         Step = new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) },
//                         Expected = true
//                     }
//                 };
//                 foreach (var variableSet in variableSets)
//                 {
//                     _ec.Object.Result = null;

//                     // Act.
//                     await _stepsRunner.RunAsync(
//                         jobContext: _ec.Object,
//                         steps: variableSet.Step.Select(x => x.Object).ToList());

//                     // Assert.                    
//                     Assert.Equal(2, variableSet.Step.Length);
//                     variableSet.Step[0].Verify(x => x.RunAsync());
//                     variableSet.Step[1].Verify(x => x.RunAsync(), variableSet.Expected ? Times.Once() : Times.Never());
//                 }
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public async Task AlwaysMeansAlways()
//         {
//             using (TestHostContext hc = CreateTestContext())
//             {
//                 // Arrange.
//                 var variableSets = new[]
//                 {
//                     new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) },
//                     new[] { CreateStep(TaskResult.SucceededWithIssues, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) },
//                     new[] { CreateStep(TaskResult.Failed, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) },
//                     new[] { CreateStep(TaskResult.Canceled, ExpressionManager.Succeeded), CreateStep(TaskResult.Succeeded, ExpressionManager.Always) }
//                 };
//                 foreach (var variableSet in variableSets)
//                 {
//                     _ec.Object.Result = null;

//                     // Act.
//                     await _stepsRunner.RunAsync(
//                         jobContext: _ec.Object,
//                         steps: variableSet.Select(x => x.Object).ToList());

//                     // Assert.
//                     Assert.Equal(2, variableSet.Length);
//                     variableSet[0].Verify(x => x.RunAsync());
//                     variableSet[1].Verify(x => x.RunAsync(), Times.Once());
//                 }
//             }
//         }

//         [Fact]
//         [Trait("Level", "L0")]
//         [Trait("Category", "Worker")]
//         public async Task TreatsConditionErrorAsFailure()
//         {
//             using (TestHostContext hc = CreateTestContext())
//             {
//                 var expressionManager = new Mock<IExpressionManager>();
//                 expressionManager.Object.Initialize(hc);
//                 hc.SetSingleton<IExpressionManager>(expressionManager.Object);
//                 expressionManager.Setup(x => x.Evaluate(It.IsAny<IExecutionContext>(), It.IsAny<IExpressionNode>(), It.IsAny<bool>())).Throws(new Exception());

//                 // Arrange.
//                 var variableSets = new[]
//                 {
//                     new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded) },
//                     new[] { CreateStep(TaskResult.Succeeded, ExpressionManager.Succeeded) },
//                 };
//                 foreach (var variableSet in variableSets)
//                 {
//                     _ec.Object.Result = null;

//                     // Act.
//                     await _stepsRunner.RunAsync(
//                         jobContext: _ec.Object,
//                         steps: variableSet.Select(x => x.Object).ToList());

//                     // Assert.
//                     Assert.Equal(TaskResult.Failed, _ec.Object.Result ?? TaskResult.Succeeded);
//                 }
//             }
//         }

//         private Mock<IStep> CreateStep(TaskResult result, IExpressionNode condition, Boolean continueOnError = false)
//         {
//             // Setup the step.
//             var step = new Mock<IStep>();
//             step.Setup(x => x.Condition).Returns(condition);
//             step.Setup(x => x.ContinueOnError).Returns(continueOnError);
//             step.Setup(x => x.Enabled).Returns(true);
//             step.Setup(x => x.RunAsync()).Returns(Task.CompletedTask);

//             // Setup the step execution context.
//             var stepContext = new Mock<IExecutionContext>();
//             stepContext.SetupAllProperties();
//             stepContext.Setup(x => x.Variables).Returns(_variables);
//             stepContext.Setup(x => x.ExpressionValues).Returns(_contexts);
//             stepContext.Setup(x => x.Complete(It.IsAny<TaskResult?>(), It.IsAny<string>(), It.IsAny<string>()))
//                 .Callback((TaskResult? r, string currentOperation, string resultCode) =>
//                 {
//                     if (r != null)
//                     {
//                         stepContext.Object.Result = r;
//                     }
//                 });
//             stepContext.Object.Result = result;
//             step.Setup(x => x.ExecutionContext).Returns(stepContext.Object);

//             return step;
//         }

//         private string FormatSteps(IEnumerable<Mock<IStep>> steps)
//         {
//             return String.Join(
//                 " ; ",
//                 steps.Select(x => String.Format(
//                     CultureInfo.InvariantCulture,
//                     "Returns={0},Condition=[{1}],ContinueOnError={2},Enabled={3}",
//                     x.Object.ExecutionContext.Result,
//                     x.Object.Condition,
//                     x.Object.ContinueOnError,
//                     x.Object.Enabled)));
//         }
//     }
// }
