using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class ActionManifestManagerL0
    {
        private CancellationTokenSource _ecTokenSource;
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_ContainerAction_Dockerfile()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "dockerfileaction.yml"));

                //Assert

                Assert.Equal(result.Name, "Hello World");
                Assert.Equal(result.Description, "Greet the world and record the time");
                Assert.Equal(result.Inputs.Count, 2);
                Assert.Equal(result.Inputs[0].Key.AssertString("key").Value, "greeting");
                Assert.Equal(result.Inputs[0].Value.AssertString("value").Value, "Hello");
                Assert.Equal(result.Inputs[1].Key.AssertString("key").Value, "entryPoint");
                Assert.Equal(result.Inputs[1].Value.AssertString("value").Value, "");

                Assert.Equal(result.Execution.ExecutionType, ActionExecutionType.Container);

                var containerAction = result.Execution as ContainerActionExecutionData;

                Assert.Equal(containerAction.Image, "Dockerfile");
                Assert.Equal(containerAction.EntryPoint, "main.sh");
                Assert.Equal(containerAction.Arguments[0].ToString(), "bzz");
                Assert.Equal(containerAction.Environment[0].Key.ToString(), "Token");
                Assert.Equal(containerAction.Environment[0].Value.ToString(), "foo");
                Assert.Equal(containerAction.Environment[1].Key.ToString(), "Url");
                Assert.Equal(containerAction.Environment[1].Value.ToString(), "bar");
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_ContainerAction_Dockerfile_Post()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "dockerfileaction_cleanup.yml"));

                //Assert

                Assert.Equal(result.Name, "Hello World");
                Assert.Equal(result.Description, "Greet the world and record the time");
                Assert.Equal(result.Inputs.Count, 2);
                Assert.Equal(result.Inputs[0].Key.AssertString("key").Value, "greeting");
                Assert.Equal(result.Inputs[0].Value.AssertString("value").Value, "Hello");
                Assert.Equal(result.Inputs[1].Key.AssertString("key").Value, "entryPoint");
                Assert.Equal(result.Inputs[1].Value.AssertString("value").Value, "");

                Assert.Equal(result.Execution.ExecutionType, ActionExecutionType.Container);

                var containerAction = result.Execution as ContainerActionExecutionData;

                Assert.Equal(containerAction.Image, "Dockerfile");
                Assert.Equal(containerAction.EntryPoint, "main.sh");
                Assert.Equal(containerAction.Cleanup, "cleanup.sh");
                Assert.Equal(containerAction.Arguments[0].ToString(), "bzz");
                Assert.Equal(containerAction.Environment[0].Key.ToString(), "Token");
                Assert.Equal(containerAction.Environment[0].Value.ToString(), "foo");
                Assert.Equal(containerAction.Environment[1].Key.ToString(), "Url");
                Assert.Equal(containerAction.Environment[1].Value.ToString(), "bar");
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_ContainerAction_NoArgsNoEnv()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "dockerfileaction_noargs_noenv_noentrypoint.yml"));

                //Assert
                Assert.Equal(result.Name, "Hello World");
                Assert.Equal(result.Description, "Greet the world and record the time");
                Assert.Equal(result.Inputs.Count, 2);
                Assert.Equal(result.Inputs[0].Key.AssertString("key").Value, "greeting");
                Assert.Equal(result.Inputs[0].Value.AssertString("value").Value, "Hello");
                Assert.Equal(result.Inputs[1].Key.AssertString("key").Value, "entryPoint");
                Assert.Equal(result.Inputs[1].Value.AssertString("value").Value, "");

                Assert.Equal(result.Execution.ExecutionType, ActionExecutionType.Container);

                var containerAction = result.Execution as ContainerActionExecutionData;

                Assert.Equal(containerAction.Image, "Dockerfile");
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_ContainerAction_Dockerfile_Expression()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "dockerfileaction_arg_env_expression.yml"));

                //Assert

                Assert.Equal(result.Name, "Hello World");
                Assert.Equal(result.Description, "Greet the world and record the time");
                Assert.Equal(result.Inputs.Count, 2);
                Assert.Equal(result.Inputs[0].Key.AssertString("key").Value, "greeting");
                Assert.Equal(result.Inputs[0].Value.AssertString("value").Value, "Hello");
                Assert.Equal(result.Inputs[1].Key.AssertString("key").Value, "entryPoint");
                Assert.Equal(result.Inputs[1].Value.AssertString("value").Value, "");

                Assert.Equal(result.Execution.ExecutionType, ActionExecutionType.Container);

                var containerAction = result.Execution as ContainerActionExecutionData;

                Assert.Equal(containerAction.Image, "Dockerfile");
                Assert.Equal(containerAction.EntryPoint, "main.sh");
                Assert.Equal(containerAction.Arguments[0].ToString(), "${{ inputs.greeting }}");
                Assert.Equal(containerAction.Environment[0].Key.ToString(), "Token");
                Assert.Equal(containerAction.Environment[0].Value.ToString(), "foo");
                Assert.Equal(containerAction.Environment[1].Key.ToString(), "Url");
                Assert.Equal(containerAction.Environment[1].Value.ToString(), "${{ inputs.entryPoint }}");
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_ContainerAction_DockerHub()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "dockerhubaction.yml"));

                //Assert
                Assert.Equal(result.Name, "Hello World");
                Assert.Equal(result.Description, "Greet the world and record the time");
                Assert.Equal(result.Inputs.Count, 2);
                Assert.Equal(result.Inputs[0].Key.AssertString("key").Value, "greeting");
                Assert.Equal(result.Inputs[0].Value.AssertString("value").Value, "Hello");
                Assert.Equal(result.Inputs[1].Key.AssertString("key").Value, "entryPoint");
                Assert.Equal(result.Inputs[1].Value.AssertString("value").Value, "");

                Assert.Equal(result.Execution.ExecutionType, ActionExecutionType.Container);

                var containerAction = result.Execution as ContainerActionExecutionData;

                Assert.Equal(containerAction.Image, "docker://ubuntu:18.04");
                Assert.Equal(containerAction.EntryPoint, "main.sh");
                Assert.Equal(containerAction.Arguments[0].ToString(), "bzz");
                Assert.Equal(containerAction.Environment[0].Key.ToString(), "Token");
                Assert.Equal(containerAction.Environment[0].Value.ToString(), "foo");
                Assert.Equal(containerAction.Environment[1].Key.ToString(), "Url");
                Assert.Equal(containerAction.Environment[1].Value.ToString(), "bar");
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_NodeAction()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "nodeaction.yml"));

                //Assert
                Assert.Equal(result.Name, "Hello World");
                Assert.Equal(result.Description, "Greet the world and record the time");
                Assert.Equal(result.Inputs.Count, 2);
                Assert.Equal(result.Inputs[0].Key.AssertString("key").Value, "greeting");
                Assert.Equal(result.Inputs[0].Value.AssertString("value").Value, "Hello");
                Assert.Equal(result.Inputs[1].Key.AssertString("key").Value, "entryPoint");
                Assert.Equal(result.Inputs[1].Value.AssertString("value").Value, "");
                Assert.Equal(result.Deprecated.Count, 1);

                Assert.True(result.Deprecated.ContainsKey("greeting"));
                result.Deprecated.TryGetValue("greeting", out string value);
                Assert.Equal(value, "This property has been deprecated");

                Assert.Equal(result.Execution.ExecutionType, ActionExecutionType.NodeJS);

                var nodeAction = result.Execution as NodeJSActionExecutionData;

                Assert.Equal(nodeAction.Script, "main.js");
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_NodeAction_Cleanup()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "nodeaction_cleanup.yml"));

                //Assert
                Assert.Equal(result.Name, "Hello World");
                Assert.Equal(result.Description, "Greet the world and record the time");
                Assert.Equal(result.Inputs.Count, 2);
                Assert.Equal(result.Inputs[0].Key.AssertString("key").Value, "greeting");
                Assert.Equal(result.Inputs[0].Value.AssertString("value").Value, "Hello");
                Assert.Equal(result.Inputs[1].Key.AssertString("key").Value, "entryPoint");
                Assert.Equal(result.Inputs[1].Value.AssertString("value").Value, "");
                Assert.Equal(result.Deprecated.Count, 1);

                Assert.True(result.Deprecated.ContainsKey("greeting"));
                result.Deprecated.TryGetValue("greeting", out string value);
                Assert.Equal(value, "This property has been deprecated");

                Assert.Equal(result.Execution.ExecutionType, ActionExecutionType.NodeJS);

                var nodeAction = result.Execution as NodeJSActionExecutionData;

                Assert.Equal(nodeAction.Script, "main.js");
                Assert.Equal(nodeAction.Cleanup, "cleanup.js");
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_PluginAction()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "pluginaction.yml"));

                //Assert
                Assert.Equal(result.Name, "Hello World");
                Assert.Equal(result.Description, "Greet the world and record the time");
                Assert.Equal(result.Inputs.Count, 2);
                Assert.Equal(result.Inputs[0].Key.AssertString("key").Value, "greeting");
                Assert.Equal(result.Inputs[0].Value.AssertString("value").Value, "Hello");
                Assert.Equal(result.Inputs[1].Key.AssertString("key").Value, "entryPoint");
                Assert.Equal(result.Inputs[1].Value.AssertString("value").Value, "");

                Assert.Equal(result.Execution.ExecutionType, ActionExecutionType.Plugin);

                var pluginAction = result.Execution as PluginActionExecutionData;

                Assert.Equal(pluginAction.Plugin, "someplugin");
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Evaluate_ContainerAction_Args()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                var arguments = new SequenceToken(null, null, null);
                arguments.Add(new BasicExpressionToken(null, null, null, "inputs.greeting"));
                arguments.Add(new StringToken(null, null, null, "test"));

                var inputsContext = new DictionaryContextData();
                inputsContext.Add("greeting", new StringContextData("hello"));

                var evaluateContext = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
                evaluateContext["inputs"] = inputsContext;
                //Act

                var result = actionManifest.EvaluateContainerArguments(_ec.Object, arguments, evaluateContext);

                //Assert
                Assert.Equal(result[0], "hello");
                Assert.Equal(result[1], "test");
                Assert.Equal(result.Count, 2);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Evaluate_ContainerAction_Env()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                var environment = new MappingToken(null, null, null);
                environment.Add(new StringToken(null, null, null, "hello"), new BasicExpressionToken(null, null, null, "inputs.greeting"));
                environment.Add(new StringToken(null, null, null, "test"), new StringToken(null, null, null, "test"));

                var inputsContext = new DictionaryContextData();
                inputsContext.Add("greeting", new StringContextData("hello"));

                var evaluateContext = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
                evaluateContext["inputs"] = inputsContext;

                //Act
                var result = actionManifest.EvaluateContainerEnvironment(_ec.Object, environment, evaluateContext);

                //Assert
                Assert.Equal(result["hello"], "hello");
                Assert.Equal(result["test"], "test");
                Assert.Equal(result.Count, 2);
            }
            finally
            {
                Teardown();
            }
        }

        private void Setup([CallerMemberName] string name = "")
        {
            _ecTokenSource?.Dispose();
            _ecTokenSource = new CancellationTokenSource();

            // Test host context.
            _hc = new TestHostContext(this, name);

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);
            _ec.Setup(x => x.Variables).Returns(new Variables(_hc, new Dictionary<string, VariableValue>()));
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { _hc.GetTrace().Info($"[{tag}]{message}"); });
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<string>())).Callback((Issue issue, string message) => { _hc.GetTrace().Info($"[{issue.Type}]{issue.Message ?? message}"); });
        }

        private void Teardown()
        {
            _hc?.Dispose();
        }
    }
}
