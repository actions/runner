using Microsoft.VisualStudio.Services.Agent.Util;
using System.IO;
using Xunit;
using System;

namespace Microsoft.VisualStudio.Services.Agent.Tests
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

        public static string GetSrcPath()
        {
            string srcDir = Environment.GetEnvironmentVariable("VSTS_AGENT_SRC_DIR");
            if (String.IsNullOrEmpty(srcDir))
            {
                srcDir = Path.GetDirectoryName(
                            Path.GetDirectoryName(
                                Path.GetDirectoryName(
                                    Path.GetDirectoryName(
                                        Path.GetDirectoryName(
                                            Path.GetDirectoryName(
                                                IOUtil.GetBinPath()))))));
            }
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
