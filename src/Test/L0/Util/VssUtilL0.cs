using GitHub.Runner.Common.Util;
using GitHub.Services.Common;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Xunit;
using System.Text.RegularExpressions;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Tests.Util
{
    public sealed class VssUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyOverwriteVssConnectionSetting()
        {
            Regex _serverSideAgentPlatformMatchingRegex = new Regex("vstsagentcore-(.+)(?=/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Act.
                try
                {
                    trace.Info("Set httpretry to 10.");
                    Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_RETRY", "10");
                    trace.Info("Set httptimeout to 360.");
                    Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_TIMEOUT", "360");

                    var connect = VssUtil.CreateConnection(new Uri("https://github.com/Microsoft/vsts-agent"), new VssCredentials());

                    // Assert.
                    Assert.Equal(connect.Settings.MaxRetryRequest.ToString(), "10");
                    Assert.Equal(connect.Settings.SendTimeout.TotalSeconds.ToString(), "360");

                    trace.Info("Set httpretry to 100.");
                    Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_RETRY", "100");
                    trace.Info("Set httptimeout to 3600.");
                    Environment.SetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTP_TIMEOUT", "3600");

                    connect = VssUtil.CreateConnection(new Uri("https://github.com/Microsoft/vsts-agent"), new VssCredentials());

                    // Assert.
                    Assert.Equal(connect.Settings.MaxRetryRequest.ToString(), "10");
                    Assert.Equal(connect.Settings.SendTimeout.TotalSeconds.ToString(), "1200");
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
