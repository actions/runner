using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Tests;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Dap;
using GitHub.Runner.Worker.Handlers;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class DapReplExecutorL0
    {
        private TestHostContext _hc;
        private DapReplExecutor _executor;
        private List<Event> _sentEvents;

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            _hc = new TestHostContext(this, testName);
            _sentEvents = new List<Event>();
            _executor = new DapReplExecutor(_hc, (category, text) =>
            {
                _sentEvents.Add(new Event
                {
                    EventType = "output",
                    Body = new OutputEventBody
                    {
                        Category = category,
                        Output = text
                    }
                });
            });
            return _hc;
        }

        private Mock<IExecutionContext> CreateMockContext(
            DictionaryContextData exprValues = null,
            IDictionary<string, IDictionary<string, string>> jobDefaults = null,
            ContainerInfo container = null)
        {
            var mock = new Mock<IExecutionContext>();
            mock.Setup(x => x.ExpressionValues).Returns(exprValues ?? new DictionaryContextData());
            mock.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());

            var global = new GlobalContext
            {
                PrependPath = new List<string>(),
                JobDefaults = jobDefaults
                    ?? new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase),
                Container = container,
            };
            mock.Setup(x => x.Global).Returns(global);

            return mock;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ExecuteRunCommand_NullContext_ReturnsError()
        {
            using (CreateTestContext())
            {
                var command = new RunCommand { Script = "echo hello" };
                var result = await _executor.ExecuteRunCommandAsync(command, null, false, CancellationToken.None);

                Assert.Equal("error", result.Type);
                Assert.Contains("No execution context available", result.Result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandExpressions_NoExpressions_ReturnsInput()
        {
            using (CreateTestContext())
            {
                var context = CreateMockContext();
                var result = _executor.ExpandExpressions("echo hello", context.Object);

                Assert.Equal("echo hello", result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandExpressions_NullInput_ReturnsEmpty()
        {
            using (CreateTestContext())
            {
                var context = CreateMockContext();
                var result = _executor.ExpandExpressions(null, context.Object);

                Assert.Equal(string.Empty, result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandExpressions_EmptyInput_ReturnsEmpty()
        {
            using (CreateTestContext())
            {
                var context = CreateMockContext();
                var result = _executor.ExpandExpressions("", context.Object);

                Assert.Equal(string.Empty, result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandExpressions_UnterminatedExpression_KeepsLiteral()
        {
            using (CreateTestContext())
            {
                var context = CreateMockContext();
                var result = _executor.ExpandExpressions("echo ${{ github.repo", context.Object);

                Assert.Equal("echo ${{ github.repo", result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResolveDefaultShell_NoJobDefaults_ReturnsPlatformDefault()
        {
            using (CreateTestContext())
            {
                var context = CreateMockContext();
                var result = _executor.ResolveDefaultShell(context.Object);

#if OS_WINDOWS
                Assert.True(result == "pwsh" || result == "powershell");
#else
                Assert.Equal("sh", result);
#endif
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ResolveDefaultShell_WithJobDefault_ReturnsJobDefault()
        {
            using (CreateTestContext())
            {
                var jobDefaults = new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["run"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["shell"] = "bash"
                    }
                };
                var context = CreateMockContext(jobDefaults: jobDefaults);
                var result = _executor.ResolveDefaultShell(context.Object);

                Assert.Equal("bash", result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void BuildEnvironment_MergesEnvContextAndReplOverrides()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                var envData = new DictionaryContextData
                {
                    ["FOO"] = new StringContextData("bar"),
                };
                exprValues["env"] = envData;

                var context = CreateMockContext(exprValues);
                var replEnv = new Dictionary<string, string> { { "BAZ", "qux" } };
                var result = _executor.BuildEnvironment(context.Object, replEnv);

                Assert.Equal("bar", result["FOO"]);
                Assert.Equal("qux", result["BAZ"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void BuildEnvironment_ReplOverridesWin()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                var envData = new DictionaryContextData
                {
                    ["FOO"] = new StringContextData("original"),
                };
                exprValues["env"] = envData;

                var context = CreateMockContext(exprValues);
                var replEnv = new Dictionary<string, string> { { "FOO", "override" } };
                var result = _executor.BuildEnvironment(context.Object, replEnv);

                Assert.Equal("override", result["FOO"]);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void BuildEnvironment_NullReplEnv_ReturnsContextEnvOnly()
        {
            using (CreateTestContext())
            {
                var exprValues = new DictionaryContextData();
                var envData = new DictionaryContextData
                {
                    ["FOO"] = new StringContextData("bar"),
                };
                exprValues["env"] = envData;

                var context = CreateMockContext(exprValues);
                var result = _executor.BuildEnvironment(context.Object, null);

                Assert.Equal("bar", result["FOO"]);
                Assert.False(result.ContainsKey("BAZ"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepHost_NoContainer_ReturnsDefaultStepHost()
        {
            using (var hc = CreateTestContext())
            {
                hc.EnqueueInstance<IDefaultStepHost>(new DefaultStepHost());
                var context = CreateMockContext();
                var result = _executor.CreateStepHost(context.Object, isActionStep: true);

                Assert.IsType<DefaultStepHost>(result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepHost_WithContainer_ActionStep_ReturnsContainerStepHost()
        {
            using (var hc = CreateTestContext())
            {
                hc.EnqueueInstance<IContainerStepHost>(new ContainerStepHost());
                var container = new ContainerInfo { ContainerId = "abc123" };
                var context = CreateMockContext(container: container);
                var result = _executor.CreateStepHost(context.Object, isActionStep: true);

                Assert.IsType<ContainerStepHost>(result);
                var containerHost = (ContainerStepHost)result;
                Assert.Same(container, containerHost.Container);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepHost_WithContainer_InfrastructureStep_ReturnsDefaultStepHost()
        {
            using (var hc = CreateTestContext())
            {
                hc.EnqueueInstance<IDefaultStepHost>(new DefaultStepHost());
                var container = new ContainerInfo { ContainerId = "abc123" };
                var context = CreateMockContext(container: container);
                var result = _executor.CreateStepHost(context.Object, isActionStep: false);

                Assert.IsType<DefaultStepHost>(result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepHost_ContainerWithoutId_NoHooks_ReturnsDefaultStepHost()
        {
            using (var hc = CreateTestContext())
            {
                hc.EnqueueInstance<IDefaultStepHost>(new DefaultStepHost());
                // Container exists but hasn't been started yet (no ContainerId)
                var container = new ContainerInfo();
                var context = CreateMockContext(container: container);
                var result = _executor.CreateStepHost(context.Object, isActionStep: true);

                Assert.IsType<DefaultStepHost>(result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CreateStepHost_ContainerWithoutId_HooksEnabled_ReturnsContainerStepHost()
        {
            using (var hc = CreateTestContext())
            {
                hc.EnqueueInstance<IContainerStepHost>(new ContainerStepHost());
                // Container hooks need both the feature flag and the env var
                Environment.SetEnvironmentVariable("ACTIONS_RUNNER_CONTAINER_HOOKS", "/some/hook/path");
                try
                {
                    var container = new ContainerInfo();
                    var context = CreateMockContext(container: container);
                    context.Object.Global.Variables = new Variables(
                        hc,
                        new Dictionary<string, VariableValue>
                        {
                            { Constants.Runner.Features.AllowRunnerContainerHooks, new VariableValue("true") }
                        });
                    var result = _executor.CreateStepHost(context.Object, isActionStep: true);
                    Assert.IsAssignableFrom<IContainerStepHost>(result);
                }
                finally
                {
                    Environment.SetEnvironmentVariable("ACTIONS_RUNNER_CONTAINER_HOOKS", null);
                }
            }
        }
    }
}
