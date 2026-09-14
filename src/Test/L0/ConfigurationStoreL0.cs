using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using GitHub.Runner.Sdk;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class ConfigurationStoreL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SaveMigratedSettings_UpdatesLoadedMigratedSettingsCache()
        {
            using (var fixture = CreateFixture())
            {
                string migratedConfigFile = fixture.HostContext.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                RunnerSettings oldSettings = CreateSettings("agent-a");
                RunnerSettings newSettings = CreateSettings("agent-b");
                IOUtil.SaveObject(oldSettings, migratedConfigFile);

                Assert.Equal("agent-a", fixture.Store.GetMigratedSettings().AgentName);

                fixture.Store.SaveMigratedSettings(newSettings);

                Assert.Equal("agent-b", fixture.Store.GetMigratedSettings().AgentName);
                Assert.Equal("agent-b", IOUtil.LoadObject<RunnerSettings>(migratedConfigFile).AgentName);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SaveMigratedSettings_WhenWriteFails_DoesNotExposeUnsavedSettings()
        {
            using (var fixture = CreateFixture())
            {
                string migratedConfigFile = fixture.HostContext.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                RunnerSettings oldSettings = CreateSettings("agent-a");
                RunnerSettings newSettings = CreateSettings("agent-b");
                IOUtil.SaveObject(oldSettings, migratedConfigFile);

                Assert.Equal("agent-a", fixture.Store.GetMigratedSettings().AgentName);
                File.Delete(migratedConfigFile);
                Directory.CreateDirectory(migratedConfigFile);

                try
                {
                    Assert.ThrowsAny<Exception>(() => fixture.Store.SaveMigratedSettings(newSettings));
                    Assert.Equal("agent-a", fixture.Store.GetMigratedSettings().AgentName);
                }
                finally
                {
                    if (Directory.Exists(migratedConfigFile))
                    {
                        Directory.Delete(migratedConfigFile);
                    }
                }
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteMigratedSettings_InvalidatesLoadedMigratedSettingsCache()
        {
            using (var fixture = CreateFixture())
            {
                string migratedConfigFile = fixture.HostContext.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                RunnerSettings settings = CreateSettings("agent-a");
                IOUtil.SaveObject(settings, migratedConfigFile);

                Assert.Equal("agent-a", fixture.Store.GetMigratedSettings().AgentName);

                fixture.Store.DeleteMigratedSettings();

                Assert.False(File.Exists(migratedConfigFile));
                Assert.Throws<ArgumentNullException>(() => fixture.Store.GetMigratedSettings());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteSettings_InvalidatesLoadedMigratedSettingsCache()
        {
            using (var fixture = CreateFixture())
            {
                string migratedConfigFile = fixture.HostContext.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                RunnerSettings settings = CreateSettings("agent-a");
                IOUtil.SaveObject(settings, migratedConfigFile);

                Assert.Equal("agent-a", fixture.Store.GetMigratedSettings().AgentName);

                fixture.Store.DeleteSettings();

                Assert.False(File.Exists(migratedConfigFile));
                Assert.Throws<ArgumentNullException>(() => fixture.Store.GetMigratedSettings());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SaveMigratedSettings_DoesNotChangeOriginalSettings()
        {
            using (var fixture = CreateFixture())
            {
                string configFile = fixture.HostContext.GetConfigFile(WellKnownConfigFile.Runner);
                RunnerSettings originalSettings = CreateSettings("agent-a");
                RunnerSettings migratedSettings = CreateSettings("agent-b");
                IOUtil.SaveObject(originalSettings, configFile);

                Assert.Equal("agent-a", fixture.Store.GetSettings().AgentName);

                fixture.Store.SaveMigratedSettings(migratedSettings);

                Assert.Equal("agent-a", fixture.Store.GetSettings().AgentName);
                Assert.Equal("agent-a", IOUtil.LoadObject<RunnerSettings>(configFile).AgentName);
            }
        }

        private ConfigurationStoreFixture CreateFixture([CallerMemberName] string testName = "")
        {
            return new ConfigurationStoreFixture(this, testName);
        }

        private static RunnerSettings CreateSettings(string agentName)
        {
            return new RunnerSettings
            {
                AgentId = 1,
                AgentName = agentName,
                PoolId = 1,
                ServerUrl = "https://example.test/org",
                WorkFolder = "_work",
            };
        }

        private sealed class ConfigurationStoreFixture : IDisposable
        {
            private readonly string _previousBinOverride;
            private readonly string _rootDirectory;

            public ConfigurationStoreFixture(object testClass, string testName)
            {
                _previousBinOverride = Environment.GetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR");
                _rootDirectory = Path.Combine(Path.GetTempPath(), nameof(ConfigurationStoreL0), Guid.NewGuid().ToString("D"));
                string binDirectory = Path.Combine(_rootDirectory, "bin");
                Directory.CreateDirectory(binDirectory);
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", binDirectory);

                HostContext = new TestHostContext(testClass, testName);
                Store = new ConfigurationStore();
                Store.Initialize(HostContext);
            }

            public TestHostContext HostContext { get; }

            public ConfigurationStore Store { get; }

            public void Dispose()
            {
                HostContext.Dispose();
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", _previousBinOverride);
                IOUtil.Delete(_rootDirectory, CancellationToken.None);
            }
        }
    }
}
