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

            return hostContext;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void NodeVersionForceUpgradedIfServerFFIsOn()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = new Variables(hc, new Dictionary<string, VariableValue>() { { "DistributedTask.ForceGithubJavascriptActionsToNode16", "true" } }),
                });

                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node12";
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
                Assert.Equal("node12", handler.Data.NodeVersion);
            }
        }
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void NodeVersionForceUpgradedIfServerFFIsOn_WorkflowEnvOverridesMachineEnvOptOut()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();

                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = new Variables(hc, new Dictionary<string, VariableValue>() { { "DistributedTask.ForceGithubJavascriptActionsToNode16", "true" } }),
                    EnvironmentVariables = new Dictionary<string, string>() { { Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion, "false" } }
                });

                // Even though we have a machine env variable set to true, we still don't want to opt out because the workflow env variable is set to false
                Environment.SetEnvironmentVariable(Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion, "true");

                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node12";
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
                Assert.Equal("node12", handler.Data.NodeVersion);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void NodeVersionUnchangedIfServerFFIsOff()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();

                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node12";
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
                Assert.Equal("node12", handler.Data.NodeVersion);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void NodeVersionUnchangedIfWorkflowOptOut()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = new Variables(hc, new Dictionary<string, VariableValue>() { { "DistributedTask.ForceGithubJavascriptActionsToNode16", "true" } }),
                    EnvironmentVariables = new Dictionary<string, string>() { { Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion, "true" } }
                });

                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node12";
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
                Assert.Equal("node12", handler.Data.NodeVersion);
            }
        }
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void NodeVersionUnchangedIfMachineEnvOptOut()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange.
                var hf = new HandlerFactory();
                _ec.Setup(x => x.Global).Returns(new GlobalContext()
                {
                    Variables = new Variables(hc, new Dictionary<string, VariableValue>() { { "DistributedTask.ForceGithubJavascriptActionsToNode16", "true" } }),
                });

                Environment.SetEnvironmentVariable(Constants.Variables.Actions.AllowActionsUseUnsecureNodeVersion, "true");

                // Act.
                var data = new NodeJSActionExecutionData();
                data.NodeVersion = "node12";
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
                Assert.Equal("node12", handler.Data.NodeVersion);
            }
        }
    }
}
