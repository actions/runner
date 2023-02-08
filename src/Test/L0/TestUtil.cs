using System.IO;
using Xunit;
using GitHub.Runner.Sdk;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;

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

        public static IReadOnlyIssue CreateTestIssue(IssueType type, string message, IssueMetadata metadata, bool writeToLog)
        {
            var result = new Issue()
            {
                Type = type,
                Message = message,
            };

            if (metadata != null)
            {
                result.Category = metadata.Category;
                result.IsInfrastructureIssue = metadata.IsInfrastructureIssue;
                foreach (var kvp in metadata.Data)
                {
                    result[kvp.Key] = kvp.Value;
                }
            }

            return result;
        }
    }
}
