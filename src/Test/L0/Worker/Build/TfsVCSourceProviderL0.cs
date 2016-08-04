using System.IO;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Build
{
    public sealed class TfsVCSourceProviderL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InterpretsRecursive()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                // Arrange.
                Tracing trace = tc.GetTrace();
                var mapping = new TfsVCSourceProvider.DefinitionWorkspaceMapping
                {
                    MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                    ServerPath = "$/myProj",
                };

                // Act/Assert.
                Assert.True(mapping.Recursive);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void InterpretsSingleLevel()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                // Arrange.
                Tracing trace = tc.GetTrace();
                var mapping = new TfsVCSourceProvider.DefinitionWorkspaceMapping
                {
                    MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                    ServerPath = "$/myProj/*",
                };

                // Act/Assert.
                Assert.False(mapping.Recursive);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void NormalizesLocalPath()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                // Arrange.
                Tracing trace = tc.GetTrace();
                var mapping = new TfsVCSourceProvider.DefinitionWorkspaceMapping
                {
                    MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                    ServerPath = "$/myProj",
                    LocalPath = @"myProj/myDir\mySubDir",
                };

                // Act.
                string actual = mapping.GetRootedLocalPath(IOUtil.GetBinPath());

                // Assert.
                string expected = Path.Combine(IOUtil.GetBinPath(), "myProj", "myDir", "mySubDir");
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void NormalizesRootSingleLevelServerPath()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                // Arrange.
                Tracing trace = tc.GetTrace();
                var mapping = new TfsVCSourceProvider.DefinitionWorkspaceMapping
                {
                    MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                    ServerPath = "$/*",
                };

                // Act/Assert.
                Assert.Equal("$/", mapping.NormalizedServerPath);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void NormalizesSingleLevelServerPath()
        {
            using (TestHostContext tc = new TestHostContext(this))
            {
                // Arrange.
                Tracing trace = tc.GetTrace();
                var mapping = new TfsVCSourceProvider.DefinitionWorkspaceMapping
                {
                    MappingType = TfsVCSourceProvider.DefinitionMappingType.Map,
                    ServerPath = "$/myProj/*",
                };

                // Act/Assert.
                Assert.Equal("$/myProj", mapping.NormalizedServerPath);
            }
        }
    }
}
