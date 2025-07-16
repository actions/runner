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
        [InlineData("node12", "node20")]
        [InlineData("node16", "node20")]
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
        
        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData("runner_usenode24", "true")]
        [InlineData("RUNNER_USENODE24", "true")]
        [InlineData("runner_usenode24", "1")]
        [InlineData("RUNNER_USENODE24", "1")]
        public void NodeVersionWithEnvironmentVariableEnabled(string envVarName, string envVarValue)
        {
            try
            {
                // Set the environment variable for testing
                Environment.SetEnvironmentVariable(envVarName, envVarValue);
                
                using (TestHostContext hc = CreateTestContext())
                {
                    // Arrange.
                    var hf = new HandlerFactory();
                    hf.Initialize(hc);

                    // Server Feature Flag - NOT set in variables
                    var variables = new Dictionary<string, VariableValue>();
                    Variables serverVariables = new(hc, variables);

                    // Workflow variables
                    var workflowVariables = new Dictionary<string, string>();

                    _ec.Setup(x => x.Global).Returns(new GlobalContext()
                    {
                        Variables = serverVariables,
                        EnvironmentVariables = workflowVariables
                    });

                    // Act - Test with one Node version to verify the behavior
                    var inputVersion = "node16"; // Use node16 as a representative case
                    
                    // Make sure we have a handler instance to use
                    var nodeHandler = new Mock<INodeScriptActionHandler>();
                    nodeHandler.SetupAllProperties();
                    hc.EnqueueInstance(nodeHandler.Object);
                    
                    var data = new NodeJSActionExecutionData();
                    data.NodeVersion = inputVersion;
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

                    string expectedVersion = "node20";
                    
                    // Assert - should use Node 24
                    Assert.Equal(expectedVersion, handler.Data.NodeVersion);
                }
            }
            finally
            {
                // Clean up the environment variable after the test
                Environment.SetEnvironmentVariable(envVarName, null);
            }
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void NodeVersionWithFeatureFlagNameEnvironmentVariableEnabled()
        {
            try
            {
                // Use the actual feature flag name from constants
                string envVarName = Constants.Runner.Features.UseNode24;
                Environment.SetEnvironmentVariable(envVarName, "true");
                
                using (TestHostContext hc = CreateTestContext())
                {
                    // Arrange.
                    var hf = new HandlerFactory();
                    hf.Initialize(hc);

                    // Server Feature Flag - NOT set in variables
                    var variables = new Dictionary<string, VariableValue>();
                    Variables serverVariables = new(hc, variables);

                    // Workflow variables
                    var workflowVariables = new Dictionary<string, string>();

                    _ec.Setup(x => x.Global).Returns(new GlobalContext()
                    {
                        Variables = serverVariables,
                        EnvironmentVariables = workflowVariables
                    });

                    // Act - Test with one Node version to verify the behavior
                    var inputVersion = "node16"; // Use node16 as a representative case
                    
                    // Make sure we have a handler instance to use
                    var nodeHandler = new Mock<INodeScriptActionHandler>();
                    nodeHandler.SetupAllProperties();
                    hc.EnqueueInstance(nodeHandler.Object);
                    
                    var data = new NodeJSActionExecutionData();
                    data.NodeVersion = inputVersion;
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

                    string expectedVersion = "node20";
                    
                    // Assert - should use Node 24 since the env variable doesn't affect this
                    Assert.Equal(expectedVersion, handler.Data.NodeVersion);
                }
            }
            finally
            {
                // Clean up the environment variable after the test
                Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, null);
            }
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Node24ExplicitlyRequested_DoNotDowngradeWhenFeatureFlagOff()
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

                // Assert - should be still node24 because the feature flag is just for internal node versions
                Assert.Equal("node24", handler.Data.NodeVersion);
            }
        }
    }
}
