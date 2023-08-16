using System.IO;
using Xunit;
using GitHub.Runner.Sdk;
using System.Runtime.CompilerServices;

namespace GitHub.Runner.Common.Tests
{
    public static class TestUtil
    {
        private const string Src = "src";
        private const string TestData = "TestData";

        public static string GetProjectPath(string name = "Test")
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            string projectDir = Path.Combine(
                GetSrcPath(),
                name);
            Assert.True(Directory.Exists(projectDir));
            return projectDir;
        }

        public static string GetTestFilePath([CallerFilePath] string path = null)
        {
            return path;
        }

        public static string GetSrcPath()
        {
            string L0dir = Path.GetDirectoryName(GetTestFilePath());
            string testDir = Path.GetDirectoryName(L0dir);
            string srcDir = Path.GetDirectoryName(testDir);
            ArgUtil.Directory(srcDir, nameof(srcDir));
            Assert.Equal(Src, Path.GetFileName(srcDir));
            return srcDir;
        }

        public static string GetTestDataPath()
        {
            string testDataDir = Path.Combine(GetProjectPath(), TestData);
            Assert.True(Directory.Exists(testDataDir));
            return testDataDir;
        }
    }
}
