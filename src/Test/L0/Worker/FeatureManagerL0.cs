using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class FeatureManagerL0
    {
        private TestHostContext CreateTestContext([CallerMemberName] string testName = "")
        {
            var hostContext = new TestHostContext(this, testName);
            return hostContext;
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IsNode24EnabledWhenFeatureFlagSet()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange
                var variables = new Dictionary<string, VariableValue>
                {
                    { Constants.Runner.Features.UseNode24, new VariableValue("true") }
                };
                Variables serverVariables = new(hc, variables);

                // Act
                bool isNode24Enabled = FeatureManager.IsNode24Enabled(serverVariables);

                // Assert
                Assert.True(isNode24Enabled);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void IsNode24DisabledWhenFeatureFlagNotSet()
        {
            using (TestHostContext hc = CreateTestContext())
            {
                // Arrange
                var variables = new Dictionary<string, VariableValue>();
                Variables serverVariables = new(hc, variables);

                // Act
                bool isNode24Enabled = FeatureManager.IsNode24Enabled(serverVariables);

                // Assert
                Assert.False(isNode24Enabled);
            }
        }
    }
}
