using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

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
            var expressionManager = new ExpressionManager();
            expressionManager.Initialize(hc);
            hc.SetSingleton<IExpressionManager>(expressionManager);
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
            _ec.Setup(x => x.Variables).Returns(_variables);

            _contexts = new DictionaryContextData();
            _jobContext = new JobContext();
            _contexts["github"] = new DictionaryContextData();
            _contexts["runner"] = new DictionaryContextData();
            _contexts["job"] = _jobContext;
            _ec.Setup(x => x.ExpressionValues).Returns(_contexts);
            _ec.Setup(x => x.JobContext).Returns(_jobContext);

            _stepContext = new StepsContext();
            _ec.Setup(x => x.StepsContext).Returns(_stepContext);

            _ec.Setup(x => x.PostJobSteps).Returns(new Stack<IStep>());

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
                    new[] { CreateStep(TaskResult.Succeeded, "success()"), CreateStep(TaskResult.Succeeded, "success()")  },
                    new[] { CreateStep(TaskResult.Succeeded, "success()"), CreateStep(TaskResult.Succeeded, "success() || failure()") },
                    new[] { CreateStep(TaskResult.Succeeded, "success()"), CreateStep(TaskResult.Succeeded, "always()") }
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
                    new[] { CreateStep(TaskResult.Failed, "success()", true), CreateStep(TaskResult.Succeeded, "success()")  },
                    new[] { CreateStep(TaskResult.Failed, "success()", true), CreateStep(TaskResult.Succeeded, "success() || failure()") },
                    new[] { CreateStep(TaskResult.Failed, "success()", true), CreateStep(TaskResult.Succeeded, "always()") },
                    new[] { CreateStep(TaskResult.Failed, "success()", true), CreateStep(TaskResult.Failed, "success()", true)  },
                    new[] { CreateStep(TaskResult.Failed, "success()", true), CreateStep(TaskResult.Failed, "success() || failure()", true) },
                    new[] { CreateStep(TaskResult.Failed, "success()", true), CreateStep(TaskResult.Failed, "always()", true) }
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
                        Steps = new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "success()") },
                        Expected = false,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "success() || failure()") },
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
                        Steps = new[] { CreateStep(TaskResult.Succeeded, "success()"), CreateStep(TaskResult.Succeeded, "always()") },
                        Expected = TaskResult.Succeeded,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "always()") },
                        Expected = TaskResult.Failed,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "always()") },
                        Expected = TaskResult.Failed,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Succeeded, "success()"), CreateStep(TaskResult.Failed, "always()") },
                        Expected = TaskResult.Failed,
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Succeeded, "success()"), CreateStep(TaskResult.Failed, "always()", true) },
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
                        Steps = new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "success()") },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "success() || failure()") },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "always()") },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, "success()", continueOnError: true), CreateStep(TaskResult.Failed, "success()") },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, "success()", continueOnError: true), CreateStep(TaskResult.Succeeded, "success()") },
                        Expected = TaskResult.Succeeded
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Failed, "success()", continueOnError: true), CreateStep(TaskResult.Failed, "success()", continueOnError: true) },
                        Expected = TaskResult.Succeeded
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Succeeded, "success() || failure()") },
                        Expected = TaskResult.Succeeded
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Succeeded, "success()"), CreateStep(TaskResult.Failed, "success()") },
                        Expected = TaskResult.Failed
                    },
                    new
                    {
                        Steps = new[] { CreateStep(TaskResult.Succeeded, "success()"), CreateStep(TaskResult.Succeeded, "success()") },
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
                        Step = new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "success()") },
                        Expected = false
                    },
                    new
                    {
                        Step = new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "success() || failure()") },
                        Expected = true
                    },
                    new
                    {
                        Step = new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "always()") },
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
                    new[] { CreateStep(TaskResult.Succeeded, "success()"), CreateStep(TaskResult.Succeeded, "always()") },
                    new[] { CreateStep(TaskResult.Failed, "success()"), CreateStep(TaskResult.Succeeded, "always()") },
                    new[] { CreateStep(TaskResult.Canceled, "success()"), CreateStep(TaskResult.Succeeded, "always()") }
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
                var expressionManager = new Mock<IExpressionManager>();
                expressionManager.Object.Initialize(hc);
                hc.SetSingleton<IExpressionManager>(expressionManager.Object);
                expressionManager.Setup(x => x.Evaluate(It.IsAny<IExecutionContext>(), It.IsAny<string>(), It.IsAny<bool>())).Throws(new Exception());

                // Arrange.
                var variableSets = new[]
                {
                    new[] { CreateStep(TaskResult.Succeeded, "success()") },
                    new[] { CreateStep(TaskResult.Succeeded, "success()") },
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
                var step1 = CreateStep(TaskResult.Succeeded, "success()", env: env1);

                _ec.Object.Result = null;

                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new[] { step1.Object }));

                // Act.
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert.
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);

#if OS_WINDOWS
                Assert.Equal("100", _ec.Object.ExpressionValues["env"].AssertDictionary("env")["env1"].AssertString("100"));
                Assert.Equal("github_actions", _ec.Object.ExpressionValues["env"].AssertDictionary("env")["env2"].AssertString("github_actions"));
#else
                Assert.Equal("100", _ec.Object.ExpressionValues["env"].AssertCaseSensitiveDictionary("env")["env1"].AssertString("100"));
                Assert.Equal("github_actions", _ec.Object.ExpressionValues["env"].AssertCaseSensitiveDictionary("env")["env2"].AssertString("github_actions"));
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
                var step1 = CreateStep(TaskResult.Succeeded, "success()", env: env1);

                var env2 = new MappingToken(null, null, null);
                env2.Add(new StringToken(null, null, null, "env1"), new StringToken(null, null, null, "1000"));
                env2.Add(new StringToken(null, null, null, "env3"), new BasicExpressionToken(null, null, null, "env.test"));
                var step2 = CreateStep(TaskResult.Succeeded, "success()", env: env2);

                _ec.Object.Result = null;

                _ec.Setup(x => x.JobSteps).Returns(new Queue<IStep>(new[] { step1.Object, step2.Object }));

                // Act.
                await _stepsRunner.RunAsync(jobContext: _ec.Object);

                // Assert.
                Assert.Equal(TaskResult.Succeeded, _ec.Object.Result ?? TaskResult.Succeeded);
#if OS_WINDOWS
                Assert.Equal("1000", _ec.Object.ExpressionValues["env"].AssertDictionary("env")["env1"].AssertString("1000"));
                Assert.Equal("github_actions", _ec.Object.ExpressionValues["env"].AssertDictionary("env")["env3"].AssertString("github_actions"));
                Assert.False(_ec.Object.ExpressionValues["env"].AssertDictionary("env").ContainsKey("env2"));
#else
                Assert.Equal("1000", _ec.Object.ExpressionValues["env"].AssertCaseSensitiveDictionary("env")["env1"].AssertString("1000"));
                Assert.Equal("github_actions", _ec.Object.ExpressionValues["env"].AssertCaseSensitiveDictionary("env")["env3"].AssertString("github_actions"));
                Assert.False(_ec.Object.ExpressionValues["env"].AssertCaseSensitiveDictionary("env").ContainsKey("env2"));
#endif
            }
        }

        private Mock<IActionRunner> CreateStep(TaskResult result, string condition, Boolean continueOnError = false, MappingToken env = null)
        {
            // Setup the step.
            var step = new Mock<IActionRunner>();
            step.Setup(x => x.Condition).Returns(condition);
            step.Setup(x => x.ContinueOnError).Returns(new BooleanToken(null, null, null, continueOnError));
            step.Setup(x => x.RunAsync()).Returns(Task.CompletedTask);
            step.Setup(x => x.Action)
                .Returns(new DistributedTask.Pipelines.ActionStep()
                {
                    Name = "Test",
                    Id = Guid.NewGuid(),
                    Environment = env
                });

            // Setup the step execution context.
            var stepContext = new Mock<IExecutionContext>();
            stepContext.SetupAllProperties();
            stepContext.Setup(x => x.Variables).Returns(_variables);
            stepContext.Setup(x => x.EnvironmentVariables).Returns(_env);
            stepContext.Setup(x => x.ExpressionValues).Returns(_contexts);
            stepContext.Setup(x => x.JobContext).Returns(_jobContext);
            stepContext.Setup(x => x.StepsContext).Returns(_stepContext);
            stepContext.Setup(x => x.Complete(It.IsAny<TaskResult?>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback((TaskResult? r, string currentOperation, string resultCode) =>
                {
                    if (r != null)
                    {
                        stepContext.Object.Result = r;
                    }
                });
            stepContext.Object.Result = result;
            step.Setup(x => x.ExecutionContext).Returns(stepContext.Object);

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
