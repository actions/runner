using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using Xunit;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class ExpressionManagerL0
    {
        private Mock<IExecutionContext> _ec;
        private ExpressionManager _expressionManager;
        private Dictionary<String, PipelineContextData> _expressions;
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
                    bool actual = _expressionManager.Evaluate(_ec.Object, "always()").Value;

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
                    bool actual = _expressionManager.Evaluate(_ec.Object, "cancelled()").Value;

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
                    bool actual = _expressionManager.Evaluate(_ec.Object, "failure()").Value;

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
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
                    bool actual = _expressionManager.Evaluate(_ec.Object, "success()").Value;

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ContextNamedValue()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new { Condition = "github.ref == 'refs/heads/master'", VariableName = "ref", VariableValue = "refs/heads/master", Expected = true },
                    new { Condition = "github['ref'] == 'refs/heads/master'", VariableName = "ref", VariableValue = "refs/heads/master", Expected = true },
                    new { Condition = "github.nosuch || '' == ''", VariableName = "ref", VariableValue = "refs/heads/master", Expected = true },
                    new { Condition = "github['ref'] == 'refs/heads/release'", VariableName = "ref", VariableValue = "refs/heads/master", Expected = false },
                    new { Condition = "github.ref == 'refs/heads/release'", VariableName = "ref", VariableValue = "refs/heads/master", Expected = false },
                };
                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _ec.Object.ExpressionValues["github"] = new GitHubContext() { { variableSet.VariableName, new StringContextData(variableSet.VariableValue) } };

                    // Act.
                    bool actual = _expressionManager.Evaluate(_ec.Object, variableSet.Condition).Value;

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _expressionManager = new ExpressionManager();
            _expressionManager.Initialize(hc);
            return hc;
        }

        private void InitializeExecutionContext(TestHostContext hc)
        {
            _expressions = new Dictionary<String, PipelineContextData>();
            _jobContext = new JobContext();

            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            _ec.Setup(x => x.ExpressionValues).Returns(_expressions);
            _ec.Setup(x => x.JobContext).Returns(_jobContext);
        }
    }
}
