using GitHub.Services.Common;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using Xunit;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Tests.Util
{
    public sealed class VssUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CertificateValidationCallbackLogsFailuresAndRejectsInvalidCertificates()
        {
            string diagnostic = null;
            var callback = VssUtil.CreateCertificateValidationCallback(message => diagnostic = message);
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource?token=secret");

            var accepted = callback(request, null, null, SslPolicyErrors.RemoteCertificateNotAvailable);

            Assert.False(accepted);
            Assert.Contains("Host: 'example.test'", diagnostic);
            Assert.Contains("Policy errors: 'RemoteCertificateNotAvailable'", diagnostic);
            Assert.DoesNotContain("token=secret", diagnostic);
            Assert.Contains("UTC time:", diagnostic);
            Assert.Contains("Chain status: 'Unavailable'", diagnostic);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void CertificateValidationCallbackAcceptsValidCertificates()
        {
            var callback = VssUtil.CreateCertificateValidationCallback(_ => throw new InvalidOperationException("Should not log valid certificates."));

            var accepted = callback(new HttpRequestMessage(HttpMethod.Get, "https://example.test"), null, null, SslPolicyErrors.None);

            Assert.True(accepted);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyOverwriteVssConnectionSetting()
        {
            using (TestHostContext hc = new(this))
            {
                Tracing trace = hc.GetTrace();

                // Act.
                try
                {
                    trace.Info("Set httpretry to 10.");
                    Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_RETRY", "10");
                    trace.Info("Set httptimeout to 360.");
                    Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_TIMEOUT", "360");

                    var connect = VssUtil.CreateConnection(new Uri("https://github.com/actions/runner"), new VssCredentials());

                    // Assert.
                    Assert.Equal("10", connect.Settings.MaxRetryRequest.ToString());
                    Assert.Equal("360", connect.Settings.SendTimeout.TotalSeconds.ToString());

                    trace.Info("Set httpretry to 100.");
                    Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_RETRY", "100");
                    trace.Info("Set httptimeout to 3600.");
                    Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_TIMEOUT", "3600");

                    connect = VssUtil.CreateConnection(new Uri("https://github.com/actions/runner"), new VssCredentials());

                    // Assert.
                    Assert.Equal("10", connect.Settings.MaxRetryRequest.ToString());
                    Assert.Equal("1200", connect.Settings.SendTimeout.TotalSeconds.ToString());
                }
                finally
                {
                    Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_RETRY", "");
                    Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_TIMEOUT", "");
                }
            }
        }
    }
}
