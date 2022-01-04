using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Sdk;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using System.IO;

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
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var updater = new Runner.Listener.SelfUpdater();
                hc.SetSingleton<ITerminal>(_term.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
                hc.SetSingleton<IHttpClientHandlerFactory>(new HttpClientHandlerFactory());

                var p = new ProcessInvokerWrapper();
                p.Initialize(hc);
                hc.EnqueueInstance<IProcessInvoker>(p);
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_NoUpdateOnOldVersion()
        {
            using (var hc = new TestHostContext(this))
            {
                //Arrange
                var updater = new Runner.Listener.SelfUpdater();
                hc.SetSingleton<ITerminal>(_term.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);
                hc.SetSingleton<IConfigurationStore>(_configStore.Object);
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_DownloadRetry()
        {
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

                var p = new ProcessInvokerWrapper();
                p.Initialize(hc);
                hc.EnqueueInstance<IProcessInvoker>(p);
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync_ValidateHash()
        {
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

                var p = new ProcessInvokerWrapper();
                p.Initialize(hc);
                hc.EnqueueInstance<IProcessInvoker>(p);
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
    }
}
