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
    public sealed class LocStringsL0
    {
        private static readonly Regex ValidKeyRegex = new Regex("^[_a-zA-Z0-9]+$");

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void IsNotMissingCommonLocStrings()
        {
            ValidateLocStrings(new TestHostContext(this), project: "Runner.Common");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void IsNotMissingListenerLocStrings()
        {
            ValidateLocStrings(new TestHostContext(this), project: "Runner.Listener");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IsNotMissingWorkerLocStrings()
        {
            ValidateLocStrings(new TestHostContext(this), project: "Runner.Worker");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "LocString")]
        public void IsLocStringsPrettyPrint()
        {
            // Load the strings.
            string stringsFile = Path.Combine(TestUtil.GetSrcPath(), "Misc", "layoutbin", "en-US", "strings.json");
            Assert.True(File.Exists(stringsFile), $"File does not exist: {stringsFile}");
            var resourceDictionary = IOUtil.LoadObject<Dictionary<string, object>>(stringsFile);

            // sort the dictionary.
            Dictionary<string, object> sortedResourceDictionary = new Dictionary<string, object>();
            foreach (var res in resourceDictionary.OrderBy(r => r.Key))
            {
                sortedResourceDictionary[res.Key] = res.Value;
            }

            // print to file.
            string prettyStringsFile = Path.Combine(TestUtil.GetSrcPath(), "Misc", "layoutbin", "en-US", "strings.json.pretty");
            IOUtil.SaveObject(sortedResourceDictionary, prettyStringsFile);

            Assert.True(string.Equals(File.ReadAllText(stringsFile), File.ReadAllText(prettyStringsFile)), $"Original string.json file: {stringsFile} is not pretty printed, replace it with: {prettyStringsFile}");

            // delete file on succeed
            File.Delete(prettyStringsFile);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "LocString")]
        public void FindExtraLocStrings()
        {
            // Load the strings.
            string stringsFile = Path.Combine(TestUtil.GetSrcPath(), "Misc", "layoutbin", "en-US", "strings.json");
            Assert.True(File.Exists(stringsFile), $"File does not exist: {stringsFile}");
            var resourceDictionary = IOUtil.LoadObject<Dictionary<string, object>>(stringsFile);

            // Find all loc string key in source file.
            //
            // Note, narrow the search to each project folder only. Otherwise intermittent errors occur
            // when recursively searching due to parallel tests are deleting temp folders (DirectoryNotFoundException).
            var keys = new List<string>();
            string[] sourceFiles =
                Directory.GetFiles(TestUtil.GetProjectPath("Runner.Common"), "*.cs", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(TestUtil.GetProjectPath("Runner.Listener"), "*.cs", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(TestUtil.GetProjectPath("Runner.Worker"), "*.cs", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(TestUtil.GetProjectPath("Runner.Plugins"), "*.cs", SearchOption.AllDirectories))
                .Concat(Directory.GetFiles(TestUtil.GetProjectPath("Runner.Sdk"), "*.cs", SearchOption.AllDirectories))
                .ToArray();
            foreach (string sourceFile in sourceFiles)
            {
                // Skip files in the obj directory.
                if (sourceFile.Contains(StringUtil.Format("{0}obj{0}", Path.DirectorySeparatorChar)))
                {
                    continue;
                }

                foreach (string line in File.ReadAllLines(sourceFile))
                {
                    // Search for calls to the StringUtil.Loc method within the line.
                    const string Pattern = "StringUtil.Loc(";
                    int searchIndex = 0;
                    int patternIndex;
                    while (searchIndex < line.Length &&
                        (patternIndex = line.IndexOf(Pattern, searchIndex)) >= 0)
                    {
                        // Bump the search index in preparation for the for the next iteration within the same line.
                        searchIndex = patternIndex + Pattern.Length;

                        // Extract the resource key.
                        int keyStartIndex = patternIndex + Pattern.Length;
                        int keyEndIndex;
                        if (keyStartIndex + 2 < line.Length &&  // Key should start with a ", be followed by at least
                            line[keyStartIndex] == '"' &&       // one character, and end with a ".
                            (keyEndIndex = line.IndexOf('"', keyStartIndex + 1)) > 0)
                        {
                            // Remove the first and last double quotes.
                            keyStartIndex++;
                            keyEndIndex--;
                            string key = line.Substring(
                                startIndex: keyStartIndex,
                                length: keyEndIndex - keyStartIndex + 1);
                            if (ValidKeyRegex.IsMatch(key))
                            {
                                // A valid key was extracted.
                                keys.Add(key);
                                continue;
                            }
                        }
                    }
                }
            }

            // find extra loc strings.
            var extraKeys = resourceDictionary.Keys.Where(x => !keys.Contains(x))?.ToList();

            string trimStringsFile = Path.Combine(TestUtil.GetSrcPath(), "Misc", "layoutbin", "en-US", "strings.json.trim");
            if (extraKeys != null)
            {
                if (extraKeys.Count != 0)
                {
                    foreach (var extra in extraKeys)
                    {
                        resourceDictionary.Remove(extra);
                    }


                    IOUtil.SaveObject(resourceDictionary, trimStringsFile);
                    Assert.True(extraKeys.Count == 0, $"Please save company's money by removing extra loc strings, replace {stringsFile} with: {trimStringsFile}");
                }
            }

            File.Delete(trimStringsFile);
        }

        private void ValidateLocStrings(TestHostContext hc, string project)
        {
            using (hc)
            {
                Tracing trace = hc.GetTrace();
                var keys = new List<string>();
                var badLines = new List<BadLineInfo>();

                // Search for source files within the project.
                trace.Verbose("Searching source files:");
                string[] sourceFiles = Directory.GetFiles(
                    TestUtil.GetProjectPath(project),
                    "*.cs",
                    SearchOption.AllDirectories);
                foreach (string sourceFile in sourceFiles)
                {
                    // Skip files in the obj directory.
                    if (sourceFile.Contains(StringUtil.Format("{0}obj{0}", Path.DirectorySeparatorChar)))
                    {
                        continue;
                    }

                    trace.Verbose($"  {sourceFile}");
                    foreach (string line in File.ReadAllLines(sourceFile))
                    {
                        // Search for calls to the StringUtil.Loc method within the line.
                        const string Pattern = "StringUtil.Loc(";
                        int searchIndex = 0;
                        int patternIndex;
                        while (searchIndex < line.Length &&
                            (patternIndex = line.IndexOf(Pattern, searchIndex)) >= 0)
                        {
                            // Bump the search index in preparation for the for the next iteration within the same line.
                            searchIndex = patternIndex + Pattern.Length;

                            // Extract the resource key.
                            int keyStartIndex = patternIndex + Pattern.Length;
                            int keyEndIndex;
                            if (keyStartIndex + 2 < line.Length &&  // Key should start with a ", be followed by at least
                                line[keyStartIndex] == '"' &&       // one character, and end with a ".
                                (keyEndIndex = line.IndexOf('"', keyStartIndex + 1)) > 0)
                            {
                                // Remove the first and last double quotes.
                                keyStartIndex++;
                                keyEndIndex--;
                                string key = line.Substring(
                                    startIndex: keyStartIndex,
                                    length: keyEndIndex - keyStartIndex + 1);
                                if (ValidKeyRegex.IsMatch(key))
                                {
                                    // A valid key was extracted.
                                    keys.Add(key);
                                    continue;
                                }
                            }

                            // Something went wrong. The pattern was found, but the resource key could not be determined.
                            badLines.Add(new BadLineInfo { File = sourceFile, Line = line });
                        }
                    }
                }

                // Load the strings.
                string stringsFile = Path.Combine(TestUtil.GetSrcPath(), "Misc", "layoutbin", "en-US", "strings.json");
                Assert.True(File.Exists(stringsFile), $"File does not exist: {stringsFile}");
                var resourceDictionary = IOUtil.LoadObject<Dictionary<string, object>>(stringsFile);

                // Find missing keys.
                string[] missingKeys =
                    keys
                    .Where(x => !resourceDictionary.ContainsKey(x))
                    .OrderBy(x => x)
                    .ToArray();
                if (missingKeys.Length > 0)
                {
                    trace.Error("One or more resource keys missing from resources file:");
                    foreach (string missingKey in missingKeys)
                    {
                        trace.Error($"  {missingKey}");
                    }
                }

                // Validate whether resource keys couldn't be interpreted.
                if (badLines.Count > 0)
                {
                    trace.Error("Bad lines detected. Unable to interpret resource key(s).");
                    IEnumerable<IGrouping<string, BadLineInfo>> badLineGroupings =
                        badLines
                        .GroupBy(x => x.File)
                        .OrderBy(x => x.Key)
                        .ToArray();
                    foreach (IGrouping<string, BadLineInfo> badLineGrouping in badLineGroupings)
                    {
                        trace.Error($"File: {badLineGrouping.First().File}");
                        foreach (BadLineInfo badLine in badLineGrouping)
                        {
                            trace.Error($"  Line: {badLine.Line}");
                        }
                    }
                }

                Assert.True(missingKeys.Length == 0, $"One or more resource keys missing from resources files. Consult the trace log: {hc.TraceFileName}");
                Assert.True(badLines.Count == 0, $"Unable to determine one or more resource keys. Consult the trace log: {hc.TraceFileName}");
            }
        }

        private sealed class BadLineInfo
        {
            public string File { get; set; }
            public string Line { get; set; }
        }
    }
}
