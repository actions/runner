using GitHub.Actions.Expressions;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;
using GitHub.Actions.Expressions.Data;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using GitHub.Actions.WorkflowParser;
using LegacyContextData = GitHub.DistributedTask.Pipelines.ContextData;
using LegacyExpressions = GitHub.DistributedTask.Expressions2;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);

                Assert.Equal(ActionExecutionType.Container, result.Execution.ExecutionType);

                var containerAction = result.Execution as ContainerActionExecutionDataNew;

                Assert.Equal("Dockerfile", containerAction.Image);
                Assert.Equal("main.sh", containerAction.EntryPoint);
                Assert.Equal("bzz", containerAction.Arguments[0].ToString());
                Assert.Equal("Token", containerAction.Environment[0].Key.ToString());
                Assert.Equal("foo", containerAction.Environment[0].Value.ToString());
                Assert.Equal("Url", containerAction.Environment[1].Key.ToString());
                Assert.Equal("bar", containerAction.Environment[1].Value.ToString());
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_ContainerAction_Dockerfile_Pre()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "dockerfileaction_init.yml"));

                //Assert

                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);

                Assert.Equal(ActionExecutionType.Container, result.Execution.ExecutionType);

                var containerAction = result.Execution as ContainerActionExecutionDataNew;

                Assert.Equal("Dockerfile", containerAction.Image);
                Assert.Equal("main.sh", containerAction.EntryPoint);
                Assert.Equal("init.sh", containerAction.Pre);
                Assert.Equal("success()", containerAction.InitCondition);
                Assert.Equal("bzz", containerAction.Arguments[0].ToString());
                Assert.Equal("Token", containerAction.Environment[0].Key.ToString());
                Assert.Equal("foo", containerAction.Environment[0].Value.ToString());
                Assert.Equal("Url", containerAction.Environment[1].Key.ToString());
                Assert.Equal("bar", containerAction.Environment[1].Value.ToString());
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

                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);

                Assert.Equal(ActionExecutionType.Container, result.Execution.ExecutionType);

                var containerAction = result.Execution as ContainerActionExecutionDataNew;

                Assert.Equal("Dockerfile", containerAction.Image);
                Assert.Equal("main.sh", containerAction.EntryPoint);
                Assert.Equal("cleanup.sh", containerAction.Post);
                Assert.Equal("failure()", containerAction.CleanupCondition);
                Assert.Equal("bzz", containerAction.Arguments[0].ToString());
                Assert.Equal("Token", containerAction.Environment[0].Key.ToString());
                Assert.Equal("foo", containerAction.Environment[0].Value.ToString());
                Assert.Equal("Url", containerAction.Environment[1].Key.ToString());
                Assert.Equal("bar", containerAction.Environment[1].Value.ToString());
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_ContainerAction_Dockerfile_Pre_DefaultCondition()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "dockerfileaction_init_default.yml"));

                //Assert

                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);

                Assert.Equal(ActionExecutionType.Container, result.Execution.ExecutionType);

                var containerAction = result.Execution as ContainerActionExecutionDataNew;

                Assert.Equal("Dockerfile", containerAction.Image);
                Assert.Equal("main.sh", containerAction.EntryPoint);
                Assert.Equal("init.sh", containerAction.Pre);
                Assert.Equal("always()", containerAction.InitCondition);
                Assert.Equal("bzz", containerAction.Arguments[0].ToString());
                Assert.Equal("Token", containerAction.Environment[0].Key.ToString());
                Assert.Equal("foo", containerAction.Environment[0].Value.ToString());
                Assert.Equal("Url", containerAction.Environment[1].Key.ToString());
                Assert.Equal("bar", containerAction.Environment[1].Value.ToString());
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_ContainerAction_Dockerfile_Post_DefaultCondition()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "dockerfileaction_cleanup_default.yml"));

                //Assert

                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);

                Assert.Equal(ActionExecutionType.Container, result.Execution.ExecutionType);

                var containerAction = result.Execution as ContainerActionExecutionDataNew;

                Assert.Equal("Dockerfile", containerAction.Image);
                Assert.Equal("main.sh", containerAction.EntryPoint);
                Assert.Equal("cleanup.sh", containerAction.Post);
                Assert.Equal("always()", containerAction.CleanupCondition);
                Assert.Equal("bzz", containerAction.Arguments[0].ToString());
                Assert.Equal("Token", containerAction.Environment[0].Key.ToString());
                Assert.Equal("foo", containerAction.Environment[0].Value.ToString());
                Assert.Equal("Url", containerAction.Environment[1].Key.ToString());
                Assert.Equal("bar", containerAction.Environment[1].Value.ToString());
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
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);

                Assert.Equal(ActionExecutionType.Container, result.Execution.ExecutionType);

                var containerAction = result.Execution as ContainerActionExecutionDataNew;

                Assert.Equal("Dockerfile", containerAction.Image);
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

                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);

                Assert.Equal(ActionExecutionType.Container, result.Execution.ExecutionType);

                var containerAction = result.Execution as ContainerActionExecutionDataNew;

                Assert.Equal("Dockerfile", containerAction.Image);
                Assert.Equal("main.sh", containerAction.EntryPoint);
                Assert.Equal("${{ inputs.greeting }}", containerAction.Arguments[0].ToString());
                Assert.Equal("Token", containerAction.Environment[0].Key.ToString());
                Assert.Equal("foo", containerAction.Environment[0].Value.ToString());
                Assert.Equal("Url", containerAction.Environment[1].Key.ToString());
                Assert.Equal("${{ inputs.entryPoint }}", containerAction.Environment[1].Value.ToString());
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
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);

                Assert.Equal(ActionExecutionType.Container, result.Execution.ExecutionType);

                var containerAction = result.Execution as ContainerActionExecutionDataNew;

                Assert.Equal("docker://ubuntu:18.04", containerAction.Image);
                Assert.Equal("main.sh", containerAction.EntryPoint);
                Assert.Equal("bzz", containerAction.Arguments[0].ToString());
                Assert.Equal("Token", containerAction.Environment[0].Key.ToString());
                Assert.Equal("foo", containerAction.Environment[0].Value.ToString());
                Assert.Equal("Url", containerAction.Environment[1].Key.ToString());
                Assert.Equal("bar", containerAction.Environment[1].Value.ToString());
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
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);
                Assert.Equal(1, result.Deprecated.Count);

                Assert.True(result.Deprecated.ContainsKey("greeting"));
                result.Deprecated.TryGetValue("greeting", out string value);
                Assert.Equal("This property has been deprecated", value);

                Assert.Equal(ActionExecutionType.NodeJS, result.Execution.ExecutionType);

                var nodeAction = result.Execution as NodeJSActionExecutionData;

                Assert.Equal("main.js", nodeAction.Script);
                Assert.Equal("node12", nodeAction.NodeVersion);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_Node16Action()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "node16action.yml"));

                //Assert
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);
                Assert.Equal(1, result.Deprecated.Count);

                Assert.True(result.Deprecated.ContainsKey("greeting"));
                result.Deprecated.TryGetValue("greeting", out string value);
                Assert.Equal("This property has been deprecated", value);

                Assert.Equal(ActionExecutionType.NodeJS, result.Execution.ExecutionType);

                var nodeAction = result.Execution as NodeJSActionExecutionData;

                Assert.Equal("main.js", nodeAction.Script);
                Assert.Equal("node16", nodeAction.NodeVersion);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_Node20Action()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "node20action.yml"));

                //Assert
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);
                Assert.Equal(1, result.Deprecated.Count);

                Assert.True(result.Deprecated.ContainsKey("greeting"));
                result.Deprecated.TryGetValue("greeting", out string value);
                Assert.Equal("This property has been deprecated", value);

                Assert.Equal(ActionExecutionType.NodeJS, result.Execution.ExecutionType);

                var nodeAction = result.Execution as NodeJSActionExecutionData;

                Assert.Equal("main.js", nodeAction.Script);
                Assert.Equal("node20", nodeAction.NodeVersion);
            }
            finally
            {
                Teardown();
            }
        }

         [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_Node24Action()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "node24action.yml"));

                //Assert
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);
                Assert.Equal(1, result.Deprecated.Count);

                Assert.True(result.Deprecated.ContainsKey("greeting"));
                result.Deprecated.TryGetValue("greeting", out string value);
                Assert.Equal("This property has been deprecated", value);

                Assert.Equal(ActionExecutionType.NodeJS, result.Execution.ExecutionType);

                var nodeAction = result.Execution as NodeJSActionExecutionData;

                Assert.Equal("main.js", nodeAction.Script);
                Assert.Equal("node24", nodeAction.NodeVersion);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_NodeAction_Pre()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "nodeaction_init.yml"));

                //Assert
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);
                Assert.Equal(1, result.Deprecated.Count);

                Assert.True(result.Deprecated.ContainsKey("greeting"));
                result.Deprecated.TryGetValue("greeting", out string value);
                Assert.Equal("This property has been deprecated", value);

                Assert.Equal(ActionExecutionType.NodeJS, result.Execution.ExecutionType);

                var nodeAction = result.Execution as NodeJSActionExecutionData;

                Assert.Equal("main.js", nodeAction.Script);
                Assert.Equal("init.js", nodeAction.Pre);
                Assert.Equal("cancelled()", nodeAction.InitCondition);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_NodeAction_Init_DefaultCondition()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "nodeaction_init_default.yml"));

                //Assert
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);
                Assert.Equal(1, result.Deprecated.Count);

                Assert.True(result.Deprecated.ContainsKey("greeting"));
                result.Deprecated.TryGetValue("greeting", out string value);
                Assert.Equal("This property has been deprecated", value);

                Assert.Equal(ActionExecutionType.NodeJS, result.Execution.ExecutionType);

                var nodeAction = result.Execution as NodeJSActionExecutionData;

                Assert.Equal("main.js", nodeAction.Script);
                Assert.Equal("init.js", nodeAction.Pre);
                Assert.Equal("always()", nodeAction.InitCondition);
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
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);
                Assert.Equal(1, result.Deprecated.Count);

                Assert.True(result.Deprecated.ContainsKey("greeting"));
                result.Deprecated.TryGetValue("greeting", out string value);
                Assert.Equal("This property has been deprecated", value);

                Assert.Equal(ActionExecutionType.NodeJS, result.Execution.ExecutionType);

                var nodeAction = result.Execution as NodeJSActionExecutionData;

                Assert.Equal("main.js", nodeAction.Script);
                Assert.Equal("cleanup.js", nodeAction.Post);
                Assert.Equal("cancelled()", nodeAction.CleanupCondition);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_NodeAction_Cleanup_DefaultCondition()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "nodeaction_cleanup_default.yml"));

                //Assert
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);
                Assert.Equal(1, result.Deprecated.Count);

                Assert.True(result.Deprecated.ContainsKey("greeting"));
                result.Deprecated.TryGetValue("greeting", out string value);
                Assert.Equal("This property has been deprecated", value);

                Assert.Equal(ActionExecutionType.NodeJS, result.Execution.ExecutionType);

                var nodeAction = result.Execution as NodeJSActionExecutionData;

                Assert.Equal("main.js", nodeAction.Script);
                Assert.Equal("cleanup.js", nodeAction.Post);
                Assert.Equal("always()", nodeAction.CleanupCondition);
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
                Assert.Equal("Hello World", result.Name);
                Assert.Equal("Greet the world and record the time", result.Description);
                Assert.Equal(2, result.Inputs.Count);
                Assert.Equal("greeting", result.Inputs[0].Key.AssertString("key").Value);
                Assert.Equal("Hello", result.Inputs[0].Value.AssertString("value").Value);
                Assert.Equal("entryPoint", result.Inputs[1].Key.AssertString("key").Value);
                Assert.Equal("", result.Inputs[1].Value.AssertString("value").Value);

                Assert.Equal(ActionExecutionType.Plugin, result.Execution.ExecutionType);

                var pluginAction = result.Execution as PluginActionExecutionData;

                Assert.Equal("someplugin", pluginAction.Plugin);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_ConditionalCompositeAction()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                //Act
                var result = actionManifest.Load(_ec.Object, Path.Combine(TestUtil.GetTestDataPath(), "conditional_composite_action.yml"));

                //Assert
                Assert.Equal("Conditional Composite", result.Name);
                Assert.Equal(ActionExecutionType.Composite, result.Execution.ExecutionType);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Load_CompositeActionNoUsing()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);
                var action_path = Path.Combine(TestUtil.GetTestDataPath(), "composite_action_without_using_token.yml");

                //Assert
                var err = Assert.Throws<ArgumentException>(() => actionManifest.Load(_ec.Object, action_path));
                Assert.Contains($"Failed to load {action_path}", err.Message);
                _ec.Verify(x => x.AddIssue(It.Is<Issue>(s => s.Message.Contains("Missing 'using' value. 'using' requires 'composite', 'docker', 'node12', 'node16', 'node20' or 'node24'.")), It.IsAny<ExecutionContextLogOptions>()), Times.Once);
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

                var inputsContext = new DictionaryExpressionData();
                inputsContext.Add("greeting", new StringExpressionData("hello"));

                var evaluateContext = new Dictionary<string, ExpressionData>(StringComparer.OrdinalIgnoreCase);
                evaluateContext["inputs"] = inputsContext;
                //Act

                var result = actionManifest.EvaluateContainerArguments(_ec.Object, arguments, evaluateContext);

                //Assert
                Assert.Equal("hello", result[0]);
                Assert.Equal("test", result[1]);
                Assert.Equal(2, result.Count);
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

                var inputsContext = new DictionaryExpressionData();
                inputsContext.Add("greeting", new StringExpressionData("hello"));

                var evaluateContext = new Dictionary<string, ExpressionData>(StringComparer.OrdinalIgnoreCase);
                evaluateContext["inputs"] = inputsContext;

                //Act
                var result = actionManifest.EvaluateContainerEnvironment(_ec.Object, environment, evaluateContext);

                //Assert
                Assert.Equal("hello", result["hello"]);
                Assert.Equal("test", result["test"]);
                Assert.Equal(2, result.Count);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Evaluate_Default_Input()
        {
            try
            {
                //Arrange
                Setup();

                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                _ec.Object.ExpressionValues["github"] = new LegacyContextData.DictionaryContextData
                {
                    { "ref", new LegacyContextData.StringContextData("refs/heads/main") },
                };
                _ec.Object.ExpressionValues["strategy"] = new LegacyContextData.DictionaryContextData();
                _ec.Object.ExpressionValues["matrix"] = new LegacyContextData.DictionaryContextData();
                _ec.Object.ExpressionValues["steps"] = new LegacyContextData.DictionaryContextData();
                _ec.Object.ExpressionValues["job"] = new LegacyContextData.DictionaryContextData();
                _ec.Object.ExpressionValues["runner"] = new LegacyContextData.DictionaryContextData();
                _ec.Object.ExpressionValues["env"] = new LegacyContextData.DictionaryContextData();
                _ec.Object.ExpressionFunctions.Add(new LegacyExpressions.FunctionInfo<GitHub.Runner.Worker.Expressions.HashFilesFunction>("hashFiles", 1, 255));

                //Act
                var result = actionManifest.EvaluateDefaultInput(_ec.Object, "testInput", new StringToken(null, null, null, "defaultValue"));

                //Assert
                Assert.Equal("defaultValue", result);

                //Act
                result = actionManifest.EvaluateDefaultInput(_ec.Object, "testInput", new BasicExpressionToken(null, null, null, "github.ref"));

                //Assert
                Assert.Equal("refs/heads/main", result);
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

            var expressionValues = new LegacyContextData.DictionaryContextData();
            var expressionFunctions = new List<LegacyExpressions.IFunctionInfo>();

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.Global)
                .Returns(new GlobalContext
                {
                    FileTable = new List<String>(),
                    Variables = new Variables(_hc, new Dictionary<string, VariableValue>()),
                    WriteDebug = true,
                });
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);
            _ec.Setup(x => x.ExpressionValues).Returns(expressionValues);
            _ec.Setup(x => x.ExpressionFunctions).Returns(expressionFunctions);
            _ec.Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>())).Callback((string tag, string message) => { _hc.GetTrace().Info($"{tag}{message}"); });
            _ec.Setup(x => x.AddIssue(It.IsAny<Issue>(), It.IsAny<ExecutionContextLogOptions>())).Callback((Issue issue, ExecutionContextLogOptions logOptions) => { _hc.GetTrace().Info($"[{issue.Type}]{logOptions.LogMessageOverride ?? issue.Message}"); });
        }

        private void Teardown()
        {
            _hc?.Dispose();
        }
    }
}
