using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Xunit;
using GitHub.Runner.Common.Tests;

namespace GitHub.Runner.Common.Tests.Worker.Handlers
{
    public sealed class NodeScriptActionHandlerL0
    {
        private Mock<IExecutionContext> _ec;
        private Mock<IStepHost> _stepHost;

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);
            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            var globalContext = new GlobalContext
            {
                WriteDebug = true,
                PrependPath = new List<string>(),
                Endpoints = new List<ServiceEndpoint>()
            };
            globalContext.Endpoints.Add(new ServiceEndpoint()
            {
                Name = WellKnownServiceEndpointNames.SystemVssConnection,
                Url = new Uri("https://pipelines.actions.githubusercontent.com"),
                Authorization = new EndpointAuthorization()
                {
                    Scheme = "Test",
                    Parameters = {
                        {EndpointAuthorizationParameters.AccessToken, "token"}
                    }
                },
                Data = new Dictionary<string, string>()
            });
            _ec.Setup(x => x.Global).Returns(globalContext);
            _ec.Object.Global.Variables = new Variables(hc, new Dictionary<string, VariableValue>());
            _ec.Setup(x => x.StepTelemetry).Returns(new ActionsStepTelemetry());
            var forceCompletedSource = new System.Threading.Tasks.TaskCompletionSource<bool>();
            _ec.Setup(x => x.ForceCompleted).Returns(forceCompletedSource.Task);
            _ec.Setup(x => x.CancellationToken).Returns(System.Threading.CancellationToken.None);
            _ec.Setup(x => x.GetGitHubContext(It.IsAny<string>())).Returns<string>(key => null);
            _ec.Setup(x => x.ExpressionValues).Returns(new DictionaryContextData());

            var trace = hc.GetTrace();
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });

            _stepHost = new Mock<IStepHost>();
            _stepHost.Setup(x => x.ResolvePathForStepHost(It.IsAny<IExecutionContext>(), It.IsAny<string>()))
                .Returns<IExecutionContext, string>((ec, path) => path);

            hc.EnqueueInstance<IActionCommandManager>(new Mock<IActionCommandManager>().Object);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunAsync_BunRuntime_FeatureFlagDisabled_ThrowsNotSupportedException()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange
                var actionDirectory = Path.Combine(TestUtil.GetTestDataPath());
                var mainJsPath = Path.Combine(actionDirectory, "main.js");
                if (!File.Exists(mainJsPath))
                {
                    File.WriteAllText(mainJsPath, "// test file");
                }

                var handler = new NodeScriptActionHandler();
                handler.Initialize(hc);
                handler.ExecutionContext = _ec.Object;
                handler.StepHost = _stepHost.Object;
                handler.ActionDirectory = actionDirectory;
                handler.Data = new NodeJSActionExecutionData
                {
                    NodeVersion = "bun",
                    Script = "main.js"
                };
                handler.Inputs = new Dictionary<string, string>();
                handler.Environment = new Dictionary<string, string>();

                // Feature flag is not set (defaults to false)
                _ec.Object.Global.Variables = new Variables(hc, new Dictionary<string, VariableValue>());

                // Act & Assert
                var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await handler.RunAsync(ActionRunStage.Main));
                Assert.Contains(Constants.Runner.Features.AllowBunRuntime, exception.Message);
                Assert.Contains("not enabled", exception.Message);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunAsync_BunRuntime_FeatureFlagEnabled_CallsDetermineBunRuntimeVersion()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange
                var actionDirectory = Path.Combine(TestUtil.GetTestDataPath());
                var mainJsPath = Path.Combine(actionDirectory, "main.js");
                if (!File.Exists(mainJsPath))
                {
                    File.WriteAllText(mainJsPath, "// test file");
                }

                var handler = new NodeScriptActionHandler();
                handler.Initialize(hc);
                handler.ExecutionContext = _ec.Object;
                handler.StepHost = _stepHost.Object;
                handler.ActionDirectory = actionDirectory;
                handler.Data = new NodeJSActionExecutionData
                {
                    NodeVersion = "bun",
                    Script = "main.js"
                };
                handler.Inputs = new Dictionary<string, string>();
                handler.Environment = new Dictionary<string, string>();

                // Enable feature flag
                var variables = new Dictionary<string, VariableValue>
                {
                    [Constants.Runner.Features.AllowBunRuntime] = new VariableValue("true")
                };
                _ec.Object.Global.Variables = new Variables(hc, variables);

                _stepHost.Setup(x => x.DetermineBunRuntimeVersion(It.IsAny<IExecutionContext>())).ReturnsAsync("bun");
                _stepHost.Setup(x => x.ExecuteAsync(
                    It.IsAny<IExecutionContext>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<bool>(),
                    It.IsAny<System.Text.Encoding>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync(0);

                // Act
                await handler.RunAsync(ActionRunStage.Main);

                // Assert
                _stepHost.Verify(x => x.DetermineBunRuntimeVersion(_ec.Object), Times.Once);
                _stepHost.Verify(x => x.ExecuteAsync(
                    It.IsAny<IExecutionContext>(),
                    It.IsAny<string>(),
                    It.Is<string>(f => f.Contains("bun") && f.Contains("bin")),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<bool>(),
                    It.IsAny<System.Text.Encoding>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Threading.CancellationToken>()), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunAsync_BunRuntime_FeatureFlagEnabled_SetsCorrectFilePath()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange
                var actionDirectory = Path.Combine(TestUtil.GetTestDataPath());
                var mainJsPath = Path.Combine(actionDirectory, "main.js");
                if (!File.Exists(mainJsPath))
                {
                    File.WriteAllText(mainJsPath, "// test file");
                }

                var handler = new NodeScriptActionHandler();
                handler.Initialize(hc);
                handler.ExecutionContext = _ec.Object;
                handler.StepHost = _stepHost.Object;
                handler.ActionDirectory = actionDirectory;
                handler.Data = new NodeJSActionExecutionData
                {
                    NodeVersion = "bun",
                    Script = "main.js"
                };
                handler.Inputs = new Dictionary<string, string>();
                handler.Environment = new Dictionary<string, string>();

                // Enable feature flag
                var variables = new Dictionary<string, VariableValue>
                {
                    [Constants.Runner.Features.AllowBunRuntime] = new VariableValue("true")
                };
                _ec.Object.Global.Variables = new Variables(hc, variables);

                string capturedFilePath = null;
                _stepHost.Setup(x => x.DetermineBunRuntimeVersion(It.IsAny<IExecutionContext>())).ReturnsAsync("bun");
                _stepHost.Setup(x => x.ExecuteAsync(
                    It.IsAny<IExecutionContext>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>(),
                    It.IsAny<bool>(),
                    It.IsAny<System.Text.Encoding>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<System.Threading.CancellationToken>()))
                    .Callback<IExecutionContext, string, string, string, IDictionary<string, string>, bool, System.Text.Encoding, bool, bool, string, System.Threading.CancellationToken>(
                        (ec, workingDir, fileName, args, env, requireExitCodeZero, outputEncoding, killProcessOnCancel, inheritConsoleHandler, standardInInput, cancellationToken) =>
                        {
                            capturedFilePath = fileName;
                        })
                    .ReturnsAsync(0);

                // Act
                await handler.RunAsync(ActionRunStage.Main);

                // Assert
                Assert.NotNull(capturedFilePath);
                Assert.Contains("bun", capturedFilePath);
                Assert.Contains("bin", capturedFilePath);
                var externalsDir = hc.GetDirectory(WellKnownDirectory.Externals);
                Assert.StartsWith(externalsDir, capturedFilePath);
            }
        }
    }
}
