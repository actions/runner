using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Xunit;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Util
{
    public sealed class ApiUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyUserAgentIsVstsAgent()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Act.
                var connect = ApiUtil.CreateConnection(new Uri("https://github.com/Microsoft/vsts-agent"), new VssCredentials());

                // Trace
                foreach (var ua in connect.Settings.UserAgent ?? new List<ProductInfoHeaderValue>())
                {
                    if (ua.Product != null)
                    {
                        trace.Info(ua.Product.Name);
                        trace.Info(ua.Product.Version);
                    }

                    if (!string.IsNullOrEmpty(ua.Comment))
                    {
                        trace.Info(ua.Comment);
                    }
                }

                // Assert.
                Assert.True(connect.Settings.UserAgent?.Exists(u => u.Product.Name.IndexOf("vstsagentcore", StringComparison.OrdinalIgnoreCase) >= 0));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void VerifyUserAgentHasPlatformInfo()
        {
            Regex _serverSideAgentPlatformMatchingRegex = new Regex("vstsagentcore-(.+)(?=/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Act.
                var connect = ApiUtil.CreateConnection(new Uri("https://github.com/Microsoft/vsts-agent"), new VssCredentials());

                string platformInfo = null;
                // Trace
                foreach (var ua in connect.Settings.UserAgent ?? new List<ProductInfoHeaderValue>())
                {
                    if (ua.Product != null)
                    {
                        trace.Info(ua.Product.Name);
                        trace.Info(ua.Product.Version);

                        if (ua.Product.Name.IndexOf("vstsagentcore", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            platformInfo = ua.Product.Name + '/' + ua.Product.Version;
                        }
                    }

                    if (!string.IsNullOrEmpty(ua.Comment))
                    {
                        trace.Info(ua.Comment);
                    }
                }

                // Assert.
                var regMatch = _serverSideAgentPlatformMatchingRegex.Match(platformInfo);
                Assert.True(regMatch.Success && regMatch.Groups != null && regMatch.Groups.Count == 2);
                string platform = regMatch.Groups[1].Value;
                List<string> validPackageNames = new List<string>()
                {
                    "win7-x64",
                    "ubuntu.14.04-x64",
                    "ubuntu.16.04-x64",
                    "centos.7-x64",
                    "rhel.7.2-x64",
                    "osx.10.11-x64"
                };
                Assert.True(validPackageNames.Contains(platform));
            }
        }
    }
}
