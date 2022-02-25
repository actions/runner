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
        [InlineData("node12", "", "", "", "node12")]
        [InlineData("node12", "true", "", "", "node16")]
        [InlineData("node12", "true", "", "true", "node12")]
        [InlineData("node12", "true", "true", "", "node12")]
        [InlineData("node12", "true", "true", "true", "node12")]
        [InlineData("node12", "true", "false", "true", "node16")] // workflow overrides env
        [InlineData("node16", "", "", "", "node16")]
        [InlineData("node16", "true", "", "", "node16")]
        [InlineData("node16", "true", "", "true", "node16")]
        [InlineData("node16", "true", "true", "", "node16")]
        [InlineData("node16", "true", "true", "true", "node16")]
        [InlineData("node16", "true", "false", "true", "node16")]
        public void IsNodeVersionUpgraded(string inputVersion, string serverFeatureFlag, string workflowOptOut, string machineOptOut, string expectedVersion)
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                hf.Initialize(hc);

                // Server Feature Flag
                var variables = new Dictionary<string, VariableValue>();
                if (!string.IsNullOrEmpty(serverFeatureFlag))
                {
                    variables["DistributedTask.ForceGithubJavascriptActionsToNode16"] = serverFeatureFlag;
                }
                Variables serverVariables = new Variables(hc, variables);

                // Workflow opt-out
                var workflowVariables = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(workflowOptOut))
                {
                    workflowVariables[Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion] = workflowOptOut;
                }

                // Machine opt-out
                if (!string.IsNullOrEmpty(machineOptOut))
                {
                    Environment.SetEnvironmentVariable(Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion, machineOptOut);
                }

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
                Environment.SetEnvironmentVariable(Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion, null);
            }
        }
    }
}
