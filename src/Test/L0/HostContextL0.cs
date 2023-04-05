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
        private const string EXPECTED_SECRET_MASK = "***";

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
                Assert.Equal("OlBh***==", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($":Password123!"))));
                Assert.Equal("YTpQ***=", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($"a:Password123!"))));
                Assert.Equal("YWI6***", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($"ab:Password123!"))));
                Assert.Equal("YWJjOlBh***==", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($"abc:Password123!"))));
                Assert.Equal("YWJjZDpQ***=", _hc.SecretMasker.MaskSecrets(Convert.ToBase64String(Encoding.UTF8.GetBytes($"abcd:Password123!"))));
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
        public void Base64SecretMaskers()
        {

            // The following are good candidate strings for Base64 encoding because they include
            // both standard and RFC 4648 Base64 digits in all offset variations.
            //   TeLL? noboDy~ SEcreT?
            //   tElL~ NEVER~ neveR?
            //   TIGht? Tight~ guard~
            //   pRIVAte~ guARd? TIghT~
            //   KeeP~ TIgHT? tIgHT~
            //   LoCk? TiGhT~ TIght~
            //   DIvULGe~ nObODY~ noBOdy?
            //   foreVER~ Tight~ GUaRd?

            try
            {
                // Arrange.
                Setup();

                // Act.
                _hc.SecretMasker.AddValue("TeLL? noboDy~ SEcreT?");

                // The above string has the following Base64 variations based on the chop leading byte(s) method of Base64 aliasing:
                var base64Variations = new[]
                {
                    "VGVMTD8gbm9ib0R5fiBTRWNyZVQ/",
                    "ZUxMPyBub2JvRHl+IFNFY3JlVD8",
                    "TEw/IG5vYm9EeX4gU0VjcmVUPw",

                    // RFC 4648 (URL-safe Base64)
                    "VGVMTD8gbm9ib0R5fiBTRWNyZVQ_",
                    "ZUxMPyBub2JvRHl-IFNFY3JlVD8",
                    "TEw_IG5vYm9EeX4gU0VjcmVUPw"
                };

                var bookends = new[]
                {
                    (string.Empty, string.Empty),
                    (string.Empty, "="),
                    (string.Empty, "=="),
                    (string.Empty, "==="),
                    ("a", "z"),
                    ("A", "Z"),
                    ("abc", "abc"),
                    ("ABC", "ABC"),
                    ("0", "0"),
                    ("00", "00"),
                    ("000", "000"),
                    ("123", "789"),
                    ("`", "`"),
                    ("'", "'"),
                    ("\"", "\""),
                    ("[", "]"),
                    ("(", ")"),
                    ("$(", ")"),
                    ("{", "}"),
                    ("${", "}"),
                    ("!", "!"),
                    ("!!", "!!"),
                    ("%", "%"),
                    ("%%", "%%"),
                    ("_", "_"),
                    ("__", "__"),
                    (":", ":"),
                    ("::", "::"),
                    (";", ";"),
                    (";;", ";;"),
                    (":", string.Empty),
                    (";", string.Empty),
                    (string.Empty, ":"),
                    (string.Empty, ";"),
                    ("VGVMTD8gbm9ib", "ZUxMPy"),
                    ("VGVMTD8gbm9ib", "TEw/IG5vYm9EeX4"),
                    ("ZUxMPy", "TEw/IG5vYm9EeX4"),
                    ("VGVMTD8gbm9ib", string.Empty),
                    ("TEw/IG5vYm9EeX4", string.Empty),
                    ("ZUxMPy", string.Empty),
                    (string.Empty, "VGVMTD8gbm9ib"),
                    (string.Empty, "TEw/IG5vYm9EeX4"),
                    (string.Empty, "ZUxMPy"),
                };

                foreach (var variation in base64Variations)
                {
                    foreach (var pair in bookends)
                    {
                        var (prefix, suffix) = pair;
                        var expected = string.Format("{0}{1}{2}", prefix, EXPECTED_SECRET_MASK, suffix);
                        var payload = string.Format("{0}{1}{2}", prefix, variation, suffix);
                        Assert.Equal(expected, _hc.SecretMasker.MaskSecrets(payload));
                    }

                    // Verify no masking is performed on a partial match.
                    for (int i = 1; i < variation.Length - 1; i++)
                    {
                        var fragment = variation[..i];
                        Assert.Equal(fragment, _hc.SecretMasker.MaskSecrets(fragment));
                    }
                }
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
