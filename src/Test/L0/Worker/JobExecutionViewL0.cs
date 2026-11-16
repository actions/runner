using System;
using GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Dap;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class JobExecutionViewL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RendersPreMainAndPostSections()
        {
            var pre = CreateStep("Pre cache", ActionRunStage.Pre);
            var checkout = CreateStep("Checkout");
            var post = CreateStep("Post cache", ActionRunStage.Post);

            var view = new JobExecutionView(
                "job",
                new[] { pre.Object, checkout.Object },
                new[] { post.Object });

            Assert.Equal(
                "pre:\n  - step: \"Set up job\"\n  - step: \"Pre cache\"\n\nmain:\n  - step: \"Checkout\"\n\npost:\n  - step: \"Post cache\"\n  - step: \"Complete job\"\n", 
                view.Content);
            Assert.Equal(3, view.TryGetLineForStep(pre.Object));
            Assert.Equal(6, view.TryGetLineForStep(checkout.Object));
            Assert.Equal(9, view.TryGetLineForStep(post.Object));
            Assert.Equal(10, view.CompleteJobLine);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ClaimsPredictedPostStepWithoutChangingLine()
        {
            var action = CreateRepositoryActionStep("actions/cache");
            var checkout = CreateActionRunner("Checkout", ActionRunStage.Main, action);
            var predicted = new JobExecutionView.PredictedPostStep(
                "Post Checkout",
                MatchKeyFor(action.Id));

            var view = new JobExecutionView(
                "job",
                new[] { checkout.Object },
                Array.Empty<IStep>(),
                new[] { predicted });

            var post = CreateActionRunner("Post Checkout", ActionRunStage.Post, action);
            var line = view.TryClaimPredictedStep(MatchKeyFor(action.Id), post.Object);

            Assert.Equal(8, line);
            Assert.Equal(8, view.TryGetLineForStep(post.Object));
            Assert.Equal(
                "pre:\n  - step: \"Set up job\"\n\nmain:\n  - step: \"Checkout\"\n\npost:\n  - step: \"Post Checkout\"\n  - step: \"Complete job\"\n", 
                view.Content);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void UsesSyntheticCompleteJobLineWhenPostStepSharesName()
        {
            var checkout = CreateStep("Checkout");
            var realPost = CreateStep("Complete job", ActionRunStage.Post);

            var view = new JobExecutionView(
                "job",
                new[] { checkout.Object },
                new[] { realPost.Object });

            Assert.Equal(8, view.TryGetLineForStep(realPost.Object));
            Assert.Equal(9, view.CompleteJobLine);
        }

        private static Mock<IStep> CreateStep(string displayName, ActionRunStage? stage = null)
        {
            var step = new Mock<IStep>();
            step.Setup(s => s.DisplayName).Returns(displayName);
            if (stage.HasValue)
            {
                var executionContext = new Mock<IExecutionContext>();
                executionContext.Setup(x => x.Stage).Returns(stage.Value);
                step.Setup(s => s.ExecutionContext).Returns(executionContext.Object);
            }
            else
            {
                step.Setup(s => s.ExecutionContext).Returns((IExecutionContext)null);
            }

            return step;
        }

        private static Mock<IActionRunner> CreateActionRunner(string displayName, ActionRunStage stage, ActionStep action)
        {
            var executionContext = new Mock<IExecutionContext>();
            executionContext.Setup(x => x.Stage).Returns(stage);

            var runner = new Mock<IActionRunner>();
            runner.Setup(s => s.DisplayName).Returns(displayName);
            runner.Setup(s => s.ExecutionContext).Returns(executionContext.Object);
            runner.Setup(s => s.Stage).Returns(stage);
            runner.Setup(s => s.Action).Returns(action);
            return runner;
        }

        private static ActionStep CreateRepositoryActionStep(string name)
        {
            return new ActionStep
            {
                Id = Guid.NewGuid(),
                Name = name,
                Reference = new RepositoryPathReference
                {
                    Name = name,
                    Ref = "v1",
                    RepositoryType = RepositoryTypes.GitHub
                }
            };
        }

        private static string MatchKeyFor(Guid actionId)
        {
            return $"post:{actionId:N}";
        }
    }
}
