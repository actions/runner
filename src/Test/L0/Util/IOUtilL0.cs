using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Util
{
    public sealed class IOUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeletesDirectoriesRecursively()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a grandchild directory.
                string directory = Path.Combine(IOUtil.GetBinPath(), Path.GetRandomFileName());
                try
                {
                    Directory.CreateDirectory(Path.Combine(directory, "some child directory", "some grandchild directory"));

                    // Act.
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeletesFilesRecursively()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a grandchild file.
                string directory = Path.Combine(IOUtil.GetBinPath(), Path.GetRandomFileName());
                try
                {
                    string file = Path.Combine(directory, "some subdirectory", "some file");
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllText(path: file, contents: "some contents");

                    // Act.
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeletesReadOnlyDirectories()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a read-only subdirectory.
                string directory = Path.Combine(IOUtil.GetBinPath(), Path.GetRandomFileName());
                string subdirectory = Path.Combine(directory, "some subdirectory");
                try
                {
                    var subdirectoryInfo = new DirectoryInfo(subdirectory);
                    subdirectoryInfo.Create();
                    subdirectoryInfo.Attributes = subdirectoryInfo.Attributes | FileAttributes.ReadOnly;

                    // Act.
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    var subdirectoryInfo = new DirectoryInfo(subdirectory);
                    if (subdirectoryInfo.Exists)
                    {
                        subdirectoryInfo.Attributes = subdirectoryInfo.Attributes & ~FileAttributes.ReadOnly;
                    }

                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeletesReadOnlyRootDirectory()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a read-only directory.
                string directory = Path.Combine(IOUtil.GetBinPath(), Path.GetRandomFileName());
                try
                {
                    var directoryInfo = new DirectoryInfo(directory);
                    directoryInfo.Create();
                    directoryInfo.Attributes = directoryInfo.Attributes | FileAttributes.ReadOnly;

                    // Act.
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    var directoryInfo = new DirectoryInfo(directory);
                    if (directoryInfo.Exists)
                    {
                        directoryInfo.Attributes = directoryInfo.Attributes & ~FileAttributes.ReadOnly;
                        directoryInfo.Delete();
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeletesReadOnlyFiles()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a read-only file.
                string directory = Path.Combine(IOUtil.GetBinPath(), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");
                    File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.ReadOnly);

                    // Act.
                    IOUtil.DeleteDirectory(directory, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(directory));
                }
                finally
                {
                    // Cleanup.
                    if (File.Exists(file))
                    {
                        File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                    }

                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void UseWhichFindGit()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Act.
                string gitPath = IOUtil.Which("git");

                trace.Info($"Which(\"git\") returns: {gitPath ?? string.Empty}");

                // Assert.
                Assert.True(!string.IsNullOrEmpty(gitPath) && File.Exists(gitPath), $"Unable to find Git through: {nameof(IOUtil.Which)}");
            }
        }
    }
}
