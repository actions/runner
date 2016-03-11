using Microsoft.VisualStudio.Services.Agent.Util;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public static class TestUtil
    {
        private const string TestData = "TestData";

        public static string GetTestDataPath()
        {
            string projectDir =
                Path.GetDirectoryName(
                    Path.GetDirectoryName(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(
                                Path.GetDirectoryName(
                                    IOUtil.GetBinPath())))));
            string testDataDir = Path.Combine(projectDir, TestData);
            Assert.True(Directory.Exists(testDataDir));
            return testDataDir;
        }
    }
}
