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
        [Fact]
        public void IsUseNode24ByDefaultEnabled_ReturnsCorrectValue()
        {
            // Arrange
            var mockVariables = new Mock<Variables>(MockBehavior.Strict, null, null);
            mockVariables.Setup(x => x.GetBoolean(Constants.Runner.NodeMigration.UseNode24ByDefaultFlag))
                .Returns(true);

            // Act
            bool result = FeatureManager.IsUseNode24ByDefaultEnabled(mockVariables.Object);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsUseNode24ByDefaultEnabled_ReturnsFalseWhenNotSet()
        {
            // Arrange
            var mockVariables = new Mock<Variables>(MockBehavior.Strict, null, null);
            mockVariables.Setup(x => x.GetBoolean(Constants.Runner.NodeMigration.UseNode24ByDefaultFlag))
                .Returns((bool?)null);

            // Act
            bool result = FeatureManager.IsUseNode24ByDefaultEnabled(mockVariables.Object);

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
            var mockVariables = new Mock<Variables>(MockBehavior.Strict, null, null);
            mockVariables.Setup(x => x.GetBoolean(Constants.Runner.NodeMigration.RequireNode24Flag))
                .Returns(true);

            // Act
            bool result = FeatureManager.IsRequireNode24Enabled(mockVariables.Object);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRequireNode24Enabled_ReturnsFalseWhenNotSet()
        {
            // Arrange
            var mockVariables = new Mock<Variables>(MockBehavior.Strict, null, null);
            mockVariables.Setup(x => x.GetBoolean(Constants.Runner.NodeMigration.RequireNode24Flag))
                .Returns((bool?)null);

            // Act
            bool result = FeatureManager.IsRequireNode24Enabled(mockVariables.Object);

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
