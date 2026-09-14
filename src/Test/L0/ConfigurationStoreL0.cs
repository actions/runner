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
                store.SaveMigratedSettings(oldSettings);

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
                store.SaveMigratedSettings(oldSettings);

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
                store.SaveMigratedSettings(settings);

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
                store.SaveMigratedSettings(settings);

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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SaveMigratedSettings_ReplacesExistingFileAndPreservesHiddenAttribute()
        {
            var originalOverrideBinDir = Environment.GetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR");
            var root = SetIsolatedBinDirectory();

            try
            {
                using (var hc = new TestHostContext(this))
                {
                    var store = new ConfigurationStore();
                    store.Initialize(hc);

                    var migratedSettingsPath = hc.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                    var previousSettings = new RunnerSettings { PoolId = 1, AgentId = 1, AgentName = "agent1" };
                    var nextSettings = new RunnerSettings { PoolId = 2, AgentId = 1, AgentName = "agent1" };

                    store.SaveMigratedSettings(previousSettings);
                    store.SaveMigratedSettings(nextSettings);

                    var savedSettings = IOUtil.LoadObject<RunnerSettings>(migratedSettingsPath);
                    Assert.Equal(nextSettings.PoolId, savedSettings.PoolId);
                    Assert.Equal(nextSettings.AgentId, savedSettings.AgentId);
                    Assert.Equal(nextSettings.AgentName, savedSettings.AgentName);
                    Assert.True((File.GetAttributes(migratedSettingsPath) & FileAttributes.Hidden) == FileAttributes.Hidden);
                    Assert.Empty(Directory.GetFiles(root, $".{Path.GetFileName(migratedSettingsPath)}.*.tmp"));
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", originalOverrideBinDir);
                DeleteDirectory(root);
            }
        }

#if !OS_WINDOWS
#pragma warning disable CA1416
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SaveMigratedSettings_ReplacesExistingFileAndPreservesUserOnlyUnixMode()
        {
            AssertMigratedSettingsPreservesUnixMode(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SaveMigratedSettings_ReplacesExistingFileAndPreservesGroupReadUnixMode()
        {
            AssertMigratedSettingsPreservesUnixMode(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SaveMigratedSettings_WriteFailurePreservesPreviousFile()
        {
            var originalOverrideBinDir = Environment.GetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR");
            var root = SetIsolatedBinDirectory();
            var originalMode = File.GetUnixFileMode(root);

            try
            {
                using (var hc = new TestHostContext(this))
                {
                    var store = new ConfigurationStore();
                    store.Initialize(hc);

                    var migratedSettingsPath = hc.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                    var previousSettings = new RunnerSettings { PoolId = 1, AgentId = 1, AgentName = "agent1" };
                    var nextSettings = new RunnerSettings { PoolId = 2, AgentId = 1, AgentName = "agent1" };
                    store.SaveMigratedSettings(previousSettings);
                    var previousFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                    File.SetUnixFileMode(migratedSettingsPath, previousFileMode);

                    File.SetUnixFileMode(
                        root,
                        originalMode & ~UnixFileMode.UserWrite & ~UnixFileMode.GroupWrite & ~UnixFileMode.OtherWrite);

                    Assert.ThrowsAny<Exception>(() => store.SaveMigratedSettings(nextSettings));

                    File.SetUnixFileMode(root, originalMode);
                    var savedSettings = IOUtil.LoadObject<RunnerSettings>(migratedSettingsPath);
                    Assert.Equal(previousSettings.PoolId, savedSettings.PoolId);
                    Assert.Equal(previousFileMode, File.GetUnixFileMode(migratedSettingsPath));
                    Assert.Empty(Directory.GetFiles(root, $".{Path.GetFileName(migratedSettingsPath)}.*.tmp"));
                }
            }
            finally
            {
                File.SetUnixFileMode(root, originalMode);
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", originalOverrideBinDir);
                DeleteDirectory(root);
            }
        }

        private void AssertMigratedSettingsPreservesUnixMode(UnixFileMode expectedMode)
        {
            var originalOverrideBinDir = Environment.GetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR");
            var root = SetIsolatedBinDirectory();

            try
            {
                using (var hc = new TestHostContext(this))
                {
                    var store = new ConfigurationStore();
                    store.Initialize(hc);

                    var migratedSettingsPath = hc.GetConfigFile(WellKnownConfigFile.MigratedRunner);
                    var previousSettings = new RunnerSettings { PoolId = 1, AgentId = 1, AgentName = "agent1" };
                    var nextSettings = new RunnerSettings { PoolId = 2, AgentId = 1, AgentName = "agent1" };
                    store.SaveMigratedSettings(previousSettings);
                    File.SetUnixFileMode(migratedSettingsPath, expectedMode);

                    store.SaveMigratedSettings(nextSettings);

                    var savedSettings = IOUtil.LoadObject<RunnerSettings>(migratedSettingsPath);
                    Assert.Equal(nextSettings.PoolId, savedSettings.PoolId);
                    Assert.Equal(expectedMode, File.GetUnixFileMode(migratedSettingsPath));
                    Assert.Empty(Directory.GetFiles(root, $".{Path.GetFileName(migratedSettingsPath)}.*.tmp"));
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", originalOverrideBinDir);
                DeleteDirectory(root);
            }
        }
#pragma warning restore CA1416
#endif

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

        private static string SetIsolatedBinDirectory()
        {
            var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D"));
            Directory.CreateDirectory(Path.Combine(root, "bin"));
            Environment.SetEnvironmentVariable("RUNNER_L0_OVERRIDEBINDIR", Path.Combine(root, "bin"));
            return root;
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}
