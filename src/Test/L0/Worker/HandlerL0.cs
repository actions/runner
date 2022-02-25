using System;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class HandlerL0
    {
        private Mock<IExecutionContext> _ec;
        private ActionsStepTelemetry _stepTelemetry;
        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _stepTelemetry = new ActionsStepTelemetry();
            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            _ec.Setup(x => x.StepTelemetry).Returns(_stepTelemetry);

            var trace = hc.GetTrace();
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });

            hc.EnqueueInstance<IActionCommandManager>(new Mock<IActionCommandManager>().Object);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void PrepareExecution_PopulateTelemetry_RepoActions()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var nodeHandler = new NodeScriptActionHandler();
                nodeHandler.Initialize(hc);

                nodeHandler.ExecutionContext = _ec.Object;
                nodeHandler.Action = new RepositoryPathReference()
                {
                    Name = "actions/checkout",
                    Ref = "v2"
                };

                // Act.
                nodeHandler.PrepareExecution(ActionRunStage.Main);
                hc.GetTrace().Info($"Telemetry: {StringUtil.ConvertToJson(_stepTelemetry)}");

                // Assert.
                Assert.Equal("repository", _stepTelemetry.Type);
                Assert.Equal("actions/checkout", _stepTelemetry.Action);
                Assert.Equal("v2", _stepTelemetry.Ref);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void PrepareExecution_PopulateTelemetry_DockerActions()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var nodeHandler = new NodeScriptActionHandler();
                nodeHandler.Initialize(hc);

                nodeHandler.ExecutionContext = _ec.Object;
                nodeHandler.Action = new ContainerRegistryReference()
                {
                    Image = "ubuntu:20.04"
                };

                // Act.
                nodeHandler.PrepareExecution(ActionRunStage.Main);
                hc.GetTrace().Info($"Telemetry: {StringUtil.ConvertToJson(_stepTelemetry)}");

                // Assert.
                Assert.Equal("docker", _stepTelemetry.Type);
                Assert.Equal("ubuntu:20.04", _stepTelemetry.Action);
            }
        }
    }
}
