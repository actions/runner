using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Moq;
using Xunit;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class StepsRunnerL0
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
            Dictionary<string, VariableValue> variablesToCopy = new Dictionary<string, VariableValue>();
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

            _contexts = new DictionaryContextData();
            _jobContext = new JobContext();
            _contexts["github"] = new GitHubContext();
            _contexts["runner"] = new DictionaryContextData();
            _contexts["job"] = _jobContext;
            _ec.Setup(x => x.ExpressionValues).Returns(_contexts);
            _ec.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            _ec.Setup(x => x.JobContext).Returns(_jobContext);

            _stepContext = new StepsContext();
            _ec.Object.Global.StepsContext = _stepContext;

            _ec.Setup(x => x.PostJobSteps).Returns(new Stack<IStep>());

            var trace = hc.GetTrace();
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });

            _stepsRunner = new StepsRunner();
            _stepsRunner.Initialize(hc);
            return hc;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunNormalStepsAllStepPass()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new[] { CreateStep(hc, TaskResult.Succeeded, "success()"), CreateStep(hc, TaskResult.Succeeded, "success()")  },
                    new[] { CreateStep(hc, TaskResult.Succeeded, "success()"), CreateStep(hc, TaskResult.Succeeded, "success() || failure()") },
                    new[] { CreateStep(hc, TaskResult.Succeeded, "success()"), CreateStep(hc, TaskResult.Succeeded, "always()") }
                };
                foreach (var variableSet in variableSets)
                {
                    _ec.Object.Result = null;

                    _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(variableSet.Select(x => x.Object).ToList()));

                    // Act.
                    await _stepsRunner.RunAsync(jobContext: _ec.Object);

                    // Assert.
                    Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
                    Assert.Equal(2, variableSet.Length);
                    variableSet[0].Verify(x => x.RunAsync());
                    variableSet[1].Verify(x => x.RunAsync());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunNormalStepsContinueOnError()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new[] { CreateStep(hc, TaskResult.Failed, "success()", true), CreateStep(hc, TaskResult.Succeeded, "success()")  },
                    new[] { CreateStep(hc, TaskResult.Failed, "success()", true), CreateStep(hc, TaskResult.Succeeded, "success() || failure()") },
                    new[] { CreateStep(hc, TaskResult.Failed, "success()", true), CreateStep(hc, TaskResult.Succeeded, "always()") },
                    new[] { CreateStep(hc, TaskResult.Failed, "success()", true), CreateStep(hc, TaskResult.Failed, "success()", true)  },
                    new[] { CreateStep(hc, TaskResult.Failed, "success()", true), CreateStep(hc, TaskResult.Failed, "success() || failure()", true) },
                    new[] { CreateStep(hc, TaskResult.Failed, "success()", true), CreateStep(hc, TaskResult.Failed, "always()", true) }
                };
                foreach (var variableSet in variableSets)
                {
                    _ec.Object.Result = null;

                    _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(variableSet.Select(x => x.Object).ToList()));

                    // Act.
                    await _stepsRunner.RunAsync(jobContext: _ec.Object);

                    // Assert.
                    Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
                    Assert.Equal(2, variableSet.Length);
                    variableSet[0].Verify(x => x.RunAsync());
                    variableSet[1].Verify(x => x.RunAsync());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunsAfterFailureBasedOnCondition()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "success()") },
                        Expected = false,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "success() || failure()") },
                        Expected = true,
                    },
                };
                foreach (var variableSet in variableSets)
                {
                    _ec.Object.Result = null;

                    _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(variableSet.Steps.Select(x => x.Object).ToList()));

                    // Act.
                    await _stepsRunner.RunAsync(jobContext: _ec.Object);

                    // Assert.
                    Assert.Equal(TaskResult.Failed, _ec.Object.Result ?? TaskResult.Succeeded);
                    Assert.Equal(2, variableSet.Steps.Length);
                    variableSet.Steps[0].Verify(x => x.RunAsync());
                    variableSet.Steps[1].Verify(x => x.RunAsync(), variableSet.Expected ? Times.Once() : Times.Never());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task RunsAlwaysSteps()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Succeeded, "success()"), CreateStep(hc, TaskResult.Succeeded, "always()") },
                        Expected = TaskResult.Succeeded,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "always()") },
                        Expected = TaskResult.Failed,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "always()") },
                        Expected = TaskResult.Failed,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Succeeded, "success()"), CreateStep(hc, TaskResult.Failed, "always()") },
                        Expected = TaskResult.Failed,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Succeeded, "success()"), CreateStep(hc, TaskResult.Failed, "always()", true) },
                        Expected = TaskResult.Succeeded,
                    },
                };
                foreach (var variableSet in variableSets)
                {
                    _ec.Object.Result = null;

                    _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(variableSet.Steps.Select(x => x.Object).ToList()));

                    // Act.
                    await _stepsRunner.RunAsync(jobContext: _ec.Object);

                    // Assert.
                    Assert.Equal(variableSet.Expected, _ec.Object.Result ?? TaskResult.Succeeded);
                    Assert.Equal(2, variableSet.Steps.Length);
                    variableSet.Steps[0].Verify(x => x.RunAsync());
                    variableSet.Steps[1].Verify(x => x.RunAsync());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SetsJobResultCorrectly()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "success()") },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "success() || failure()") },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "always()") },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Failed, "success()", continueOnError: true), CreateStep(hc, TaskResult.Failed, "success()") },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Failed, "success()", continueOnError: true), CreateStep(hc, TaskResult.Succeeded, "success()") },
                        Expected = TaskResult.Succeeded
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Failed, "success()", continueOnError: true), CreateStep(hc, TaskResult.Failed, "success()", continueOnError: true) },
                        Expected = TaskResult.Succeeded
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Succeeded, "success() || failure()") },
                        Expected = TaskResult.Succeeded
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Succeeded, "success()"), CreateStep(hc, TaskResult.Failed, "success()") },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(hc, TaskResult.Succeeded, "success()"), CreateStep(hc, TaskResult.Succeeded, "success()") },
                        Expected = TaskResult.Succeeded
                    },
                //  Abandoned
                //  Canceled
                //  Failed
                //  Skipped
                //  Succeeded
                };
                foreach (var variableSet in variableSets)
                {
                    _ec.Object.Result = null;

                    _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(variableSet.Steps.Select(x => x.Object).ToList()));

                    // Act.
                    await _stepsRunner.RunAsync(jobContext: _ec.Object);

                    // Assert.
                    Assert.True(
                        variableSet.Expected == (_ec.Object.Result ?? TaskResult.Succeeded),
                        $"Expected '{variableSet.Expected}'. Actual '{_ec.Object.Result}'. Steps: {FormatSteps(variableSet.Steps)}");
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task SkipsAfterFailureOnlyBaseOnCondition()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new
                    {
                        Step = new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "success()") },
                        Expected = false
                    },
                    new
                    {
                        Step = new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "success() || failure()") },
                        Expected = true
                    },
                    new
                    {
                        Step = new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "always()") },
                        Expected = true
                    }
                };
                foreach (var variableSet in variableSets)
                {
                    _ec.Object.Result = null;

                    _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(variableSet.Step.Select(x => x.Object).ToList()));

                    // Act.
                    await _stepsRunner.RunAsync(jobContext: _ec.Object);

                    // Assert.                    
                    Assert.Equal(2, variableSet.Step.Length);
                    variableSet.Step[0].Verify(x => x.RunAsync());
                    variableSet.Step[1].Verify(x => x.RunAsync(), variableSet.Expected ? Times.Once() : Times.Never());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task AlwaysMeansAlways()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new[] { CreateStep(hc, TaskResult.Succeeded, "success()"), CreateStep(hc, TaskResult.Succeeded, "always()") },
                    new[] { CreateStep(hc, TaskResult.Failed, "success()"), CreateStep(hc, TaskResult.Succeeded, "always()") },
                    new[] { CreateStep(hc, TaskResult.Canceled, "success()"), CreateStep(hc, TaskResult.Succeeded, "always()") }
                };
                foreach (var variableSet in variableSets)
                {
                    _ec.Object.Result = null;

                    _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(variableSet.Select(x => x.Object).ToList()));

                    // Act.
                    await _stepsRunner.RunAsync(jobContext: _ec.Object);

                    // Assert.
                    Assert.Equal(2, variableSet.Length);
                    variableSet[0].Verify(x => x.RunAsync());
                    variableSet[1].Verify(x => x.RunAsync(), Times.Once());
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task TreatsConditionErrorAsFailure()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var variableSets = new[]
                {
                    new[] { CreateStep(hc, TaskResult.Succeeded, "fromJson('not json')") },
                    new[] { CreateStep(hc, TaskResult.Succeeded, "fromJson('not json')") },
                };
                foreach (var variableSet in variableSets)
                {
                    _ec.Object.Result = null;

                    _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(variableSet.Select(x => x.Object).ToList()));

                    // Act.
                    await _stepsRunner.RunAsync(jobContext: _ec.Object);

                    // Assert.
                    Assert.Equal(TaskResult.Failed, _ec.Object.Result ?? TaskResult.Succeeded);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StepEnvOverrideJobEnvContext()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var env1 = new MappingToken(null, null, null);
                env1.Add(new StringToken(null, null, null, "env1"), new StringToken(null, null, null, "100"));
                env1.Add(new StringToken(null, null, null, "env2"), new BasicExpressionToken(null, null, null, "env.test"));
                var step1 = CreateStep(hc, TaskResult.Succeeded, "success()", env: env1);

                _ec.Object.Result = null;

                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new[] { step1.Object }));

                // Act.
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert.
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);

#if OS_WINDOWS
                Assert.Equal("100", step1.Object.ExecutionContext.ExpressionValues["env"].AssertDictionary("env")["env1"].AssertString("100"));
                Assert.Equal("github_actions", step1.Object.ExecutionContext.ExpressionValues["env"].AssertDictionary("env")["env2"].AssertString("github_actions"));
#else
                Assert.Equal("100", step1.Object.ExecutionContext.ExpressionValues["env"].AssertCaseSensitiveDictionary("env")["env1"].AssertString("100"));
                Assert.Equal("github_actions", step1.Object.ExecutionContext.ExpressionValues["env"].AssertCaseSensitiveDictionary("env")["env2"].AssertString("github_actions"));
#endif
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task PopulateEnvContextForEachStep()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var env1 = new MappingToken(null, null, null);
                env1.Add(new StringToken(null, null, null, "env1"), new StringToken(null, null, null, "100"));
                env1.Add(new StringToken(null, null, null, "env2"), new BasicExpressionToken(null, null, null, "env.test"));
                var step1 = CreateStep(hc, TaskResult.Succeeded, "success()", env: env1);

                var env2 = new MappingToken(null, null, null);
                env2.Add(new StringToken(null, null, null, "env1"), new StringToken(null, null, null, "1000"));
                env2.Add(new StringToken(null, null, null, "env3"), new BasicExpressionToken(null, null, null, "env.test"));
                var step2 = CreateStep(hc, TaskResult.Succeeded, "success()", env: env2);

                _ec.Object.Result = null;

                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new[] { step1.Object, step2.Object }));

                // Act.
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert.
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
#if OS_WINDOWS
                Assert.Equal("1000", step2.Object.ExecutionContext.ExpressionValues["env"].AssertDictionary("env")["env1"].AssertString("1000"));
                Assert.Equal("github_actions", step2.Object.ExecutionContext.ExpressionValues["env"].AssertDictionary("env")["env3"].AssertString("github_actions"));
                Assert.False(step2.Object.ExecutionContext.ExpressionValues["env"].AssertDictionary("env").ContainsKey("env2"));
#else
                Assert.Equal("1000", step2.Object.ExecutionContext.ExpressionValues["env"].AssertCaseSensitiveDictionary("env")["env1"].AssertString("1000"));
                Assert.Equal("github_actions", step2.Object.ExecutionContext.ExpressionValues["env"].AssertCaseSensitiveDictionary("env")["env3"].AssertString("github_actions"));
                Assert.False(step2.Object.ExecutionContext.ExpressionValues["env"].AssertCaseSensitiveDictionary("env").ContainsKey("env2"));
#endif
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task PopulateEnvContextAfterSetupStepsContext()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var env1 = new MappingToken(null, null, null);
                env1.Add(new StringToken(null, null, null, "env1"), new StringToken(null, null, null, "100"));
                var step1 = CreateStep(hc, TaskResult.Succeeded, "success()", env: env1, name: "foo", setOutput: true);

                var env2 = new MappingToken(null, null, null);
                env2.Add(new StringToken(null, null, null, "env1"), new StringToken(null, null, null, "1000"));
                env2.Add(new StringToken(null, null, null, "env2"), new BasicExpressionToken(null, null, null, "steps.foo.outputs.test"));
                var step2 = CreateStep(hc, TaskResult.Succeeded, "success()", env: env2);

                _ec.Object.Result = null;

                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new[] { step1.Object, step2.Object }));

                // Act.
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert.
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
#if OS_WINDOWS
                Assert.Equal("1000", step2.Object.ExecutionContext.ExpressionValues["env"].AssertDictionary("env")["env1"].AssertString("1000"));
                Assert.Equal("something", step2.Object.ExecutionContext.ExpressionValues["env"].AssertDictionary("env")["env2"].AssertString("something"));
#else
                Assert.Equal("1000", step2.Object.ExecutionContext.ExpressionValues["env"].AssertCaseSensitiveDictionary("env")["env1"].AssertString("1000"));
                Assert.Equal("something", step2.Object.ExecutionContext.ExpressionValues["env"].AssertCaseSensitiveDictionary("env")["env2"].AssertString("something"));
#endif
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StepContextOutcome()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var step1 = CreateStep(hc, TaskResult.Succeeded, "success()", contextName: "step1");
                var step2 = CreateStep(hc, TaskResult.Failed, "steps.step1.outcome == 'success'", continueOnError: true, contextName: "step2");
                var step3 = CreateStep(hc, TaskResult.Succeeded, "steps.step1.outcome == 'success' && steps.step2.outcome == 'failure'", contextName: "step3");

                _ec.Object.Result = null;

                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new[] { step1.Object, step2.Object, step3.Object }));

                // Act.
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert.
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);

                step1.Verify(x => x.RunAsync(), Times.Once);
                step2.Verify(x => x.RunAsync(), Times.Once);
                step3.Verify(x => x.RunAsync(), Times.Once);

                Assert.Equal(TaskResult.Succeeded.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step1"].AssertDictionary("")["outcome"].AssertString(""));
                Assert.Equal(TaskResult.Succeeded.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step1"].AssertDictionary("")["conclusion"].AssertString(""));
                Assert.Equal(TaskResult.Failed.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step2"].AssertDictionary("")["outcome"].AssertString(""));
                Assert.Equal(TaskResult.Succeeded.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step2"].AssertDictionary("")["conclusion"].AssertString(""));
                Assert.Equal(TaskResult.Succeeded.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step3"].AssertDictionary("")["outcome"].AssertString(""));
                Assert.Equal(TaskResult.Succeeded.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step3"].AssertDictionary("")["conclusion"].AssertString(""));

            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task StepContextConclusion()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var step1 = CreateStep(hc, TaskResult.Succeeded, "false", contextName: "step1");
                var step2 = CreateStep(hc, TaskResult.Failed, "steps.step1.conclusion == 'skipped'", continueOnError: true, contextName: "step2");
                var step3 = CreateStep(hc, TaskResult.Succeeded, "steps.step1.outcome == 'skipped' && steps.step2.outcome == 'failure' && steps.step2.conclusion == 'success'", contextName: "step3");

                _ec.Object.Result = null;

                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new[] { step1.Object, step2.Object, step3.Object }));

                // Act.
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert.
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);

                step1.Verify(x => x.RunAsync(), Times.Never);
                step2.Verify(x => x.RunAsync(), Times.Once);
                step3.Verify(x => x.RunAsync(), Times.Once);

                Assert.Equal(TaskResult.Skipped.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step1"].AssertDictionary("")["outcome"].AssertString(""));
                Assert.Equal(TaskResult.Skipped.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step1"].AssertDictionary("")["conclusion"].AssertString(""));
                Assert.Equal(TaskResult.Failed.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step2"].AssertDictionary("")["outcome"].AssertString(""));
                Assert.Equal(TaskResult.Succeeded.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step2"].AssertDictionary("")["conclusion"].AssertString(""));
                Assert.Equal(TaskResult.Succeeded.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step3"].AssertDictionary("")["outcome"].AssertString(""));
                Assert.Equal(TaskResult.Succeeded.ToActionResult().ToString().ToLowerInvariant(), _stepContext.GetScope(null)["step3"].AssertDictionary("")["conclusion"].AssertString(""));
            }
        }

        private Mock<IActionRunner> CreateStep(TestHostContext hc, TaskResult result, string condition, Boolean continueOnError = false, MappingToken env = null, string name = "Test", bool setOutput = false, string contextName = null)
        {
            // Setup the step.
            var step = new Mock<IActionRunner>();
            step.Setup(x => x.Condition).Returns(condition);
            step.Setup(x => x.ContinueOnError).Returns(new BooleanToken(null, null, null, continueOnError));
            step.Setup(x => x.Action)
                .Returns(new DistributedTask.Pipelines.ActionStep()
                {
                    Name = name,
                    Id = Guid.NewGuid(),
                    Environment = env,
                    ContextName = contextName ?? "Test"
                });

            // Setup the step execution context.
            var stepContext = new Mock<IExecutionContext>();
            stepContext.SetupAllProperties();
            stepContext.Setup(x => x.Global).Returns(() => _ec.Object.Global);
            var expressionValues = new DictionaryContextData();
            foreach (var pair in _ec.Object.ExpressionValues)
            {
                expressionValues[pair.Key] = pair.Value;
            }
            stepContext.Setup(x => x.ExpressionValues).Returns(expressionValues);
            stepContext.Setup(x => x.ExpressionFunctions).Returns(new List<IFunctionInfo>());
            stepContext.Setup(x => x.JobContext).Returns(_jobContext);
            stepContext.Setup(x => x.ContextName).Returns(step.Object.Action.ContextName);
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
            var trace = hc.GetTrace();
            stepContext.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { trace.Info($"[{tag}]{message}"); });
            stepContext.Object.Result = result;
            step.Setup(x => x.ExecutionContext).Returns(stepContext.Object);

            if (setOutput)
            {
                step.Setup(x => x.RunAsync()).Callback(() => { _stepContext.SetOutput(null, name, "test", "something", out string reference); }).Returns(Task.CompletedTask);
            }
            else
            {
                step.Setup(x => x.RunAsync()).Returns(Task.CompletedTask);
            }

            return step;
        }

        private string FormatSteps(IEnumerable<Mock<IActionRunner>> steps)
        {
            return String.Join(
                " ; ",
                steps.Select(x => String.Format(
                    CultureInfo.InvariantCulture,
                    "Returns={0},Condition=[{1}],ContinueOnError={2}",
                    x.Object.ExecutionContext.Result,
                    x.Object.Condition,
                    x.Object.ContinueOnError)));
        }
    }
}
