using System;
using GitHub.Runner.Common.Util;
using Xunit;

namespace GitHub.Runner.Common.Tests.Util
{
    public class NodeUtilL0
    {
        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        [InlineData(null)]
        [InlineData("node24")]
        [InlineData("node20")]
        [InlineData("node16")]
        [InlineData("invalid")]
        public void InternalNodeVersionIsAlwaysNode24(string forcedVersion)
        {
            const string variable = "ACTIONS_RUNNER_FORCED_INTERNAL_NODE_VERSION";
            var originalValue = Environment.GetEnvironmentVariable(variable);
            try
            {
                Environment.SetEnvironmentVariable(variable, forcedVersion);

                Assert.Equal("node24", NodeUtil.GetInternalNodeVersion());
            }
            finally
            {
                Environment.SetEnvironmentVariable(variable, originalValue);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void OnlyNode24IsBundled()
        {
            Assert.Equal(new[] { "node24" }, NodeUtil.BuiltInNodeVersions);
        }
    }
}
