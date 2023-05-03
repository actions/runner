using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class PackagesTrimL0
    {

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunnerLayoutParts_NewFilesCrossAll()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();
                var runnerCoreAssetsFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnercoreassets");
                var runnerDotnetRuntimeFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnerdotnetruntimeassets");
                string layoutBin = Path.Combine(TestUtil.GetSrcPath(), @"../_layout/bin");
                var newFiles = new List<string>();
                if (Directory.Exists(layoutBin))
                {
                    var coreAssets = await File.ReadAllLinesAsync(runnerCoreAssetsFile);
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
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();
                var runnerCoreAssetsFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnercoreassets");
                var runnerDotnetRuntimeFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnerdotnetruntimeassets");

                var coreAssets = await File.ReadAllLinesAsync(runnerCoreAssetsFile);
                var runtimeAssets = await File.ReadAllLinesAsync(runnerDotnetRuntimeFile);

                foreach (var line in coreAssets)
                {
                    if (runtimeAssets.Contains(line, StringComparer.OrdinalIgnoreCase))
                    {
                        Assert.True(false, $"'Misc/runnercoreassets' and 'Misc/runnerdotnetruntimeassets' should not overlap.");
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunnerLayoutParts_NewRunnerCoreAssets()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();
                var runnerCoreAssetsFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnercoreassets");
                var coreAssets = await File.ReadAllLinesAsync(runnerCoreAssetsFile);

                string layoutBin = Path.Combine(TestUtil.GetSrcPath(), @"../_layout/bin");
                var newFiles = new List<string>();
                if (Directory.Exists(layoutBin))
                {
                    var binDirs = Directory.GetDirectories(TestUtil.GetSrcPath(), "net7.0", SearchOption.AllDirectories);
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
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();
                var runnerDotnetRuntimeFile = Path.Combine(TestUtil.GetSrcPath(), @"Misc/runnerdotnetruntimeassets");
                var runtimeAssets = await File.ReadAllLinesAsync(runnerDotnetRuntimeFile);

                string layoutTrimsRuntimeAssets = Path.Combine(TestUtil.GetSrcPath(), @"../_layout_trims/runnerdotnetruntimeassets");
                var newFiles = new List<string>();
                if (File.Exists(layoutTrimsRuntimeAssets))
                {
                    var runtimeAssetsCurrent = await File.ReadAllLinesAsync(layoutTrimsRuntimeAssets);
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
                        Assert.True(false, $"Found new dotnet runtime files '{string.Join('\n', newFiles)}'. These will break runner update using trimmed packages. You might need to update `Misc/runnerdotnetruntimeassets`.");
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunnerLayoutParts_CheckDotnetRuntimeHash()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();
                var dotnetRuntimeHashFile = Path.Combine(TestUtil.GetSrcPath(), $"Misc/contentHash/dotnetRuntime/{BuildConstants.RunnerPackage.PackageName}");
                trace.Info($"Current hash: {File.ReadAllText(dotnetRuntimeHashFile)}");
                string layoutTrimsRuntimeAssets = Path.Combine(TestUtil.GetSrcPath(), @"../_layout_trims/runtime");

                string binDir = Path.Combine(TestUtil.GetSrcPath(), @"../_layout/bin");

#if OS_WINDOWS
                string node = Path.Combine(TestUtil.GetSrcPath(), @"..\_layout\externals\node16\bin\node");
#else
                string node = Path.Combine(TestUtil.GetSrcPath(), @"../_layout/externals/node16/bin/node");
#endif
                string hashFilesScript = Path.Combine(binDir, "hashFiles");
                var hashResult = string.Empty;

                var p1 = new ProcessInvokerWrapper();
                p1.Initialize(hc);

                p1.ErrorDataReceived += (_, data) =>
                {
                    if (!string.IsNullOrEmpty(data.Data) && data.Data.StartsWith("__OUTPUT__") && data.Data.EndsWith("__OUTPUT__"))
                    {
                        hashResult = data.Data.Substring(10, data.Data.Length - 20);
                        trace.Info($"Hash result: '{hashResult}'");
                    }
                    else
                    {
                        trace.Info(data.Data);
                    }
                };

                p1.OutputDataReceived += (_, data) =>
                {
                    trace.Info(data.Data);
                };

                var env = new Dictionary<string, string>
                {
                    ["patterns"] = "**"
                };

                int exitCode = await p1.ExecuteAsync(workingDirectory: layoutTrimsRuntimeAssets,
                                              fileName: node,
                                              arguments: $"\"{hashFilesScript.Replace("\"", "\\\"")}\"",
                                              environment: env,
                                              requireExitCodeZero: true,
                                              outputEncoding: null,
                                              killProcessOnCancel: true,
                                              cancellationToken: CancellationToken.None);

                Assert.True(string.Equals(hashResult, File.ReadAllText(dotnetRuntimeHashFile).Trim()), $"Hash mismatch for dotnet runtime. You might need to update `Misc/contentHash/dotnetRuntime/{BuildConstants.RunnerPackage.PackageName}` or check if `hashFiles.ts` ever changed recently.");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task RunnerLayoutParts_CheckExternalsHash()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();
                var externalsHashFile = Path.Combine(TestUtil.GetSrcPath(), $"Misc/contentHash/externals/{BuildConstants.RunnerPackage.PackageName}");
                trace.Info($"Current hash: {File.ReadAllText(externalsHashFile)}");

                string layoutTrimsExternalsAssets = Path.Combine(TestUtil.GetSrcPath(), @"../_layout_trims/externals");

                string binDir = Path.Combine(TestUtil.GetSrcPath(), @"../_layout/bin");

#if OS_WINDOWS
                string node = Path.Combine(TestUtil.GetSrcPath(), @"..\_layout\externals\node16\bin\node");
#else
                string node = Path.Combine(TestUtil.GetSrcPath(), @"../_layout/externals/node16/bin/node");
#endif
                string hashFilesScript = Path.Combine(binDir, "hashFiles");
                var hashResult = string.Empty;

                var p1 = new ProcessInvokerWrapper();
                p1.Initialize(hc);

                p1.ErrorDataReceived += (_, data) =>
                {
                    if (!string.IsNullOrEmpty(data.Data) && data.Data.StartsWith("__OUTPUT__") && data.Data.EndsWith("__OUTPUT__"))
                    {
                        hashResult = data.Data.Substring(10, data.Data.Length - 20);
                        trace.Info($"Hash result: '{hashResult}'");
                    }
                    else
                    {
                        trace.Info(data.Data);
                    }
                };

                p1.OutputDataReceived += (_, data) =>
                {
                    trace.Info(data.Data);
                };

                var env = new Dictionary<string, string>
                {
                    ["patterns"] = "**"
                };

                int exitCode = await p1.ExecuteAsync(workingDirectory: layoutTrimsExternalsAssets,
                                              fileName: node,
                                              arguments: $"\"{hashFilesScript.Replace("\"", "\\\"")}\"",
                                              environment: env,
                                              requireExitCodeZero: true,
                                              outputEncoding: null,
                                              killProcessOnCancel: true,
                                              cancellationToken: CancellationToken.None);

                Assert.True(string.Equals(hashResult, File.ReadAllText(externalsHashFile).Trim()), $"Hash mismatch for externals. You might need to update `Misc/contentHash/externals/{BuildConstants.RunnerPackage.PackageName}` or check if `hashFiles.ts` ever changed recently.");
            }
        }
    }
}
