using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Moq;
using Xunit;
using Microsoft.TeamFoundation.DistributedTask.Expressions;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{
    public sealed class ExpressionManagerL0
    {
        private Mock<IExecutionContext> _ec;
        private ExpressionManager _expressionManager;
        private Variables _variables;

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
                    new { JobStatus = (TaskResult?)null, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.Canceled, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.Failed, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.Succeeded, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.SucceededWithIssues, Expected = true },
                };
                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _ec.Object.Variables.Agent_JobStatus = variableSet.JobStatus;
                    IExpressionNode condition = _expressionManager.Parse(_ec.Object, "always()");

                    // Act.
                    bool actual = _expressionManager.Evaluate(_ec.Object, condition).Value;

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CanceledFunction()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new { JobStatus = (TaskResult?)TaskResult.Canceled, Expected = true },
                    new { JobStatus = (TaskResult?)null, Expected = false },
                    new { JobStatus = (TaskResult?)TaskResult.Failed, Expected = false },
                    new { JobStatus = (TaskResult?)TaskResult.Succeeded, Expected = false },
                    new { JobStatus = (TaskResult?)TaskResult.SucceededWithIssues, Expected = false },
                };
                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _ec.Object.Variables.Agent_JobStatus = variableSet.JobStatus;
                    IExpressionNode condition = _expressionManager.Parse(_ec.Object, "canceled()");

                    // Act.
                    bool actual = _expressionManager.Evaluate(_ec.Object, condition).Value;

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void FailedFunction()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new { JobStatus = (TaskResult?)TaskResult.Failed, Expected = true },
                    new { JobStatus = (TaskResult?)null, Expected = false },
                    new { JobStatus = (TaskResult?)TaskResult.Canceled, Expected = false },
                    new { JobStatus = (TaskResult?)TaskResult.Succeeded, Expected = false },
                    new { JobStatus = (TaskResult?)TaskResult.SucceededWithIssues, Expected = false },
                };
                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _ec.Object.Variables.Agent_JobStatus = variableSet.JobStatus;
                    IExpressionNode condition = _expressionManager.Parse(_ec.Object, "failed()");

                    // Act.
                    bool actual = _expressionManager.Evaluate(_ec.Object, condition).Value;

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SucceededFunction()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new { JobStatus = (TaskResult?)null, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.Succeeded, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.SucceededWithIssues, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.Canceled, Expected = false },
                    new { JobStatus = (TaskResult?)TaskResult.Failed, Expected = false },
                };
                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _ec.Object.Variables.Agent_JobStatus = variableSet.JobStatus;
                    IExpressionNode condition = _expressionManager.Parse(_ec.Object, "succeeded()");

                    // Act.
                    bool actual = _expressionManager.Evaluate(_ec.Object, condition).Value;

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void SucceededOrFailedFunction()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new { JobStatus = (TaskResult?)null, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.Succeeded, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.SucceededWithIssues, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.Failed, Expected = true },
                    new { JobStatus = (TaskResult?)TaskResult.Canceled, Expected = false },
                };
                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _ec.Object.Variables.Agent_JobStatus = variableSet.JobStatus;
                    IExpressionNode condition = _expressionManager.Parse(_ec.Object, "succeededOrFailed()");

                    // Act.
                    bool actual = _expressionManager.Evaluate(_ec.Object, condition).Value;

                    // Assert.
                    Assert.Equal(variableSet.Expected, actual);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void VariablesNamedValue()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new { Condition = "eq(variables.someVARIABLE, 'someVALUE')", VariableName = "SOMEvariable", VariableValue = "SOMEvalue", Expected = true },
                    new { Condition = "eq(variables['some.VARIABLE'], 'someVALUE')", VariableName = "SOME.variable", VariableValue = "SOMEvalue", Expected = true },
                    new { Condition = "eq(variables.nosuch, '')", VariableName = "SomeVariable", VariableValue = "SomeValue", Expected = true },
                    new { Condition = "eq(variables['some.VARIABLE'], 'other value')", VariableName = "SOME.variable", VariableValue = "SOMEvalue", Expected = false },
                    new { Condition = "eq(variables.nosuch, 'SomeValue')", VariableName = "SomeVariable", VariableValue = "SomeValue", Expected = false },
                };
                foreach (var variableSet in variableSets)
                {
                    InitializeExecutionContext(hc);
                    _ec.Object.Variables.Set(variableSet.VariableName, variableSet.VariableValue);
                    IExpressionNode condition = _expressionManager.Parse(_ec.Object, variableSet.Condition);

                    // Act.
                    bool actual = _expressionManager.Evaluate(_ec.Object, condition).Value;

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
            List<string> warnings;
            _variables = new Variables(
                hostContext: hc,
                copy: new Dictionary<string, VariableValue>(),
                warnings: out warnings);
            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            _ec.Setup(x => x.Variables).Returns(_variables);
        }
    }
}
