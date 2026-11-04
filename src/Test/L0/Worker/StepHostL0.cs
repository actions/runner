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

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData(0, null)]
        [InlineData(0, "ID=ubuntu")]
        [InlineData(0, "ID=unknown")]
        [InlineData(1, null)]
        public async Task DetermineNode24RuntimeVersionInContainerAsync(int exitCode, string distribution)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var stepHost = new ContainerStepHost();
                stepHost.Initialize(hc);
                stepHost.Container = new ContainerInfo() { ContainerId = "1234abcd" };
                _dc.Setup(d => d.DockerExec(_ec.Object, "1234abcd", string.Empty, It.IsAny<string>(), It.IsAny<List<string>>()))
                    .Callback((IExecutionContext ec, string id, string options, string command, List<string> output) =>
                    {
                        if (distribution != null)
                        {
                            output.Add(distribution);
                        }
                    })
                    .ReturnsAsync(exitCode);

                Assert.Equal("node24", await stepHost.DetermineNodeRuntimeVersion(_ec.Object, "node24"));
                _dc.Verify(d => d.DockerExec(_ec.Object, "1234abcd", string.Empty, It.IsAny<string>(), It.IsAny<List<string>>()), Times.Once);
            }
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task DetermineNode24RuntimeVersionInAlpineOrHookContainerAsync(bool useHooks, bool isAlpine)
        {
            var originalHooksPath = Environment.GetEnvironmentVariable(Constants.Hooks.ContainerHooksPath);
            try
            {
                Environment.SetEnvironmentVariable(Constants.Hooks.ContainerHooksPath, useHooks ? "/hooks/index.js" : null);
                using (TestHostContext hc = CreateTestContext())
                {
                    _ec.Object.Global.Variables = new Variables(hc, new Dictionary<string, VariableValue>
                    {
                        { Constants.Runner.Features.AllowRunnerContainerHooks, new VariableValue(useHooks.ToString()) }
                    });
                    var stepHost = new ContainerStepHost();
                    stepHost.Initialize(hc);
                    stepHost.Container = new ContainerInfo() { ContainerId = "1234abcd", IsAlpine = isAlpine };
                    _dc.Setup(d => d.DockerExec(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()))
                        .Callback((IExecutionContext ec, string id, string options, string command, List<string> output) => output.Add("ID=alpine"))
                        .ReturnsAsync(0);

                    bool alpine = !useHooks || isAlpine;
                    if (alpine && Constants.Runner.PlatformArchitecture != Constants.Architecture.X64)
                    {
                        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => stepHost.DetermineNodeRuntimeVersion(_ec.Object, "node24"));
                        Assert.Contains("only supported on x64 Linux runners", exception.Message);
                    }
                    else
                    {
                        Assert.Equal(alpine ? "node24_alpine" : "node24", await stepHost.DetermineNodeRuntimeVersion(_ec.Object, "node24"));
                    }
                    _dc.Verify(d => d.DockerExec(_ec.Object, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>()),
                        useHooks ? Times.Never() : Times.Once());
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(Constants.Hooks.ContainerHooksPath, originalHooksPath);
            }
        }
    }
}
