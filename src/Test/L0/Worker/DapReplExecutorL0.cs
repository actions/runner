using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Tests;
using GitHub.Runner.Sdk;
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
        private const string Secret = "super-secret-token";
        private TestHostContext _hc;
        private DapReplExecutor _executor;
        private List<Event> _sentEvents;

        /// <summary>
        /// A step host that never launches a process. It replays canned stdout
        /// and stderr lines through the same events a real process raises, so
        /// the REPL output pipeline — including secret masking — can be
        /// exercised without executing anything.
        /// </summary>
        private sealed class FakeStepHost : RunnerService, IDefaultStepHost
        {
            private readonly IEnumerable<string> _stdout;
            private readonly IEnumerable<string> _stderr;
            private readonly Exception _executeException;

            public FakeStepHost(
                IEnumerable<string> stdout = null,
                IEnumerable<string> stderr = null,
                Exception executeException = null)
            {
                _stdout = stdout ?? Array.Empty<string>();
                _stderr = stderr ?? Array.Empty<string>();
                _executeException = executeException;
            }

            public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
            public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

            public string ResolvePathForStepHost(IExecutionContext executionContext, string path) => path;

            public Task<string> DetermineNodeRuntimeVersion(IExecutionContext executionContext, string preferredVersion)
                => Task.FromResult(preferredVersion);

            public Task<int> ExecuteAsync(
                IExecutionContext context,
                string workingDirectory,
                string fileName,
                string arguments,
                IDictionary<string, string> environment,
                bool requireExitCodeZero,
                Encoding outputEncoding,
                bool killProcessOnCancel,
                bool inheritConsoleHandler,
                string standardInInput,
                CancellationToken cancellationToken)
            {
                if (_executeException != null)
                {
                    throw _executeException;
                }

                foreach (var line in _stdout)
                {
                    OutputDataReceived?.Invoke(this, new ProcessDataReceivedEventArgs(line));
                }

                foreach (var line in _stderr)
                {
                    ErrorDataReceived?.Invoke(this, new ProcessDataReceivedEventArgs(line));
                }

                return Task.FromResult(0);
            }
        }

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

        /// <summary>
        /// Concatenates the text of every output event emitted so far, optionally
        /// filtered to a single DAP output category.
        /// </summary>
        private string CapturedOutput(string category = null)
        {
            var builder = new StringBuilder();
            foreach (var evt in _sentEvents)
            {
                var body = (OutputEventBody)evt.Body;
                if (category == null || string.Equals(body.Category, category, StringComparison.Ordinal))
                {
                    builder.Append(body.Output);
                }
            }

            return builder.ToString();
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
                FileTable = new List<string>(),
                Variables = new Variables(_hc, new Dictionary<string, VariableValue>()),
            };
            mock.Setup(x => x.Global).Returns(global);

            // ToPipelineTemplateEvaluator builds a trace writer that calls
            // context.Write — provide a no-op so expression expansion doesn't NRE.
            mock.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()));

            return mock;
        }

        /// <summary>
        /// Runs a REPL command end-to-end against a <see cref="FakeStepHost"/>.
        /// The runner's temp directory is created up front because the executor
        /// writes the generated script there before invoking the step host.
        /// </summary>
        private Task<EvaluateResponseBody> ExecuteWithFakeStepHostAsync(
            TestHostContext hc,
            FakeStepHost stepHost,
            RunCommand command,
            DictionaryContextData exprValues = null)
        {
            Directory.CreateDirectory(hc.GetDirectory(WellKnownDirectory.Temp));
            hc.EnqueueInstance<IDefaultStepHost>(stepHost);
            var context = CreateMockContext(exprValues);
            return _executor.ExecuteRunCommandAsync(command, context.Object, isActionStep: false, CancellationToken.None);
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ExecuteRunCommand_EchoesScriptVerbatimAndTruncatesLongScripts()
        {
            using (var hc = CreateTestContext())
            {
                // The console echo reflects the user's own input back to the
                // session that typed it, so it is intentionally not masked. It
                // is truncated at 80 characters to keep the console readable.
                var script = new string('a', 90);
                await ExecuteWithFakeStepHostAsync(hc, new FakeStepHost(), new RunCommand { Script = script });

                var console = CapturedOutput("console");
                Assert.Contains($"{new string('a', 80)}...\n", console, StringComparison.Ordinal);
                Assert.DoesNotContain(new string('a', 81), console, StringComparison.Ordinal);
            }
        }

        #region Secret masking regression tests
        //
        // Output the REPL relays from elsewhere — process output, exception
        // messages, evaluated expressions — must run through the runner's
        // SecretMasker before it reaches the DAP transport, because the DAP
        // console bypasses the normal job-log masking. (The console echo of the
        // user's own typed input is deliberately excluded; see the test above.)
        // The tests below pin each sink independently so a future refactor
        // cannot silently drop masking from one of them.

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ExecuteRunCommand_MasksSecretsInStdout()
        {
            using (var hc = CreateTestContext())
            {
                hc.SecretMasker.AddValue(Secret);
                var stepHost = new FakeStepHost(stdout: new[] { $"the token is {Secret}", "second line" });

                var result = await ExecuteWithFakeStepHostAsync(hc, stepHost, new RunCommand { Script = "echo hi" });

                Assert.Equal("string", result.Type);
                var stdout = CapturedOutput("stdout");
                Assert.DoesNotContain(Secret, stdout, StringComparison.Ordinal);
                Assert.Contains("the token is ***", stdout, StringComparison.Ordinal);
                Assert.Contains("second line", stdout, StringComparison.Ordinal);
                Assert.DoesNotContain(Secret, CapturedOutput(), StringComparison.Ordinal);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ExecuteRunCommand_MasksSecretsInStderr()
        {
            using (var hc = CreateTestContext())
            {
                hc.SecretMasker.AddValue(Secret);
                var stepHost = new FakeStepHost(stderr: new[] { $"auth failed for {Secret}" });

                await ExecuteWithFakeStepHostAsync(hc, stepHost, new RunCommand { Script = "echo hi" });

                var stderr = CapturedOutput("stderr");
                Assert.DoesNotContain(Secret, stderr, StringComparison.Ordinal);
                Assert.Contains("auth failed for ***", stderr, StringComparison.Ordinal);
                Assert.DoesNotContain(Secret, CapturedOutput(), StringComparison.Ordinal);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ExecuteRunCommand_MasksSecretsInFailureResult()
        {
            using (var hc = CreateTestContext())
            {
                hc.SecretMasker.AddValue(Secret);
                var stepHost = new FakeStepHost(
                    executeException: new InvalidOperationException($"spawn failed using {Secret}"));

                var result = await ExecuteWithFakeStepHostAsync(hc, stepHost, new RunCommand { Script = "echo hi" });

                Assert.Equal("error", result.Type);
                Assert.DoesNotContain(Secret, result.Result, StringComparison.Ordinal);
                Assert.Contains("spawn failed using ***", result.Result, StringComparison.Ordinal);
                Assert.DoesNotContain(Secret, CapturedOutput(), StringComparison.Ordinal);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExpandExpressions_MasksSecretsInEvaluatedResult()
        {
            using (var hc = CreateTestContext())
            {
                hc.SecretMasker.AddValue(Secret);
                var exprValues = new DictionaryContextData
                {
                    ["env"] = new DictionaryContextData
                    {
                        ["TOKEN"] = new StringContextData(Secret)
                    }
                };

                var context = CreateMockContext(exprValues);
                var result = _executor.ExpandExpressions("echo ${{ env.TOKEN }} done", context.Object);

                Assert.DoesNotContain(Secret, result, StringComparison.Ordinal);
                Assert.Equal("echo *** done", result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void BuildEnvironment_MasksNothingButExpandsSecretValuesForExecution()
        {
            using (var hc = CreateTestContext())
            {
                // The environment handed to the process is *not* a user-visible
                // sink — it must keep the real value so the command still works.
                // Only what we echo back over DAP gets masked.
                hc.SecretMasker.AddValue(Secret);
                var exprValues = new DictionaryContextData
                {
                    ["env"] = new DictionaryContextData
                    {
                        ["TOKEN"] = new StringContextData(Secret)
                    }
                };

                var context = CreateMockContext(exprValues);
                var result = _executor.BuildEnvironment(context.Object, replEnv: null);

                Assert.Equal(Secret, result["TOKEN"]);
            }
        }

        #endregion
    }
}
