using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Tests;
using GitHub.Runner.Listener;
using GitHub.Runner.Sdk;
using Moq;
using Xunit;

namespace GitHub.Runner.Tests.Listener
{
    public class RunnerConfigUpdaterL0
    {
        private Mock<IConfigurationStore> _configurationStore;
        private Mock<IRunnerServer> _runnerServer;

        public RunnerConfigUpdaterL0()
        {
            _configurationStore = new Mock<IConfigurationStore>();
            _runnerServer = new Mock<IRunnerServer>();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_InvalidRunnerQualifiedId_ShouldReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var invalidRunnerQualifiedId = "invalid/runner/qualified/id";
                var configType = "runner";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(invalidRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>((s) => s.Contains("Runner qualified id")), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_ValidRunnerQualifiedId_ShouldNotReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "runner";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>((s) => s.Contains("Runner qualified id")), It.IsAny<CancellationToken>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_InvalidConfigType_ShouldReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var invalidConfigType = "invalidConfigType";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, invalidConfigType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>((s) => s.Contains("Invalid config type")), It.IsAny<CancellationToken>()), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_UpdateRunnerSettings_ShouldSucceed()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));

                var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(StringUtil.ConvertToJson(setting)));
                _runnerServer.Setup(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.Is<string>(s => s == "runner"), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(encodedConfig);

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "runner";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.RefreshRunnerConfigAsync(1, "runner", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>(s => s.Contains("Runner settings updated successfully")), It.IsAny<CancellationToken>()), Times.Once);
                _configurationStore.Verify(x => x.SaveMigratedSettings(It.IsAny<RunnerSettings>()), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_UpdateRunnerSettings_IgnoredEmptyRefreshResult()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "runner";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.RefreshRunnerConfigAsync(1, "runner", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>(s => s.Contains("Runner settings updated successfully")), It.IsAny<CancellationToken>()), Times.Never);
                _configurationStore.Verify(x => x.SaveMigratedSettings(It.IsAny<RunnerSettings>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_UpdateRunnerCredentials_ShouldSucceed()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                var credData = new CredentialData
                {
                    Scheme = "OAuth"
                };
                credData.Data.Add("ClientId", "12345");
                _configurationStore.Setup(x => x.GetCredentials()).Returns(credData);

                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));
                IOUtil.SaveObject(credData, hc.GetConfigFile(WellKnownConfigFile.Credentials));

                var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(StringUtil.ConvertToJson(credData)));
                _runnerServer.Setup(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.Is<string>(s => s == "credentials"), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(encodedConfig);


                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "credentials";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.RefreshRunnerConfigAsync(1, "credentials", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>(s => s.Contains("Runner credentials updated successfully")), It.IsAny<CancellationToken>()), Times.Once);
                _configurationStore.Verify(x => x.SaveMigratedCredential(It.IsAny<CredentialData>()), Times.Once);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_UpdateRunnerCredentials_IgnoredEmptyRefreshResult()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                var credData = new CredentialData
                {
                    Scheme = "OAuth"
                };
                credData.Data.Add("ClientId", "12345");
                _configurationStore.Setup(x => x.GetCredentials()).Returns(credData);

                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));
                IOUtil.SaveObject(credData, hc.GetConfigFile(WellKnownConfigFile.Credentials));

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "credentials";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.RefreshRunnerConfigAsync(1, "credentials", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>(s => s.Contains("Runner credentials updated successfully")), It.IsAny<CancellationToken>()), Times.Never);
                _configurationStore.Verify(x => x.SaveMigratedCredential(It.IsAny<CredentialData>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_RefreshRunnerSettingsFailure_ShouldReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));
                _runnerServer.Setup(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.Is<string>(s => s == "runner"), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Refresh failed"));

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "runner";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>((s) => s.Contains("Failed to refresh")), It.IsAny<CancellationToken>()), Times.Once);
                _configurationStore.Verify(x => x.SaveMigratedSettings(It.IsAny<RunnerSettings>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_RefreshRunnerCredetialsFailure_ShouldReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                var credData = new CredentialData
                {
                    Scheme = "OAuth"
                };
                credData.Data.Add("ClientId", "12345");
                _configurationStore.Setup(x => x.GetCredentials()).Returns(credData);

                _runnerServer.Setup(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.Is<string>(s => s == "credentials"), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Refresh failed"));

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "credentials";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>((s) => s.Contains("Failed to refresh")), It.IsAny<CancellationToken>()), Times.Once);
                _configurationStore.Verify(x => x.SaveMigratedSettings(It.IsAny<RunnerSettings>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_RefreshRunnerSettingsWithDifferentRunnerId_ShouldReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));

                var differentRunnerSetting = new RunnerSettings { AgentId = 2, AgentName = "agent1" };
                var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(StringUtil.ConvertToJson(differentRunnerSetting)));
                _runnerServer.Setup(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.Is<string>(s => s == "runner"), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(encodedConfig);

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "runner";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>(s => s.Contains("Runner id in refreshed config")), It.IsAny<CancellationToken>()), Times.Once);
                _configurationStore.Verify(x => x.SaveMigratedSettings(It.IsAny<RunnerSettings>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_RefreshRunnerSettingsWithDifferentRunnerName_ShouldReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));

                var differentRunnerSetting = new RunnerSettings { AgentId = 1, AgentName = "agent2" };
                var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(StringUtil.ConvertToJson(differentRunnerSetting)));
                _runnerServer.Setup(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.Is<string>(s => s == "runner"), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(encodedConfig);

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "runner";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>(s => s.Contains("Runner name in refreshed config")), It.IsAny<CancellationToken>()), Times.Once);
                _configurationStore.Verify(x => x.SaveMigratedSettings(It.IsAny<RunnerSettings>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_RefreshCredentialsWithDifferentScheme_ShouldReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                var credData = new CredentialData
                {
                    Scheme = "OAuth"
                };
                credData.Data.Add("ClientId", "12345");
                _configurationStore.Setup(x => x.GetCredentials()).Returns(credData);

                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));
                IOUtil.SaveObject(credData, hc.GetConfigFile(WellKnownConfigFile.Credentials));

                var differentCredData = new CredentialData
                {
                    Scheme = "PAT"
                };
                differentCredData.Data.Add("ClientId", "12345");
                var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(StringUtil.ConvertToJson(differentCredData)));
                _runnerServer.Setup(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.Is<string>(s => s == "credentials"), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(encodedConfig);

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "credentials";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>(s => s.Contains("Credential scheme in refreshed config")), It.IsAny<CancellationToken>()), Times.Once);
                _configurationStore.Verify(x => x.SaveMigratedCredential(It.IsAny<CredentialData>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_RefreshOAuthCredentialsWithDifferentClientId_ShouldReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                var credData = new CredentialData
                {
                    Scheme = "OAuth"
                };
                credData.Data.Add("clientId", "12345");
                _configurationStore.Setup(x => x.GetCredentials()).Returns(credData);

                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));
                IOUtil.SaveObject(credData, hc.GetConfigFile(WellKnownConfigFile.Credentials));

                var differentCredData = new CredentialData
                {
                    Scheme = "OAuth"
                };
                differentCredData.Data.Add("clientId", "67890");
                var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(StringUtil.ConvertToJson(differentCredData)));
                _runnerServer.Setup(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.Is<string>(s => s == "credentials"), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(encodedConfig);

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "credentials";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>(s => s.Contains("Credential clientId in refreshed config")), It.IsAny<CancellationToken>()), Times.Once);
                _configurationStore.Verify(x => x.SaveMigratedCredential(It.IsAny<CredentialData>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_RefreshOAuthCredentialsWithDifferentAuthUrl_ShouldReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                var credData = new CredentialData
                {
                    Scheme = "OAuth"
                };
                credData.Data.Add("clientId", "12345");
                credData.Data.Add("authorizationUrl", "http://example.com/");
                _configurationStore.Setup(x => x.GetCredentials()).Returns(credData);

                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));
                IOUtil.SaveObject(credData, hc.GetConfigFile(WellKnownConfigFile.Credentials));

                var differentCredData = new CredentialData
                {
                    Scheme = "OAuth"
                };
                differentCredData.Data.Add("clientId", "12345");
                differentCredData.Data.Add("authorizationUrl", "http://example2.com/");
                var encodedConfig = Convert.ToBase64String(Encoding.UTF8.GetBytes(StringUtil.ConvertToJson(differentCredData)));
                _runnerServer.Setup(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.Is<string>(s => s == "credentials"), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(encodedConfig);

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "credentials";
                var serviceType = "pipelines";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>(s => s.Contains("Credential authorizationUrl in refreshed config")), It.IsAny<CancellationToken>()), Times.Once);
                _configurationStore.Verify(x => x.SaveMigratedCredential(It.IsAny<CredentialData>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_UnsupportedServiceType_ShouldReportTelemetry()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "runner";
                var serviceType = "unsupported-service";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>((s) => s.Contains("Invalid service type")), It.IsAny<CancellationToken>()), Times.Once);
                _runnerServer.Verify(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
                _configurationStore.Verify(x => x.SaveMigratedSettings(It.IsAny<RunnerSettings>()), Times.Never);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public async Task UpdateRunnerConfigAsync_RunnerAdminService_ShouldThrowNotSupported()
        {
            using (var hc = new TestHostContext(this))
            {
                hc.SetSingleton<IConfigurationStore>(_configurationStore.Object);
                hc.SetSingleton<IRunnerServer>(_runnerServer.Object);

                // Arrange
                var setting = new RunnerSettings { AgentId = 1, AgentName = "agent1" };
                _configurationStore.Setup(x => x.GetSettings()).Returns(setting);
                IOUtil.SaveObject(setting, hc.GetConfigFile(WellKnownConfigFile.Runner));

                var _runnerConfigUpdater = new RunnerConfigUpdater();
                _runnerConfigUpdater.Initialize(hc);

                var validRunnerQualifiedId = "valid/runner/qualifiedid/1";
                var configType = "runner";
                var serviceType = "runner-admin";
                var configRefreshUrl = "http://example.com";

                // Act
                await _runnerConfigUpdater.UpdateRunnerConfigAsync(validRunnerQualifiedId, configType, serviceType, configRefreshUrl);

                // Assert
                _runnerServer.Verify(x => x.UpdateAgentUpdateStateAsync(It.IsAny<int>(), It.IsAny<ulong>(), It.IsAny<string>(), It.Is<string>((s) => s.Contains("Runner admin service is not supported")), It.IsAny<CancellationToken>()), Times.Once);
                _runnerServer.Verify(x => x.RefreshRunnerConfigAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
                _configurationStore.Verify(x => x.SaveMigratedSettings(It.IsAny<RunnerSettings>()), Times.Never);
            }
        }
    }
}
