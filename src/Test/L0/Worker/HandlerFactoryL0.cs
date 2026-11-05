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
            return hostContext;
        }

        public static IEnumerable<object[]> NodeVersions()
        {
            foreach (var version in new[] { "node12", "node16", "node20", "node24", "Node12", "Node16", "Node20", "Node24" })
            {
                // Old server flags and environment opt-outs cannot select a removed runtime.
                yield return new object[] { version, false, false, false };
                yield return new object[] { version, true, false, false };
                yield return new object[] { version, false, true, false };
                yield return new object[] { version, true, true, true };
            }
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [MemberData(nameof(NodeVersions))]
        public void JavaScriptActionsAlwaysUseNode24(string inputVersion, bool systemOptOut, bool workflowOptOut, bool oldServerFlags)
        {
            const string allowUnsecure = "ACTIONS_ALLOW_USE_UNSECURE_NODE_VERSION";
            const string forceNode24 = "FORCE_JAVASCRIPT_ACTIONS_TO_NODE24";
            var originalOptOut = Environment.GetEnvironmentVariable(allowUnsecure);
            var originalForce = Environment.GetEnvironmentVariable(forceNode24);
            try
            {
                Environment.SetEnvironmentVariable(allowUnsecure, systemOptOut ? "true" : null);
                Environment.SetEnvironmentVariable(forceNode24, oldServerFlags ? "true" : null);
                using (TestHostContext hc = CreateTestContext())
                {
                    var factory = new HandlerFactory();
                    factory.Initialize(hc);
                    var variables = new Dictionary<string, VariableValue>
                    {
                        { "actions.runner.usenode24bydefault", new VariableValue(oldServerFlags.ToString()) },
                        { "actions.runner.requirenode24", new VariableValue("false") },
                        { "actions.runner.warnonnode20", new VariableValue(oldServerFlags.ToString()) },
                        { "actions_runner_deprecate_linux_arm32", new VariableValue(oldServerFlags.ToString()) },
                        { "actions_runner_kill_linux_arm32", new VariableValue(oldServerFlags.ToString()) }
                    };
                    var environment = new Dictionary<string, string>();
                    if (workflowOptOut)
                    {
                        environment[allowUnsecure] = "true";
                    }
                    if (oldServerFlags)
                    {
                        environment[forceNode24] = "true";
                    }
                    _ec.Setup(x => x.Global).Returns(new GlobalContext()
                    {
                        Variables = new Variables(hc, variables),
                        EnvironmentVariables = environment
                    });
                    var data = new NodeJSActionExecutionData() { NodeVersion = inputVersion };
                    var stepHost = new Mock<IStepHost>().Object;
                    var inputs = new Dictionary<string, string>();
                    var runtimeVariables = new Variables(hc, variables);
                    var action = new RepositoryPathReference { Name = "actions/test", Ref = "v1" };

                    var handler = factory.Create(
                        _ec.Object, action, stepHost, data, inputs, environment,
                        runtimeVariables, "action-directory", new List<JobExtensionRunner>()) as INodeScriptActionHandler;

                    Assert.NotNull(handler);
                    Assert.Same(data, handler.Data);
                    Assert.Equal("node24", handler.Data.NodeVersion);
                    Assert.Same(action, handler.Action);
                    Assert.Same(stepHost, handler.StepHost);
                    Assert.Same(environment, handler.Environment);
                    Assert.Same(inputs, handler.Inputs);
                    Assert.Same(runtimeVariables, handler.RuntimeVariables);
                    Assert.Equal("action-directory", handler.ActionDirectory);
                    _ec.Verify(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<ExecutionContextLogOptions>()), Times.Never);
                    _ec.Verify(x => x.Write(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(allowUnsecure, originalOptOut);
                Environment.SetEnvironmentVariable(forceNode24, originalForce);
            }
        }
    }
}
