using System;
using System.Collections.Generic;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Worker;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public class FeatureManagerL0
    {
        private Variables GetVariables()
        {
            var hostContext = new Mock<IHostContext>();
            return new Variables(hostContext.Object, new Dictionary<string, VariableValue>());
        }

        [Fact]
        public void IsUseNode24ByDefaultEnabled_ReturnsCorrectValue()
        {
            // Arrange
            var variables = GetVariables();
            variables.Set(Constants.Runner.NodeMigration.UseNode24ByDefaultFlag, "true");

            // Act
            bool result = FeatureManager.IsUseNode24ByDefaultEnabled(variables);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUseNode24ByDefaultEnabled_ReturnsFalseWhenNotSet()
        {
            // Arrange
            var variables = GetVariables();

            // Act
            bool result = FeatureManager.IsUseNode24ByDefaultEnabled(variables);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsUseNode24ByDefaultEnabled_ReturnsFalseWhenNull()
        {
            // Act
            bool result = FeatureManager.IsUseNode24ByDefaultEnabled(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRequireNode24Enabled_ReturnsCorrectValue()
        {
            // Arrange
            var variables = GetVariables();
            variables.Set(Constants.Runner.NodeMigration.RequireNode24Flag, "true");

            // Act
            bool result = FeatureManager.IsRequireNode24Enabled(variables);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRequireNode24Enabled_ReturnsFalseWhenNotSet()
        {
            // Arrange
            var variables = GetVariables();

            // Act
            bool result = FeatureManager.IsRequireNode24Enabled(variables);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRequireNode24Enabled_ReturnsFalseWhenNull()
        {
            // Act
            bool result = FeatureManager.IsRequireNode24Enabled(null);

            // Assert
            Assert.False(result);
        }
    }
}
