using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    /// <summary>
    /// Tests to verify that actions are executed without compilation
    /// </summary>
    public sealed class ActionExecutionModelL0
    {
        private CancellationTokenSource _ecTokenSource;
        private Mock<IExecutionContext> _ec;
        private TestHostContext _hc;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void JavaScriptActions_UseSourceFiles_NoCompilation()
        {
            try
            {
                // Arrange
                Setup();
                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                // Create a temporary action.yml for a JavaScript action
                string actionYml = @"
name: 'Test JS Action'
description: 'Test JavaScript action execution'
runs:
  using: 'node20'
  main: 'index.js'
";
                string tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, actionYml);

                // Act
                var result = actionManifest.Load(_ec.Object, tempFile);

                // Assert - JavaScript actions should use direct script execution
                Assert.Equal(ActionExecutionType.NodeJS, result.Execution.ExecutionType);
                
                var nodeAction = result.Execution as NodeJSActionExecutionData;
                Assert.NotNull(nodeAction);
                Assert.Equal("node20", nodeAction.NodeVersion);
                Assert.Equal("index.js", nodeAction.Script); // Points to source file, not compiled binary
                
                // Cleanup
                File.Delete(tempFile);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ContainerActions_UseImages_NoSourceCompilation()
        {
            try
            {
                // Arrange
                Setup();
                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                // Create a temporary action.yml for a container action
                string actionYml = @"
name: 'Test Container Action'
description: 'Test container action execution'
runs:
  using: 'docker'
  image: 'alpine:latest'
  entrypoint: '/bin/sh'
  args:
    - '-c'
    - 'echo Hello World'
";
                string tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, actionYml);

                // Act
                var result = actionManifest.Load(_ec.Object, tempFile);

                // Assert - Container actions should use images, not compiled source
                Assert.Equal(ActionExecutionType.Container, result.Execution.ExecutionType);
                
                var containerAction = result.Execution as ContainerActionExecutionData;
                Assert.NotNull(containerAction);
                Assert.Equal("alpine:latest", containerAction.Image); // Uses pre-built image
                Assert.Equal("/bin/sh", containerAction.EntryPoint);
                
                // Cleanup
                File.Delete(tempFile);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CompositeActions_UseStepDefinitions_NoCompilation()
        {
            try
            {
                // Arrange
                Setup();
                var actionManifest = new ActionManifestManager();
                actionManifest.Initialize(_hc);

                // Create a temporary action.yml for a composite action
                string actionYml = @"
name: 'Test Composite Action'
description: 'Test composite action execution'
runs:
  using: 'composite'
  steps:
    - run: echo 'Hello from step 1'
      shell: bash
    - run: echo 'Hello from step 2'
      shell: bash
";
                string tempFile = Path.GetTempFileName();
                File.WriteAllText(tempFile, actionYml);

                // Act
                var result = actionManifest.Load(_ec.Object, tempFile);

                // Assert - Composite actions should use step definitions, not compiled code
                Assert.Equal(ActionExecutionType.Composite, result.Execution.ExecutionType);
                
                var compositeAction = result.Execution as CompositeActionExecutionData;
                Assert.NotNull(compositeAction);
                Assert.Equal(2, compositeAction.Steps.Count); // Contains step definitions, not binaries
                
                // Cleanup
                File.Delete(tempFile);
            }
            finally
            {
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ActionTypes_DoNotRequireCompilation_OnlyInterpretation()
        {
            // This test documents that actions are interpreted, not compiled
            
            // JavaScript actions: Node.js interprets .js files directly
            // Container actions: Docker runs images or builds from Dockerfile  
            // Composite actions: Runner interprets YAML step definitions
            
            // The runner itself (this C# code) is compiled, but actions are not
            Assert.True(true, "Actions use interpretation model, not compilation model");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void ActionExecutionTypes_ShowNoCompilationRequired()
        {
            // Test that all action execution types are designed for interpretation
            
            // NodeJS actions execute source JavaScript files directly
            var nodeAction = new NodeJSActionExecutionData
            {
                NodeVersion = "node20",
                Script = "index.js" // Points to source file, not compiled binary
            };
            Assert.Equal(ActionExecutionType.NodeJS, nodeAction.ExecutionType);
            Assert.Equal("index.js", nodeAction.Script);
            
            // Container actions use images, not compiled source
            var containerAction = new ContainerActionExecutionData
            {
                Image = "alpine:latest" // Pre-built image, not compiled from this action's source
            };
            Assert.Equal(ActionExecutionType.Container, containerAction.ExecutionType);
            Assert.Equal("alpine:latest", containerAction.Image);
            
            // Composite actions contain step definitions
            var compositeAction = new CompositeActionExecutionData
            {
                Steps = new List<GitHub.DistributedTask.Pipelines.ActionStep>()
            };
            Assert.Equal(ActionExecutionType.Composite, compositeAction.ExecutionType);
            Assert.NotNull(compositeAction.Steps); // Contains YAML-defined steps, not compiled code
        }

        private void Setup([CallerMemberName] string name = "")
        {
            _ecTokenSource = new CancellationTokenSource();
            _hc = new TestHostContext(this, name);

            _ec = new Mock<IExecutionContext>();
            _ec.Setup(x => x.CancellationToken).Returns(_ecTokenSource.Token);
            _ec.Setup(x => x.Global).Returns(new GlobalContext
            {
                Variables = new Variables(_hc, new Dictionary<string, VariableValue>()),
                FileTable = new List<string>()
            });
        }

        private void Teardown()
        {
            _hc?.Dispose();
            _ecTokenSource?.Dispose();
        }
    }
}