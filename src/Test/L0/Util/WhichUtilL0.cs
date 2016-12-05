using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Util
{
    public sealed class WhichUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void UseWhichFindGit()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                //Arrange
                Tracing trace = hc.GetTrace();
                var whichTool = new WhichUtil();
                whichTool.Initialize(hc);

                // Act.
                string gitPath = whichTool.Which("git");

                trace.Info($"Which(\"git\") returns: {gitPath ?? string.Empty}");

                // Assert.
                Assert.True(!string.IsNullOrEmpty(gitPath) && File.Exists(gitPath), $"Unable to find Git through: {nameof(WhichUtil.Which)}");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WhichReturnsNullWhenNotFound()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                //Arrange
                Tracing trace = hc.GetTrace();
                var whichUtil = new WhichUtil();
                whichUtil.Initialize(hc);

                // Act.
                string nosuch = whichUtil.Which("no-such-file-cf7e351f");

                trace.Info($"result: {nosuch ?? string.Empty}");

                // Assert.
                Assert.True(string.IsNullOrEmpty(nosuch), "Path should not be resolved");
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void WhichThrowsWhenRequireAndNotFound()
        {
            using (TestHostContext hc = new TestHostContext(this))
            {
                //Arrange
                Tracing trace = hc.GetTrace();
                var whichUtil = new WhichUtil();
                whichUtil.Initialize(hc);

                // Act.
                try
                {
                    whichUtil.Which("no-such-file-cf7e351f", require: true);
                    throw new Exception("which should have thrown");
                }
                catch (FileNotFoundException ex)
                {
                    Assert.Equal("no-such-file-cf7e351f", ex.FileName);
                }
            }
        }
    }
}
