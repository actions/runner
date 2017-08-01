using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Xunit;
using YamlDotNet.Core;
using Yaml = Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener
{
    public sealed class PipelineParserL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void TaskStep()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String expected = @"
steps:
- task: myTask@1
- task: myOtherTask@2
  name: Fancy task
  enabled: false
  condition: always()
  continueOnError: true
  timeoutInMinutes: 123
  inputs:
    myInput: input value
  env:
    MY_VAR: val
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "taskStep.yml")] = expected;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "taskStep.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void ScriptStep()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String expected = @"
steps:
- script: echo hello from script 1
- script: echo hello from script 2
  name: Fancy script
  enabled: false
  condition: always()
  continueOnError: true
  timeoutInMinutes: 123
  failOnStderr: $(failOnStderrVariable)
  workingDirectory: $(workingDirectoryVariable)
  env:
    MY_VAR: value
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "scriptStep.yml")] = expected;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "scriptStep.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void BashStep()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String expected = @"
steps:
- bash: echo hello from bash
- bash: echo hello again from bash
  name: Fancy script
  enabled: false
  condition: always()
  continueOnError: true
  timeoutInMinutes: 123
  failOnStderr: $(failOnStderrVariable)
  workingDirectory: $(workingDirectoryVariable)
  env:
    MY_VAR: value
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "bashStep.yml")] = expected;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "bashStep.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void PowerShellStep()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String expected = @"
steps:
- powershell: write-host 'hello from powershell'
- powershell: write-host 'hello again from powershell'
  name: Fancy script
  enabled: false
  condition: always()
  continueOnError: true
  timeoutInMinutes: 123
  errorActionPreference: $(errorActionPreferenceVariable)
  failOnStderr: $(failOnStderrVariable)
  ignoreLASTEXITCODE: $(ignoreLASTEXITCODEVariable)
  workingDirectory: $(workingDirectoryVariable)
  env:
    MY_VAR: value
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "powershellStep.yml")] = expected;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "powershellStep.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void CheckoutStep()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String expected = @"
phases:
- name: phase1
  steps:
  - checkout: none
- name: phase2
  steps:
  - checkout: self
- name: phase3
  steps:
  - checkout: self
    clean: $(cleanVariable)
    fetchDepth: $(fetchDepthVariable)
    lfs: $(fetchDepthVariable)
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "checkoutStep.yml")] = expected;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "checkoutStep.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void CheckoutStep_RepoDefined()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String expected = @"
resources:
- repo: self
  clean: true
phases:
- name: phase1
  steps:
  - checkout: none
- name: phase2
  steps:
  - checkout: self
- name: phase3
  steps:
  - checkout: self
    clean: $(cleanVariable)
    fetchDepth: $(fetchDepthVariable)
    lfs: $(fetchDepthVariable)
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "checkoutStep_repoDefined.yml")] = expected;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "checkoutStep_repoDefined.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void Phase()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String expected = @"
phases:
- name: phase1
  steps:
  - script: echo hello
- name: phase2
  dependsOn: phase1
  condition: always()
  continueOnError: $(continueOnErrorVariable)
  enableAccessToken: $(enableAccessTokenVariable)
  target:
    queue: myQueue
  execution:
    continueOnError: $(executionContinueOnErrorVariable)
  variables:
    var1: val1
  steps:
  - script: echo hello
- name: phase3
  dependsOn:
  - phase1
  - phase2
  steps:
  - script: echo hello
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "phase.yml")] = expected;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "phase.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void PhaseTarget()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String expected = @"
phases:
- name: buildPhase1
  target:
    queue: myQueue
  steps:
  - script: echo hello
- name: buildPhase2
  target:
    demands: myDemand
  steps:
  - script: echo hello
- name: buildPhase3
  target:
    queue: myQueue
    demands:
    - myDemand1
    - myDemand2
  steps:
  - script: echo hello
- name: deployPhase1
  target:
    deploymentGroup: myDeploymentGroup
  steps:
  - script: echo hello
- name: deployPhase2
  target:
    deploymentGroup: myDeploymentGroup
    tags: myTag
  steps:
  - script: echo hello
- name: deployPhase3
  target:
    deploymentGroup: myDeploymentGroup
    healthOption: percentage
    percentage: 50
    tags:
    - myTag1
    - myTag2
  steps:
  - script: echo hello
- name: serverPhase
  target: server
  steps:
  - task: myServerTask@1
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "phaseTarget.yml")] = expected;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "phaseTarget.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void PhaseExecution()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String expected = @"
phases:
- name: phase1
  continueOnError: $(phaseContinueOnErrorVariable)
  execution:
    continueOnError: $(executionContinueOnErrorVariable)
  steps:
  - script: echo hello
- name: phase2
  execution:
    maxConcurrency: $(maxConcurrencyVariable)
  steps:
  - script: echo hello
- name: phase3
  execution:
    continueOnError: true
    maxConcurrency: $(maxConcurrencyVariable)
    timeoutInMinutes: $(timeoutInMinutesVariable)
    matrix:
      x86_debug:
        arch: x86
        config: debug
      x64_release:
        arch: x64
        config: release
  steps:
  - script: echo hello
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "phaseExecution.yml")] = expected;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "phaseExecution.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void PhaseVariables_Simple()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String expected = @"
variables:
  var1: val1
steps:
- script: echo hello
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "phaseVariables_simple.yml")] = expected;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "phaseVariables_simple.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void PhaseVariables_NameValue()
        {
            using (CreateTestContext())
            {
                // Arrange.
                String content = @"
variables:
- name: var1
  value: val1
steps:
- script: echo hello
";
                String expected = @"
variables:
  var1: val1
steps:
- script: echo hello
";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "phaseVariables_nameValue.yml")] = content;

                // Act.
                String actual = m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "phaseVariables_nameValue.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Assert.
                Assert.Equal(expected.Trim(), actual.Trim());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void MaxObjectDepth_Mapping()
        {
            using (CreateTestContext())
            {
                // Arrange - sanity test allowed threshold
                String contentFormat = @"
resources:
- endpoint: someEndpoint
  myProperty: {0}";
                String allowedObject = "{a: {a: {a: {a: {a: {a: {a: {a: {a: {a: \"b\"} } } } } } } } } }";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "maxObjectDepth_mapping_allowed.yml")] = String.Format(CultureInfo.InvariantCulture, contentFormat, allowedObject);
                m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "maxObjectDepth_mapping_allowed.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Arrange - setup exceeding threshold
                String unallowedObject = "{a: " + allowedObject + " }";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "maxObjectDepth_mapping_unallowed.yml")] = String.Format(CultureInfo.InvariantCulture, contentFormat, unallowedObject);

                try
                {
                    // Act.
                    m_pipelineParser.DeserializeAndSerialize(
                        c_defaultRoot,
                        "maxObjectDepth_mapping_unallowed.yml",
                        mustacheContext: null,
                        cancellationToken: CancellationToken.None);

                    // Assert.
                    Assert.True(false, "Should have thrown syntax error exception");
                }
                catch (SyntaxErrorException ex)
                {
                    // Assert.
                    Assert.Contains("Max object depth of 10 exceeded", ex.Message);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void MaxObjectDepth_Sequence()
        {
            using (CreateTestContext())
            {
                // Arrange - sanity test allowed threshold
                String contentFormat = @"
resources:
- endpoint: someEndpoint
  myProperty: {0}";
                String allowedObject = "[ [ [ [ [ [ [ [ [ [ \"a\" ] ] ] ] ] ] ] ] ] ]";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "maxObjectDepth_sequence_allowed.yml")] = String.Format(CultureInfo.InvariantCulture, contentFormat, allowedObject);
                m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "maxObjectDepth_sequence_allowed.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Arrange - setup exceeding threshold
                String unallowedObject = "[ " + allowedObject + " ]";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "maxObjectDepth_sequence_unallowed.yml")] = String.Format(CultureInfo.InvariantCulture, contentFormat, unallowedObject);

                try
                {
                    // Act.
                    m_pipelineParser.DeserializeAndSerialize(
                        c_defaultRoot,
                        "maxObjectDepth_sequence_unallowed.yml",
                        mustacheContext: null,
                        cancellationToken: CancellationToken.None);

                    // Assert.
                    Assert.True(false, "Should have thrown syntax error exception");
                }
                catch (SyntaxErrorException ex)
                {
                    // Assert.
                    Assert.Contains("Max object depth of 10 exceeded", ex.Message);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void MaxObjectDepth_Mixed()
        {
            using (CreateTestContext())
            {
                // Arrange - sanity test allowed threshold
                String contentFormat = @"
resources:
- endpoint: someEndpoint
  myProperty: {0}";
                String allowedObject = "{a: [ {a: [ {a: [ {a: [ {a: [ \"a\" ] } ] } ] } ] } ] }";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "maxObjectDepth_mixed_allowed.yml")] = String.Format(CultureInfo.InvariantCulture, contentFormat, allowedObject);
                m_pipelineParser.DeserializeAndSerialize(
                    c_defaultRoot,
                    "maxObjectDepth_mixed_allowed.yml",
                    mustacheContext: null,
                    cancellationToken: CancellationToken.None);

                // Arrange - setup exceeding threshold
                String unallowedObject = "[ " + allowedObject + " ]";
                m_fileProvider.FileContent[Path.Combine(c_defaultRoot, "maxObjectDepth_mixed_unallowed.yml")] = String.Format(CultureInfo.InvariantCulture, contentFormat, unallowedObject);

                try
                {
                    // Act.
                    m_pipelineParser.DeserializeAndSerialize(
                        c_defaultRoot,
                        "maxObjectDepth_mixed_unallowed.yml",
                        mustacheContext: null,
                        cancellationToken: CancellationToken.None);

                    // Assert.
                    Assert.True(false, "Should have thrown syntax error exception");
                }
                catch (SyntaxErrorException ex)
                {
                    // Assert.
                    Assert.Contains("Max object depth of 10 exceeded", ex.Message);
                }
            }
        }

        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            TestHostContext hc = new TestHostContext(this, testName);
            m_fileProvider = new YamlFileProvider();
            m_pipelineParser = new Yaml.PipelineParser(
                new YamlTraceWriter(hc),
                m_fileProvider,
                new Yaml.ParseOptions()
                {
                    MaxFiles = 10,
                    MustacheEvaluationMaxResultLength = 512 * 1024, // 512k string length
                    MustacheEvaluationTimeout = TimeSpan.FromSeconds(10),
                    MustacheMaxDepth = 5,
                });
            Yaml.ITraceWriter traceWriter = new YamlTraceWriter(hc);
            return hc;
        }

        private sealed class YamlFileProvider : Yaml.IFileProvider
        {
            public Dictionary<String, String> FileContent => m_fileContent;

            public Yaml.FileData GetFile(String path)
            {
                return new Yaml.FileData
                {
                    Name = Path.GetFileName(path),
                    Directory = Path.GetDirectoryName(path),
                    Content = m_fileContent[path],
                };
            }

            public String ResolvePath(String defaultRoot, String path)
            {
                return Path.Combine(defaultRoot, path);
            }

            private readonly Dictionary<String, String> m_fileContent = new Dictionary<String, String>();
        }

        private sealed class YamlTraceWriter : Yaml.ITraceWriter
        {
            public YamlTraceWriter(TestHostContext hostContext)
            {
                m_trace = hostContext.GetTrace(nameof(YamlTraceWriter));
            }

            public void Info(String format, params Object[] args)
            {
                m_trace.Info(format, args);
            }

            public void Verbose(String format, params Object[] args)
            {
                m_trace.Verbose(format, args);
            }

            private readonly Tracing m_trace;
        }

        private const String c_defaultRoot = @"C:\TestYamlFiles";
        private Yaml.PipelineParser m_pipelineParser;
        private YamlFileProvider m_fileProvider;
    }
}
