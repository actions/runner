using Microsoft.VisualStudio.Services.Agent.Util;
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
    }
}
