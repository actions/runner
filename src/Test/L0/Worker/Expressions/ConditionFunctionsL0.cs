using System;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Expressions;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker.Expressions
{
    public sealed class ConditionFunctionsL0
    {
        private TemplateContext _templateContext;
        private JobContext _jobContext;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void AlwaysFunction()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new { JobStatus = (ActionResult?)null, Expected = true },
                    new { JobStatus = (ActionResult?)ActionResult.Cancelled, Expected = true },
                    new { JobStatus = (ActionResult?)ActionResult.Failure, Expected = true },
                    new { JobStatus = (ActionResult?)ActionResult.Success, Expected = true },
                };
                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _jobContext.Status = variableSet.JobStatus;

                    // Act.
                    bool actual = Evaluate("always()");

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CancelledFunction()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new { JobStatus = (ActionResult?)ActionResult.Cancelled, Expected = true },
                    new { JobStatus = (ActionResult?)null, Expected = false },
                    new { JobStatus = (ActionResult?)ActionResult.Failure, Expected = false },
                    new { JobStatus = (ActionResult?)ActionResult.Success, Expected = false },
                };

                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _jobContext.Status = variableSet.JobStatus;

                    // Act.
                    bool actual = Evaluate("cancelled()");

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FailureFunction()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new { JobStatus = (ActionResult?)ActionResult.Failure, Expected = true },
                    new { JobStatus = (ActionResult?)null, Expected = false },
                    new { JobStatus = (ActionResult?)ActionResult.Cancelled, Expected = false },
                    new { JobStatus = (ActionResult?)ActionResult.Success, Expected = false },
                };
                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _jobContext.Status = variableSet.JobStatus;

                    // Act.
                    bool actual = Evaluate("failure()");

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData(ActionResult.Failure, ActionResult.Failure, true)]
        [InlineData(ActionResult.Failure, ActionResult.Success, false)]
        [InlineData(ActionResult.Success, ActionResult.Failure, true)]
        [InlineData(ActionResult.Success, ActionResult.Success, false)]
        [InlineData(ActionResult.Success, null, false)]
        public void FailureFunctionComposite(ActionResult jobStatus, ActionResult? actionStatus, bool expected)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange

                var executionContext = InitializeExecutionContext(hc);
                executionContext.Setup(x => x.GetGitHubContext("action_status")).Returns(actionStatus.ToString());
                executionContext.Setup(x => x.IsEmbedded).Returns(true);
                executionContext.Setup(x => x.Stage).Returns(ActionRunStage.Main);

                _jobContext.Status = jobStatus;

                // Act.
                bool actual = Evaluate("failure()");

                // Assert.
                Assert.Equal(expected, actual);

            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SuccessFunction()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new { JobStatus = (ActionResult?)null, Expected = true },
                    new { JobStatus = (ActionResult?)ActionResult.Success, Expected = true },
                    new { JobStatus = (ActionResult?)ActionResult.Cancelled, Expected = false },
                    new { JobStatus = (ActionResult?)ActionResult.Failure, Expected = false },
                };
                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _jobContext.Status = variableSet.JobStatus;

                    // Act.
                    bool actual = Evaluate("success()");

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }


        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData(ActionResult.Failure, ActionResult.Failure, false)]
        [InlineData(ActionResult.Failure, ActionResult.Success, true)]
        [InlineData(ActionResult.Success, ActionResult.Failure, false)]
        [InlineData(ActionResult.Success, ActionResult.Success, true)]
        [InlineData(ActionResult.Success, null, true)]
        public void SuccessFunctionComposite(ActionResult jobStatus, ActionResult? actionStatus, bool expected)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange

                var executionContext = InitializeExecutionContext(hc);
                executionContext.Setup(x => x.GetGitHubContext("action_status")).Returns(actionStatus.ToString());
                executionContext.Setup(x => x.IsEmbedded).Returns(true);
                executionContext.Setup(x => x.Stage).Returns(ActionRunStage.Main);

                _jobContext.Status = jobStatus;

                // Act.
                bool actual = Evaluate("success()");

                // Assert.
                Assert.Equal(expected, actual);

            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            return new TestHostContext(this, testName);
        }

        private Mock<IExecutionContext> InitializeExecutionContext(TestHostContext hc)
        {
            _jobContext = new JobContext();

            var executionContext = new Mock<IExecutionContext>();
            executionContext.SetupAllProperties();
            executionContext.Setup(x => x.JobContext).Returns(_jobContext);

            _templateContext = new TemplateContext();
            _templateContext.State[nameof(IExecutionContext)] = executionContext.Object;

            return executionContext;
        }

        private bool Evaluate(string expression)
        {
            var parser = new ExpressionParser();
            var functions = new IFunctionInfo[]
            {
                new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0),
                new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0),
                new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, 0),
                new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, 0),
            };
            var tree = parser.CreateTree(expression, null, null, functions);
            var result = tree.Evaluate(null, null, _templateContext, null);
            return result.IsTruthy;
        }
    }
}
