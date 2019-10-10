using GitHub.Runner.Common.Util;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using System;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Tests
{
    public sealed class ProxyConfigL0
    {
        private static readonly Regex NewHttpClientHandlerRegex = new Regex("New\\s+HttpClientHandler\\s*\\(", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NewHttpClientRegex = new Regex("New\\s+HttpClient\\s*\\(\\s*\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly List<string> SkippedFiles = new List<string>()
        {
            "Runner.Common\\HostContext.cs",
            "Runner.Common/HostContext.cs"
        };

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void IsNotUseRawHttpClientHandler()
        {
            List<string> sourceFiles = Directory.GetFiles(
                    TestUtil.GetProjectPath("Runner.Common"),
                    "*.cs",
                    SearchOption.AllDirectories).ToList();
            sourceFiles.AddRange(Directory.GetFiles(
                     TestUtil.GetProjectPath("Runner.Listener"),
                     "*.cs",
                     SearchOption.AllDirectories));
            sourceFiles.AddRange(Directory.GetFiles(
                    TestUtil.GetProjectPath("Runner.Worker"),
                    "*.cs",
                    SearchOption.AllDirectories));

            List<string> badCode = new List<string>();
            foreach (string sourceFile in sourceFiles)
            {
                // Skip skipped files.
                if (SkippedFiles.Any(s => sourceFile.Contains(s)))
                {
                    continue;
                }

                // Skip files in the obj directory.
                if (sourceFile.Contains(StringUtil.Format("{0}obj{0}", Path.DirectorySeparatorChar)))
                {
                    continue;
                }

                int lineCount = 0;
                foreach (string line in File.ReadAllLines(sourceFile))
                {
                    lineCount++;
                    if (NewHttpClientHandlerRegex.IsMatch(line))
                    {
                        badCode.Add($"{sourceFile} (line {lineCount})");
                    }
                }
            }

            Assert.True(badCode.Count == 0, $"The following code is using Raw HttpClientHandler() which will not follow the proxy setting agent have. Please use HostContext.CreateHttpClientHandler() instead.\n {string.Join("\n", badCode)}");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void IsNotUseRawHttpClient()
        {
            List<string> sourceFiles = Directory.GetFiles(
                    TestUtil.GetProjectPath("Runner.Common"),
                    "*.cs",
                    SearchOption.AllDirectories).ToList();
            sourceFiles.AddRange(Directory.GetFiles(
                     TestUtil.GetProjectPath("Runner.Listener"),
                     "*.cs",
                     SearchOption.AllDirectories));
            sourceFiles.AddRange(Directory.GetFiles(
                    TestUtil.GetProjectPath("Runner.Worker"),
                    "*.cs",
                    SearchOption.AllDirectories));

            List<string> badCode = new List<string>();
            foreach (string sourceFile in sourceFiles)
            {
                // Skip skipped files.
                if (SkippedFiles.Any(s => sourceFile.Contains(s)))
                {
                    continue;
                }

                // Skip files in the obj directory.
                if (sourceFile.Contains(StringUtil.Format("{0}obj{0}", Path.DirectorySeparatorChar)))
                {
                    continue;
                }

                int lineCount = 0;
                foreach (string line in File.ReadAllLines(sourceFile))
                {
                    lineCount++;
                    if (NewHttpClientRegex.IsMatch(line))
                    {
                        badCode.Add($"{sourceFile} (line {lineCount})");
                    }
                }
            }

            Assert.True(badCode.Count == 0, $"The following code is using Raw HttpClient() which will not follow the proxy setting agent have. Please use New HttpClient(HostContext.CreateHttpClientHandler()) instead.\n {string.Join("\n", badCode)}");
        }
    }
}
