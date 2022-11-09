using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using System;
using System.IO;
using Xunit;

namespace GitHub.Runner.Common.Tests.Util
{
    public sealed class WhichUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void UseWhichFindGit()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                //Arrange
                Tracing trace = hc.GetTrace();

                // Act.
                string gitPath = WhichUtil.Which("git", trace: trace);

                trace.Info($"Which(\"git\") returns: {gitPath ?? string.Empty}");

                // Assert.
                Assert.True(!string.IsNullOrEmpty(gitPath) && File.Exists(gitPath), $"Unable to find Git through: {nameof(WhichUtil.Which)}");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WhichReturnsNullWhenNotFound()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                //Arrange
                Tracing trace = hc.GetTrace();

                // Act.
                string nosuch = WhichUtil.Which("no-such-file-cf7e351f", trace: trace);

                trace.Info($"result: {nosuch ?? string.Empty}");

                // Assert.
                Assert.True(string.IsNullOrEmpty(nosuch), "Path should not be resolved");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WhichThrowsWhenRequireAndNotFound()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                //Arrange
                Tracing trace = hc.GetTrace();

                // Act.
                try
                {
                    WhichUtil.Which("no-such-file-cf7e351f", require: true, trace: trace);
                    throw new Exception("which should have thrown");
                }
                catch (FileNotFoundException ex)
                {
                    Assert.Equal("no-such-file-cf7e351f", ex.FileName);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WhichHandleFullyQualifiedPath()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                //Arrange
                Tracing trace = hc.GetTrace();

                // Act.
                var gitPath = WhichUtil.Which("git", require: true, trace: trace);
                var gitPath2 = WhichUtil.Which(gitPath, require: true, trace: trace);

                // Assert.
                Assert.Equal(gitPath, gitPath2);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WhichHandlesSymlinkToTargetFullPath()
        {
            // Arrange
            using TestHostContext hc = new TestHostContext(this);
            Tracing trace = hc.GetTrace();
            string oldValue = Environment.GetEnvironmentVariable(PathUtil.PathVariable);
#if OS_WINDOWS
            string newValue = oldValue + @$";{Path.GetTempPath()}";
            string symlinkName = $"symlink-{Guid.NewGuid()}";
            string symlink = Path.GetTempPath() + $"{symlinkName}.exe";
            string target = Path.GetTempPath() + $"target-{Guid.NewGuid()}.exe";
#else
            string newValue = oldValue + @$":{Path.GetTempPath()}";
            string symlinkName = $"symlink-{Guid.NewGuid()}";
            string symlink = Path.GetTempPath() + $"{symlinkName}";
            string target = Path.GetTempPath() + $"target-{Guid.NewGuid()}";
#endif

            Environment.SetEnvironmentVariable(PathUtil.PathVariable, newValue);


            using (File.Create(target))
            {
                File.CreateSymbolicLink(symlink, target);

                // Act.
                var result = WhichUtil.Which(symlinkName, require: true, trace: trace);

                // Assert
                Assert.True(!string.IsNullOrEmpty(result) && File.Exists(result), $"Unable to find symlink through: {nameof(WhichUtil.Which)}");

            }


            // Cleanup
            File.Delete(symlink);
            File.Delete(target);
            Environment.SetEnvironmentVariable(PathUtil.PathVariable, oldValue);

        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WhichHandlesSymlinkToTargetRelativePath()
        {
            // Arrange
            using TestHostContext hc = new TestHostContext(this);
            Tracing trace = hc.GetTrace();
            string oldValue = Environment.GetEnvironmentVariable(PathUtil.PathVariable);
#if OS_WINDOWS
            string newValue = oldValue + @$";{Path.GetTempPath()}";
            string symlinkName = $"symlink-{Guid.NewGuid()}";
            string symlink = Path.GetTempPath() + $"{symlinkName}.exe";
            string targetName = $"target-{Guid.NewGuid()}.exe";
            string target = Path.GetTempPath() + targetName;
#else
            string newValue = oldValue + @$":{Path.GetTempPath()}";
            string symlinkName = $"symlink-{Guid.NewGuid()}";
            string symlink = Path.GetTempPath() + $"{symlinkName}";
            string targetName = $"target-{Guid.NewGuid()}";
            string target = Path.GetTempPath() + targetName;
#endif
            Environment.SetEnvironmentVariable(PathUtil.PathVariable, newValue);


            using (File.Create(target))
            {
                File.CreateSymbolicLink(symlink, targetName);

                // Act.
                var result = WhichUtil.Which(symlinkName, require: true, trace: trace);

                // Assert
                Assert.True(!string.IsNullOrEmpty(result) && File.Exists(result), $"Unable to find {symlinkName} through: {nameof(WhichUtil.Which)}");
            }

            // Cleanup
            File.Delete(symlink);
            File.Delete(target);
            Environment.SetEnvironmentVariable(PathUtil.PathVariable, oldValue);

        }
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WhichThrowsWhenSymlinkBroken()
        {
            // Arrange
            using TestHostContext hc = new TestHostContext(this);
            Tracing trace = hc.GetTrace();
            string oldValue = Environment.GetEnvironmentVariable(PathUtil.PathVariable);

#if OS_WINDOWS
            string newValue = oldValue + @$";{Path.GetTempPath()}";
            string brokenSymlinkName = $"broken-symlink-{Guid.NewGuid()}";
            string brokenSymlink = Path.GetTempPath() + $"{brokenSymlinkName}.exe";
#else
            string newValue = oldValue + @$":{Path.GetTempPath()}";
            string brokenSymlinkName = $"broken-symlink-{Guid.NewGuid()}";
            string brokenSymlink = Path.GetTempPath() + $"{brokenSymlinkName}";
#endif


            string target = "no-such-file-cf7e351f";
            Environment.SetEnvironmentVariable(PathUtil.PathVariable, newValue);

            File.CreateSymbolicLink(brokenSymlink, target);

            // Act.
            var exception = Assert.Throws<FileNotFoundException>(()=>WhichUtil.Which(brokenSymlinkName, require: true, trace: trace));

            // Assert
            Assert.Equal(brokenSymlinkName, exception.FileName);

            // Cleanup
            File.Delete(brokenSymlink);
            Environment.SetEnvironmentVariable(PathUtil.PathVariable, oldValue);
        }
    }
}
