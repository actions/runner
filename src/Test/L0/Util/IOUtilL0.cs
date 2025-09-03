using GitHub.Runner.Sdk;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GitHub.Runner.Common.Tests.Util
{
    public sealed class IOUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Delete_DeletesDirectory()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");

                    // Act.
                    IOUtil.Delete(directory, CancellationToken.None);

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
        public void Delete_DeletesFile()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");

                    // Act.
                    IOUtil.Delete(file, CancellationToken.None);

                    // Assert.
                    Assert.False(File.Exists(file));
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
        public void DeleteDirectory_DeletesDirectoriesRecursively()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a grandchild directory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
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
        public async Task DeleteDirectory_DeletesDirectoryReparsePointChain()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create the following structure:
                //   randomDir
                //   randomDir/<guid 1> -> <guid 2>
                //   randomDir/<guid 2> -> <guid 3>
                //   randomDir/<guid 3> -> <guid 4>
                //   randomDir/<guid 4> -> <guid 5>
                //   randomDir/<guid 5> -> targetDir
                //   randomDir/targetDir
                //   randomDir/targetDir/file.txt
                //
                // The purpose of this test is to verify that DirectoryNotFoundException is gracefully handled when
                // deleting a chain of reparse point directories. Since the reparse points are named in a random order,
                // the DirectoryNotFoundException case is likely to be encountered.
                string randomDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string targetDir = Directory.CreateDirectory(Path.Combine(randomDir, "targetDir")).FullName;
                    string file = Path.Combine(targetDir, "file.txt");
                    File.WriteAllText(path: file, contents: "some contents");
                    string linkDir1 = Path.Combine(randomDir, $"{Guid.NewGuid()}_linkDir1");
                    string linkDir2 = Path.Combine(randomDir, $"{Guid.NewGuid()}_linkDir2");
                    string linkDir3 = Path.Combine(randomDir, $"{Guid.NewGuid()}_linkDir3");
                    string linkDir4 = Path.Combine(randomDir, $"{Guid.NewGuid()}_linkDir4");
                    string linkDir5 = Path.Combine(randomDir, $"{Guid.NewGuid()}_linkDir5");
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir1, target: linkDir2);
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir2, target: linkDir3);
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir3, target: linkDir4);
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir4, target: linkDir5);
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir5, target: targetDir);

                    // Sanity check to verify the link was created properly:
                    Assert.True(Directory.Exists(linkDir1));
                    Assert.True(new DirectoryInfo(linkDir1).Attributes.HasFlag(FileAttributes.ReparsePoint));
                    Assert.True(File.Exists(Path.Combine(linkDir1, "file.txt")));

                    // Act.
                    IOUtil.DeleteDirectory(randomDir, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(linkDir1));
                    Assert.False(Directory.Exists(targetDir));
                    Assert.False(File.Exists(file));
                    Assert.False(Directory.Exists(randomDir));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(randomDir))
                    {
                        Directory.Delete(randomDir, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task DeleteDirectory_DeletesDirectoryReparsePointsBeforeDirectories()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create the following structure:
                //   randomDir
                //   randomDir/linkDir -> targetDir
                //   randomDir/targetDir
                //   randomDir/targetDir/file.txt
                //
                // The accuracy of this test relies on an assumption that IOUtil sorts the directories in
                // descending order before deleting them - either by length or by default sort order.
                string randomDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string targetDir = Directory.CreateDirectory(Path.Combine(randomDir, "targetDir")).FullName;
                    string file = Path.Combine(targetDir, "file.txt");
                    File.WriteAllText(path: file, contents: "some contents");
                    string linkDir = Path.Combine(randomDir, "linkDir");
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir, target: targetDir);

                    // Sanity check to verify the link was created properly:
                    Assert.True(Directory.Exists(linkDir));
                    Assert.True(new DirectoryInfo(linkDir).Attributes.HasFlag(FileAttributes.ReparsePoint));
                    Assert.True(File.Exists(Path.Combine(linkDir, "file.txt")));

                    // Act.
                    IOUtil.DeleteDirectory(randomDir, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(linkDir));
                    Assert.False(Directory.Exists(targetDir));
                    Assert.False(File.Exists(file));
                    Assert.False(Directory.Exists(randomDir));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(randomDir))
                    {
                        Directory.Delete(randomDir, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteDirectory_DeletesFilesRecursively()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a grandchild file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
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
        public void DeleteDirectory_DeletesReadOnlyDirectories()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a read-only subdirectory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
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
        public void DeleteDirectory_DeletesReadOnlyRootDirectory()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a read-only directory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
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
        public void DeleteDirectory_DeletesReadOnlyFiles()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a read-only file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
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
        public async Task DeleteDirectory_DoesNotFollowDirectoryReparsePoint()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create the following structure:
                //   randomDir
                //   randomDir/targetDir
                //   randomDir/targetDir/file.txt
                //   randomDir/linkDir -> targetDir
                string randomDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string targetDir = Directory.CreateDirectory(Path.Combine(randomDir, "targetDir")).FullName;
                    string file = Path.Combine(targetDir, "file.txt");
                    File.WriteAllText(path: file, contents: "some contents");
                    string linkDir = Path.Combine(randomDir, "linkDir");
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir, target: targetDir);

                    // Sanity check to verify the link was created properly:
                    Assert.True(Directory.Exists(linkDir));
                    Assert.True(new DirectoryInfo(linkDir).Attributes.HasFlag(FileAttributes.ReparsePoint));
                    Assert.True(File.Exists(Path.Combine(linkDir, "file.txt")));

                    // Act.
                    IOUtil.DeleteDirectory(linkDir, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(linkDir));
                    Assert.True(Directory.Exists(targetDir));
                    Assert.True(File.Exists(file));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(randomDir))
                    {
                        Directory.Delete(randomDir, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task DeleteDirectory_DoesNotFollowNestLevel1DirectoryReparsePoint()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create the following structure:
                //   randomDir
                //   randomDir/targetDir
                //   randomDir/targetDir/file.txt
                //   randomDir/subDir
                //   randomDir/subDir/linkDir -> ../targetDir
                string randomDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string targetDir = Directory.CreateDirectory(Path.Combine(randomDir, "targetDir")).FullName;
                    string file = Path.Combine(targetDir, "file.txt");
                    File.WriteAllText(path: file, contents: "some contents");
                    string subDir = Directory.CreateDirectory(Path.Combine(randomDir, "subDir")).FullName;
                    string linkDir = Path.Combine(subDir, "linkDir");
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir, target: targetDir);

                    // Sanity check to verify the link was created properly:
                    Assert.True(Directory.Exists(linkDir));
                    Assert.True(new DirectoryInfo(linkDir).Attributes.HasFlag(FileAttributes.ReparsePoint));
                    Assert.True(File.Exists(Path.Combine(linkDir, "file.txt")));

                    // Act.
                    IOUtil.DeleteDirectory(subDir, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(subDir));
                    Assert.True(Directory.Exists(targetDir));
                    Assert.True(File.Exists(file));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(randomDir))
                    {
                        Directory.Delete(randomDir, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task DeleteDirectory_DoesNotFollowNestLevel2DirectoryReparsePoint()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create the following structure:
                //   randomDir
                //   randomDir/targetDir
                //   randomDir/targetDir/file.txt
                //   randomDir/subDir1
                //   randomDir/subDir1/subDir2
                //   randomDir/subDir1/subDir2/linkDir -> ../../targetDir
                string randomDir = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    string targetDir = Directory.CreateDirectory(Path.Combine(randomDir, "targetDir")).FullName;
                    string file = Path.Combine(targetDir, "file.txt");
                    File.WriteAllText(path: file, contents: "some contents");
                    string subDir1 = Directory.CreateDirectory(Path.Combine(randomDir, "subDir1")).FullName;
                    string subDir2 = Directory.CreateDirectory(Path.Combine(subDir1, "subDir2")).FullName;
                    string linkDir = Path.Combine(subDir2, "linkDir");
                    await CreateDirectoryReparsePoint(context: hc, link: linkDir, target: targetDir);

                    // Sanity check to verify the link was created properly:
                    Assert.True(Directory.Exists(linkDir));
                    Assert.True(new DirectoryInfo(linkDir).Attributes.HasFlag(FileAttributes.ReparsePoint));
                    Assert.True(File.Exists(Path.Combine(linkDir, "file.txt")));

                    // Act.
                    IOUtil.DeleteDirectory(subDir1, CancellationToken.None);

                    // Assert.
                    Assert.False(Directory.Exists(subDir1));
                    Assert.True(Directory.Exists(targetDir));
                    Assert.True(File.Exists(file));
                }
                finally
                {
                    // Cleanup.
                    if (Directory.Exists(randomDir))
                    {
                        Directory.Delete(randomDir, recursive: true);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteDirectory_IgnoresFile()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");

                    // Act: Call "DeleteDirectory" against the file. The method should not blow up and
                    // should simply ignore the file since it is not a directory.
                    IOUtil.DeleteDirectory(file, CancellationToken.None);

                    // Assert.
                    Assert.True(File.Exists(file));
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
        public void DeleteFile_DeletesFile()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");

                    // Act.
                    IOUtil.DeleteFile(file);

                    // Assert.
                    Assert.False(File.Exists(file));
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
        public void DeleteFile_DeletesReadOnlyFile()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a read-only file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");
                    File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.ReadOnly);

                    // Act.
                    IOUtil.DeleteFile(file);

                    // Assert.
                    Assert.False(File.Exists(file));
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
        public void DeleteFile_IgnoresDirectory()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    Directory.CreateDirectory(directory);

                    // Act: Call "DeleteFile" against a directory. The method should not blow up and
                    // should simply ignore the directory since it is not a file.
                    IOUtil.DeleteFile(directory);

                    // Assert.
                    Assert.True(Directory.Exists(directory));
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
        public void GetRelativePath()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                string relativePath;
#if OS_WINDOWS
                /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\src") -> @"project\foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\src\project\foo.cpp", @"d:\src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project\foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:\", @"d:\specs") -> @"d:\"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\", @"d:\specs");
                // Assert.
                Assert.True(string.Equals(relativePath, @"d:\", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\src\proj") -> @"d:\src\project\foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\src\project\foo.cpp", @"d:\src\proj");
                // Assert.
                Assert.True(string.Equals(relativePath, @"d:\src\project\foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:\src\project\foo", @"d:\src") -> @"project\foo"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\src\project\foo", @"d:\src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project\foo", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:\src\project\foo.cpp", @"d:\src\project\foo.cpp") -> @""
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\src\project", @"d:\src\project");
                // Assert.
                Assert.True(string.Equals(relativePath, string.Empty, StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:/src/project/foo.cpp", @"d:/src") -> @"project/foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:/src/project/foo.cpp", @"d:/src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project\foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:/src/project/foo.cpp", @"d:\src") -> @"d:/src/project/foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:/src/project/foo.cpp", @"d:/src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project\foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d:/src/project/foo", @"d:/src") -> @"project/foo"
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:/src/project/foo", @"d:/src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project\foo", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"d\src\project", @"d:/src/project") -> @""
                // Act.
                relativePath = IOUtil.MakeRelative(@"d:\src\project", @"d:/src/project");
                // Assert.
                Assert.True(string.Equals(relativePath, string.Empty, StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");
#else
                /// MakeRelative(@"/user/src/project/foo.cpp", @"/user/src") -> @"project/foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"/user/src/project/foo.cpp", @"/user/src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project/foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"/user", @"/user/specs") -> @"/user"
                // Act.
                relativePath = IOUtil.MakeRelative(@"/user", @"/user/specs");
                // Assert.
                Assert.True(string.Equals(relativePath, @"/user", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"/user/src/project/foo.cpp", @"/user/src/proj") -> @"/user/src/project/foo.cpp"
                // Act.
                relativePath = IOUtil.MakeRelative(@"/user/src/project/foo.cpp", @"/user/src/proj");
                // Assert.
                Assert.True(string.Equals(relativePath, @"/user/src/project/foo.cpp", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"/user/src/project/foo", @"/user/src") -> @"project/foo"
                // Act.
                relativePath = IOUtil.MakeRelative(@"/user/src/project/foo", @"/user/src");
                // Assert.
                Assert.True(string.Equals(relativePath, @"project/foo", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");

                /// MakeRelative(@"/user/src/project", @"/user/src/project") -> @""
                // Act.
                relativePath = IOUtil.MakeRelative(@"/user/src/project", @"/user/src/project");
                // Assert.
                Assert.True(string.Equals(relativePath, string.Empty, StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {relativePath}");
#endif
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ResolvePath()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                string resolvePath;
#if OS_WINDOWS
                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\src\project\", @"foo");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\src\project\foo", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\", @"specs");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\specs", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\src\project\", @"src\proj");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\src\project\src\proj", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\src\project\foo", @"..");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\src\project", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\src\project", @"..\..\");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:/src/project", @"../.");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\src", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:/src/project/", @"../../foo");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\foo", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:/src/project/foo", @".././bar/.././../foo");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\src\foo", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"d:\", @".");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"d:\", StringComparison.OrdinalIgnoreCase), $"resolvePath does not expected: {resolvePath}");
#else
                // Act.
                resolvePath = IOUtil.ResolvePath(@"/user/src/project", @"foo");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/user/src/project/foo", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/root", @"./user/./specs");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/root/user/specs", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/", @"user/specs/.");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/user/specs", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/user/src/project", @"../");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/user/src", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/user/src/project", @"../../");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/user", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/user/src/project/foo", @"../../../../user/./src");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/user/src", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/user/src", @"../../.");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");

                // Act.
                resolvePath = IOUtil.ResolvePath(@"/", @"./");
                // Assert.
                Assert.True(string.Equals(resolvePath, @"/", StringComparison.OrdinalIgnoreCase), $"RelativePath does not expected: {resolvePath}");
#endif
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CopyDirectory_CopiesSymlink()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string destination = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                string file = Path.Combine(directory, "some file");
                string fileLink = Path.Combine(directory, "some file link");
                string destinationLink = Path.Combine(destination, "some file link");
                try
                {
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(path: file, contents: "some contents");
                    File.CreateSymbolicLink(path: fileLink, pathToTarget: "some file");

                    // Act.
                    IOUtil.CopyDirectory(directory, destination, CancellationToken.None);

                    // Assert.
                    Assert.True(Directory.Exists(destination));
                    Assert.True(File.Exists(destinationLink));
                    Assert.Equal("some file", new FileInfo(destinationLink).LinkTarget);
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
        public void ValidateExecutePermission_DoesNotExceedFailsafe()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());
                try
                {
                    Directory.CreateDirectory(directory);

                    // Act/Assert: Call "ValidateExecutePermission". The method should not blow up.
                    IOUtil.ValidateExecutePermission(directory);
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
        public void ValidateExecutePermission_ExceedsFailsafe()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a deep directory.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName(), "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20");
                try
                {
                    Directory.CreateDirectory(directory);
                    Environment.SetEnvironmentVariable("AGENT_TEST_VALIDATE_EXECUTE_PERMISSIONS_FAILSAFE", "20");

                    try
                    {
                        // Act: Call "ValidateExecutePermission". The method should throw since
                        // it exceeds the failsafe recursion depth.
                        IOUtil.ValidateExecutePermission(directory);

                        // Assert.
                        throw new Exception("Should have thrown not supported exception.");
                    }
                    catch (NotSupportedException)
                    {
                    }
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
        public void LoadObject_ThrowsOnRequiredLoadObject()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Arrange: Create a directory with a file.
                string directory = Path.Combine(hc.GetDirectory(WellKnownDirectory.Bin), Path.GetRandomFileName());

                string file = Path.Combine(directory, "empty file");
                Directory.CreateDirectory(directory);

                File.WriteAllText(path: file, contents: "");
                Assert.Throws<ArgumentNullException>(() => IOUtil.LoadObject<RunnerSettings>(file, true));

                file = Path.Combine(directory, "invalid type file");
                File.WriteAllText(path: file, contents: " ");
                Assert.Throws<ArgumentException>(() => IOUtil.LoadObject<RunnerSettings>(file, true));

                // Cleanup.
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void ReplaceInvalidFileNameChars()
        {
            Assert.Equal(string.Empty, IOUtil.ReplaceInvalidFileNameChars(null));
            Assert.Equal(string.Empty, IOUtil.ReplaceInvalidFileNameChars(string.Empty));
            Assert.Equal("hello.txt", IOUtil.ReplaceInvalidFileNameChars("hello.txt"));
#if OS_WINDOWS
            // Refer https://github.com/dotnet/runtime/blob/ce84f1d8a3f12711bad678a33efbc37b461f684f/src/libraries/System.Private.CoreLib/src/System/IO/Path.Windows.cs#L15
            Assert.Equal(
                "1_ 2_ 3_ 4_ 5_ 6_ 7_ 8_ 9_ 10_ 11_ 12_ 13_ 14_ 15_ 16_ 17_ 18_ 19_ 20_ 21_ 22_ 23_ 24_ 25_ 26_ 27_ 28_ 29_ 30_ 31_ 32_ 33_ 34_ 35_ 36_ 37_ 38_ 39_ 40_ 41_",
                IOUtil.ReplaceInvalidFileNameChars($"1\" 2< 3> 4| 5\0 6{(char)1} 7{(char)2} 8{(char)3} 9{(char)4} 10{(char)5} 11{(char)6} 12{(char)7} 13{(char)8} 14{(char)9} 15{(char)10} 16{(char)11} 17{(char)12} 18{(char)13} 19{(char)14} 20{(char)15} 21{(char)16} 22{(char)17} 23{(char)18} 24{(char)19} 25{(char)20} 26{(char)21} 27{(char)22} 28{(char)23} 29{(char)24} 30{(char)25} 31{(char)26} 32{(char)27} 33{(char)28} 34{(char)29} 35{(char)30} 36{(char)31} 37: 38* 39? 40\\ 41/"));
#else
            // Refer https://github.com/dotnet/runtime/blob/ce84f1d8a3f12711bad678a33efbc37b461f684f/src/libraries/System.Private.CoreLib/src/System/IO/Path.Unix.cs#L12
            Assert.Equal("1_ 2_", IOUtil.ReplaceInvalidFileNameChars("1\0 2/"));
#endif
            Assert.Equal("_leading", IOUtil.ReplaceInvalidFileNameChars("/leading"));
            Assert.Equal("__consecutive leading", IOUtil.ReplaceInvalidFileNameChars("//consecutive leading"));
            Assert.Equal("trailing_", IOUtil.ReplaceInvalidFileNameChars("trailing/"));
            Assert.Equal("consecutive trailing__", IOUtil.ReplaceInvalidFileNameChars("consecutive trailing//"));
            Assert.Equal("middle_middle", IOUtil.ReplaceInvalidFileNameChars("middle/middle"));
            Assert.Equal("consecutive middle__consecutive middle", IOUtil.ReplaceInvalidFileNameChars("consecutive middle//consecutive middle"));
            Assert.Equal("_leading_middle_trailing_", IOUtil.ReplaceInvalidFileNameChars("/leading/middle/trailing/"));
            Assert.Equal("__consecutive leading__consecutive middle__consecutive trailing__", IOUtil.ReplaceInvalidFileNameChars("//consecutive leading//consecutive middle//consecutive trailing//"));
        }

        private static async Task CreateDirectoryReparsePoint(IHostContext context, string link, string target)
        {
#if OS_WINDOWS
            string fileName = Environment.GetEnvironmentVariable("ComSpec");
            string arguments = $@"/c ""mklink /J ""{link}"" {target}""""";
#else
            string fileName = "/bin/ln";
            string arguments = $@"-s ""{target}"" ""{link}""";
#endif
            ArgUtil.File(fileName, nameof(fileName));
            using (var processInvoker = new ProcessInvokerWrapper())
            {
                processInvoker.Initialize(context);
                await processInvoker.ExecuteAsync(
                    workingDirectory: context.GetDirectory(WellKnownDirectory.Bin),
                    fileName: fileName,
                    arguments: arguments,
                    environment: null,
                    requireExitCodeZero: true,
                    cancellationToken: CancellationToken.None);
            }
        }
    }
}
