using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    public sealed class HostContextL0
    {
        private HostContext _hc;
        private CancellationTokenSource _tokenSource;

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CreateServiceReturnsNewInstance()
        {
            try
            {
                // Arrange.
                Setup();

                // Act.
                var reference1 = _hc.CreateService<IRunnerServer>();
                var reference2 = _hc.CreateService<IRunnerServer>();

                // Assert.
                Assert.NotNull(reference1);
                Assert.IsType<RunnerServer>(reference1);
                Assert.NotNull(reference2);
                Assert.IsType<RunnerServer>(reference2);
                Assert.False(object.ReferenceEquals(reference1, reference2));
            }
            finally
            {
                // Cleanup.
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetServiceReturnsSingleton()
        {
            try
            {
                // Arrange.
                Setup();

                // Act.
                var reference1 = _hc.GetService<IRunnerServer>();
                var reference2 = _hc.GetService<IRunnerServer>();

                // Assert.
                Assert.NotNull(reference1);
                Assert.IsType<RunnerServer>(reference1);
                Assert.NotNull(reference2);
                Assert.True(object.ReferenceEquals(reference1, reference2));
            }
            finally
            {
                // Cleanup.
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void DefaultSecretMaskers()
        {
            try
            {
                // Arrange.
                Setup();

                // Act.
                _hc.SecretMasker.AddValue("Password123!");
                _hc.SecretMasker.AddValue("Pass\"word\"123!");
                _hc.SecretMasker.AddValue("Pass word 123!");
                _hc.SecretMasker.AddValue("Pass<word>123!");
                _hc.SecretMasker.AddValue("Pass'word'123!");
                _hc.SecretMasker.AddValue("\"Password123!!\"");
                _hc.SecretMasker.AddValue("\"short\"");

                // Assert.
                Assert.Equal("123***123", _hc.SecretMasker.MaskSecrets("123Password123!123"));
                Assert.Equal("password123", _hc.SecretMasker.MaskSecrets("password123"));
                Assert.Equal("123***123", _hc.SecretMasker.MaskSecrets("123Pass\\\"word\\\"123!123"));
                Assert.Equal("123***123", _hc.SecretMasker.MaskSecrets("123Pass%20word%20123%21123"));
                Assert.Equal("123***123", _hc.SecretMasker.MaskSecrets("123Pass&lt;word&gt;123!123"));
                Assert.Equal("123***123", _hc.SecretMasker.MaskSecrets("123Pass''word''123!123"));
                Assert.Equal("OlBh***", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($":Password123!"))));
                Assert.Equal("YTpQ***", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($"a:Password123!"))));
                Assert.Equal("YWI6***", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($"ab:Password123!"))));
                Assert.Equal("YWJjOlBh***", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($"abc:Password123!"))));
                Assert.Equal("YWJjZDpQ***", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($"abcd:Password123!"))));
                Assert.Equal("YWJjZGU6***", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($"abcde:Password123!"))));
                Assert.Equal("123***123", _hc.SecretMasker.MaskSecrets("123Password123!!123"));
                Assert.Equal("123short123", _hc.SecretMasker.MaskSecrets("123short123"));
                Assert.Equal("123***123", _hc.SecretMasker.MaskSecrets("123\"short\"123"));
            }
            finally
            {
                // Cleanup.
                Teardown();
            }
        }

        [Theory]
        [InlineData("secret&secret&secret", "secret&secret&\x0033[96msecret\x0033[0m", "***\x0033[96m***\x0033[0m")]
        [InlineData("secret&secret+secret", "secret&\x0033[96msecret+secret\x0033[0m", "***\x0033[96m***\x0033[0m")]
        [InlineData("secret+secret&secret", "secret+secret&\x0033[96msecret\x0033[0m", "***\x0033[96m***\x0033[0m")]
        [InlineData("secret&secret&+secretsecret", "secret&secret&+\x0033[96ms\x0033[0mecretsecret", "***\x0033[96ms\x0033[0m***")]
        [InlineData("secret&+secret&secret", "secret&+\x0033[96ms\x0033[0mecret&secret", "***\x0033[96ms\x0033[0m***")]
        [InlineData("secret&+secret&+secret", "secret&+\x0033[96ms\x0033[0mecret&+secret", "***\x0033[96ms\x0033[0m***")]
        [InlineData("secret&+secret&secret&+secret", "secret&+\x0033[96ms\x0033[0mecret&secret&+secret", "***\x0033[96ms\x0033[0m***")]
        [InlineData("secret&secret&+", "secret&secret&+\x0033[96m\x0033[0m", "***\x0033[96m\x0033[0m")]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SecretSectionMasking(string secret, string rawOutput, string maskedOutput)
        {
            try
            {
                // Arrange.
                Setup();

                // Act.
                _hc.SecretMasker.AddValue(secret);

                // Assert.
                Assert.Equal(maskedOutput, _hc.SecretMasker.MaskSecrets(rawOutput));
            }
            finally
            {
                // Cleanup.
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void SecretMaskerForProxy()
        {
            try
            {
                Environment.SetEnvironmentVariable("http_proxy", "http://user:password123@127.0.0.1:8888");

                // Arrange.
                Setup();

                // Assert.
                var logFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), $"trace_{nameof(HostContextL0)}_{nameof(SecretMaskerForProxy)}.log");
                var tempFile = Path.GetTempFileName();
                File.Delete(tempFile);
                File.Copy(logFile, tempFile);
                var content = File.ReadAllText(tempFile);
                Assert.DoesNotContain("password123", content);
                Assert.Contains("http://user:***@127.0.0.1:8888", content);
            }
            finally
            {
                Environment.SetEnvironmentVariable("http_proxy", null);
                // Cleanup.
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void AuthMigrationDisabledByDefault()
        {
            try
            {
                Environment.SetEnvironmentVariable("_GITHUB_ACTION_AUTH_MIGRATION_REFRESH_INTERVAL", "100");

                // Arrange.
                Setup();

                // Assert.
                Assert.False(_hc.AllowAuthMigration);

                // Change migration state is error free.
                _hc.EnableAuthMigration("L0Test");
                _hc.DeferAuthMigration(TimeSpan.FromHours(1), "L0Test");
            }
            finally
            {
                Environment.SetEnvironmentVariable("_GITHUB_ACTION_AUTH_MIGRATION_REFRESH_INTERVAL", null);
                // Cleanup.
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task AuthMigrationReenableTaskNotRunningByDefault()
        {
            try
            {
                Environment.SetEnvironmentVariable("_GITHUB_ACTION_AUTH_MIGRATION_REFRESH_INTERVAL", "50");

                // Arrange.
                Setup();

                // Assert.
                Assert.False(_hc.AllowAuthMigration);
                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }
            finally
            {
                Environment.SetEnvironmentVariable("_GITHUB_ACTION_AUTH_MIGRATION_REFRESH_INTERVAL", null);
                // Cleanup.
                Teardown();
            }

            var logFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), $"trace_{nameof(HostContextL0)}_{nameof(AuthMigrationReenableTaskNotRunningByDefault)}.log");
            var logContent = await File.ReadAllTextAsync(logFile);
            Assert.Contains("HostContext", logContent);
            Assert.DoesNotContain("Auth migration defer timer", logContent);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void AuthMigrationEnableDisable()
        {
            try
            {
                // Arrange.
                Setup();

                var eventFiredCount = 0;
                _hc.AuthMigrationChanged += (sender, e) =>
                {
                    eventFiredCount++;
                    Assert.Equal("L0Test", e.Trace);
                };

                // Assert.
                _hc.EnableAuthMigration("L0Test");
                Assert.True(_hc.AllowAuthMigration);

                _hc.DeferAuthMigration(TimeSpan.FromHours(1), "L0Test");
                Assert.False(_hc.AllowAuthMigration);
                Assert.Equal(2, eventFiredCount);
            }
            finally
            {
                // Cleanup.
                Teardown();
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task AuthMigrationAutoReset()
        {
            try
            {
                Environment.SetEnvironmentVariable("_GITHUB_ACTION_AUTH_MIGRATION_REFRESH_INTERVAL", "100");

                // Arrange.
                Setup();

                var eventFiredCount = 0;
                _hc.AuthMigrationChanged += (sender, e) =>
                {
                    eventFiredCount++;
                    Assert.NotEmpty(e.Trace);
                };

                // Assert.
                _hc.EnableAuthMigration("L0Test");
                Assert.True(_hc.AllowAuthMigration);

                _hc.DeferAuthMigration(TimeSpan.FromMilliseconds(500), "L0Test");
                Assert.False(_hc.AllowAuthMigration);

                await Task.Delay(TimeSpan.FromSeconds(1));
                Assert.True(_hc.AllowAuthMigration);
                Assert.Equal(3, eventFiredCount);
            }
            finally
            {
                Environment.SetEnvironmentVariable("_GITHUB_ACTION_AUTH_MIGRATION_REFRESH_INTERVAL", null);

                // Cleanup.
                Teardown();
            }
        }

        private void Setup([CallerMemberName] string testName = "")
        {
            _tokenSource = new CancellationTokenSource();
            _hc = new HostContext(
                hostType: "L0Test",
                logFile: Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), $"trace_{nameof(HostContextL0)}_{testName}.log"));
        }

        private void Teardown()
        {
            _hc?.Dispose();
            _tokenSource?.Dispose();
        }
    }
}
