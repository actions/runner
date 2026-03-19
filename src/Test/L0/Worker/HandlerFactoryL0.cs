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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void LocalNode20Action_TrackedWhenWarnFlagEnabled()
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

                // Local action: Name is empty, Path is the local path
                var actionRef = new RepositoryPathReference
                {
                    Name = "",
                    Path = "./.github/actions/my-action",
                    RepositoryType = "self"
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

                // Assert - local action should be tracked with its path
                Assert.Contains("./.github/actions/my-action", deprecatedActions);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Node20Action_TrackedAsUpgradedWhenUseNode24ByDefaultEnabled()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                var variables = new Dictionary<string, VariableValue>
                {
                    { Constants.Runner.NodeMigration.WarnOnNode20Flag, new VariableValue("true") },
                    { Constants.Runner.NodeMigration.UseNode24ByDefaultFlag, new VariableValue("true") }
                };
                Variables serverVariables = new(hc, variables);
                var deprecatedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var upgradedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>(),
                    DeprecatedNode20Actions = deprecatedActions,
                    UpgradedToNode24Actions = upgradedActions
                });

                var actionRef = new RepositoryPathReference
                {
                    Name = "actions/checkout",
                    Ref = "v4"
                };

                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node20";
                var handler = hf.Create(
                    _ec.Object,
                    actionRef,
                    new Mock<IStepHost>().Object,
                    data,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Variables(hc, new Dictionary<string, VariableValue>()),
                    "",
                    new List<JobExtensionRunner>()
                ) as INodeScriptActionHandler;

                // On non-ARM32 platforms, action should be upgraded to node24
                // and tracked in UpgradedToNode24Actions, NOT in DeprecatedNode20Actions
                bool isArm32Linux = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm &&
                                   System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

                if (!isArm32Linux)
                {
                    Assert.Equal("node24", handler.Data.NodeVersion);
                    Assert.Contains("actions/checkout@v4", upgradedActions);
                    Assert.DoesNotContain("actions/checkout@v4", deprecatedActions);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Node20Action_NotUpgradedWhenPhase1Only()
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
                var upgradedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>(),
                    DeprecatedNode20Actions = deprecatedActions,
                    UpgradedToNode24Actions = upgradedActions
                });

                var actionRef = new RepositoryPathReference
                {
                    Name = "actions/checkout",
                    Ref = "v4"
                };

                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node20";
                var handler = hf.Create(
                    _ec.Object,
                    actionRef,
                    new Mock<IStepHost>().Object,
                    data,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Variables(hc, new Dictionary<string, VariableValue>()),
                    "",
                    new List<JobExtensionRunner>()
                ) as INodeScriptActionHandler;

                // In Phase 1 (no UseNode24ByDefault), action stays on node20
                // and should be in DeprecatedNode20Actions
                Assert.Equal("node20", handler.Data.NodeVersion);
                Assert.Contains("actions/checkout@v4", deprecatedActions);
                Assert.Empty(upgradedActions);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExplicitNode24Action_KillArm32Flag_ThrowsOnArm32()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                var variables = new Dictionary<string, VariableValue>
                {
                    { Constants.Runner.NodeMigration.KillLinuxArm32Flag, new VariableValue("true") }
                };
                Variables serverVariables = new(hc, variables);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>()
                });

                var actionRef = new RepositoryPathReference
                {
                    Name = "actions/checkout",
                    Ref = "v5"
                };

                // Act - action explicitly declares node24
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node24";

                bool isArm32Linux = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm &&
                                   System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

                if (isArm32Linux)
                {
                    // On ARM32 Linux, kill flag should cause the handler to throw
                    Assert.Throws<InvalidOperationException>(() => hf.Create(
                        _ec.Object,
                        actionRef,
                        new Mock<IStepHost>().Object,
                        data,
                        new Dictionary<string, string>(),
                        new Dictionary<string, string>(),
                        new Variables(hc, new Dictionary<string, VariableValue>()),
                        "",
                        new List<JobExtensionRunner>()
                    ));
                }
                else
                {
                    // On other platforms, should proceed normally
                    var handler = hf.Create(
                        _ec.Object,
                        actionRef,
                        new Mock<IStepHost>().Object,
                        data,
                        new Dictionary<string, string>(),
                        new Dictionary<string, string>(),
                        new Variables(hc, new Dictionary<string, VariableValue>()),
                        "",
                        new List<JobExtensionRunner>()
                    ) as INodeScriptActionHandler;

                    Assert.Equal("node24", handler.Data.NodeVersion);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExplicitNode24Action_DeprecateArm32Flag_DowngradesToNode20OnArm32()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                var variables = new Dictionary<string, VariableValue>
                {
                    { Constants.Runner.NodeMigration.DeprecateLinuxArm32Flag, new VariableValue("true") }
                };
                Variables serverVariables = new(hc, variables);
                var arm32Actions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>(),
                    Arm32Node20Actions = arm32Actions
                });

                var actionRef = new RepositoryPathReference
                {
                    Name = "actions/checkout",
                    Ref = "v5"
                };

                // Act - action explicitly declares node24
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node24";
                var handler = hf.Create(
                    _ec.Object,
                    actionRef,
                    new Mock<IStepHost>().Object,
                    data,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Variables(hc, new Dictionary<string, VariableValue>()),
                    "",
                    new List<JobExtensionRunner>()
                ) as INodeScriptActionHandler;

                bool isArm32Linux = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm &&
                                   System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

                if (isArm32Linux)
                {
                    // On ARM32 Linux, should downgrade to node20 and track
                    Assert.Equal("node20", handler.Data.NodeVersion);
                    Assert.Contains("actions/checkout@v5", arm32Actions);
                }
                else
                {
                    // On other platforms, should remain node24
                    Assert.Equal("node24", handler.Data.NodeVersion);
                    Assert.Empty(arm32Actions);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExplicitNode24Action_NoArm32Flags_StaysNode24()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                var variables = new Dictionary<string, VariableValue>();
                Variables serverVariables = new(hc, variables);
                var arm32Actions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var deprecatedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>(),
                    Arm32Node20Actions = arm32Actions,
                    DeprecatedNode20Actions = deprecatedActions
                });

                var actionRef = new RepositoryPathReference
                {
                    Name = "actions/checkout",
                    Ref = "v5"
                };

                // Act - action explicitly declares node24, no ARM32 flags
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node24";
                var handler = hf.Create(
                    _ec.Object,
                    actionRef,
                    new Mock<IStepHost>().Object,
                    data,
                    new Dictionary<string, string>(),
                    new Dictionary<string, string>(),
                    new Variables(hc, new Dictionary<string, VariableValue>()),
                    "",
                    new List<JobExtensionRunner>()
                ) as INodeScriptActionHandler;

                // On non-ARM32 platforms, should stay node24 and not be tracked in any list
                bool isArm32Linux = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm &&
                                   System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

                if (!isArm32Linux)
                {
                    Assert.Equal("node24", handler.Data.NodeVersion);
                    Assert.Empty(arm32Actions);
                    Assert.Empty(deprecatedActions);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Node20Action_RequireNode24_ForcesNode24()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                var variables = new Dictionary<string, VariableValue>
                {
                    { Constants.Runner.NodeMigration.RequireNode24Flag, new VariableValue("true") },
                    { Constants.Runner.NodeMigration.WarnOnNode20Flag, new VariableValue("true") }
                };
                Variables serverVariables = new(hc, variables);
                var upgradedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var deprecatedActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>(),
                    UpgradedToNode24Actions = upgradedActions,
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

                bool isArm32Linux = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm &&
                                   System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

                if (!isArm32Linux)
                {
                    var handler = hf.Create(
                        _ec.Object,
                        actionRef,
                        new Mock<IStepHost>().Object,
                        data,
                        new Dictionary<string, string>(),
                        new Dictionary<string, string>(),
                        new Variables(hc, new Dictionary<string, VariableValue>()),
                        "",
                        new List<JobExtensionRunner>()
                    ) as INodeScriptActionHandler;

                    // Phase 3: RequireNode24 forces node24, ignoring env vars
                    Assert.Equal("node24", handler.Data.NodeVersion);
                    Assert.Contains("actions/checkout@v4", upgradedActions);
                    Assert.Empty(deprecatedActions);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Node20Action_KillArm32Flag_ThrowsOnArm32()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                var variables = new Dictionary<string, VariableValue>
                {
                    { Constants.Runner.NodeMigration.KillLinuxArm32Flag, new VariableValue("true") }
                };
                Variables serverVariables = new(hc, variables);

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = serverVariables,
                    EnvironmentVariables = new Dictionary<string, string>()
                });

                var actionRef = new RepositoryPathReference
                {
                    Name = "actions/checkout",
                    Ref = "v4"
                };

                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node20";

                bool isArm32Linux = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm &&
                                   System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

                if (isArm32Linux)
                {
                    Assert.Throws<InvalidOperationException>(() => hf.Create(
                        _ec.Object,
                        actionRef,
                        new Mock<IStepHost>().Object,
                        data,
                        new Dictionary<string, string>(),
                        new Dictionary<string, string>(),
                        new Variables(hc, new Dictionary<string, VariableValue>()),
                        "",
                        new List<JobExtensionRunner>()
                    ));
                }
                else
                {
                    // On non-ARM32, should proceed normally (node20 stays)
                    var handler = hf.Create(
                        _ec.Object,
                        actionRef,
                        new Mock<IStepHost>().Object,
                        data,
                        new Dictionary<string, string>(),
                        new Dictionary<string, string>(),
                        new Variables(hc, new Dictionary<string, VariableValue>()),
                        "",
                        new List<JobExtensionRunner>()
                    ) as INodeScriptActionHandler;

                    Assert.Equal("node20", handler.Data.NodeVersion);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ExplicitNode24Action_DeprecateArm32_UsesOriginalVersionForTracking()
        {
            // Regression test: verifies that when an action explicitly declares node24
            // and ARM32 deprecation downgrades it to node20, the tracking call uses
            // the original preferred version ("node24"), not the already-overwritten
            // nodeData.NodeVersion ("node20"). Without this fix, ShouldTrackAsArm32Node20
            // would receive (preferred="node20", final="node20") and never return true.
            string originalPreferred = "node24";
            string finalAfterArm32Downgrade = "node20";
            string deprecationWarning = "Linux ARM32 runners are deprecated and will no longer be supported after September 16th, 2026. Please migrate to a supported platform.";

            // Correct: use the original preferred version before assignment
            bool correctTracking = HandlerFactory.ShouldTrackAsArm32Node20(
                deprecateArm32: true,
                preferredNodeVersion: originalPreferred,
                finalNodeVersion: finalAfterArm32Downgrade,
                platformWarningMessage: deprecationWarning);
            Assert.True(correctTracking);

            // Bug scenario: if nodeData.NodeVersion was already overwritten to finalNodeVersion
            bool buggyTracking = HandlerFactory.ShouldTrackAsArm32Node20(
                deprecateArm32: true,
                preferredNodeVersion: finalAfterArm32Downgrade,
                finalNodeVersion: finalAfterArm32Downgrade,
                platformWarningMessage: deprecationWarning);
            Assert.False(buggyTracking);
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData(true, "node24", "node20", "Linux ARM32 runners are deprecated", true)]
        [InlineData(true, "node20", "node20", "Linux ARM32 runners are deprecated", false)]
        [InlineData(true, "node24", "node24", "Linux ARM32 runners are deprecated", false)]
        [InlineData(true, "node24", "node20", null, false)]
        [InlineData(false, "node24", "node20", "Linux ARM32 runners are deprecated", false)]
        public void ShouldTrackAsArm32Node20_ClassifiesOnlyPlatformDowngrades(
            bool deprecateArm32,
            string preferredNodeVersion,
            string finalNodeVersion,
            string platformWarningMessage,
            bool expected)
        {
            bool actual = HandlerFactory.ShouldTrackAsArm32Node20(
                deprecateArm32,
                preferredNodeVersion,
                finalNodeVersion,
                platformWarningMessage);

            Assert.Equal(expected, actual);
        }
    }
}
