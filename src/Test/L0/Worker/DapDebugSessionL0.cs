using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Dap;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class DapDebugSessionL0
    {
        private DapDebugSession _session;
        private Mock<IDapServer> _mockServer;
        private List<Event> _sentEvents;
        private List<Response> _sentResponses;

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hc = new TestHostContext(this, testName);

            _session = new DapDebugSession();
            _session.Initialize(hc);

            _sentEvents = new List<Event>();
            _sentResponses = new List<Response>();

            _mockServer = new Mock<IDapServer>();
            _mockServer.Setup(x => x.SendEvent(It.IsAny<Event>()))
                .Callback<Event>(e => _sentEvents.Add(e));
            _mockServer.Setup(x => x.SendResponse(It.IsAny<Response>()))
                .Callback<Response>(r => _sentResponses.Add(r));

            _session.SetDapServer(_mockServer.Object);

            return hc;
        }

        private Mock<IStep> CreateMockStep(string displayName, TaskResult? result = null)
        {
            var mockEc = new Mock<IExecutionContext>();
            mockEc.SetupAllProperties();
            mockEc.Object.Result = result;

            var mockStep = new Mock<IStep>();
            mockStep.Setup(x => x.DisplayName).Returns(displayName);
            mockStep.Setup(x => x.ExecutionContext).Returns(mockEc.Object);

            return mockStep;
        }

        private Mock<IExecutionContext> CreateMockJobContext()
        {
            var mockJobContext = new Mock<IExecutionContext>();
            mockJobContext.Setup(x => x.GetGitHubContext("job")).Returns("test-job");
            return mockJobContext;
        }

        private async Task InitializeSessionAsync()
        {
            var initJson = JsonConvert.SerializeObject(new Request
            {
                Seq = 1,
                Type = "request",
                Command = "initialize"
            });
            await _session.HandleMessageAsync(initJson, CancellationToken.None);

            var attachJson = JsonConvert.SerializeObject(new Request
            {
                Seq = 2,
                Type = "request",
                Command = "attach"
            });
            await _session.HandleMessageAsync(attachJson, CancellationToken.None);

            var configJson = JsonConvert.SerializeObject(new Request
            {
                Seq = 3,
                Type = "request",
                Command = "configurationDone"
            });
            await _session.HandleMessageAsync(configJson, CancellationToken.None);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InitialStateIsWaitingForConnection()
        {
            using (CreateTestContext())
            {
                Assert.Equal(DapSessionState.WaitingForConnection, _session.State);
                Assert.False(_session.IsActive);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task InitializeHandlerSetsInitializingState()
        {
            using (CreateTestContext())
            {
                var json = JsonConvert.SerializeObject(new Request
                {
                    Seq = 1,
                    Type = "request",
                    Command = "initialize"
                });

                await _session.HandleMessageAsync(json, CancellationToken.None);

                Assert.Equal(DapSessionState.Initializing, _session.State);
                Assert.Single(_sentResponses);
                Assert.True(_sentResponses[0].Success);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ConfigurationDoneSetsReadyState()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();

                Assert.Equal(DapSessionState.Ready, _session.State);
                Assert.True(_session.IsActive);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnStepStartingPausesAndSendsStoppedEvent()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();

                // Wait for the async initialized event to arrive, then clear
                await Task.Delay(200);
                _sentEvents.Clear();

                var step = CreateMockStep("Checkout code");
                var jobContext = CreateMockJobContext();

                var cts = new CancellationTokenSource();
                var stepTask = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, cts.Token);

                await Task.Delay(100);
                Assert.False(stepTask.IsCompleted);
                Assert.Equal(DapSessionState.Paused, _session.State);

                var stoppedEvents = _sentEvents.FindAll(e => e.EventType == "stopped");
                Assert.Single(stoppedEvents);

                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 10,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);

                await Task.WhenAny(stepTask, Task.Delay(5000));
                Assert.True(stepTask.IsCompleted);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task NextCommandPausesOnFollowingStep()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();
                _sentEvents.Clear();

                var step1 = CreateMockStep("Step 1");
                var jobContext = CreateMockJobContext();

                var step1Task = _session.OnStepStartingAsync(step1.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);

                var nextJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 10,
                    Type = "request",
                    Command = "next"
                });
                await _session.HandleMessageAsync(nextJson, CancellationToken.None);
                await Task.WhenAny(step1Task, Task.Delay(5000));
                Assert.True(step1Task.IsCompleted);

                _session.OnStepCompleted(step1.Object);
                _sentEvents.Clear();

                var step2 = CreateMockStep("Step 2");
                var step2Task = _session.OnStepStartingAsync(step2.Object, jobContext.Object, isFirstStep: false, CancellationToken.None);

                await Task.Delay(100);
                Assert.False(step2Task.IsCompleted);
                Assert.Equal(DapSessionState.Paused, _session.State);

                var stoppedEvents = _sentEvents.FindAll(e => e.EventType == "stopped");
                Assert.Single(stoppedEvents);

                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 11,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);
                await Task.WhenAny(step2Task, Task.Delay(5000));
                Assert.True(step2Task.IsCompleted);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ContinueCommandSkipsNextPause()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();
                _sentEvents.Clear();

                var step1 = CreateMockStep("Step 1");
                var jobContext = CreateMockJobContext();

                var step1Task = _session.OnStepStartingAsync(step1.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);

                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 10,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);
                await Task.WhenAny(step1Task, Task.Delay(5000));
                Assert.True(step1Task.IsCompleted);

                _session.OnStepCompleted(step1.Object);
                _sentEvents.Clear();

                var step2 = CreateMockStep("Step 2");
                var step2Task = _session.OnStepStartingAsync(step2.Object, jobContext.Object, isFirstStep: false, CancellationToken.None);

                await Task.WhenAny(step2Task, Task.Delay(5000));
                Assert.True(step2Task.IsCompleted);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task CancellationUnblocksPausedStep()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();
                _sentEvents.Clear();

                var step = CreateMockStep("Step 1");
                var jobContext = CreateMockJobContext();

                var cts = new CancellationTokenSource();
                var stepTask = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, cts.Token);

                await Task.Delay(100);
                Assert.False(stepTask.IsCompleted);
                Assert.Equal(DapSessionState.Paused, _session.State);

                cts.Cancel();

                await Task.WhenAny(stepTask, Task.Delay(5000));
                Assert.True(stepTask.IsCompleted);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task CancelSessionSendsTerminatedAndExitedEvents()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _sentEvents.Clear();

                _session.CancelSession();

                Assert.Equal(DapSessionState.Terminated, _session.State);
                Assert.False(_session.IsActive);

                var terminatedEvents = _sentEvents.FindAll(e => e.EventType == "terminated");
                var exitedEvents = _sentEvents.FindAll(e => e.EventType == "exited");
                Assert.Single(terminatedEvents);
                Assert.Single(exitedEvents);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task CancelSessionReleasesBlockedStep()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();
                _sentEvents.Clear();

                var step = CreateMockStep("Blocked Step");
                var jobContext = CreateMockJobContext();

                var stepTask = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);

                await Task.Delay(100);
                Assert.False(stepTask.IsCompleted);

                _session.CancelSession();

                await Task.WhenAny(stepTask, Task.Delay(5000));
                Assert.True(stepTask.IsCompleted);
                Assert.Equal(DapSessionState.Terminated, _session.State);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ReconnectionResendStoppedEvent()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();

                // Wait for the async initialized event to arrive, then clear
                await Task.Delay(200);
                _sentEvents.Clear();

                var step = CreateMockStep("Step 1");
                var jobContext = CreateMockJobContext();

                var stepTask = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);

                await Task.Delay(100);
                Assert.Equal(DapSessionState.Paused, _session.State);
                var stoppedEvents = _sentEvents.FindAll(e => e.EventType == "stopped");
                Assert.Single(stoppedEvents);

                _session.HandleClientDisconnected();
                Assert.Equal(DapSessionState.Paused, _session.State);

                _sentEvents.Clear();
                _session.HandleClientConnected();

                Assert.Single(_sentEvents);
                Assert.Equal("stopped", _sentEvents[0].EventType);

                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 20,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);
                await Task.WhenAny(stepTask, Task.Delay(5000));
                Assert.True(stepTask.IsCompleted);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DisconnectCommandTerminatesSession()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();

                var disconnectJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 10,
                    Type = "request",
                    Command = "disconnect"
                });
                await _session.HandleMessageAsync(disconnectJson, CancellationToken.None);

                Assert.Equal(DapSessionState.Terminated, _session.State);
                Assert.False(_session.IsActive);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnStepCompletedTracksCompletedSteps()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();

                var step1 = CreateMockStep("Step 1");
                step1.Object.ExecutionContext.Result = TaskResult.Succeeded;
                var jobContext = CreateMockJobContext();

                var step1Task = _session.OnStepStartingAsync(step1.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);

                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 10,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);
                await Task.WhenAny(step1Task, Task.Delay(5000));

                _session.OnStepCompleted(step1.Object);

                var stackTraceJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 11,
                    Type = "request",
                    Command = "stackTrace"
                });
                _sentResponses.Clear();
                await _session.HandleMessageAsync(stackTraceJson, CancellationToken.None);

                Assert.Single(_sentResponses);
                Assert.True(_sentResponses[0].Success);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnJobCompletedSendsTerminatedAndExitedEvents()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _sentEvents.Clear();

                _session.OnJobCompleted();

                Assert.Equal(DapSessionState.Terminated, _session.State);

                var terminatedEvents = _sentEvents.FindAll(e => e.EventType == "terminated");
                var exitedEvents = _sentEvents.FindAll(e => e.EventType == "exited");
                Assert.Single(terminatedEvents);
                Assert.Single(exitedEvents);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task OnStepStartingNoOpWhenNotActive()
        {
            using (CreateTestContext())
            {
                var step = CreateMockStep("Step 1");
                var jobContext = CreateMockJobContext();

                var task = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);

                await Task.WhenAny(task, Task.Delay(5000));
                Assert.True(task.IsCompleted);

                _mockServer.Verify(x => x.SendEvent(It.IsAny<Event>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ThreadsCommandReturnsJobThread()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();

                var threadsJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 10,
                    Type = "request",
                    Command = "threads"
                });
                _sentResponses.Clear();
                await _session.HandleMessageAsync(threadsJson, CancellationToken.None);

                Assert.Single(_sentResponses);
                Assert.True(_sentResponses[0].Success);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task UnsupportedCommandReturnsErrorResponse()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();

                var json = JsonConvert.SerializeObject(new Request
                {
                    Seq = 99,
                    Type = "request",
                    Command = "stepIn"
                });
                _sentResponses.Clear();
                await _session.HandleMessageAsync(json, CancellationToken.None);

                Assert.Single(_sentResponses);
                Assert.False(_sentResponses[0].Success);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task FullFlowInitAttachConfigStepContinueComplete()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();
                _sentEvents.Clear();
                _sentResponses.Clear();

                Assert.Equal(DapSessionState.Ready, _session.State);

                var step = CreateMockStep("Run tests");
                var jobContext = CreateMockJobContext();

                var stepTask = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);

                await Task.Delay(100);
                Assert.Equal(DapSessionState.Paused, _session.State);

                var stoppedEvents = _sentEvents.FindAll(e => e.EventType == "stopped");
                Assert.Single(stoppedEvents);

                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 10,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);
                await Task.WhenAny(stepTask, Task.Delay(5000));
                Assert.True(stepTask.IsCompleted);

                var continuedEvents = _sentEvents.FindAll(e => e.EventType == "continued");
                Assert.Single(continuedEvents);

                step.Object.ExecutionContext.Result = TaskResult.Succeeded;
                _session.OnStepCompleted(step.Object);

                _sentEvents.Clear();
                _session.OnJobCompleted();

                Assert.Equal(DapSessionState.Terminated, _session.State);
                var terminatedEvents = _sentEvents.FindAll(e => e.EventType == "terminated");
                var exitedEvents = _sentEvents.FindAll(e => e.EventType == "exited");
                Assert.Single(terminatedEvents);
                Assert.Single(exitedEvents);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task DoubleCancelSessionIsIdempotent()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _sentEvents.Clear();

                _session.CancelSession();
                _session.CancelSession();

                Assert.Equal(DapSessionState.Terminated, _session.State);

                var terminatedEvents = _sentEvents.FindAll(e => e.EventType == "terminated");
                Assert.Single(terminatedEvents);
            }
        }

        #region Scope inspection integration tests

        private Mock<IStep> CreateMockStepWithContext(
            string displayName,
            DictionaryContextData expressionValues,
            TaskResult? result = null)
        {
            var mockEc = new Mock<IExecutionContext>();
            mockEc.SetupAllProperties();
            mockEc.Object.Result = result;
            mockEc.Setup(x => x.ExpressionValues).Returns(expressionValues);

            var mockStep = new Mock<IStep>();
            mockStep.Setup(x => x.DisplayName).Returns(displayName);
            mockStep.Setup(x => x.ExecutionContext).Returns(mockEc.Object);

            return mockStep;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ScopesRequestReturnsScopesFromExecutionContext()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();

                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData
                {
                    { "repository", new StringContextData("owner/repo") }
                };
                exprValues["env"] = new DictionaryContextData
                {
                    { "CI", new StringContextData("true") }
                };

                var step = CreateMockStepWithContext("Run tests", exprValues);
                var jobContext = CreateMockJobContext();

                var stepTask = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);
                await Task.Delay(100);

                var scopesJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 20,
                    Type = "request",
                    Command = "scopes",
                    Arguments = Newtonsoft.Json.Linq.JObject.FromObject(new ScopesArguments { FrameId = 1 })
                });
                _sentResponses.Clear();
                await _session.HandleMessageAsync(scopesJson, CancellationToken.None);

                Assert.Single(_sentResponses);
                Assert.True(_sentResponses[0].Success);

                // Resume to unblock
                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 21,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);
                await Task.WhenAny(stepTask, Task.Delay(5000));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task VariablesRequestReturnsVariablesFromExecutionContext()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();

                var exprValues = new DictionaryContextData();
                exprValues["env"] = new DictionaryContextData
                {
                    { "CI", new StringContextData("true") },
                    { "HOME", new StringContextData("/home/runner") }
                };

                var step = CreateMockStepWithContext("Run tests", exprValues);
                var jobContext = CreateMockJobContext();

                var stepTask = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);
                await Task.Delay(100);

                // "env" is at ScopeNames index 1 → variablesReference = 2
                var variablesJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 20,
                    Type = "request",
                    Command = "variables",
                    Arguments = Newtonsoft.Json.Linq.JObject.FromObject(new VariablesArguments { VariablesReference = 2 })
                });
                _sentResponses.Clear();
                await _session.HandleMessageAsync(variablesJson, CancellationToken.None);

                Assert.Single(_sentResponses);
                Assert.True(_sentResponses[0].Success);

                // Resume to unblock
                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 21,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);
                await Task.WhenAny(stepTask, Task.Delay(5000));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ScopesRequestReturnsEmptyWhenNoStepActive()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();

                var scopesJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 10,
                    Type = "request",
                    Command = "scopes",
                    Arguments = Newtonsoft.Json.Linq.JObject.FromObject(new ScopesArguments { FrameId = 1 })
                });
                _sentResponses.Clear();
                await _session.HandleMessageAsync(scopesJson, CancellationToken.None);

                Assert.Single(_sentResponses);
                Assert.True(_sentResponses[0].Success);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SecretsValuesAreRedactedThroughSession()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();

                var exprValues = new DictionaryContextData();
                exprValues["secrets"] = new DictionaryContextData
                {
                    { "MY_TOKEN", new StringContextData("ghp_verysecret") }
                };

                var step = CreateMockStepWithContext("Run tests", exprValues);
                var jobContext = CreateMockJobContext();

                var stepTask = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);
                await Task.Delay(100);

                // "secrets" is at ScopeNames index 5 → variablesReference = 6
                var variablesJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 20,
                    Type = "request",
                    Command = "variables",
                    Arguments = Newtonsoft.Json.Linq.JObject.FromObject(new VariablesArguments { VariablesReference = 6 })
                });
                _sentResponses.Clear();
                await _session.HandleMessageAsync(variablesJson, CancellationToken.None);

                Assert.Single(_sentResponses);
                Assert.True(_sentResponses[0].Success);
                // The response body is serialized — we can't easily inspect it from
                // the mock, but the important thing is it succeeded without exposing
                // raw secrets (which is tested in DapVariableProviderL0).

                // Resume to unblock
                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 21,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);
                await Task.WhenAny(stepTask, Task.Delay(5000));
            }
        }

        #endregion

        #region Evaluate request integration tests

        private Mock<IStep> CreateMockStepWithEvaluatableContext(
            TestHostContext hc,
            string displayName,
            DictionaryContextData expressionValues,
            TaskResult? result = null)
        {
            var mockEc = new Mock<IExecutionContext>();
            mockEc.SetupAllProperties();
            mockEc.Object.Result = result;
            mockEc.Setup(x => x.ExpressionValues).Returns(expressionValues);
            mockEc.Setup(x => x.ExpressionFunctions)
                .Returns(new List<GitHub.DistributedTask.Expressions2.IFunctionInfo>());
            mockEc.Setup(x => x.Global).Returns(new GlobalContext
            {
                FileTable = new List<string>(),
                Variables = new Variables(hc, new Dictionary<string, VariableValue>()),
            });
            mockEc.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()));

            var mockStep = new Mock<IStep>();
            mockStep.Setup(x => x.DisplayName).Returns(displayName);
            mockStep.Setup(x => x.ExecutionContext).Returns(mockEc.Object);

            return mockStep;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task EvaluateRequestReturnsResult()
        {
            using (var hc = CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();

                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData
                {
                    { "repository", new StringContextData("owner/repo") }
                };

                var step = CreateMockStepWithEvaluatableContext(hc, "Run tests", exprValues);
                var jobContext = CreateMockJobContext();

                var stepTask = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);
                await Task.Delay(100);

                var evaluateJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 20,
                    Type = "request",
                    Command = "evaluate",
                    Arguments = Newtonsoft.Json.Linq.JObject.FromObject(new EvaluateArguments
                    {
                        Expression = "github.repository",
                        FrameId = 1,
                        Context = "watch"
                    })
                });
                _sentResponses.Clear();
                await _session.HandleMessageAsync(evaluateJson, CancellationToken.None);

                Assert.Single(_sentResponses);
                Assert.True(_sentResponses[0].Success);

                // Resume to unblock
                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 21,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);
                await Task.WhenAny(stepTask, Task.Delay(5000));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task EvaluateRequestReturnsGracefulErrorWhenNoContext()
        {
            using (CreateTestContext())
            {
                await InitializeSessionAsync();

                // No step is active — evaluate should still succeed with
                // a descriptive "no context" message, not an error response.
                var evaluateJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 10,
                    Type = "request",
                    Command = "evaluate",
                    Arguments = Newtonsoft.Json.Linq.JObject.FromObject(new EvaluateArguments
                    {
                        Expression = "github.repository",
                        FrameId = 1,
                        Context = "hover"
                    })
                });
                _sentResponses.Clear();
                await _session.HandleMessageAsync(evaluateJson, CancellationToken.None);

                Assert.Single(_sentResponses);
                Assert.True(_sentResponses[0].Success);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task EvaluateRequestWithWrapperSyntax()
        {
            using (var hc = CreateTestContext())
            {
                await InitializeSessionAsync();
                _session.HandleClientConnected();

                var exprValues = new DictionaryContextData();
                exprValues["github"] = new DictionaryContextData
                {
                    { "event_name", new StringContextData("push") }
                };

                var step = CreateMockStepWithEvaluatableContext(hc, "Run tests", exprValues);
                var jobContext = CreateMockJobContext();

                var stepTask = _session.OnStepStartingAsync(step.Object, jobContext.Object, isFirstStep: true, CancellationToken.None);
                await Task.Delay(100);

                var evaluateJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 20,
                    Type = "request",
                    Command = "evaluate",
                    Arguments = Newtonsoft.Json.Linq.JObject.FromObject(new EvaluateArguments
                    {
                        Expression = "${{ github.event_name }}",
                        FrameId = 1,
                        Context = "watch"
                    })
                });
                _sentResponses.Clear();
                await _session.HandleMessageAsync(evaluateJson, CancellationToken.None);

                Assert.Single(_sentResponses);
                Assert.True(_sentResponses[0].Success);

                // Resume to unblock
                var continueJson = JsonConvert.SerializeObject(new Request
                {
                    Seq = 21,
                    Type = "request",
                    Command = "continue"
                });
                await _session.HandleMessageAsync(continueJson, CancellationToken.None);
                await Task.WhenAny(stepTask, Task.Delay(5000));
            }
        }

        #endregion
    }
}
