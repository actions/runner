using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Sdk;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class SelfUpdaterL0
    {
        private Mock<IRunnerServer> _runnerServer;
        private Mock<ITerminal> _term;
        private Mock<IConfigurationStore> _configStore;
        private Mock<IJobDispatcher> _jobDispatcher;
        private AgentRefreshMessage _refreshMessage = new AgentRefreshMessage(1, "2.299.0");

#if !OS_WINDOWS
        private string _packageUrl = $"https://github.com/actions/runner/releases/download/v2.285.1/actions-runner-{BuildConstants.RunnerPackage.PackageName}-2.285.1.tar.gz";
#else
        private string _packageUrl = $"https://github.com/actions/runner/releases/download/v2.285.1/actions-runner-{BuildConstants.RunnerPackage.PackageName}-2.285.1.zip";
#endif
        public SelfUpdaterL0()
        {
            _runnerServer = new Mock<IRunnerServer>();
            _term = new Mock<ITerminal>();
            _configStore = new Mock<IConfigurationStore>();
            _jobDispatcher = new Mock<IJobDispatcher>();
            _configStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1, AgentId = 1 });

            _runnerServer.Setup(x => x.GetPackageAsync("agent", BuildConstants.RunnerPackage.PackageName, "2.299.0", true, It.IsAny<CancellationToken>()))
                         .Returns(Task.FromResult(new PackageMetadata() { Platform = BuildConstants.RunnerPackage.PackageName, Version = new PackageVersion("2.299.0"), DownloadUrl = _packageUrl }));

            Environment.SetEnvironmentVariable("_GITHUB_ACTION_EXECUTE_UPDATE_SCRIPT", "1");
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync()
        {
            try
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), "..", "_layout", "bin")));
                using (var hc = new TestHostContext(this))
                {
                    //Arrange
                    var updater = new Runner.Listener.SelfUpdater();
                    hc.SetSingleton<ITerminal>(_term.Object);
                    hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                    hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                    hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());

                    var p1 = new ProcessInvokerWrapper();
                    p1.Initialize(hc);
                    var p2 = new ProcessInvokerWrapper();
                    p2.Initialize(hc);
                    var p3 = new ProcessInvokerWrapper();
                    p3.Initialize(hc);
                    hc.EnqueueInstance<IProcessInvoker>(p1);
                    hc.EnqueueInstance<IProcessInvoker>(p2);
                    hc.EnqueueInstance<IProcessInvoker>(p3);
                    updater.Initialize(hc);

                    _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(1, 1, It.IsAny<string>(), It.IsAny<string>()))
                                 .Callback((int p, int a, string s, string t) =>
                                 {
                                     hc.GetTrace().Info(t);
                                 })
                                 .Returns(Task.FromResult(new TaskAgent()));

                    try
                    {
                        var result = await updater.SelfUpdate(_refreshMessage, _jobDispatcher.Object, true, hc.RunnerShutdownToken);
                        Assert.True(result);
                        Assert.True(Directory.Exists(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "bin.2.299.0")));
                        Assert.True(Directory.Exists(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "externals.2.299.0")));
                    }
                    finally
                    {
                        IOUtil.DeleteDirectory(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "bin.2.299.0"), CancellationToken.None);
                        IOUtil.DeleteDirectory(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "externals.2.299.0"), CancellationToken.None);
                    }
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", null);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_NoUpdateOnOldVersion()
        {
            try
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), "..", "_layout", "bin")));
                using (var hc = new TestHostContext(this))
                {
                    //Arrange
                    var updater = new Runner.Listener.SelfUpdater();
                    hc.SetSingleton<ITerminal>(_term.Object);
                    hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                    hc.SetSingleton<IConfigurationStore>(_configStore.Object);

                    var p1 = new ProcessInvokerWrapper();
                    p1.Initialize(hc);
                    var p2 = new ProcessInvokerWrapper();
                    p2.Initialize(hc);
                    var p3 = new ProcessInvokerWrapper();
                    p3.Initialize(hc);
                    hc.EnqueueInstance<IProcessInvoker>(p1);
                    hc.EnqueueInstance<IProcessInvoker>(p2);
                    hc.EnqueueInstance<IProcessInvoker>(p3);
                    updater.Initialize(hc);

                    _runnerServer.Setup(x => x.GetPackageAsync("agent", BuildConstants.RunnerPackage.PackageName, "2.200.0", true, It.IsAny<CancellationToken>()))
                             .Returns(Task.FromResult(new PackageMetadata() { Platform = BuildConstants.RunnerPackage.PackageName, Version = new PackageVersion("2.200.0"), DownloadUrl = _packageUrl }));

                    _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(1, 1, It.IsAny<string>(), It.IsAny<string>()))
                                 .Callback((int p, int a, string s, string t) =>
                                 {
                                     hc.GetTrace().Info(t);
                                 })
                                 .Returns(Task.FromResult(new TaskAgent()));

                    var result = await updater.SelfUpdate(new AgentRefreshMessage(1, "2.200.0"), _jobDispatcher.Object, true, hc.RunnerShutdownToken);
                    Assert.False(result);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", null);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_DownloadRetry()
        {
            try
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), "..", "_layout", "bin")));
                using (var hc = new TestHostContext(this))
                {
                    //Arrange
                    var updater = new Runner.Listener.SelfUpdater();
                    hc.SetSingleton<ITerminal>(_term.Object);
                    hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                    hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                    hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());

                    _runnerServer.Setup(x => x.GetPackageAsync("agent", BuildConstants.RunnerPackage.PackageName, "2.299.0", true, It.IsAny<CancellationToken>()))
                             .Returns(Task.FromResult(new PackageMetadata() { Platform = BuildConstants.RunnerPackage.PackageName, Version = new PackageVersion("2.299.0"), DownloadUrl = $"https://github.com/actions/runner/notexists" }));

                    var p1 = new ProcessInvokerWrapper();
                    p1.Initialize(hc);
                    var p2 = new ProcessInvokerWrapper();
                    p2.Initialize(hc);
                    var p3 = new ProcessInvokerWrapper();
                    p3.Initialize(hc);
                    hc.EnqueueInstance<IProcessInvoker>(p1);
                    hc.EnqueueInstance<IProcessInvoker>(p2);
                    hc.EnqueueInstance<IProcessInvoker>(p3);
                    updater.Initialize(hc);

                    _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(1, 1, It.IsAny<string>(), It.IsAny<string>()))
                                 .Callback((int p, int a, string s, string t) =>
                                 {
                                     hc.GetTrace().Info(t);
                                 })
                                 .Returns(Task.FromResult(new TaskAgent()));


                    var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => updater.SelfUpdate(_refreshMessage, _jobDispatcher.Object, true, hc.RunnerShutdownToken));
                    Assert.Contains($"failed after {Constants.RunnerDownloadRetryMaxAttempts} download attempts", ex.Message);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", null);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_ValidateHash()
        {
            try
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), "..", "_layout", "bin")));
                using (var hc = new TestHostContext(this))
                {
                    //Arrange
                    var updater = new Runner.Listener.SelfUpdater();
                    hc.SetSingleton<ITerminal>(_term.Object);
                    hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                    hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                    hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());

                    _runnerServer.Setup(x => x.GetPackageAsync("agent", BuildConstants.RunnerPackage.PackageName, "2.299.0", true, It.IsAny<CancellationToken>()))
                             .Returns(Task.FromResult(new PackageMetadata() { Platform = BuildConstants.RunnerPackage.PackageName, Version = new PackageVersion("2.299.0"), DownloadUrl = _packageUrl, HashValue = "bad_hash" }));

                    var p1 = new ProcessInvokerWrapper();
                    p1.Initialize(hc);
                    var p2 = new ProcessInvokerWrapper();
                    p2.Initialize(hc);
                    var p3 = new ProcessInvokerWrapper();
                    p3.Initialize(hc);
                    hc.EnqueueInstance<IProcessInvoker>(p1);
                    hc.EnqueueInstance<IProcessInvoker>(p2);
                    hc.EnqueueInstance<IProcessInvoker>(p3);
                    updater.Initialize(hc);

                    _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(1, 1, It.IsAny<string>(), It.IsAny<string>()))
                                 .Callback((int p, int a, string s, string t) =>
                                 {
                                     hc.GetTrace().Info(t);
                                 })
                                 .Returns(Task.FromResult(new TaskAgent()));


                    var ex = await Assert.ThrowsAsync<Exception>(() => updater.SelfUpdate(_refreshMessage, _jobDispatcher.Object, true, hc.RunnerShutdownToken));
                    Assert.Contains("did not match expected Runner Hash", ex.Message);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", null);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_CloneHash_RuntimeAndExternals()
        {
            try
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), "..", "_layout", "bin")));
                using (var hc = new TestHostContext(this))
                {
                    //Arrange
                    var updater = new Runner.Listener.SelfUpdater();
                    hc.SetSingleton<ITerminal>(_term.Object);
                    hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                    hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                    hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());

                    var p1 = new ProcessInvokerWrapper();
                    p1.Initialize(hc);
                    var p2 = new ProcessInvokerWrapper();
                    p2.Initialize(hc);
                    var p3 = new ProcessInvokerWrapper();
                    p3.Initialize(hc);
                    hc.EnqueueInstance<IProcessInvoker>(p1);
                    hc.EnqueueInstance<IProcessInvoker>(p2);
                    hc.EnqueueInstance<IProcessInvoker>(p3);
                    updater.Initialize(hc);

                    _runnerServer.Setup(x => x.GetPackageAsync("agent", BuildConstants.RunnerPackage.PackageName, "2.299.0", true, It.IsAny<CancellationToken>()))
                         .Returns(Task.FromResult(new PackageMetadata() { Platform = BuildConstants.RunnerPackage.PackageName, Version = new PackageVersion("2.299.0"), DownloadUrl = _packageUrl, TrimmedPackages = new List<TrimmedPackageMetadata>() { new TrimmedPackageMetadata() } }));

                    _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(1, 1, It.IsAny<string>(), It.IsAny<string>()))
                                 .Callback((int p, int a, string s, string t) =>
                                 {
                                     hc.GetTrace().Info(t);
                                 })
                                 .Returns(Task.FromResult(new TaskAgent()));

                    try
                    {
                        var result = await updater.SelfUpdate(_refreshMessage, _jobDispatcher.Object, true, hc.RunnerShutdownToken);
                        Assert.True(result);
                        Assert.True(Directory.Exists(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "bin.2.299.0")));
                        Assert.True(Directory.Exists(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "externals.2.299.0")));

                        FieldInfo contentHashesProperty = updater.GetType().GetField("_contentHashes", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        Assert.NotNull(contentHashesProperty);
                        Dictionary<string, string> contentHashes = (Dictionary<string, string>)contentHashesProperty.GetValue(updater);
                        hc.GetTrace().Info(StringUtil.ConvertToJson(contentHashes));

                        var dotnetRuntimeHashFile = Path.Combine(TestUtil.GetSrcPath(), $"Misc/contentHash/dotnetRuntime/{BuildConstants.RunnerPackage.PackageName}");
                        var externalsHashFile = Path.Combine(TestUtil.GetSrcPath(), $"Misc/contentHash/externals/{BuildConstants.RunnerPackage.PackageName}");

                        Assert.Equal(File.ReadAllText(dotnetRuntimeHashFile).Trim(), contentHashes["dotnetRuntime"]);
                        Assert.Equal(File.ReadAllText(externalsHashFile).Trim(), contentHashes["externals"]);
                    }
                    finally
                    {
                        IOUtil.DeleteDirectory(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "bin.2.299.0"), CancellationToken.None);
                        IOUtil.DeleteDirectory(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "externals.2.299.0"), CancellationToken.None);
                    }
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", null);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_Cancel_CloneHashTask_WhenNotNeeded()
        {
            try
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), "..", "_layout", "bin")));
                using (var hc = new TestHostContext(this))
                {
                    //Arrange
                    var updater = new Runner.Listener.SelfUpdater();
                    hc.SetSingleton<ITerminal>(_term.Object);
                    hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                    hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                    hc.SetSingleton<IHttpClientHandlerFactory>(new Mock<IHttpClientHandlerFactory>().Object);

                    var p1 = new ProcessInvokerWrapper();
                    p1.Initialize(hc);
                    var p2 = new ProcessInvokerWrapper();
                    p2.Initialize(hc);
                    var p3 = new ProcessInvokerWrapper();
                    p3.Initialize(hc);
                    hc.EnqueueInstance<IProcessInvoker>(p1);
                    hc.EnqueueInstance<IProcessInvoker>(p2);
                    hc.EnqueueInstance<IProcessInvoker>(p3);
                    updater.Initialize(hc);

                    _runnerServer.Setup(x => x.UpdateAgentUpdateStateAsync(1, 1, It.IsAny<string>(), It.IsAny<string>()))
                                 .Callback((int p, int a, string s, string t) =>
                                 {
                                     hc.GetTrace().Info(t);
                                 })
                                 .Returns(Task.FromResult(new TaskAgent()));

                    try
                    {
                        var result = await updater.SelfUpdate(_refreshMessage, _jobDispatcher.Object, true, hc.RunnerShutdownToken);

                        FieldInfo contentHashesProperty = updater.GetType().GetField("_contentHashes", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        Assert.NotNull(contentHashesProperty);
                        Dictionary<string, string> contentHashes = (Dictionary<string, string>)contentHashesProperty.GetValue(updater);
                        hc.GetTrace().Info(StringUtil.ConvertToJson(contentHashes));

                        Assert.NotEqual(2, contentHashes.Count);
                    }
                    catch(Exception ex)
                    {
                        hc.GetTrace().Error(ex);
                    }
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", null);
            }
        }
    }
}
