using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class StepRunnerL0
    {
        public StepRunnerL0()
        {
            this.context = new Mock<IExecutionContext>();
            this.stepRunner = new StepRunner();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunsAfterContinueOnError()
        {
            // Arrange.
            var variableSets = new[]
            {
                new[] { this.CreateStep(TaskResult.Failed, continueOnError: true), this.CreateStep(TaskResult.Succeeded) },
                new[] { this.CreateStep(TaskResult.Failed, continueOnError: true), this.CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                new[] { this.CreateStep(TaskResult.Failed, continueOnError: true), this.CreateStep(TaskResult.Succeeded, continueOnError: true) },
                new[] { this.CreateStep(TaskResult.Failed, continueOnError: true), this.CreateStep(TaskResult.Succeeded, critical: true) },
                new[] { this.CreateStep(TaskResult.Failed, continueOnError: true), this.CreateStep(TaskResult.Succeeded, isFinally: true) },
                new[] { this.CreateStep(TaskResult.Failed, continueOnError: true, critical: true), this.CreateStep(TaskResult.Succeeded) },
                new[] { this.CreateStep(TaskResult.Failed, continueOnError: true, critical: true), this.CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                new[] { this.CreateStep(TaskResult.Failed, continueOnError: true, critical: true), this.CreateStep(TaskResult.Succeeded, continueOnError: true) },
                new[] { this.CreateStep(TaskResult.Failed, continueOnError: true, critical: true), this.CreateStep(TaskResult.Succeeded, critical: true) },
                new[] { this.CreateStep(TaskResult.Failed, continueOnError: true, critical: true), this.CreateStep(TaskResult.Succeeded, isFinally: true) },
            };
            foreach (var variableSet in variableSets)
            {
                // Act.
                TaskResult jobResult = await this.stepRunner.RunAsync(
                    context: this.context.Object,
                    steps: variableSet.Select(x => x.Object).ToList());

                // Assert.
                Assert.Equal(TaskResult.SucceededWithIssues, jobResult);
                Assert.Equal(2, variableSet.Length);
                variableSet[0].Verify(x => x.RunAsync(this.context.Object));
                variableSet[1].Verify(x => x.RunAsync(this.context.Object));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunsAlwaysRuns()
        {
            // Arrange.
            var variableSets = new[]
            {
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Succeeded), this.CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                    Expected = TaskResult.Succeeded,
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Failed), this.CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                    Expected = TaskResult.Failed,
                },
            };
            foreach (var variableSet in variableSets)
            {
                // Act.
                TaskResult jobResult = await this.stepRunner.RunAsync(
                    context: this.context.Object,
                    steps: variableSet.Steps.Select(x => x.Object).ToList());

                // Assert.
                Assert.Equal(variableSet.Expected, jobResult);
                Assert.Equal(2, variableSet.Steps.Length);
                variableSet.Steps[0].Verify(x => x.RunAsync(this.context.Object));
                variableSet.Steps[1].Verify(x => x.RunAsync(this.context.Object));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunsFinally()
        {
            // Arrange.
            var variableSets = new[]
            {
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Succeeded), this.CreateStep(TaskResult.Succeeded, isFinally: true) },
                    Expected = TaskResult.Succeeded,
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Failed), this.CreateStep(TaskResult.Succeeded, isFinally: true) },
                    Expected = TaskResult.Failed,
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Failed, critical: true), this.CreateStep(TaskResult.Succeeded, isFinally: true) },
                    Expected = TaskResult.Failed,
                },
            };
            foreach (var variableSet in variableSets)
            {
                // Act.
                TaskResult jobResult = await this.stepRunner.RunAsync(
                    context: this.context.Object,
                    steps: variableSet.Steps.Select(x => x.Object).ToList());

                // Assert.
                Assert.Equal(variableSet.Expected, jobResult);
                Assert.Equal(2, variableSet.Steps.Length);
                variableSet.Steps[0].Verify(x => x.RunAsync(this.context.Object));
                variableSet.Steps[1].Verify(x => x.RunAsync(this.context.Object));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SetsJobResultCorrectly()
        {
            // Arrange.
            var variableSets = new[]
            {
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Abandoned) },
                    Expected = TaskResult.Succeeded
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Canceled) },
                    Expected = TaskResult.Succeeded
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Failed), this.CreateStep(TaskResult.Succeeded) },
                    Expected = TaskResult.Failed
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Failed), this.CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                    Expected = TaskResult.Failed
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Failed), this.CreateStep(TaskResult.Succeeded, isFinally: true) },
                    Expected = TaskResult.Failed
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Failed, continueOnError: true), this.CreateStep(TaskResult.Failed) },
                    Expected = TaskResult.Failed
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Failed, continueOnError: true), this.CreateStep(TaskResult.Succeeded) },
                    Expected = TaskResult.SucceededWithIssues
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Failed, continueOnError: true, critical: true), this.CreateStep(TaskResult.Succeeded) },
                    Expected = TaskResult.SucceededWithIssues
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Skipped) },
                    Expected = TaskResult.Succeeded
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Succeeded) },
                    Expected = TaskResult.Succeeded
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Succeeded), this.CreateStep(TaskResult.Failed) },
                    Expected = TaskResult.Failed
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.Succeeded), this.CreateStep(TaskResult.SucceededWithIssues) },
                    Expected = TaskResult.SucceededWithIssues
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.SucceededWithIssues), this.CreateStep(TaskResult.Succeeded) },
                    Expected = TaskResult.SucceededWithIssues
                },
                new
                {
                    Steps = new[] { this.CreateStep(TaskResult.SucceededWithIssues), this.CreateStep(TaskResult.Failed) },
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
                TaskResult jobResult = await this.stepRunner.RunAsync(
                    context: this.context.Object,
                    steps: variableSet.Steps.Select(x => x.Object).ToList());

                // Assert.
                Assert.True(
                    variableSet.Expected == jobResult,
                    String.Format(
                        CultureInfo.InvariantCulture,
                        "Expected '{0}'. Actual '{1}'. Steps: {2}",
                        variableSet.Expected,
                        jobResult,
                        this.FormatSteps(variableSet.Steps)));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SkipsAfterCriticalFailure()
        {
            // Arrange.
            var variableSets = new[]
            {
                new[] { this.CreateStep(TaskResult.Failed, critical: true), this.CreateStep(TaskResult.Succeeded) },
                new[] { this.CreateStep(TaskResult.Failed, critical: true), this.CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                new[] { this.CreateStep(TaskResult.Failed, critical: true), this.CreateStep(TaskResult.Succeeded, continueOnError: true) },
                new[] { this.CreateStep(TaskResult.Failed, critical: true), this.CreateStep(TaskResult.Succeeded, critical: true) },
                new[] { this.CreateStep(TaskResult.Failed, critical: true), this.CreateStep(TaskResult.Succeeded, alwaysRun: true, continueOnError: true, critical: true) },
            };
            foreach (var variableSet in variableSets)
            {
                // Act.
                TaskResult jobResult = await this.stepRunner.RunAsync(
                    context: this.context.Object,
                    steps: variableSet.Select(x => x.Object).ToList());

                // Assert.
                Assert.Equal(TaskResult.Failed, jobResult);
                Assert.Equal(2, variableSet.Length);
                variableSet[0].Verify(x => x.RunAsync(this.context.Object));
                variableSet[1].Verify(x => x.RunAsync(It.IsAny<IExecutionContext>()), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SkipsAfterFailure()
        {
            // Arrange.
            var variableSets = new[]
            {
                new[] { this.CreateStep(TaskResult.Failed), this.CreateStep(TaskResult.Succeeded) },
                new[] { this.CreateStep(TaskResult.Failed), this.CreateStep(TaskResult.Succeeded, continueOnError: true) },
                new[] { this.CreateStep(TaskResult.Failed), this.CreateStep(TaskResult.Succeeded, critical: true) },
                new[] { this.CreateStep(TaskResult.Failed, critical: true), this.CreateStep(TaskResult.Succeeded) },
                new[] { this.CreateStep(TaskResult.Failed, critical: true), this.CreateStep(TaskResult.Succeeded, alwaysRun: true) },
                new[] { this.CreateStep(TaskResult.Failed, critical: true), this.CreateStep(TaskResult.Succeeded, continueOnError: true) },
                new[] { this.CreateStep(TaskResult.Failed, critical: true), this.CreateStep(TaskResult.Succeeded, critical: true) },
            };
            foreach (var variableSet in variableSets)
            {
                // Act.
                TaskResult jobResult = await this.stepRunner.RunAsync(
                    context: this.context.Object,
                    steps: variableSet.Select(x => x.Object).ToList());

                // Assert.
                Assert.Equal(TaskResult.Failed, jobResult);
                Assert.Equal(2, variableSet.Length);
                variableSet[0].Verify(x => x.RunAsync(this.context.Object));
                variableSet[1].Verify(x => x.RunAsync(It.IsAny<IExecutionContext>()), Times.Never());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SkipsDisabledTasks()
        {
            // Arrange.
            Mock<IStep> disabledStep = this.CreateStep(TaskResult.Succeeded, enabled: false);
            Mock<IStep> enabledStep = this.CreateStep(TaskResult.Succeeded, enabled: true);

            // Act.
            TaskResult jobResult = await this.stepRunner.RunAsync(
                context: this.context.Object,
                steps: new[] { disabledStep.Object, enabledStep.Object }.ToList());

            // Assert.
            Assert.Equal(TaskResult.Succeeded, jobResult);
            disabledStep.Verify(x => x.RunAsync(It.IsAny<IExecutionContext>()), Times.Never());
            enabledStep.Verify(x => x.RunAsync(this.context.Object));
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
            step.Setup(x => x.RunAsync(this.context.Object)).Returns(Task.FromResult(result));
            return step;
        }

        private String FormatSteps(IEnumerable<Mock<IStep>> steps)
        {
            return String.Join(
                " ; ",
                steps.Select(x => String.Format(
                    CultureInfo.InvariantCulture,
                    "Returns={0},AlwaysRun={1},ContinueOnError={2},Critical={3},Enabled={4},Finally={5}",
                    x.Object.RunAsync(this.context.Object).Result,
                    x.Object.AlwaysRun,
                    x.Object.ContinueOnError,
                    x.Object.Critical,
                    x.Object.Enabled,
                    x.Object.Finally)));
        }

        private readonly Mock<IExecutionContext> context;
        private readonly StepRunner stepRunner;
    }
}
