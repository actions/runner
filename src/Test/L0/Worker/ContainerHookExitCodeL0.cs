using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Container.ContainerHooks;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public class ContainerHookExitCodeL0
    {
        [Theory]
        [InlineData("{}")]
        [InlineData("{\"exitCode\":0}")]
        [InlineData("{\"exitCode\":17}")]
        [InlineData("{\"exitCode\":255}")]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ValidStepResponse(string json)
        {
            JsonConvert.DeserializeObject<StepHookResponse>(json).Validate(new HookInput());
        }

        [Theory]
        [InlineData("{\"exitCode\":-1}")]
        [InlineData("{\"exitCode\":256}")]
        [InlineData("{\"exitCode\":1.2}")]
        [InlineData("{\"exitCode\":\"17\"}")]
        [InlineData("{\"exitCode\":null}")]
        [InlineData("{\"exitCode\":true}")]
        [InlineData("{\"exitCode\":999999999999999999999999999999}")]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InvalidStepResponse(string json)
        {
            var response = JsonConvert.DeserializeObject<StepHookResponse>(json);
            Assert.Throws<Exception>(() => response.Validate(new HookInput()));
        }

        [Theory]
        [InlineData(HookCommand.RunScriptStep, true)]
        [InlineData(HookCommand.RunContainerStep, true)]
        [InlineData(HookCommand.PrepareJob, false)]
        [InlineData(HookCommand.CleanupJob, false)]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void AdvertiseCapabilityOnlyForSteps(HookCommand command, bool supported)
        {
            Assert.Equal(supported, new HookInput { Command = command }.SupportsStepExitCode);
        }

        // Container hooks are Linux-only. Mock hook execution rather than requiring
        // Python, Kubernetes, a registered runner, or any external process.
#if OS_LINUX
        [Theory]
        [InlineData("{}", 0, false, 0)]
        [InlineData("{\"exitCode\":0}", 0, false, 0)]
        [InlineData("{\"exitCode\":1}", 0, false, 1)]
        [InlineData("{\"exitCode\":17}", 0, false, 17)]
        [InlineData("{\"exitCode\":255}", 0, false, 255)]
        [InlineData("{\"exitCode\":17}", 0, true, 17)]
        [InlineData("{\"exitCode\":0}", 0, true, 0)]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WorkloadExitIsNotAHookFailure(string response, int hookExit, bool containerAction, int expectedExit)
        {
            using var test = new HookTest(response, hookExit);
            if (containerAction)
            {
                await test.Manager.RunContainerStepAsync(test.Context.Object, new ContainerInfo(test.Host), null);
                Assert.Equal(expectedExit == 0 ? (TaskResult?)null : TaskResult.Failed, test.Context.Object.Result);
            }
            else
            {
                // Exercise the actual outer ScriptHandler -> ContainerStepHost ->
                // hook manager -> inner ScriptHandler path. This catches enum
                // values being incorrectly returned as process exit codes.
                var host = new ContainerStepHost { Container = new ContainerInfo(test.Host) };
                host.Initialize(test.Host);
                var factory = test.Host.GetService<IHandlerFactory>();
                var handler = factory.Create(test.Context.Object, null, host,
                    new ScriptActionExecutionData(),
                    new Dictionary<string, string> { ["path"] = test.HookPath, ["shell"] = "sh" },
                    new Dictionary<string, string>(), test.Context.Object.Global.Variables,
                    Path.GetTempPath(), null) as ScriptHandler;
                handler.PrepareExecution(ActionRunStage.Main);
                await handler.RunAsync(ActionRunStage.Main);
                Assert.Equal(expectedExit == 0 ? (TaskResult?)null : TaskResult.Failed, test.Context.Object.Result);
            }

            if (expectedExit == 0)
            {
                Assert.Empty(test.Issues);
            }
            else
            {
                Assert.Equal(new[] { $"Process completed with exit code {expectedExit}." }, test.Issues);
            }
        }

        [Theory]
        [InlineData("{\"exitCode\":17}", 1)]
        [InlineData("{\"exitCode\":-1}", 0)]
        [InlineData("{\"exitCode\":null}", 0)]
        [InlineData("not json", 0)]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task BrokenHooksRemainInfrastructureFailures(string response, int hookExit)
        {
            using var test = new HookTest(response, hookExit);
            var failure = await Assert.ThrowsAsync<Exception>(() => test.Manager.RunScriptStepAsync(
                test.Context.Object, null, Path.GetTempPath(), "sh", "-c true", new Dictionary<string, string>(), ""));
            Assert.Contains("Please contact your self hosted runner administrator", failure.Message);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task LifecycleFailuresRemainInfrastructureFailures(bool prepare)
        {
            using var test = new HookTest("{}", 1);
            var failure = await Assert.ThrowsAsync<Exception>(() => prepare
                ? test.Manager.PrepareJobAsync(test.Context.Object, new List<ContainerInfo>())
                : test.Manager.CleanupJobAsync(test.Context.Object, new List<ContainerInfo>()));
            Assert.Contains("Please contact your self hosted runner administrator", failure.Message);
        }

        private sealed class HookTest : IDisposable
        {
            public TestHostContext Host { get; }
            public Mock<IExecutionContext> Context { get; } = new Mock<IExecutionContext>();
            public ContainerHookManager Manager { get; } = new ContainerHookManager();
            public List<string> Issues { get; } = new List<string>();
            public string HookPath { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".sh");
            private readonly string _previousHookPath;

            public HookTest(string response, int hookExit)
            {
                Host = new TestHostContext(this, "ContainerHookExitCode");
                _previousHookPath = Environment.GetEnvironmentVariable(Constants.Hooks.ContainerHooksPath);
                Environment.SetEnvironmentVariable(Constants.Hooks.ContainerHooksPath, HookPath);
                File.WriteAllText(HookPath, "# mocked hook\n");
                Context.SetupAllProperties();
                Context.SetupGet(x => x.Global).Returns(new GlobalContext
                {
                    Variables = new Variables(Host, new Dictionary<string, VariableValue>
                    {
                        [Constants.Runner.Features.AllowRunnerContainerHooks] = new VariableValue("true")
                    }),
                    PrependPath = new List<string>(),
                    JobDefaults = new Dictionary<string, IDictionary<string, string>>(),
                    EnvironmentVariables = new Dictionary<string, string>(),
                    Endpoints = new List<ServiceEndpoint>
                    {
                        new ServiceEndpoint { Name = "SystemVssConnection", Data = new Dictionary<string, string>() }
                    }
                });
                Context.SetupGet(x => x.StepTelemetry).Returns(new ActionsStepTelemetry());
                Context.SetupGet(x => x.ExpressionValues).Returns(new DictionaryContextData
                {
                    ["github"] = new GitHubContext { ["workspace"] = new StringContextData(Path.GetTempPath()) }
                });
                Context.Setup(x => x.GetMatchers()).Returns(Array.Empty<IssueMatcherConfig>());
                Context.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<ExecutionContextLogOptions>()))
                    .Callback<Issue, ExecutionContextLogOptions>((issue, options) => Issues.Add(issue.Message));
                Host.SetSingleton<IHandlerFactory>(new HandlerFactory());
                Host.EnqueueInstance<IScriptHandler>(new ScriptHandler());
                Host.EnqueueInstance<IScriptHandler>(new ScriptHandler());
                Host.EnqueueInstance<IActionCommandManager>(new Mock<IActionCommandManager>().Object);
                Host.EnqueueInstance<IActionCommandManager>(new Mock<IActionCommandManager>().Object);
                var stepHost = new Mock<IDefaultStepHost>();
                stepHost.Setup(x => x.ResolvePathForStepHost(It.IsAny<IExecutionContext>(), It.IsAny<string>()))
                    .Returns<IExecutionContext, string>((context, path) => path);
                stepHost.Setup(x => x.ExecuteAsync(It.IsAny<IExecutionContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<bool>(), It.IsAny<Encoding>(),
                    It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns<IExecutionContext, string, string, string, IDictionary<string, string>, bool, Encoding, bool, bool, string, CancellationToken>(
                        (context, directory, file, arguments, environment, requireZero, encoding, kill, inherit, input, token) =>
                        {
                            var data = JObject.Parse(input);
                            Assert.Equal(data["command"].Value<string>() == "run_script_step" || data["command"].Value<string>() == "run_container_step",
                                data["supportsStepExitCode"].Value<bool>());
                            File.WriteAllText(data["responseFile"].Value<string>(), response);
                            return Task.FromResult(hookExit);
                        });
                Host.EnqueueInstance<IDefaultStepHost>(stepHost.Object);
                Manager.Initialize(Host);
                Host.SetSingleton<IContainerHookManager>(Manager);
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable(Constants.Hooks.ContainerHooksPath, _previousHookPath);
                File.Delete(HookPath);
                Host.Dispose();
            }
        }
#endif
    }
}
