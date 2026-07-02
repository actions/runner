using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OTelPropagation_NeverClobbersWorkflowSetEnv()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange: the workflow's step env: block deliberately set its own values.
                var handler = new TestOTelEnvHandler();
                handler.Initialize(hc);
                handler.ExecutionContext = _ec.Object;
                handler.Environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["TRACEPARENT"] = "00-11111111111111111111111111111111-1111111111111111-01",
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://app-collector:4318",
                };
                _ec.Setup(x => x.GetOTelStepEnv()).Returns(new Dictionary<string, string>
                {
                    ["TRACEPARENT"] = "00-22222222222222222222222222222222-2222222222222222-01",
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://runner-collector:4318",
                    ["OTEL_RESOURCE_ATTRIBUTES"] = "service.name=github-actions-job",
                });

                // Act.
                handler.ApplyOTelPropagation();

                // Assert: workflow-set values win; only missing keys are injected.
                Assert.Equal("00-11111111111111111111111111111111-1111111111111111-01", handler.Environment["TRACEPARENT"]);
                Assert.Equal("http://app-collector:4318", handler.Environment["OTEL_EXPORTER_OTLP_ENDPOINT"]);
                Assert.Equal("service.name=github-actions-job", handler.Environment["OTEL_RESOURCE_ATTRIBUTES"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void OTelPropagation_InjectsWhenAbsent_NullSafe()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var handler = new TestOTelEnvHandler();
                handler.Initialize(hc);
                handler.ExecutionContext = _ec.Object;
                handler.Environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _ec.Setup(x => x.GetOTelStepEnv()).Returns(new Dictionary<string, string>
                {
                    ["TRACEPARENT"] = "00-22222222222222222222222222222222-2222222222222222-01",
                });

                // Act.
                handler.ApplyOTelPropagation();

                // Assert.
                Assert.Equal("00-22222222222222222222222222222222-2222222222222222-01", handler.Environment["TRACEPARENT"]);

                // A null step env (e.g. mocked-out execution context) must be a no-op, not a throw.
                _ec.Setup(x => x.GetOTelStepEnv()).Returns((IDictionary<string, string>)null);
                handler.ApplyOTelPropagation();
                Assert.Single(handler.Environment);
            }
        }

        // Minimal concrete Handler exposing the shared OTel env injection for test.
        private sealed class TestOTelEnvHandler : Handler, IHandler
        {
            public Task RunAsync(ActionRunStage stage) => Task.CompletedTask;
            public void ApplyOTelPropagation() => AddOTelPropagationToEnvironment();
        }
    }
}
