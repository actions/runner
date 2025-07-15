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
        [InlineData("node24", "node20")]
        public void IsNodeVersionUpgraded(string inputVersion, string expectedVersion)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                // Server Feature Flag
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
        
        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData("node12", "node24")]
        [InlineData("node16", "node24")]
        [InlineData("node24", "node24")]
        public void NodeVersionWithFeatureFlagEnabled(string inputVersion, string expectedVersion)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                // Server Feature Flag - enable Node 24
                var variables = new Dictionary<string, VariableValue>
                {
                    { Constants.Runner.Features.UseNode24, new VariableValue("true") }
                };
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
        public void Node24ExplicitlyRequested_DowngradedWhenFeatureFlagOff()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                // Server Feature Flag - feature flag NOT set
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

                // Assert - should be downgraded to Node 20
                Assert.Equal("node20", handler.Data.NodeVersion);
            }
        }
    }
}
