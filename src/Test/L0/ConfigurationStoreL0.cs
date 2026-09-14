using System;
using System.IO;
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
            using (TestHostContext hc = new(this))
            {
                var store = CreateStore(hc);
                string migratedConfigFile = hc.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                RunnerSettings oldSettings = CreateSettings("agent-a");
                RunnerSettings newSettings = CreateSettings("agent-b");
                IOUtil.SaveObject(oldSettings, migratedConfigFile);

                Assert.Equal("agent-a", store.GetMigratedSettings().AgentName);

                store.SaveMigratedSettings(newSettings);

                Assert.Equal("agent-b", store.GetMigratedSettings().AgentName);
                Assert.Equal("agent-b", IOUtil.LoadObject<RunnerSettings>(migratedConfigFile).AgentName);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SaveMigratedSettings_WhenWriteFails_DoesNotExposeUnsavedSettings()
        {
            using (TestHostContext hc = new(this))
            {
                var store = CreateStore(hc);
                string migratedConfigFile = hc.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                RunnerSettings oldSettings = CreateSettings("agent-a");
                RunnerSettings newSettings = CreateSettings("agent-b");
                IOUtil.SaveObject(oldSettings, migratedConfigFile);

                Assert.Equal("agent-a", store.GetMigratedSettings().AgentName);
                File.Delete(migratedConfigFile);
                Directory.CreateDirectory(migratedConfigFile);

                try
                {
                    Assert.ThrowsAny<Exception>(() => store.SaveMigratedSettings(newSettings));
                    Assert.Equal("agent-a", store.GetMigratedSettings().AgentName);
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
            using (TestHostContext hc = new(this))
            {
                var store = CreateStore(hc);
                string migratedConfigFile = hc.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                RunnerSettings settings = CreateSettings("agent-a");
                IOUtil.SaveObject(settings, migratedConfigFile);

                Assert.Equal("agent-a", store.GetMigratedSettings().AgentName);

                store.DeleteMigratedSettings();

                Assert.False(File.Exists(migratedConfigFile));
                Assert.Throws<ArgumentNullException>(() => store.GetMigratedSettings());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DeleteSettings_InvalidatesLoadedMigratedSettingsCache()
        {
            using (TestHostContext hc = new(this))
            {
                var store = CreateStore(hc);
                string migratedConfigFile = hc.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                RunnerSettings settings = CreateSettings("agent-a");
                IOUtil.SaveObject(settings, migratedConfigFile);

                Assert.Equal("agent-a", store.GetMigratedSettings().AgentName);

                store.DeleteSettings();

                Assert.False(File.Exists(migratedConfigFile));
                Assert.Throws<ArgumentNullException>(() => store.GetMigratedSettings());
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SaveMigratedSettings_DoesNotChangeOriginalSettings()
        {
            using (TestHostContext hc = new(this))
            {
                var store = CreateStore(hc);
                string configFile = hc.GetConfigFile(WellKnownConfigFile.Runner);
                RunnerSettings originalSettings = CreateSettings("agent-a");
                RunnerSettings migratedSettings = CreateSettings("agent-b");
                IOUtil.SaveObject(originalSettings, configFile);

                Assert.Equal("agent-a", store.GetSettings().AgentName);

                store.SaveMigratedSettings(migratedSettings);

                Assert.Equal("agent-a", store.GetSettings().AgentName);
                Assert.Equal("agent-a", IOUtil.LoadObject<RunnerSettings>(configFile).AgentName);
            }
        }

        private static ConfigurationStore CreateStore(TestHostContext hc)
        {
            var store = new ConfigurationStore();
            store.Initialize(hc);
            return store;
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
    }
}
