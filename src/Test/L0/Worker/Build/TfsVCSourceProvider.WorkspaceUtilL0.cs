using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Build
{
    public sealed class TfsVCSourceProvider_WorkspaceUtilL0
    {
        private TfsVCSourceProvider.DefinitionWorkspaceMapping[] _definitionMappings;
        private Mock<IExecutionContext> _executionContext;
        private string _sourceFile;
        private string _sourcesDirectory;
        private Tracing _trace;
        private string _workspaceName;
        private List<ITfsVCWorkspace> _workspaces = new List<ITfsVCWorkspace>();

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DoesNotMatch_Cloak_ServerPath()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                try
                {
                    // Arrange.
                    Prepare(tc);
                    _definitionMappings = new[]
                    {
                        new TfsVCSourceProvider.DefinitionWorkspaceMapping
                        {
                            MappingType = TfsVCSourceProvider.DefinitionMappingType.Cloak,
                            ServerPath = "$/myProj",
                        },
                    };
                    var tfWorkspace = new MockTfsVCWorkspace(
                        name: _workspaceName,
                        mappings: _definitionMappings,
                        localRoot: _sourcesDirectory);
                    (tfWorkspace.Mappings[0] as MockTfsVCMapping).ServerPath = "$/otherProj";

                    // Act.
                    ITfsVCWorkspace actual = TfsVCSourceProvider.WorkspaceUtil.MatchExactWorkspace(
                        executionContext: _executionContext.Object,
                        tfWorkspaces: new[] { tfWorkspace },
                        name: _workspaceName,
                        definitionMappings: _definitionMappings,
                        sourcesDirectory: _sourcesDirectory);

                    // Assert.
                    Assert.Null(actual);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DoesNotMatch_ComputerName()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                try
                {
                    // Arrange.
                    Prepare(tc);
                    _definitionMappings = new[]
                    {
                        new TfsVCSourceProvider.DefinitionWorkspaceMapping
                        {
                            LocalPath = "",
                            MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                            ServerPath = "$/myProj",
                        },
                    };
                    var tfWorkspace = new MockTfsVCWorkspace(
                        name: _workspaceName,
                        mappings: _definitionMappings,
                        localRoot: _sourcesDirectory,
                        computer: "NON_MATCHING_COMPUTER_NAME");

                    // Act.
                    ITfsVCWorkspace actual = TfsVCSourceProvider.WorkspaceUtil.MatchExactWorkspace(
                        executionContext: _executionContext.Object,
                        tfWorkspaces: new[] { tfWorkspace },
                        name: _workspaceName,
                        definitionMappings: _definitionMappings,
                        sourcesDirectory: _sourcesDirectory);

                    // Assert.
                    Assert.Null(actual);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DoesNotMatch_Map_LocalPath()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                try
                {
                    // Arrange.
                    Prepare(tc);
                    _definitionMappings = new[]
                    {
                        new TfsVCSourceProvider.DefinitionWorkspaceMapping
                        {
                            LocalPath = "myProj",
                            MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                            ServerPath = "$/myProj",
                        },
                    };
                    var tfWorkspace = new MockTfsVCWorkspace(
                        name: _workspaceName,
                        mappings: _definitionMappings,
                        localRoot: _sourcesDirectory);
                    (tfWorkspace.Mappings[0] as MockTfsVCMapping).LocalPath = Path.Combine(_sourcesDirectory, "otherProj");

                    // Act.
                    ITfsVCWorkspace actual = TfsVCSourceProvider.WorkspaceUtil.MatchExactWorkspace(
                        executionContext: _executionContext.Object,
                        tfWorkspaces: new[] { tfWorkspace },
                        name: _workspaceName,
                        definitionMappings: _definitionMappings,
                        sourcesDirectory: _sourcesDirectory);

                    // Assert.
                    Assert.Null(actual);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DoesNotMatch_Map_Recursive()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                try
                {
                    // Arrange.
                    Prepare(tc);
                    _definitionMappings = new[]
                    {
                        new TfsVCSourceProvider.DefinitionWorkspaceMapping
                        {
                            LocalPath = "",
                            MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                            ServerPath = "$/myProj",
                        },
                    };
                    var tfWorkspace = new MockTfsVCWorkspace(
                        name: _workspaceName,
                        mappings: _definitionMappings,
                        localRoot: _sourcesDirectory);
                    (tfWorkspace.Mappings[0] as MockTfsVCMapping).Recursive = false;

                    // Act.
                    ITfsVCWorkspace actual = TfsVCSourceProvider.WorkspaceUtil.MatchExactWorkspace(
                        executionContext: _executionContext.Object,
                        tfWorkspaces: new[] { tfWorkspace },
                        name: _workspaceName,
                        definitionMappings: _definitionMappings,
                        sourcesDirectory: _sourcesDirectory);

                    // Assert.
                    Assert.Null(actual);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DoesNotMatch_Map_ServerPath()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                try
                {
                    // Arrange.
                    Prepare(tc);
                    _definitionMappings = new[]
                    {
                        new TfsVCSourceProvider.DefinitionWorkspaceMapping
                        {
                            LocalPath = "",
                            MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                            ServerPath = "$/myProj",
                        },
                    };
                    var tfWorkspace = new MockTfsVCWorkspace(
                        name: _workspaceName,
                        mappings: _definitionMappings,
                        localRoot: _sourcesDirectory);
                    (tfWorkspace.Mappings[0] as MockTfsVCMapping).ServerPath = "$/otherProj";

                    // Act.
                    ITfsVCWorkspace actual = TfsVCSourceProvider.WorkspaceUtil.MatchExactWorkspace(
                        executionContext: _executionContext.Object,
                        tfWorkspaces: new[] { tfWorkspace },
                        name: _workspaceName,
                        definitionMappings: _definitionMappings,
                        sourcesDirectory: _sourcesDirectory);

                    // Assert.
                    Assert.Null(actual);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DoesNotMatch_Map_SingleLevel()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                try
                {
                    // Arrange.
                    Prepare(tc);
                    _definitionMappings = new[]
                    {
                        new TfsVCSourceProvider.DefinitionWorkspaceMapping
                        {
                            LocalPath = "",
                            MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                            ServerPath = "$/myProj/*",
                        },
                    };
                    var tfWorkspace = new MockTfsVCWorkspace(
                        name: _workspaceName,
                        mappings: _definitionMappings,
                        localRoot: _sourcesDirectory);
                    (tfWorkspace.Mappings[0] as MockTfsVCMapping).Recursive = true;

                    // Act.
                    ITfsVCWorkspace actual = TfsVCSourceProvider.WorkspaceUtil.MatchExactWorkspace(
                        executionContext: _executionContext.Object,
                        tfWorkspaces: new[] { tfWorkspace },
                        name: _workspaceName,
                        definitionMappings: _definitionMappings,
                        sourcesDirectory: _sourcesDirectory);

                    // Assert.
                    Assert.Null(actual);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DoesNotMatch_MappingType()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                try
                {
                    // Arrange.
                    Prepare(tc);
                    _definitionMappings = new[]
                    {
                        new TfsVCSourceProvider.DefinitionWorkspaceMapping
                        {
                            LocalPath = "",
                            MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                            ServerPath = "$/myProj",
                        },
                    };
                    var tfWorkspace = new MockTfsVCWorkspace(
                        name: _workspaceName,
                        mappings: _definitionMappings,
                        localRoot: _sourcesDirectory);
                    (tfWorkspace.Mappings[0] as MockTfsVCMapping).Cloak = true;

                    // Act.
                    ITfsVCWorkspace actual = TfsVCSourceProvider.WorkspaceUtil.MatchExactWorkspace(
                        executionContext: _executionContext.Object,
                        tfWorkspaces: new[] { tfWorkspace },
                        name: _workspaceName,
                        definitionMappings: _definitionMappings,
                        sourcesDirectory: _sourcesDirectory);

                    // Assert.
                    Assert.Null(actual);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void DoesNotMatch_WorkspaceName()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                try
                {
                    // Arrange.
                    Prepare(tc);
                    _definitionMappings = new[]
                    {
                        new TfsVCSourceProvider.DefinitionWorkspaceMapping
                        {
                            LocalPath = "",
                            MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                            ServerPath = "$/myProj",
                        },
                    };
                    var tfWorkspace = new MockTfsVCWorkspace(
                        name: "NON_MATCHING_WORKSPACE_NAME",
                        mappings: _definitionMappings,
                        localRoot: _sourcesDirectory);

                    // Act.
                    ITfsVCWorkspace actual = TfsVCSourceProvider.WorkspaceUtil.MatchExactWorkspace(
                        executionContext: _executionContext.Object,
                        tfWorkspaces: new[] { tfWorkspace },
                        name: _workspaceName,
                        definitionMappings: _definitionMappings,
                        sourcesDirectory: _sourcesDirectory);

                    // Assert.
                    Assert.Null(actual);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void Matches()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                try
                {
                    // Arrange.
                    Prepare(tc);
                    var expected = new MockTfsVCWorkspace(
                        name: _workspaceName,
                        mappings: _definitionMappings,
                        localRoot: _sourcesDirectory);

                    // Act.
                    ITfsVCWorkspace actual = TfsVCSourceProvider.WorkspaceUtil.MatchExactWorkspace(
                        executionContext: _executionContext.Object,
                        tfWorkspaces: new[] { expected },
                        name: _workspaceName,
                        definitionMappings: _definitionMappings,
                        sourcesDirectory: _sourcesDirectory);

                    // Assert.
                    Assert.Equal(expected, actual);
                }
                finally
                {
                    Cleanup();
                }
            }
        }

        private void Cleanup()
        {
            if (!string.IsNullOrEmpty(_sourcesDirectory))
            {
                Directory.Delete(_sourcesDirectory, recursive: true);
            }
        }

        private void Prepare(TestHostContext hostContext)
        {
            _trace = hostContext.GetTrace();

            // Prepare the sources directory. The workspace helper will not return any
            // matches if the sources directory does not exist with something in it.
            _sourcesDirectory = Path.Combine(hostContext.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
            _sourceFile = Path.Combine(_sourcesDirectory, "some file");
            Directory.CreateDirectory(_sourcesDirectory);
            File.WriteAllText(path: _sourceFile, contents: "some contents");

            // Prepare a basic definition workspace.
            _workspaceName = "ws_1_1";
            _definitionMappings = new[]
            {
                new TfsVCSourceProvider.DefinitionWorkspaceMapping
                {
                    LocalPath = "",
                    MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                    ServerPath = "$/*",
                },
                new TfsVCSourceProvider.DefinitionWorkspaceMapping
                {
                    LocalPath = "myProj",
                    MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                    ServerPath = "$/myProj",
                },
                new TfsVCSourceProvider.DefinitionWorkspaceMapping
                {
                    LocalPath = "myProj/Drops",
                    MappingType = TfsVCSourceProvider.DefinitionMappingType.Cloak,
                    ServerPath = "$/myProj/Drops",
                },
                new TfsVCSourceProvider.DefinitionWorkspaceMapping
                {
                    LocalPath = "otherProj/mydir",
                    MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                    ServerPath = "$/otherProj/mydir/*",
                },
            };

            _executionContext = new Mock<IExecutionContext>();
            _executionContext
                .Setup(x => x.WriteDebug)
                .Returns(true);
            _executionContext
                .Setup(x => x.Write(It.IsAny<string>(), It.IsAny<string>()))
                .Callback((string tag, string message) => _trace.Info($"[ExecutionContext]{tag} {message}"));
        }

        public sealed class MockTfsVCWorkspace : ITfsVCWorkspace
        {
            public MockTfsVCWorkspace(
                string name,
                TfsVCSourceProvider.DefinitionWorkspaceMapping[] mappings = null,
                string localRoot = null,
                string computer = null)
            {
                ArgUtil.NotNullOrEmpty(name, nameof(name));
                Computer = computer != null ? computer : Environment.MachineName;
                Mappings = (mappings ?? new TfsVCSourceProvider.DefinitionWorkspaceMapping[0])
                    .Select(x => new MockTfsVCMapping(x, localRoot))
                    .ToArray();
                Name = name;
            }

            public string Computer { get; set; }
            public string Name { get; set; }
            public string Owner { get; set; }
            public ITfsVCMapping[] Mappings { get; set; }
        }

        public sealed class MockTfsVCMapping : ITfsVCMapping
        {
            public MockTfsVCMapping(TfsVCSourceProvider.DefinitionWorkspaceMapping mapping, string localRoot)
            {
                ArgUtil.NotNull(mapping, nameof(mapping));
                ArgUtil.NotNull(localRoot, nameof(localRoot));
                Cloak = mapping.MappingType == TfsVCSourceProvider.DefinitionMappingType.Cloak;
                LocalPath = mapping.GetRootedLocalPath(localRoot);
                Recursive = mapping.Recursive;
                ServerPath = mapping.NormalizedServerPath;
            }

            public bool Cloak { get; set; }
            public string LocalPath { get; set; }
            public bool Recursive { get; set; }
            public string ServerPath { get; set; }
        }
    }
}
