using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;
using GitHub.Runner.Sdk;
using System.Linq;

namespace GitHub.Runner.Common.Tests
{
    public enum LineEndingType
    {
        Native,
        Linux   = 0x__0A,
        Windows = 0x0D0A
    }

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

        public static void WriteContent(string path, string content, LineEndingType lineEnding = LineEndingType.Native)
        {
            WriteContent(path, Enumerable.Repeat(content, 1), lineEnding);
        }

        public static void WriteContent(string path, IEnumerable<string> content, LineEndingType lineEnding = LineEndingType.Native)
        {
            string newline = lineEnding switch
            {
                LineEndingType.Linux   => "\n",
                LineEndingType.Windows => "\r\n",
                _ => Environment.NewLine,
            };
            var encoding = new UTF8Encoding(true); // Emit BOM
            var contentStr = string.Join(newline, content);
            File.WriteAllText(path, contentStr, encoding);
        }

    }
}
