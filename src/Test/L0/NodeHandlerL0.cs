using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class NodeHandlerL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void UseNodeForNodeHandlerEnvVarNotSet()
        {
            using (TestHostContext thc = CreateTestHostContext())
            {
                thc.SetSingleton(new WorkerCommandManager() as IWorkerCommandManager);
                thc.SetSingleton(new ExtensionManager() as IExtensionManager);
                thc.SetSingleton(new ActionCommandManager() as IActionCommandManager);

                NodeHandler nodeHandler = new NodeHandler();

                nodeHandler.Initialize(thc);
                nodeHandler.ExecutionContext = CreateTestExecutionContext(thc);
                nodeHandler.Data = new NodeHandlerData();

                string actualLocation = nodeHandler.GetNodeLocation();
                string expectedLocation = Path.Combine(thc.GetDirectory(WellKnownDirectory.Externals),
                    "node",
                    "bin",
                    $"node{IOUtil.ExeExtension}");
                Assert.Equal(expectedLocation, actualLocation);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void UseNode10ForNode10Handler()
        {
            using (TestHostContext thc = CreateTestHostContext())
            {
                thc.SetSingleton(new WorkerCommandManager() as IWorkerCommandManager);
                thc.SetSingleton(new ExtensionManager() as IExtensionManager);
                thc.SetSingleton(new ActionCommandManager() as IActionCommandManager);

                NodeHandler nodeHandler = new NodeHandler();

                nodeHandler.Initialize(thc);
                nodeHandler.ExecutionContext = CreateTestExecutionContext(thc);
                nodeHandler.Data = new Node10HandlerData();

                string actualLocation = nodeHandler.GetNodeLocation();
                string expectedLocation = Path.Combine(thc.GetDirectory(WellKnownDirectory.Externals),
                    "node10",
                    "bin",
                    $"node{IOUtil.ExeExtension}");
                Assert.Equal(expectedLocation, actualLocation);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void UseNode10ForNodeHandlerEnvVarSet()
        {
            try
            {
                Environment.SetEnvironmentVariable("AGENT_USE_NODE10", "true");

                using (TestHostContext thc = CreateTestHostContext())
                {
                    thc.SetSingleton(new WorkerCommandManager() as IWorkerCommandManager);
                    thc.SetSingleton(new ExtensionManager() as IExtensionManager);
                    thc.SetSingleton(new ActionCommandManager() as IActionCommandManager);

                    NodeHandler nodeHandler = new NodeHandler();

                    nodeHandler.Initialize(thc);
                    nodeHandler.ExecutionContext = CreateTestExecutionContext(thc);
                    nodeHandler.Data = new Node10HandlerData();

                    string actualLocation = nodeHandler.GetNodeLocation();
                    string expectedLocation = Path.Combine(thc.GetDirectory(WellKnownDirectory.Externals),
                        "node10",
                        "bin",
                        $"node{IOUtil.ExeExtension}");
                    Assert.Equal(expectedLocation, actualLocation);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("AGENT_USE_NODE10", null);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void UseNode10ForNodeHandlerHostContextVarSet()
        {
            using (TestHostContext thc = CreateTestHostContext())
            {
                thc.SetSingleton(new WorkerCommandManager() as IWorkerCommandManager);
                thc.SetSingleton(new ExtensionManager() as IExtensionManager);
                thc.SetSingleton(new ActionCommandManager() as IActionCommandManager);

                var variables = new Dictionary<string, VariableValue>();

                variables.Add("AGENT_USE_NODE10", new VariableValue("true"));

                NodeHandler nodeHandler = new NodeHandler();

                nodeHandler.Initialize(thc);
                nodeHandler.ExecutionContext = CreateTestExecutionContext(thc, variables);
                nodeHandler.Data = new NodeHandlerData();

                string actualLocation = nodeHandler.GetNodeLocation();
                string expectedLocation = Path.Combine(thc.GetDirectory(WellKnownDirectory.Externals),
                    "node10",
                    "bin",
                    $"node{IOUtil.ExeExtension}");
                Assert.Equal(expectedLocation, actualLocation);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void UseNode10ForNode10HandlerHostContextVarUnset()
        {
            using (TestHostContext thc = CreateTestHostContext())
            {
                thc.SetSingleton(new WorkerCommandManager() as IWorkerCommandManager);
                thc.SetSingleton(new ExtensionManager() as IExtensionManager);
                thc.SetSingleton(new ActionCommandManager() as IActionCommandManager);

                var variables = new Dictionary<string, VariableValue>();

                // Explicitly set 'AGENT_USE_NODE10' feature flag to false
                variables.Add("AGENT_USE_NODE10", new VariableValue("false"));

                NodeHandler nodeHandler = new NodeHandler();

                nodeHandler.Initialize(thc);
                nodeHandler.ExecutionContext = CreateTestExecutionContext(thc, variables);
                nodeHandler.Data = new Node10HandlerData();

                // Node10 handler is unaffected by the 'AGENT_USE_NODE10' feature flag, so folder name should be 'node10'
                string actualLocation = nodeHandler.GetNodeLocation();
                string expectedLocation = Path.Combine(thc.GetDirectory(WellKnownDirectory.Externals),
                    "node10",
                    "bin",
                    $"node{IOUtil.ExeExtension}");
                Assert.Equal(expectedLocation, actualLocation);
            }
        }

        private TestHostContext CreateTestHostContext([CallerMemberName] string testName = "")
        {
            return new TestHostContext(this, testName);
        }

        private IExecutionContext CreateTestExecutionContext(TestHostContext tc,
            Dictionary<string, VariableValue> variables = null)
        {
            var trace = tc.GetTrace();
            var executionContext = new Mock<IExecutionContext>();
            List<string> warnings;
            variables = variables ?? new Dictionary<string, VariableValue>();

            executionContext
               .Setup(x => x.Variables)
               .Returns(new Variables(tc, copy: variables, warnings: out warnings));

            return executionContext.Object;
        }
    }
}