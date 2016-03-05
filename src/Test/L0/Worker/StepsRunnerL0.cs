using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker
{
    public sealed class StepsRunnerL0
    {
        private Mock<IExecutionContext> _context;
        private StepsRunner _stepsRunner;

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var tc = new TestHostContext(nameof(StepsRunnerL0), testName);
            _context = new Mock<IExecutionContext>();
            _context.Object.Initialize(tc);
            _stepsRunner = new StepsRunner();
            _stepsRunner.Initialize(tc);
            return tc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunsAfterContinueOnError()
        {
            // Arrange.
            using (TestHostContext hc = CreateTestContext())
            {
                var variableSets = new[]
                {
                    new[] { CreateStep(TaskResult.Failed, continueOnError: true), CreateStep(TaskResult.Succeeded) },
                    new[] { CreateStep(TaskResult.Failed, continueOnError: true), CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                    new[] { CreateStep(TaskResult.Failed, continueOnError: true), CreateStep(TaskResult.Succeeded, continueOnError: true) },
                    new[] { CreateStep(TaskResult.Failed, continueOnError: true), CreateStep(TaskResult.Succeeded, critical: true) },
                    new[] { CreateStep(TaskResult.Failed, continueOnError: true), CreateStep(TaskResult.Succeeded, isFinally: true) },
                    new[] { CreateStep(TaskResult.Failed, continueOnError: true, critical: true), CreateStep(TaskResult.Succeeded) },
                    new[] { CreateStep(TaskResult.Failed, continueOnError: true, critical: true), CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                    new[] { CreateStep(TaskResult.Failed, continueOnError: true, critical: true), CreateStep(TaskResult.Succeeded, continueOnError: true) },
                    new[] { CreateStep(TaskResult.Failed, continueOnError: true, critical: true), CreateStep(TaskResult.Succeeded, critical: true) },
                    new[] { CreateStep(TaskResult.Failed, continueOnError: true, critical: true), CreateStep(TaskResult.Succeeded, isFinally: true) },
                };
                foreach (var variableSet in variableSets)
                {
                    // Act.
                    TaskResult jobResult = await _stepsRunner.RunAsync(
                        context: _context.Object,
                        steps: variableSet.Select(x => x.Object).ToList());

                    // Assert.
                    Assert.Equal(TaskResult.SucceededWithIssues, jobResult);
                    Assert.Equal(2, variableSet.Length);
                    variableSet[0].Verify(x => x.RunAsync());
                    variableSet[1].Verify(x => x.RunAsync());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunsAlwaysRuns()
        {
            // Arrange.
            using (TestHostContext hc = CreateTestContext())
            {
                var variableSets = new[]
                {
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Succeeded), CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                        Expected = TaskResult.Succeeded,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed), CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                        Expected = TaskResult.Failed,
                    },
                };
                foreach (var variableSet in variableSets)
                {
                    // Act.
                    TaskResult jobResult = await _stepsRunner.RunAsync(
                        context: _context.Object,
                        steps: variableSet.Steps.Select(x => x.Object).ToList());

                    // Assert.
                    Assert.Equal(variableSet.Expected, jobResult);
                    Assert.Equal(2, variableSet.Steps.Length);
                    variableSet.Steps[0].Verify(x => x.RunAsync());
                    variableSet.Steps[1].Verify(x => x.RunAsync());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunsFinally()
        {
            // Arrange.
            using (TestHostContext hc = CreateTestContext())
            {
                var variableSets = new[]
                {
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Succeeded), CreateStep(TaskResult.Succeeded, isFinally: true) },
                        Expected = TaskResult.Succeeded,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed), CreateStep(TaskResult.Succeeded, isFinally: true) },
                        Expected = TaskResult.Failed,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, critical: true), CreateStep(TaskResult.Succeeded, isFinally: true) },
                        Expected = TaskResult.Failed,
                    },
                };
                foreach (var variableSet in variableSets)
                {
                    // Act.
                    TaskResult jobResult = await _stepsRunner.RunAsync(
                        context: _context.Object,
                        steps: variableSet.Steps.Select(x => x.Object).ToList());

                    // Assert.
                    Assert.Equal(variableSet.Expected, jobResult);
                    Assert.Equal(2, variableSet.Steps.Length);
                    variableSet.Steps[0].Verify(x => x.RunAsync());
                    variableSet.Steps[1].Verify(x => x.RunAsync());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SetsJobResultCorrectly()
        {
            // Arrange.
            using (TestHostContext hc = CreateTestContext())
            {
                var variableSets = new[]
                {
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Abandoned) },
                        Expected = TaskResult.Succeeded
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Canceled) },
                        Expected = TaskResult.Succeeded
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed), CreateStep(TaskResult.Succeeded) },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed), CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed), CreateStep(TaskResult.Succeeded, isFinally: true) },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, continueOnError: true), CreateStep(TaskResult.Failed) },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, continueOnError: true), CreateStep(TaskResult.Succeeded) },
                        Expected = TaskResult.SucceededWithIssues
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, continueOnError: true, critical: true), CreateStep(TaskResult.Succeeded) },
                        Expected = TaskResult.SucceededWithIssues
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Skipped) },
                        Expected = TaskResult.Succeeded
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Succeeded) },
                        Expected = TaskResult.Succeeded
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Succeeded), CreateStep(TaskResult.Failed) },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Succeeded), CreateStep(TaskResult.SucceededWithIssues) },
                        Expected = TaskResult.SucceededWithIssues
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.SucceededWithIssues), CreateStep(TaskResult.Succeeded) },
                        Expected = TaskResult.SucceededWithIssues
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.SucceededWithIssues), CreateStep(TaskResult.Failed) },
                        Expected = TaskResult.Failed
                    },
                //  Abandoned
                //  Canceled
                //  Failed
                //  Skipped
                //  Succeeded
                //  SucceededWithIssues
                };
                foreach (var variableSet in variableSets)
                {
                    // Act.
                    TaskResult jobResult = await _stepsRunner.RunAsync(
                        context: _context.Object,
                        steps: variableSet.Steps.Select(x => x.Object).ToList());

                    // Assert.
                    Assert.True(
                        variableSet.Expected == jobResult,
                        String.Format(
                            CultureInfo.InvariantCulture,
                            "Expected '{0}'. Actual '{1}'. Steps: {2}",
                            variableSet.Expected,
                            jobResult,
                            FormatSteps(variableSet.Steps)));
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SkipsAfterCriticalFailure()
        {
            // Arrange.
            using (TestHostContext hc = CreateTestContext())
            {
                var variableSets = new[]
                {
                    new[] { CreateStep(TaskResult.Failed, critical: true), CreateStep(TaskResult.Succeeded) },
                    new[] { CreateStep(TaskResult.Failed, critical: true), CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                    new[] { CreateStep(TaskResult.Failed, critical: true), CreateStep(TaskResult.Succeeded, continueOnError: true) },
                    new[] { CreateStep(TaskResult.Failed, critical: true), CreateStep(TaskResult.Succeeded, critical: true) },
                    new[] { CreateStep(TaskResult.Failed, critical: true), CreateStep(TaskResult.Succeeded, alwaysRun: true, continueOnError: true, critical: true) },
                };
                foreach (var variableSet in variableSets)
                {
                    // Act.
                    TaskResult jobResult = await _stepsRunner.RunAsync(
                        context: _context.Object,
                        steps: variableSet.Select(x => x.Object).ToList());

                    // Assert.
                    Assert.Equal(TaskResult.Failed, jobResult);
                    Assert.Equal(2, variableSet.Length);
                    variableSet[0].Verify(x => x.RunAsync());
                    variableSet[1].Verify(x => x.RunAsync(), Times.Never());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SkipsAfterFailure()
        {
            // Arrange.
            using (TestHostContext hc = CreateTestContext())
            {
                var variableSets = new[]
                {
                    new[] { CreateStep(TaskResult.Failed), CreateStep(TaskResult.Succeeded) },
                    new[] { CreateStep(TaskResult.Failed), CreateStep(TaskResult.Succeeded, continueOnError: true) },
                    new[] { CreateStep(TaskResult.Failed), CreateStep(TaskResult.Succeeded, critical: true) },
                    new[] { CreateStep(TaskResult.Failed, critical: true), CreateStep(TaskResult.Succeeded) },
                    new[] { CreateStep(TaskResult.Failed, critical: true), CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                    new[] { CreateStep(TaskResult.Failed, critical: true), CreateStep(TaskResult.Succeeded, continueOnError: true) },
                    new[] { CreateStep(TaskResult.Failed, critical: true), CreateStep(TaskResult.Succeeded, critical: true) },
                };
                foreach (var variableSet in variableSets)
                {
                    // Act.
                    TaskResult jobResult = await _stepsRunner.RunAsync(
                        context: _context.Object,
                        steps: variableSet.Select(x => x.Object).ToList());

                    // Assert.
                    Assert.Equal(TaskResult.Failed, jobResult);
                    Assert.Equal(2, variableSet.Length);
                    variableSet[0].Verify(x => x.RunAsync());
                    variableSet[1].Verify(x => x.RunAsync(), Times.Never());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SkipsDisabledTasks()
        {
            // Arrange.
            using (TestHostContext hc = CreateTestContext())
            {
                Mock<IStep> disabledStep = CreateStep(TaskResult.Succeeded, enabled: false);
                Mock<IStep> enabledStep = CreateStep(TaskResult.Succeeded, enabled: true);

                // Act.
                TaskResult jobResult = await _stepsRunner.RunAsync(
                    context: _context.Object,
                    steps: new[] { disabledStep.Object, enabledStep.Object }.ToList());

                // Assert.
                Assert.Equal(TaskResult.Succeeded, jobResult);
                disabledStep.Verify(x => x.RunAsync(), Times.Never());
                enabledStep.Verify(x => x.RunAsync());
            }
        }

        private Mock<IStep> CreateStep(TaskResult result, Boolean alwaysRun = false, Boolean continueOnError = false, Boolean critical = false, Boolean enabled = true, Boolean isFinally = false)
        {
            var step = new Mock<IStep>();
            step.SetupAllProperties();
            step.Setup(x => x.AlwaysRun).Returns(alwaysRun);
            step.Setup(x => x.ContinueOnError).Returns(continueOnError);
            step.Setup(x => x.Critical).Returns(critical);
            step.Setup(x => x.Enabled).Returns(enabled);
            step.Setup(x => x.Finally).Returns(isFinally);
            step.Setup(x => x.RunAsync()).Returns(Task.FromResult(result));
            return step;
        }

        private String FormatSteps(IEnumerable<Mock<IStep>> steps)
        {
            return String.Join(
                " ; ",
                steps.Select(x => String.Format(
                    CultureInfo.InvariantCulture,
                    "Returns={0},AlwaysRun={1},ContinueOnError={2},Critical={3},Enabled={4},Finally={5}",
                    x.Object.RunAsync().Result,
                    x.Object.AlwaysRun,
                    x.Object.ContinueOnError,
                    x.Object.Critical,
                    x.Object.Enabled,
                    x.Object.Finally)));
        }
    }
}
