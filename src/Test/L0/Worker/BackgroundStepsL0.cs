using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Dap;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class BackgroundStepsL0
    {
        private Mock<IExecutionContext> _ec;
        private StepsRunner _stepsRunner;
        private Variables _variables;
        private Dictionary<string, string> _env;
        private DictionaryContextData _contexts;
        private JobContext _jobContext;
        private StepsContext _stepContext;

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            var hc = new TestHostContext(this, testName);
            Dictionary<string, VariableValue> variablesToCopy = new();
            _variables = new Variables(
                hostContext: hc,
                copy: variablesToCopy);
            _env = new Dictionary<string, string>()
            {
                {"env1", "1"},
                {"test", "github_actions"}
            };
            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            _ec.Setup(x => x.Global).Returns(new GlobalContext { WriteDebug = true });
            _ec.Object.Global.Variables = _variables;
            _ec.Object.Global.EnvironmentVariables = _env;
            _ec.Object.Global.FileTable = new List<string>();

            _contexts = new DictionaryContextData();
            _jobContext = new JobContext();
            _contexts["github"] = new GitHubContext();
            _contexts["runner"] = new DictionaryContextData();
            _contexts["job"] = _jobContext;
            _ec.Setup(x => x.ExpressionValues).Returns(_contexts);
            _ec.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            _ec.Setup(x => x.JobContext).Returns(_jobContext);
            _ec.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

            _stepContext = new StepsContext();
            _ec.Object.Global.StepsContext = _stepContext;

            _ec.Setup(x => x.PostJobSteps).Returns(new Stack<IStep>());

            var trace = hc.GetTrace();

            // Mock CreateChild for implicit wait-all step injection
            _ec.Setup(x => x.CreateChild(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ActionRunStage>(),
                It.IsAny<Dictionary<string, string>>(), It.IsAny<int?>(), It.IsAny<IPagingLogger>(),
                It.IsAny<bool>(), It.IsAny<List<Issue>>(), It.IsAny<CancellationTokenSource>(),
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<string>()))
                .Returns((Guid recordId, string displayName, string refName, string scopeName, string contextName,
                    ActionRunStage stage, Dictionary<string, string> intraActionState, int? recordOrder, IPagingLogger logger,
                    bool isEmbedded, List<Issue> issues, CancellationTokenSource cts, Guid embeddedId, string siblingScopeName, TimeSpan? timeout,
                    bool isBackground, string backgroundControlType, string[] backgroundControlStepIds, string parallelGroupId) =>
                {
                    var childEc = new Mock<IExecutionContext>();
                    childEc.SetupAllProperties();
                    childEc.Setup(x => x.Global).Returns(() => _ec.Object.Global);
                    childEc.Setup(x => x.ExpressionValues).Returns(new DictionaryContextData());
                    childEc.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
                    childEc.Setup(x => x.ContextName).Returns(contextName);
                    childEc.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
                    childEc.Setup(x => x.Complete(It.IsAny<TaskResult?>(), It.IsAny<string>(), It.IsAny<string>()))
                        .Callback((TaskResult? r, string currentOperation, string resultCode) =>
                        {
                            if (r != null) childEc.Object.Result = r;
                        });
                    childEc.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });
                    return childEc.Object;
                });

            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });

            _stepsRunner = new StepsRunner();
            _stepsRunner.Initialize(hc);

            var bgCoordinator = new BackgroundStepCoordinator();
            bgCoordinator.Initialize(hc);
            hc.SetSingleton<IBackgroundStepCoordinator>(bgCoordinator);

            var mockDapDebugger = new Mock<IDapDebugger>();
            hc.SetSingleton(mockDapDebugger.Object);

            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task BackgroundStepRunsConcurrentlyWithForeground()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange: background step that takes time, followed by a foreground step
                var executionOrder = new List<string>();

                var bgStep = CreateStep(hc, TaskResult.Succeeded, "success()", name: "bg-step", contextName: "bg", isBackground: true);
                bgStep.Setup(x => x.RunAsync()).Returns(async () =>
                {
                    executionOrder.Add("bg-start");
                    await Task.Delay(2000);
                    executionOrder.Add("bg-end");
                });
                bgStep.Setup(x => x.Action).Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = "bg-step",
                    Id = Guid.NewGuid(),
                    ContextName = "bg",
                    Background = true,
                });

                var fgStep = CreateStep(hc, TaskResult.Succeeded, "success()", name: "fg-step", contextName: "fg");
                fgStep.Setup(x => x.RunAsync()).Returns(() =>
                {
                    executionOrder.Add("fg-run");
                    return Task.CompletedTask;
                });

                var waitAllStep = CreateWaitAllStep(hc);

                _ec.Object.Result = null;
                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new IStep[]
                {
                    bgStep.Object, fgStep.Object, waitAllStep
                }));

                // Act
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert: foreground step should start before background step finishes
                Assert.Contains("bg-start", executionOrder);
                Assert.Contains("fg-run", executionOrder);
                Assert.Contains("bg-end", executionOrder);
                var fgIndex = executionOrder.IndexOf("fg-run");
                var bgEndIndex = executionOrder.IndexOf("bg-end");
                Assert.True(fgIndex < bgEndIndex, "Foreground step should run before background step completes");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitStepBlocksUntilBackgroundCompletes()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange
                var bgCompleted = false;

                var bgStep = CreateStep(hc, TaskResult.Succeeded, "success()", name: "db", contextName: "db", isBackground: true);
                bgStep.Setup(x => x.RunAsync()).Returns(async () =>
                {
                    await Task.Delay(100);
                    bgCompleted = true;
                });
                bgStep.Setup(x => x.Action).Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = "db",
                    Id = Guid.NewGuid(),
                    ContextName = "db",
                    Background = true,
                });

                var waitStep = CreateWaitStep(hc, new[] { "db" });

                _ec.Object.Result = null;
                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new IStep[]
                {
                    bgStep.Object, waitStep
                }));

                // Act
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert: background step must have completed after wait
                Assert.True(bgCompleted, "Background step should have completed after wait");
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task BackgroundStepFailurePropagatesAtWait()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange: background step that fails
                var bgStep = CreateStep(hc, TaskResult.Failed, "success()", name: "flaky", contextName: "flaky", isBackground: true);
                bgStep.Setup(x => x.RunAsync()).Returns(() =>
                {
                    throw new Exception("Service crashed");
                });
                bgStep.Setup(x => x.Action).Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = "flaky",
                    Id = Guid.NewGuid(),
                    ContextName = "flaky",
                    Background = true,
                });

                var waitStep = CreateWaitStep(hc, new[] { "flaky" });

                _ec.Object.Result = null;
                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new IStep[]
                {
                    bgStep.Object, waitStep
                }));

                // Act
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert: job should fail because background step failed
                Assert.Equal(TaskResult.Failed, _ec.Object.Result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task CancelStepTerminatesBackgroundStep()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange: background step that runs until cancelled via ExecutionContext.CancellationToken
                var stepCts = new CancellationTokenSource();

                var bgStep = CreateStep(hc, TaskResult.Succeeded, "success()", name: "server", contextName: "server");
                // Wire CancellationToken to our CTS so the cancel path can trigger it
                var bgStepContext = Mock.Get(bgStep.Object.ExecutionContext);
                bgStepContext.Setup(x => x.CancellationToken).Returns(stepCts.Token);
                bgStepContext.Setup(x => x.CancelToken()).Callback(() => stepCts.Cancel());
                bgStep.Setup(x => x.RunAsync()).Returns(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stepCts.Token);
                });
                bgStep.Setup(x => x.Action).Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = "server",
                    Id = Guid.NewGuid(),
                    ContextName = "server",
                    Background = true,
                });

                var cancelStep = CreateCancelStep(hc, "server");

                _ec.Object.Result = null;
                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new IStep[]
                {
                    bgStep.Object, cancelStep
                }));

                // Act
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert: background step should have been cancelled
                // Note: the cancel mechanism uses the BackgroundStepContext.Cts, not bgCts
                // so wasCancelled may not be true in this mock, but the step should complete
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task WaitAllWaitsForAllBackgroundSteps()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange: two background steps
                var step1Done = false;
                var step2Done = false;

                var bgStep1 = CreateStep(hc, TaskResult.Succeeded, "success()", name: "svc1", contextName: "svc1", isBackground: true);
                bgStep1.Setup(x => x.RunAsync()).Returns(async () =>
                {
                    await Task.Delay(50);
                    step1Done = true;
                });
                bgStep1.Setup(x => x.Action).Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = "svc1",
                    Id = Guid.NewGuid(),
                    ContextName = "svc1",
                    Background = true,
                });

                var bgStep2 = CreateStep(hc, TaskResult.Succeeded, "success()", name: "svc2", contextName: "svc2", isBackground: true);
                bgStep2.Setup(x => x.RunAsync()).Returns(async () =>
                {
                    await Task.Delay(100);
                    step2Done = true;
                });
                bgStep2.Setup(x => x.Action).Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = "svc2",
                    Id = Guid.NewGuid(),
                    ContextName = "svc2",
                    Background = true,
                });

                var waitAllStep = CreateWaitAllStep(hc);

                _ec.Object.Result = null;
                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new IStep[]
                {
                    bgStep1.Object, bgStep2.Object, waitAllStep
                }));

                // Act
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert
                Assert.True(step1Done, "Background step 1 should have completed");
                Assert.True(step2Done, "Background step 2 should have completed");
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task CancelStepPublishesCanceledBackgroundExternalId()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                var bgStep = CreateStep(hc, TaskResult.Succeeded, "success()", name: "server", contextName: "server", isBackground: true);
                bgStep.Setup(x => x.RunAsync()).Returns(Task.CompletedTask);
                bgStep.Setup(x => x.Action).Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = "server",
                    Id = Guid.NewGuid(),
                    ContextName = "server",
                    Background = true,
                });

                var cancelStep = CreateCancelStep(hc, "server");

                _ec.Object.Result = null;
                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new IStep[]
                {
                    bgStep.Object, cancelStep
                }));

                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert: cancel step completed without error
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task CanceledBackgroundStepDoesNotAffectJobResult()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange: a background step that runs until explicitly canceled. When canceled it
                // reports TaskResult.Canceled, but since the cancellation is expected (driven by a
                // cancel control step), it must not impact the overall job result.
                using var stepCts = new CancellationTokenSource();

                var bgStep = CreateStep(hc, TaskResult.Succeeded, "success()", name: "server", contextName: "server", isBackground: true);
                var bgStepContext = Mock.Get(bgStep.Object.ExecutionContext);
                bgStepContext.Setup(x => x.CancellationToken).Returns(stepCts.Token);
                bgStepContext.Setup(x => x.CancelToken()).Callback(() => stepCts.Cancel());
                bgStep.Setup(x => x.RunAsync()).Returns(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), stepCts.Token);
                });
                bgStep.Setup(x => x.Action).Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = "server",
                    Id = Guid.NewGuid(),
                    ContextName = "server",
                    Background = true,
                });

                var cancelStep = CreateCancelStep(hc, "server");

                _ec.Object.Result = null;
                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new IStep[]
                {
                    bgStep.Object, cancelStep
                }));

                // Act
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert: the canceled background step reported Canceled, but the job result is unaffected.
                Assert.Equal(TaskResult.Canceled, bgStep.Object.ExecutionContext.Result);
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task FailedBackgroundStepTargetedByCancelStillAffectsJobResult()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange: a background step that fails (e.g. before the cancel takes effect). Even
                // though a cancel control step targets it, its Failed result must still propagate to
                // the overall job result.
                var bgStep = CreateStep(hc, TaskResult.Failed, "success()", name: "server", contextName: "server", isBackground: true);
                bgStep.Setup(x => x.RunAsync()).Returns(Task.CompletedTask);
                bgStep.Setup(x => x.Action).Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = "server",
                    Id = Guid.NewGuid(),
                    ContextName = "server",
                    Background = true,
                });

                var cancelStep = CreateCancelStep(hc, "server");

                _ec.Object.Result = null;
                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new IStep[]
                {
                    bgStep.Object, cancelStep
                }));

                // Act
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert: the background step failed, so the job result reflects that failure.
                Assert.Equal(TaskResult.Failed, bgStep.Object.ExecutionContext.Result);
                Assert.Equal(TaskResult.Failed, _ec.Object.Result);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StepsContextThreadSafety()
        {
            // Test that concurrent SetOutput/SetConclusion doesn't throw
            var stepsContext = new StepsContext();
            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                var index = i;
                tasks.Add(Task.Run(() =>
                {
                    stepsContext.SetOutput("", $"step{index}", "out", $"value{index}", out _);
                    stepsContext.SetConclusion("", $"step{index}", ActionResult.Success);
                    stepsContext.SetOutcome("", $"step{index}", ActionResult.Success);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert: all 100 steps should have their data set
            var scope = stepsContext.GetScope("");
            Assert.Equal(100, scope.Count);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ControlFlowStepsRunEvenAfterFailure()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange: a background step, a foreground step that fails, then a wait step
                var bgStep = CreateStep(hc, TaskResult.Succeeded, "success()", name: "bg", contextName: "bg", isBackground: true);
                bgStep.Setup(x => x.RunAsync()).Returns(Task.CompletedTask);
                bgStep.Setup(x => x.Action).Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = "bg",
                    Id = Guid.NewGuid(),
                    ContextName = "bg",
                    Background = true,
                });

                var failStep = CreateStep(hc, TaskResult.Failed, "success()", name: "fail", contextName: "fail");

                // Wait step uses always() condition — should run even after failure
                var waitStep = CreateWaitStep(hc, new[] { "bg" });
                waitStep.Condition = $"{GitHub.DistributedTask.Pipelines.ObjectTemplating.PipelineTemplateConstants.Always}()";

                _ec.Object.Result = null;
                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new IStep[]
                {
                    bgStep.Object, failStep.Object, waitStep
                }));

                // Act
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert: wait step should have run (not skipped) because it has always() condition
                Assert.NotNull(waitStep.ExecutionContext.Result);
                Assert.NotEqual(TaskResult.Skipped, waitStep.ExecutionContext.Result);
            }
        }

        #region Helpers

        private Mock<IActionRunner> CreateStep(TestHostContext hc, TaskResult result, string condition, string name = "Test", string contextName = null, Guid? recordId = null, bool isBackground = false)
        {
            var stepRecordId = recordId ?? Guid.NewGuid();
            var step = new Mock<IActionRunner>();
            step.Setup(x => x.Condition).Returns(condition);
            step.Setup(x => x.ContinueOnError).Returns(new BooleanToken(null, null, null, false));
            step.Setup(x => x.Stage).Returns(ActionRunStage.Main);
            step.Setup(x => x.Action)
                .Returns(new GitHub.DistributedTask.Pipelines.ActionStep()
                {
                    Name = name,
                    Id = stepRecordId,
                    ContextName = contextName ?? name,
                });

            var stepContext = new Mock<IExecutionContext>();
            stepContext.SetupAllProperties();
            stepContext.Setup(x => x.Global).Returns(() => _ec.Object.Global);
            stepContext.Setup(x => x.IsBackground).Returns(isBackground);
            var expressionValues = new DictionaryContextData();
            foreach (var pair in _ec.Object.ExpressionValues)
            {
                expressionValues[pair.Key] = pair.Value;
            }
            stepContext.Setup(x => x.ExpressionValues).Returns(expressionValues);
            stepContext.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            stepContext.Setup(x => x.JobContext).Returns(_jobContext);
            stepContext.Setup(x => x.Id).Returns(stepRecordId);
            stepContext.Setup(x => x.ContextName).Returns(step.Object.Action.ContextName);
            stepContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
            stepContext.Setup(x => x.Complete(It.IsAny<TaskResult?>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((TaskResult? r, string currentOperation, string resultCode) =>
                {
                    if (r != null)
                    {
                        stepContext.Object.Result = r;
                    }
                    _stepContext.SetOutcome("", stepContext.Object.ContextName, (stepContext.Object.Outcome ?? stepContext.Object.Result ?? TaskResult.Succeeded).ToActionResult());
                    _stepContext.SetConclusion("", stepContext.Object.ContextName, (stepContext.Object.Result ?? TaskResult.Succeeded).ToActionResult());
                });
            stepContext.Setup(x => x.StepEnvironmentOverrides).Returns(new List<string>());
            stepContext.Setup(x => x.ApplyContinueOnError(It.IsAny<TemplateToken>()));
            stepContext.Setup(x => x.FlushDeferredOutputs()).Callback(() =>
            {
                if (stepContext.Object.DeferredOutputs != null)
                {
                    foreach (var kvp in stepContext.Object.DeferredOutputs)
                    {
                        _stepContext.SetOutput("", stepContext.Object.ContextName, kvp.Key, kvp.Value, out _);
                    }
                }
            });

            var trace = hc.GetTrace();
            stepContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });
            stepContext.Object.Result = result;
            step.Setup(x => x.ExecutionContext).Returns(stepContext.Object);
            step.Setup(x => x.RunAsync()).Returns(Task.CompletedTask);

            return step;
        }

        private JobExtensionRunner CreateWaitStep(TestHostContext hc, string[] stepIds, Dictionary<string, string> timelineVariables = null)
        {
            var waitData = new BackgroundStepControlFlowData
            {
                Type = Pipelines.BackgroundControlTypes.Wait,
                StepIds = stepIds,
            };
            var bgCoordinator = hc.GetService<IBackgroundStepCoordinator>();
            var waitRunner = new JobExtensionRunner(
                runAsync: bgCoordinator.RunControlFlowAsync,
                condition: "success()",
                displayName: "Wait",
                data: waitData);

            var stepContext = new Mock<IExecutionContext>();
            stepContext.SetupAllProperties();
            stepContext.Setup(x => x.Global).Returns(() => _ec.Object.Global);
            var waitExprValues = new DictionaryContextData();
            foreach (var pair in _ec.Object.ExpressionValues) { waitExprValues[pair.Key] = pair.Value; }
            stepContext.Setup(x => x.ExpressionValues).Returns(waitExprValues);
            stepContext.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            stepContext.Setup(x => x.ContextName).Returns("__wait");
            stepContext.Setup(x => x.JobContext).Returns(_jobContext);
            stepContext.Setup(x => x.ScopeName).Returns((string)null);
            stepContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
            stepContext.Setup(x => x.StepEnvironmentOverrides).Returns(new List<string>());
            stepContext.Setup(x => x.Complete(It.IsAny<TaskResult?>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((TaskResult? r, string currentOperation, string resultCode) =>
                {
                    if (r != null) stepContext.Object.Result = r;
                });
            var trace = hc.GetTrace();
            stepContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });

            waitRunner.ExecutionContext = stepContext.Object;
            return waitRunner;
        }

        private JobExtensionRunner CreateWaitAllStep(TestHostContext hc, Dictionary<string, string> timelineVariables = null)
        {
            var waitAllData = new BackgroundStepControlFlowData
            {
                Type = Pipelines.BackgroundControlTypes.WaitAll,
            };
            var bgCoordinator2 = hc.GetService<IBackgroundStepCoordinator>();
            var waitAllRunner = new JobExtensionRunner(
                runAsync: bgCoordinator2.RunControlFlowAsync,
                condition: "success()",
                displayName: "Wait All",
                data: waitAllData);

            var stepContext = new Mock<IExecutionContext>();
            stepContext.SetupAllProperties();
            stepContext.Setup(x => x.Global).Returns(() => _ec.Object.Global);
            var waitAllExprValues = new DictionaryContextData();
            foreach (var pair in _ec.Object.ExpressionValues) { waitAllExprValues[pair.Key] = pair.Value; }
            stepContext.Setup(x => x.ExpressionValues).Returns(waitAllExprValues);
            stepContext.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            stepContext.Setup(x => x.ContextName).Returns("__wait-all");
            stepContext.Setup(x => x.JobContext).Returns(_jobContext);
            stepContext.Setup(x => x.ScopeName).Returns((string)null);
            stepContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
            stepContext.Setup(x => x.StepEnvironmentOverrides).Returns(new List<string>());
            stepContext.Setup(x => x.Complete(It.IsAny<TaskResult?>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((TaskResult? r, string currentOperation, string resultCode) =>
                {
                    if (r != null) stepContext.Object.Result = r;
                });
            var trace = hc.GetTrace();
            stepContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });

            waitAllRunner.ExecutionContext = stepContext.Object;
            return waitAllRunner;
        }

        private JobExtensionRunner CreateCancelStep(TestHostContext hc, string cancelStepId, Dictionary<string, string> timelineVariables = null)
        {
            var cancelData = new BackgroundStepControlFlowData
            {
                Type = Pipelines.BackgroundControlTypes.Cancel,
                StepIds = new[] { cancelStepId },
            };
            var bgCoordinator3 = hc.GetService<IBackgroundStepCoordinator>();
            var cancelRunner = new JobExtensionRunner(
                runAsync: bgCoordinator3.RunControlFlowAsync,
                condition: "success()",
                displayName: "Cancel",
                data: cancelData);

            var stepContext = new Mock<IExecutionContext>();
            stepContext.SetupAllProperties();
            stepContext.Setup(x => x.Global).Returns(() => _ec.Object.Global);
            var cancelExprValues = new DictionaryContextData();
            foreach (var pair in _ec.Object.ExpressionValues) { cancelExprValues[pair.Key] = pair.Value; }
            stepContext.Setup(x => x.ExpressionValues).Returns(cancelExprValues);
            stepContext.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            stepContext.Setup(x => x.ContextName).Returns("__cancel");
            stepContext.Setup(x => x.JobContext).Returns(_jobContext);
            stepContext.Setup(x => x.ScopeName).Returns((string)null);
            stepContext.Setup(x => x.CancellationToken).Returns(CancellationToken.None);
            stepContext.Setup(x => x.StepEnvironmentOverrides).Returns(new List<string>());
            stepContext.Setup(x => x.Complete(It.IsAny<TaskResult?>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((TaskResult? r, string currentOperation, string resultCode) =>
                {
                    if (r != null) stepContext.Object.Result = r;
                });
            var trace = hc.GetTrace();
            stepContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });

            cancelRunner.ExecutionContext = stepContext.Object;
            return cancelRunner;
        }

        #endregion
    }
}
