using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Xunit;

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
                    trace.Info(ua.Product.Name);
                    trace.Info(ua.Product.Version);
                    trace.Info(ua.Comment);
                }

                // Assert.
                Assert.True(connect.Settings.UserAgent?.Exists(u => u.Product.Name.IndexOf("vstsagentcore", StringComparison.OrdinalIgnoreCase) >= 0));
            }
        }
    }
}
