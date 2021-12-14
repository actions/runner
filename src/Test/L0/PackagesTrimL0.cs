using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GitHub.Runner.Common.Util;
using System.Threading.Channels;
using GitHub.Runner.Sdk;
using System.Linq;

namespace GitHub.Runner.Common.Tests
{
    public sealed class PackagesTrimL0
    {

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunnerLayoutParts_NewFilesCrossAll()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                var runnerCoreAssertsFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnercoreassets");
                var runnerDotnetRuntimeFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnerdotnetruntimeasserts");
                string layoutBin = Path.Combine(TestUtil.GetSrcPath(), @"../_layout/bin");
                var newFiles = new List<string>();
                if (Directory.Exists(layoutBin))
                {
                    var coreAssets = await File.ReadAllLinesAsync(runnerCoreAssertsFile);
                    var runtimeAssets = await File.ReadAllLinesAsync(runnerDotnetRuntimeFile);
                    foreach (var file in Directory.GetFiles(layoutBin, "*", SearchOption.AllDirectories))
                    {
                        if (!coreAssets.Any(x => file.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).EndsWith(x)) &&
                            !runtimeAssets.Any(x => file.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).EndsWith(x)))
                        {
                            newFiles.Add(file);
                        }
                    }

                    if (newFiles.Count > 0)
                    {
                        Assert.True(false, $"Found new files '{string.Join('\n', newFiles)}'. These will break runner update using trimmed packages.");
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunnerLayoutParts_OverlapFiles()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                var runnerCoreAssertsFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnercoreassets");
                var runnerDotnetRuntimeFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnerdotnetruntimeasserts");

                var coreAssets = await File.ReadAllLinesAsync(runnerCoreAssertsFile);
                var runtimeAssets = await File.ReadAllLinesAsync(runnerDotnetRuntimeFile);

                foreach (var line in coreAssets)
                {
                    if (runtimeAssets.Contains(line, StringComparer.OrdinalIgnoreCase))
                    {
                        Assert.True(false, $"'Misc/runnercoreassets' and 'Misc/runnerdotnetruntimeasserts' should not overlap.");
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunnerLayoutParts_NewRunnerCoreAssets()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                var runnerCoreAssertsFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnercoreassets");
                var coreAssets = await File.ReadAllLinesAsync(runnerCoreAssertsFile);

                string layoutBin = Path.Combine(TestUtil.GetSrcPath(), @"../_layout/bin");
                var newFiles = new List<string>();
                if (Directory.Exists(layoutBin))
                {
                    var binDirs = Directory.GetDirectories(TestUtil.GetSrcPath(), "net6.0", SearchOption.AllDirectories);
                    foreach (var binDir in binDirs)
                    {
                        if (binDir.Contains("Test") || binDir.Contains("obj"))
                        {
                            continue;
                        }

                        Directory.GetFiles(binDir, "*", SearchOption.TopDirectoryOnly).ToList().ForEach(x =>
                        {
                            if (!x.Contains("runtimeconfig.dev.json"))
                            {
                                if (!coreAssets.Any(y => x.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).EndsWith(y)))
                                {
                                    newFiles.Add(x);
                                }
                            }
                        });
                    }

                    if (newFiles.Count > 0)
                    {
                        Assert.True(false, $"Found new files '{string.Join('\n', newFiles)}'. These will break runner update using trimmed packages. You might need to update `Misc/runnercoreassets`.");
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunnerLayoutParts_NewDotnetRuntimeAssets()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();
                var runnerDotnetRuntimeFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnerdotnetruntimeasserts");
                var runtimeAssets = await File.ReadAllLinesAsync(runnerDotnetRuntimeFile);

                string layoutTrimsRuntimeAsserts = Path.Combine(TestUtil.GetSrcPath(), @"../_layout_trims/runnerdotnetruntimeasserts");
                var newFiles = new List<string>();
                if (File.Exists(layoutTrimsRuntimeAsserts))
                {
                    var runtimeAssetsCurrent = await File.ReadAllLinesAsync(layoutTrimsRuntimeAsserts);
                    foreach (var runtimeFile in runtimeAssetsCurrent)
                    {
                        if (runtimeAssets.Any(x => runtimeFile.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                        else
                        {
                            newFiles.Add(runtimeFile);
                        }
                    }

                    if (newFiles.Count > 0)
                    {
                        Assert.True(false, $"Found new dotnet runtime files '{string.Join('\n', newFiles)}'. These will break runner update using trimmed packages. You might need to update `Misc/runnerdotnetruntimeasserts`.");
                    }
                }
            }
        }
    }
}
