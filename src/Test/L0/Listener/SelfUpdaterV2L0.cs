#if !(OS_WINDOWS && ARM64)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Listener;
using GitHub.Runner.Sdk;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class SelfUpdaterV2L0
    {
        private Mock<IRunnerServer> _runnerServer;
        private Mock<ITerminal> _term;
        private Mock<IConfigurationStore> _configStore;
        private Mock<IJobDispatcher> _jobDispatcher;
        private AgentRefreshMessage _refreshMessage = new(1, "2.999.0");
        private List<TrimmedPackageMetadata> _trimmedPackages = new();

#if !OS_WINDOWS
        private string _packageUrl = null;
#else
        private string _packageUrl = null;
#endif
        public SelfUpdaterV2L0()
        {
            _runnerServer = new Mock<IRunnerServer>();
            _term = new Mock<ITerminal>();
            _configStore = new Mock<IConfigurationStore>();
            _jobDispatcher = new Mock<IJobDispatcher>();
            _configStore.Setup(x => x.GetSettings()).Returns(new RunnerSettings() { PoolId = 1, AgentId = 1 });

            Environment.SetEnvironmentVariable("_GITHUB_ACTION_EXECUTE_UPDATE_SCRIPT", "1");
        }

        private async Task FetchLatestRunner()
        {
            var latestVersion = "";
            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.AllowAutoRedirect = false;
            using (var client = new HttpClient(httpClientHandler))
            {
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://github.com/actions/runner/releases/latest"));
                if (response.StatusCode == System.Net.HttpStatusCode.Redirect)
                {
                    var redirectUrl = response.Headers.Location.ToString();
                    Regex regex = new(@"/runner/releases/tag/v(?<version>\d+\.\d+\.\d+)");
                    var match = regex.Match(redirectUrl);
                    if (match.Success)
                    {
                        latestVersion = match.Groups["version"].Value;

#if !OS_WINDOWS
                        _packageUrl = $"https://github.com/actions/runner/releases/download/v{latestVersion}/actions-runner-{BuildConstants.RunnerPackage.PackageName}-{latestVersion}.tar.gz";
#else
                        _packageUrl = $"https://github.com/actions/runner/releases/download/v{latestVersion}/actions-runner-{BuildConstants.RunnerPackage.PackageName}-{latestVersion}.zip";
#endif
                    }
                    else
                    {
                        throw new Exception("The latest runner version could not be determined so a download URL could not be generated for it. Please check the location header of the redirect response of 'https://github.com/actions/runner/releases/latest'");
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async void TestSelfUpdateAsync()
        {
            try
            {
                await FetchLatestRunner();
                Assert.NotNull(_packageUrl);
                Assert.NotNull(_trimmedPackages);
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), "..", "_layout", "bin")));
                using (var hc = new TestHostContext(this))
                {
                    hc.GetTrace().Info(_packageUrl);
                    hc.GetTrace().Info(StringUtil.ConvertToJson(_trimmedPackages));

                    //Arrange
                    var updater = new Runner.Listener.SelfUpdaterV2();
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

                    try
                    {
                        var message = new RunnerRefreshMessage(1, "2.999.0")
                        {
                            Package = new RunnerRefreshMessage.BrokerPackageMetadata()
                            {
                                Platform = BuildConstants.RunnerPackage.PackageName,
                                DownloadUrl = _packageUrl
                            }
                        };

                        var result = await updater.SelfUpdate(message, _jobDispatcher.Object, true, hc.RunnerShutdownToken);
                        Assert.True(result);
                        Assert.True(Directory.Exists(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "bin.2.999.0")));
                        Assert.True(Directory.Exists(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "externals.2.999.0")));
                    }
                    finally
                    {
                        IOUtil.DeleteDirectory(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "bin.2.999.0"), CancellationToken.None);
                        IOUtil.DeleteDirectory(Path.Combine(hc.GetDirectory(WellKnownDirectory.Root), "externals.2.999.0"), CancellationToken.None);
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
        public async void TestSelfUpdateAsync_DownloadRetry()
        {
            try
            {
                await FetchLatestRunner();
                Assert.NotNull(_packageUrl);
                Assert.NotNull(_trimmedPackages);
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), "..", "_layout", "bin")));
                using (var hc = new TestHostContext(this))
                {
                    hc.GetTrace().Info(_packageUrl);
                    hc.GetTrace().Info(StringUtil.ConvertToJson(_trimmedPackages));

                    //Arrange
                    var updater = new Runner.Listener.SelfUpdaterV2();
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

                    var message = new RunnerRefreshMessage(1, "2.999.0")
                    {
                        Package = new RunnerRefreshMessage.BrokerPackageMetadata()
                        {
                            Platform = BuildConstants.RunnerPackage.PackageName,
                            DownloadUrl = "https://github.com/actions/runner/notexists"
                        }
                    };

                    var ex = await Assert.ThrowsAsync<TaskCanceledException>(() => updater.SelfUpdate(message, _jobDispatcher.Object, true, hc.RunnerShutdownToken));
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
                await FetchLatestRunner();
                Assert.NotNull(_packageUrl);
                Assert.NotNull(_trimmedPackages);
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", Path.GetFullPath(Path.Combine(TestUtil.GetSrcPath(), "..", "_layout", "bin")));
                using (var hc = new TestHostContext(this))
                {
                    hc.GetTrace().Info(_packageUrl);
                    hc.GetTrace().Info(StringUtil.ConvertToJson(_trimmedPackages));

                    //Arrange
                    var updater = new Runner.Listener.SelfUpdaterV2();
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

                    var message = new RunnerRefreshMessage(1, "2.999.0")
                    {
                        Package = new RunnerRefreshMessage.BrokerPackageMetadata()
                        {
                            Platform = BuildConstants.RunnerPackage.PackageName,
                            DownloadUrl = _packageUrl,
                            HashValue = "badhash"
                        }
                    };

                    var ex = await Assert.ThrowsAsync<Exception>(() => updater.SelfUpdate(message, _jobDispatcher.Object, true, hc.RunnerShutdownToken));
                    Assert.Contains("did not match expected Runner Hash", ex.Message);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", null);
            }
        }
    }
}
#endif
