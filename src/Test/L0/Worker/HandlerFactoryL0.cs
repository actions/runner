using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Moq;
using Xunit;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Handlers;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class HandlerFactoryL0
    {
        private Mock<IExecutionContext> _ec;
        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hostContext = new TestHostContext(this, testName);
            _ec = new Mock<IExecutionContext>();
            _ec.SetupAllProperties();
            _ec.Object.Initialize(hostContext);
            var handler = new Mock<INodeScriptActionHandler>();
            handler.SetupAllProperties();
            hostContext.EnqueueInstance(handler.Object);
            //hostContext.EnqueueInstance(new ActionCommandManager() as IActionCommandManager);

            return hostContext;
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData("node12", "node20")]
        [InlineData("node16", "node20")]
        [InlineData("node20", "node20")]
        [InlineData("node24", "node24")]
        public void IsNodeVersionUpgraded(string inputVersion, string expectedVersion)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                // Setup variables
                var variables = new Dictionary<string, VariableValue>();
                Variables serverVariables = new(hc, variables);

                // Workflow opt-out
                var workflowVariables = new Dictionary<string, string>();

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = workflowVariables
                });


                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = inputVersion;
                var handler = hf.Create(
                    _ec.Object,
                    new ScriptReference(),
                    new Mock<IStepHost>().Object,
                    data,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Variables(hc, new Dictionary<string, VariableValue>()), "", new List<JobExtensionRunner>()
                ) as INodeScriptActionHandler;

                // Assert.
                Assert.Equal(expectedVersion, handler.Data.NodeVersion);
            }
        }



        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Node24ExplicitlyRequested_HonoredByDefault()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                // Basic variables setup
                var variables = new Dictionary<string, VariableValue>();
                Variables serverVariables = new(hc, variables);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>()
                });

                // Act - Node 24 explicitly requested in action.yml
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node24";
                var handler = hf.Create(
                    _ec.Object,
                    new ScriptReference(),
                    new Mock<IStepHost>().Object,
                    data,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Variables(hc, new Dictionary<string, VariableValue>()),
                    "",
                    new List<JobExtensionRunner>()
                ) as INodeScriptActionHandler;

                // Assert - should be node24 as requested
                Assert.Equal("node24", handler.Data.NodeVersion);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Node20Action_TrackedWhenWarnFlagEnabled()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                var variables = new Dictionary<string, VariableValue>
                {
                    { Constants.Runner.NodeMigration.WarnOnNode20Flag, new VariableValue("true") }
                };
                Variables serverVariables = new(hc, variables);
                var deprecatedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>(),
                    DeprecatedNode20Actions = deprecatedActions
                });

                var actionRef = new RepositoryPathReference
                {
                    Name = "actions/checkout",
                    Ref = "v4"
                };

                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node20";
                hf.Create(
                    _ec.Object,
                    actionRef,
                    new Mock<IStepHost>().Object,
                    data,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Variables(hc, new Dictionary<string, VariableValue>()),
                    "",
                    new List<JobExtensionRunner>()
                );

                // Assert.
                Assert.Contains("actions/checkout@v4", deprecatedActions);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Node20Action_NotTrackedWhenWarnFlagDisabled()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                var variables = new Dictionary<string, VariableValue>();
                Variables serverVariables = new(hc, variables);
                var deprecatedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>(),
                    DeprecatedNode20Actions = deprecatedActions
                });

                var actionRef = new RepositoryPathReference
                {
                    Name = "actions/checkout",
                    Ref = "v4"
                };

                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node20";
                hf.Create(
                    _ec.Object,
                    actionRef,
                    new Mock<IStepHost>().Object,
                    data,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Variables(hc, new Dictionary<string, VariableValue>()),
                    "",
                    new List<JobExtensionRunner>()
                );

                // Assert - should not track when flag is disabled
                Assert.Empty(deprecatedActions);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Node24Action_NotTrackedEvenWhenWarnFlagEnabled()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                var variables = new Dictionary<string, VariableValue>
                {
                    { Constants.Runner.NodeMigration.WarnOnNode20Flag, new VariableValue("true") }
                };
                Variables serverVariables = new(hc, variables);
                var deprecatedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>(),
                    DeprecatedNode20Actions = deprecatedActions
                });

                var actionRef = new RepositoryPathReference
                {
                    Name = "actions/checkout",
                    Ref = "v5"
                };

                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node24";
                hf.Create(
                    _ec.Object,
                    actionRef,
                    new Mock<IStepHost>().Object,
                    data,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Variables(hc, new Dictionary<string, VariableValue>()),
                    "",
                    new List<JobExtensionRunner>()
                );

                // Assert - node24 actions should not be tracked
                Assert.Empty(deprecatedActions);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Node12Action_TrackedAsDeprecatedWhenWarnFlagEnabled()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                var variables = new Dictionary<string, VariableValue>
                {
                    { Constants.Runner.NodeMigration.WarnOnNode20Flag, new VariableValue("true") }
                };
                Variables serverVariables = new(hc, variables);
                var deprecatedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>(),
                    DeprecatedNode20Actions = deprecatedActions
                });

                var actionRef = new RepositoryPathReference
                {
                    Name = "some-org/old-action",
                    Ref = "v1"
                };

                // Act - node12 gets migrated to node20, then should be tracked
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node12";
                hf.Create(
                    _ec.Object,
                    actionRef,
                    new Mock<IStepHost>().Object,
                    data,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Variables(hc, new Dictionary<string, VariableValue>()),
                    "",
                    new List<JobExtensionRunner>()
                );

                // Assert - node12 gets migrated to node20 and should be tracked
                Assert.Contains("some-org/old-action@v1", deprecatedActions);
            }
        }
    }
}
