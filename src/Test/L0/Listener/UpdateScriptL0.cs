#if !OS_WINDOWS
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class UpdateScriptL0
    {
        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [InlineData("node12", true)]
        [InlineData("node16", true)]
        [InlineData("node20", true)]
        [InlineData("node24", true)]
        [InlineData("node24", false)]
        public async Task RepairsServiceExecutableAcrossUpdates(string nodeVersion, bool previousPackageContainsRuntime)
        {
            string root = CreateRunner();
            try
            {
                string serviceNode = ServiceNodePath(root, nodeVersion);
                if (previousPackageContainsRuntime)
                {
                    string previousPackageNode = Path.Combine(root, "externals.2.0.0", nodeVersion, "bin", "node");
                    Directory.CreateDirectory(Path.GetDirectoryName(previousPackageNode));
                    File.WriteAllText(previousPackageNode, "original service executable");
                }
                string expectedExecutable = previousPackageContainsRuntime ? "original service executable" : "new node24 executable";

                await RunUpdate(root, serviceNode, "2.0.0", "2.1.0");
                Assert.True(File.Exists(serviceNode), "The service backing executable was not restored.");
                Assert.Equal(expectedExecutable, File.ReadAllText(serviceNode));
                Assert.Equal(expectedExecutable, File.ReadAllText(Path.Combine(root, "externals.2.1.0", $"RunnerService.{nodeVersion}")));

                // The next updater deletes old versions before running the new template.
                Directory.Delete(Path.Combine(root, "externals.1.0.0"), true);
                Directory.Delete(Path.Combine(root, "externals.2.0.0"), true);
                File.Delete(Path.Combine(root, "update.finished"));
                await RunUpdate(root, serviceNode, "2.1.0", "2.2.0");

                Assert.Equal(expectedExecutable, File.ReadAllText(serviceNode));
                Assert.Equal(expectedExecutable, File.ReadAllText(Path.Combine(root, "externals.2.2.0", $"RunnerService.{nodeVersion}")));
                if (nodeVersion != "node24")
                {
                    Assert.False(Directory.Exists(Path.Combine(root, "externals.2.2.0", nodeVersion)));
                }
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [InlineData("node12")]
        [InlineData("node16")]
        [InlineData("node20")]
        [InlineData("node24")]
        public async Task PreservesExistingServiceExecutable(string nodeVersion)
        {
            string root = CreateRunner();
            try
            {
                string serviceNode = ServiceNodePath(root, nodeVersion);
                Directory.CreateDirectory(Path.GetDirectoryName(serviceNode));
                File.WriteAllText(serviceNode, "running service executable");

                await RunUpdate(root, serviceNode, "2.0.0", "2.1.0");

                Assert.Equal("running service executable", File.ReadAllText(serviceNode));
                Assert.Equal("running service executable", File.ReadAllText(Path.Combine(root, "externals.2.1.0", $"RunnerService.{nodeVersion}")));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [InlineData("Linux", false)]
        [InlineData("Darwin", true)]
        public async Task SkipsServicePreservationForOtherUpdateModes(string platform, bool restartInteractiveRunner)
        {
            string root = CreateRunner();
            try
            {
                await RunUpdate(root, ServiceNodePath(root, "node20"), "2.0.0", "2.1.0", platform, restartInteractiveRunner);

                Assert.False(File.Exists(ServiceNodePath(root, "node20")));
                Assert.False(File.Exists(Path.Combine(root, "externals.2.1.0", "RunnerService.node20")));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task StopsPreservingLegacyExecutableAfterServiceRestartsOnNode24()
        {
            string root = CreateRunner();
            try
            {
                string serviceNode = ServiceNodePath(root, "node20");
                Directory.CreateDirectory(Path.GetDirectoryName(serviceNode));
                File.WriteAllText(serviceNode, "running node20 service executable");
                await RunUpdate(root, serviceNode, "2.0.0", "2.1.0");
                Assert.True(File.Exists(Path.Combine(root, "externals.2.1.0", "RunnerService.node20")));

                // The service has restarted using the installed Node24 executable.
                serviceNode = Path.Combine(root, "externals.2.1.0", "node24", "bin", "node");
                await RunUpdate(root, serviceNode, "2.1.0", "2.2.0");

                Assert.False(File.Exists(Path.Combine(root, "externals.2.2.0", "RunnerService.node20")));
                Assert.Equal("new node24 executable", File.ReadAllText(Path.Combine(root, "externals.2.2.0", "RunnerService.node24")));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ReportsServiceExecutableCopyFailure(bool serviceExecutableExists)
        {
            string root = CreateRunner();
            try
            {
                string serviceNode = ServiceNodePath(root, "node20");
                string source = serviceExecutableExists
                    ? serviceNode
                    : Path.Combine(root, "externals.2.0.0", "node20", "bin", "node");
                Directory.CreateDirectory(Path.GetDirectoryName(source));
                File.WriteAllText(source, "running service executable");
                WriteExecutable(Path.Combine(root, "stubs", "cp"), "#!/bin/bash\nexit 1\n");

                await RunUpdate(root, serviceNode, "2.0.0", "2.1.0", expectFailure: true);

                Assert.Equal(serviceExecutableExists, File.Exists(serviceNode));
                Assert.False(File.Exists(Path.Combine(root, "externals.2.1.0", "RunnerService.node20")));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task ReportsMissingServiceExecutable()
        {
            string root = CreateRunner();
            try
            {
                await RunUpdate(root, ServiceNodePath(root, "node20"), "2.0.0", "2.1.0", expectFailure: true);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static string ServiceNodePath(string root, string nodeVersion)
        {
            return Path.Combine(root, "externals.1.0.0", nodeVersion, "bin", "node");
        }

        private static string CreateRunner()
        {
            string root = Path.Combine(Path.GetTempPath(), $"runner update {Guid.NewGuid()}");
            Directory.CreateDirectory(Path.Combine(root, "_diag"));
            Directory.CreateDirectory(Path.Combine(root, "bin.2.0.0"));
            Directory.CreateDirectory(Path.Combine(root, "externals.2.0.0"));
            Directory.CreateDirectory(Path.Combine(root, "stubs"));
            Directory.CreateSymbolicLink(Path.Combine(root, "bin"), Path.Combine(root, "bin.2.0.0"));
            Directory.CreateSymbolicLink(Path.Combine(root, "externals"), Path.Combine(root, "externals.2.0.0"));
            File.WriteAllText(Path.Combine(root, "runsvc.sh"), "old service script");
            WriteExecutable(Path.Combine(root, "safe_sleep.sh"), "#!/bin/bash\nexit 0\n");
            WriteExecutable(Path.Combine(root, "run.sh"), "#!/bin/bash\nexit 0\n");
            WriteExecutable(Path.Combine(root, "stubs", "ps"), "#!/bin/bash\nprintf '42 %s/runsvc.sh\\n' \"$TEST_RUNNER_ROOT\"\n");
            WriteExecutable(Path.Combine(root, "stubs", "lsof"), "#!/bin/bash\nprintf 'n%s\\n' \"$TEST_SERVICE_NODE\"\n");
            WriteExecutable(Path.Combine(root, "stubs", "uname"), "#!/bin/bash\nprintf '%s\\n' \"$TEST_PLATFORM\"\n");
            return root;
        }

        private static void WriteExecutable(string path, string content)
        {
            File.WriteAllText(path, content);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        private async Task RunUpdate(string root, string serviceNode, string previousVersion, string targetVersion, string platform = "Darwin", bool restartInteractiveRunner = false, bool expectFailure = false)
        {
            Directory.CreateDirectory(Path.Combine(root, $"bin.{targetVersion}"));
            Directory.CreateDirectory(Path.Combine(root, $"externals.{targetVersion}", "node24", "bin"));
            File.WriteAllText(Path.Combine(root, $"bin.{targetVersion}", "runsvc.sh"), "node24 service script");
            File.WriteAllText(Path.Combine(root, $"externals.{targetVersion}", "node24", "bin", "node"), "new node24 executable");
            string script = File.ReadAllText(Path.Combine(TestUtil.GetSrcPath(), "Misc", "layoutbin", "update.sh.template"))
                .Replace("_PROCESS_ID_", int.MaxValue.ToString())
                .Replace("_RUNNER_PROCESS_NAME_", "Runner.Listener")
                .Replace("_ROOT_FOLDER_", root)
                .Replace("_EXIST_RUNNER_VERSION_", previousVersion)
                .Replace("_DOWNLOAD_RUNNER_VERSION_", targetVersion)
                .Replace("_UPDATE_LOG_", Path.Combine(root, "_diag", $"update-{targetVersion}.log"))
                .Replace("_RESTART_INTERACTIVE_RUNNER_", restartInteractiveRunner ? "1" : "0");
            string scriptPath = Path.Combine(root, "_update.sh");
            File.WriteAllText(scriptPath, script);
            var environment = new Dictionary<string, string>
            {
                { "PATH", Path.Combine(root, "stubs") + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH") },
                { "TEST_RUNNER_ROOT", root },
                { "TEST_SERVICE_NODE", serviceNode },
                { "TEST_PLATFORM", platform }
            };
            using var hc = new TestHostContext(this);
            using var process = new ProcessInvoker(hc.GetTrace());
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            int exitCode = await process.ExecuteAsync(root, "bash", $"\"{scriptPath}\"", environment, timeout.Token);
            if (expectFailure)
            {
                Assert.Equal(1, exitCode);
                Assert.False(File.Exists(Path.Combine(root, "update.finished")));
                string failedLog = Path.Combine(root, "_diag", $"update-{targetVersion}.log.failed");
                Assert.True(File.Exists(failedLog));
                Assert.Contains("DarwinRunnerUpgrade", File.ReadAllText(failedLog));
                return;
            }

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(Path.Combine(root, "update.finished")));
            Assert.Equal("node24 service script", File.ReadAllText(Path.Combine(root, "runsvc.sh")));
            Assert.Equal("new node24 executable", File.ReadAllText(Path.Combine(root, "externals", "node24", "bin", "node")));
        }
    }
}
#endif
