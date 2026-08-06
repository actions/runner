using GitHub.Runner.Sdk;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace GitHub.Runner.Common.Tests.Util
{
    public sealed class PathUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCanonicalPath_ReturnsPath_WhenDirectoryDoesNotExist()
        {
            var fakePath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Path.GetRandomFileName());
            var result = PathUtil.GetCanonicalPath(fakePath);
            Assert.Equal(fakePath, result);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCanonicalPath_ReturnsPath_WhenNull()
        {
            Assert.Null(PathUtil.GetCanonicalPath(null));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCanonicalPath_ReturnsEmpty_WhenEmpty()
        {
            Assert.Equal(string.Empty, PathUtil.GetCanonicalPath(string.Empty));
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCanonicalPath_ReturnsValidPath_ForExistingDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "pathutil_test_" + Path.GetRandomFileName());
            try
            {
                Directory.CreateDirectory(tempDir);
                var result = PathUtil.GetCanonicalPath(tempDir);
                Assert.NotNull(result);
                Assert.True(Directory.Exists(result));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir);
                }
            }
        }

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCanonicalPath_NormalizesDriveLetter_OnWindows()
        {
            var tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

            // Skip if temp is a UNC path (no drive letter to normalize)
            if (tempDir.StartsWith(@"\\"))
            {
                return;
            }

            // Force lowercase drive letter
            var lowerCased = char.ToLower(tempDir[0]) + tempDir.Substring(1);

            var result = PathUtil.GetCanonicalPath(lowerCased);

            // The canonical path should have an uppercase drive letter
            Assert.True(char.IsUpper(result[0]),
                $"Expected uppercase drive letter but got: {result}");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCanonicalPath_NormalizesFolderCasing_OnWindows()
        {
            // Create a directory with known casing, then query with wrong casing
            var basePath = Path.GetTempPath();
            if (basePath.StartsWith(@"\\"))
            {
                return; // Skip UNC
            }

            var realName = "PathUtilTest_MiXeDcAsE_" + Path.GetRandomFileName();
            var realDir = Path.Combine(basePath, realName);
            try
            {
                Directory.CreateDirectory(realDir);

                // Query with all-lowercase version
                var wrongCased = Path.Combine(basePath, realName.ToLowerInvariant());

                var result = PathUtil.GetCanonicalPath(wrongCased);

                // The canonical result should contain the original mixed-case name
                Assert.Contains(realName, result);
            }
            finally
            {
                if (Directory.Exists(realDir))
                {
                    Directory.Delete(realDir);
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCanonicalPath_IsIdempotent_OnWindows()
        {
            // Calling GetCanonicalPath twice should return the same result
            var tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
            var first = PathUtil.GetCanonicalPath(tempDir);
            var second = PathUtil.GetCanonicalPath(first);
            Assert.Equal(first, second);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCanonicalPath_ReturnsSameResult_RegardlessOfInputCasing_OnWindows()
        {
            var tempDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
            if (tempDir.StartsWith(@"\\"))
            {
                return; // Skip UNC
            }

            var upper = tempDir.ToUpperInvariant();
            var lower = tempDir.ToLowerInvariant();

            var resultUpper = PathUtil.GetCanonicalPath(upper);
            var resultLower = PathUtil.GetCanonicalPath(lower);

            // Both should resolve to the same canonical path
            Assert.Equal(resultUpper, resultLower);
        }
#endif
    }
}
