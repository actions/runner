using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Moq;
using Xunit;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using GitHub.Runner.Worker.Container;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class StepHostL0
    {
        private Mock<IExecutionContext> _ec;
        private Mock<IDockerCommandManager> _dc;
        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);

            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            _ec.Setup(x => x.Global).Returns(new GlobalContext { WriteDebug = true });
            _ec.Object.Global.Variables = new Variables(hc, new Dictionary<string, VariableValue>());
            var trace = hc.GetTrace();
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });

            _dc = new Mock<IDockerCommandManager>();
            hc.SetSingleton(_dc.Object);
            return hc;
        }

#if OS_LINUX
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DetermineNodeRuntimeVersionInContainerAsync()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var sh = new ContainerStepHost();
                sh.Initialize(hc);
                sh.Container = new ContainerInfo() { ContainerId = "1234abcd" };

                _dc.Setup(d => d.DockerExec(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                               .ReturnsAsync(0);

                // Act.
                var nodeVersion = await sh.DetermineNodeRuntimeVersion(_ec.Object, "node12");

                // Assert.
                Assert.Equal("node12", nodeVersion);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DetermineNodeRuntimeVersionInAlpineContainerAsync()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var sh = new ContainerStepHost();
                sh.Initialize(hc);
                sh.Container = new ContainerInfo() { ContainerId = "1234abcd" };

                _dc.Setup(d => d.DockerExec(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                    .Callback((IExecutionContext ec, string id, string options, string command, List<string> output) =>
                    {
                        output.Add("alpine");
                    })
                    .ReturnsAsync(0);

                // Act.
                var nodeVersion = await sh.DetermineNodeRuntimeVersion(_ec.Object, "node16");

                // Assert.
                Assert.Equal("node16_alpine", nodeVersion);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DetermineNode20RuntimeVersionInAlpineContainerAsync()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var sh = new ContainerStepHost();
                sh.Initialize(hc);
                sh.Container = new ContainerInfo() { ContainerId = "1234abcd" };

                _dc.Setup(d => d.DockerExec(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                    .Callback((IExecutionContext ec, string id, string options, string command, List<string> output) =>
                    {
                        output.Add("alpine");
                    })
                    .ReturnsAsync(0);

                // Act.
                var nodeVersion = await sh.DetermineNodeRuntimeVersion(_ec.Object, "node20");

                // Assert.
                Assert.Equal("node20_alpine", nodeVersion);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DetermineNodeRuntimeVersionInUnknowContainerAsync()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var sh = new ContainerStepHost();
                sh.Initialize(hc);
                sh.Container = new ContainerInfo() { ContainerId = "1234abcd" };

                _dc.Setup(d => d.DockerExec(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                    .Callback((IExecutionContext ec, string id, string options, string command, List<string> output) =>
                    {
                        output.Add("github");
                    })
                    .ReturnsAsync(0);

                // Act.
                var nodeVersion = await sh.DetermineNodeRuntimeVersion(_ec.Object, "node16");

                // Assert.
                Assert.Equal("node16", nodeVersion);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DetermineNode20RuntimeVersionInUnknowContainerAsync()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var sh = new ContainerStepHost();
                sh.Initialize(hc);
                sh.Container = new ContainerInfo() { ContainerId = "1234abcd" };

                _dc.Setup(d => d.DockerExec(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                    .Callback((IExecutionContext ec, string id, string options, string command, List<string> output) =>
                    {
                        output.Add("github");
                    })
                    .ReturnsAsync(0);

                // Act.
                var nodeVersion = await sh.DetermineNodeRuntimeVersion(_ec.Object, "node20");

                // Assert.
                Assert.Equal("node20", nodeVersion);
            }
        }
#endif
    }
}
