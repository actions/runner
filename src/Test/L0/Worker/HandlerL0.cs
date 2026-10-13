using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Actions.RunService.WebApi;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Container.ContainerHooks;
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

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData("read")]
        [InlineData("none")]
        [InlineData("write")]
        [InlineData("write-only")]
        public async Task RunAsync_ExportsCacheModeEnv_WhenVariableSet(string mode)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var environment = await RunNodeScriptActionHandlerAsync(hc, new Dictionary<string, VariableValue>
                {
                    { "actions_cache_mode", mode }
                });

                Assert.True(environment.TryGetValue("ACTIONS_CACHE_MODE", out var value));
                Assert.Equal(mode, value);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunAsync_DoesNotExportCacheModeEnv_WhenVariableAbsent()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var environment = await RunNodeScriptActionHandlerAsync(hc, new Dictionary<string, VariableValue>());

                Assert.False(environment.ContainsKey("ACTIONS_CACHE_MODE"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunAsync_DoesNotExportCacheModeEnv_WhenVariableEmpty()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var environment = await RunNodeScriptActionHandlerAsync(hc, new Dictionary<string, VariableValue>
                {
                    { "actions_cache_mode", "" }
                });

                Assert.False(environment.ContainsKey("ACTIONS_CACHE_MODE"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunAsync_CacheModeCoexistsWithCacheServiceV2()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var environment = await RunNodeScriptActionHandlerAsync(hc, new Dictionary<string, VariableValue>
                {
                    { "actions_uses_cache_service_v2", "true" },
                    { "actions_cache_mode", "read" }
                });

                Assert.Equal(bool.TrueString, environment["ACTIONS_CACHE_SERVICE_V2"]);
                Assert.Equal("read", environment["ACTIONS_CACHE_MODE"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunAsync_DoesNotAffectRuntimeEnv_WhenCacheModeAbsent()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var environment = await RunNodeScriptActionHandlerAsync(hc, new Dictionary<string, VariableValue>());

                // Baseline runtime env is still exported and cache-mode adds nothing.
                Assert.Equal("https://pipelines.actions.githubusercontent.com/", environment["ACTIONS_RUNTIME_URL"]);
                Assert.Equal("token", environment["ACTIONS_RUNTIME_TOKEN"]);
                Assert.False(environment.ContainsKey("ACTIONS_CACHE_MODE"));
                Assert.False(environment.ContainsKey("ACTIONS_CACHE_SERVICE_V2"));
            }
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData("read")]
        [InlineData("none")]
        public async Task ContainerRunAsync_ExportsCacheModeEnv_WhenVariableSet(string mode)
        {
            // Container actions only run on Linux; RunAsync throws on other platforms.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            using (TestHostContext hc = CreateTestContext())
            {
                var container = await RunContainerActionHandlerAsync(hc, new Dictionary<string, VariableValue>
                {
                    { "actions_cache_mode", mode }
                });

                Assert.True(container.ContainerEnvironmentVariables.TryGetValue("ACTIONS_CACHE_MODE", out var value));
                Assert.Equal(mode, value);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ContainerRunAsync_DoesNotExportCacheModeEnv_WhenVariableAbsent()
        {
            // Container actions only run on Linux; RunAsync throws on other platforms.
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return;
            }

            using (TestHostContext hc = CreateTestContext())
            {
                var container = await RunContainerActionHandlerAsync(hc, new Dictionary<string, VariableValue>());

                Assert.False(container.ContainerEnvironmentVariables.ContainsKey("ACTIONS_CACHE_MODE"));
            }
        }

        private async Task<ContainerInfo> RunContainerActionHandlerAsync(TestHostContext hc, IDictionary<string, VariableValue> variables)
        {
            // Route through the container-hooks path so the handler skips docker build/run.
            variables[Constants.Runner.Features.AllowRunnerContainerHooks] = "true";
            Environment.SetEnvironmentVariable(Constants.Hooks.ContainerHooksPath, Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "hooks.js"));

            var tempDirectory = hc.GetDirectory(WellKnownDirectory.Temp);
            Directory.CreateDirectory(Path.Combine(tempDirectory, "_runner_file_commands"));
            Directory.CreateDirectory(Path.Combine(tempDirectory, "_github_workflow"));
            var workspace = Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), "workspace");
            Directory.CreateDirectory(workspace);

            var serverVariables = new Variables(hc, variables);
            var endpoints = new List<ServiceEndpoint>
            {
                new ServiceEndpoint()
                {
                    Name = WellKnownServiceEndpointNames.SystemVssConnection,
                    Url = new Uri("https://pipelines.actions.githubusercontent.com"),
                    Authorization = new EndpointAuthorization()
                    {
                        Scheme = "Test",
                        Parameters = { { "AccessToken", "token" } }
                    }
                }
            };

            _ec.Setup(x => x.Global).Returns(new GlobalContext()
            {
                Variables = serverVariables,
                Endpoints = endpoints,
                PrependPath = new List<string>(),
                EnvironmentVariables = new Dictionary<string, string>()
            });
            _ec.Setup(x => x.ExpressionValues).Returns(new DictionaryContextData());
            _ec.Setup(x => x.JobContext).Returns(new JobContext());
            _ec.Setup(x => x.GetGitHubContext("workspace")).Returns(workspace);

            ContainerInfo captured = null;
            var hookManager = new Mock<IContainerHookManager>();
            hookManager.Setup(x => x.RunContainerStepAsync(It.IsAny<IExecutionContext>(), It.IsAny<ContainerInfo>(), It.IsAny<string>()))
                       .Callback((IExecutionContext ec, ContainerInfo container, string dockerFile) => { captured = container; })
                       .Returns(Task.CompletedTask);
            hc.SetSingleton(hookManager.Object);
            hc.SetSingleton(new Mock<IActionManifestManagerWrapper>().Object);

            var handler = new ContainerActionHandler();
            handler.Initialize(hc);
            handler.ExecutionContext = _ec.Object;
            handler.Environment = new Dictionary<string, string>();
            handler.Inputs = new Dictionary<string, string>();
            handler.Action = new ContainerRegistryReference() { Image = "alpine:latest" };
            handler.Data = new ContainerActionExecutionData() { Image = "docker://alpine:latest" };

            await handler.RunAsync(ActionRunStage.Main);

            return captured;
        }

        private async Task<Dictionary<string, string>> RunNodeScriptActionHandlerAsync(TestHostContext hc, IDictionary<string, VariableValue> variables)
        {
            var actionDirectory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Work), Guid.NewGuid().ToString());
            Directory.CreateDirectory(actionDirectory);
            var scriptFile = "main.js";
            File.WriteAllText(Path.Combine(actionDirectory, scriptFile), "// noop");

            var serverVariables = new Variables(hc, variables);
            var endpoints = new List<ServiceEndpoint>
            {
                new ServiceEndpoint()
                {
                    Name = WellKnownServiceEndpointNames.SystemVssConnection,
                    Url = new Uri("https://pipelines.actions.githubusercontent.com"),
                    Authorization = new EndpointAuthorization()
                    {
                        Scheme = "Test",
                        Parameters = { { "AccessToken", "token" } }
                    }
                }
            };

            _ec.Setup(x => x.Global).Returns(new GlobalContext()
            {
                Variables = serverVariables,
                Endpoints = endpoints,
                PrependPath = new List<string>(),
                EnvironmentVariables = new Dictionary<string, string>()
            });
            _ec.Setup(x => x.ExpressionValues).Returns(new DictionaryContextData());
            _ec.Setup(x => x.GetGitHubContext("workspace")).Returns(actionDirectory);
            _ec.Setup(x => x.GetMatchers()).Returns(new List<IssueMatcherConfig>());
            _ec.Setup(x => x.ForceCompleted).Returns(new TaskCompletionSource<int>().Task);
            _ec.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

            var stepHost = new Mock<IStepHost>();
            stepHost.Setup(x => x.DetermineNodeRuntimeVersion(It.IsAny<IExecutionContext>(), It.IsAny<string>())).ReturnsAsync("node20");
            stepHost.Setup(x => x.ResolvePathForStepHost(It.IsAny<IExecutionContext>(), It.IsAny<string>())).Returns((IExecutionContext ec, string path) => path);
            stepHost.Setup(x => x.ExecuteAsync(
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
                It.IsAny<CancellationToken>())).ReturnsAsync(0);

            var handler = new NodeScriptActionHandler();
            handler.Initialize(hc);
            handler.ExecutionContext = _ec.Object;
            handler.StepHost = stepHost.Object;
            handler.Environment = new Dictionary<string, string>();
            handler.Inputs = new Dictionary<string, string>();
            handler.RuntimeVariables = serverVariables;
            handler.ActionDirectory = actionDirectory;
            handler.Action = new RepositoryPathReference() { Name = "actions/checkout", Ref = "v2" };
            handler.Data = new NodeJSActionExecutionData() { Script = scriptFile, NodeVersion = "node20" };

            await handler.RunAsync(ActionRunStage.Main);

            return handler.Environment;
        }
    }
}
