using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Util
{
    public sealed class IOUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void UseWhichFindGit()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                Tracing trace = hc.GetTrace();

                // Act.
                string gitPath = IOUtil.Which("git");

                trace.Info($"Which(\"git\") returns: {gitPath ?? string.Empty}");

                // Assert.
                Assert.True(!string.IsNullOrEmpty(gitPath) && File.Exists(gitPath), $"Unable to find Git through: {nameof(IOUtil.Which)}");
            }
        }
    }
}
