using GitHub.Runner.Common.Util;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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
