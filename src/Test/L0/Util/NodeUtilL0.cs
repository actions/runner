using System;
using System.Collections.Generic;
using Xunit;
using GitHub.Runner.Common.Util;

namespace GitHub.Runner.Common.Tests.Util
{
    public class NodeUtilL0
    {
        [Fact]
        public void GetInternalNodeVersion_DefaultsToNode20()
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, null);
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, null);
            Environment.SetEnvironmentVariable("RUNNER_USENODE24", null);
            Environment.SetEnvironmentVariable("runner_usenode24", null);

            // Act
            var nodeVersion = NodeUtil.GetInternalNodeVersion();

            // Assert
            Assert.Equal("node20", nodeVersion);
        }

        [Theory]
        [InlineData("node20")]
        [InlineData("node24")]
        public void GetInternalNodeVersion_ReturnsCorrectForcedInternalNodeVersion(string forcedVersion)
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, forcedVersion);
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, null);
            Environment.SetEnvironmentVariable("RUNNER_USENODE24", null);
            Environment.SetEnvironmentVariable("runner_usenode24", null);

            // Act
            var nodeVersion = NodeUtil.GetInternalNodeVersion();

            // Assert
            Assert.Equal(forcedVersion, nodeVersion);

            // Cleanup
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, null);
        }

        [Fact]
        public void GetInternalNodeVersion_IgnoresInvalidForcedInternalNodeVersion()
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, "invalidVersion");
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, null);
            Environment.SetEnvironmentVariable("RUNNER_USENODE24", null);
            Environment.SetEnvironmentVariable("runner_usenode24", null);

            // Act
            var nodeVersion = NodeUtil.GetInternalNodeVersion();

            // Assert
            Assert.Equal("node20", nodeVersion);

            // Cleanup
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, null);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("True")]
        [InlineData("1")]
        public void GetInternalNodeVersion_UsesNode24WhenFeatureFlagEnabled(string flagValue)
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, null);
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, flagValue);

            // Act
            var nodeVersion = NodeUtil.GetInternalNodeVersion();

            // Assert
            Assert.Equal("node24", nodeVersion);

            // Cleanup
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, null);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("True")]
        [InlineData("1")]
        public void GetInternalNodeVersion_UsesNode24WhenRunnerUseNode24Enabled(string flagValue)
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, null);
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, null);
            Environment.SetEnvironmentVariable("RUNNER_USENODE24", flagValue);

            // Act
            var nodeVersion = NodeUtil.GetInternalNodeVersion();

            // Assert
            Assert.Equal("node24", nodeVersion);

            // Cleanup
            Environment.SetEnvironmentVariable("RUNNER_USENODE24", null);
        }

        [Theory]
        [InlineData("true")]
        [InlineData("TRUE")]
        [InlineData("True")]
        [InlineData("1")]
        public void GetInternalNodeVersion_UsesNode24WhenrunnerUseNode24Enabled(string flagValue)
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, null);
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, null);
            Environment.SetEnvironmentVariable("RUNNER_USENODE24", null);
            Environment.SetEnvironmentVariable("runner_usenode24", flagValue);

            // Act
            var nodeVersion = NodeUtil.GetInternalNodeVersion();

            // Assert
            Assert.Equal("node24", nodeVersion);

            // Cleanup
            Environment.SetEnvironmentVariable("runner_usenode24", null);
        }

        [Theory]
        [InlineData("false")]
        [InlineData("0")]
        [InlineData("")]
        public void GetInternalNodeVersion_UsesDefaultWhenFeatureFlagDisabled(string flagValue)
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, null);
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, flagValue);

            // Act
            var nodeVersion = NodeUtil.GetInternalNodeVersion();

            // Assert
            Assert.Equal("node20", nodeVersion);

            // Cleanup
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, null);
        }

        [Fact]
        public void GetInternalNodeVersion_ForcedVersionTakesPrecedenceOverFeatureFlag()
        {
            // Arrange
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, "node20");
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, "true");

            // Act
            var nodeVersion = NodeUtil.GetInternalNodeVersion();

            // Assert
            Assert.Equal("node20", nodeVersion);

            // Cleanup
            Environment.SetEnvironmentVariable(Constants.Variables.Agent.ForcedInternalNodeVersion, null);
            Environment.SetEnvironmentVariable(Constants.Runner.Features.UseNode24, null);
        }
    }
}