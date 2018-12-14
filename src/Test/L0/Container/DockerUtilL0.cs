using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Agent.Worker.Container;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Container
{
    public sealed class DockerUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RegexParsesDockerPort()
        {
            // Arrange
            var dockerPortOutput0 = new List<string>();
            var dockerPortOutput1 = new List<string>
            {
                "80/tcp -> 0.0.0.0:32881"
            };
            var dockerPortOutput1Empty = new List<string>
            {
                ""
            };
            var dockerPortOutput2 = new List<string>
            {
                "80/tcp -> 0.0.0.0:32881",
                "6379/tcp -> 0.0.0.0:32882"
            };

            // Act
            var result0 = DockerUtil.ParseDockerPort(dockerPortOutput0);
            var result1 = DockerUtil.ParseDockerPort(dockerPortOutput1);
            var result1Empty = DockerUtil.ParseDockerPort(dockerPortOutput1Empty);
            var result2 = DockerUtil.ParseDockerPort(dockerPortOutput2);

            // Assert
            Assert.NotNull(result0);
            Assert.Equal(result0.Count, 0);

            Assert.NotNull(result1);
            Assert.Equal(result1.Count, 1);
            var result1Port80Mapping = result1.Find(pm =>
                string.Equals(pm.ContainerPort, "80") &&
                string.Equals(pm.HostPort, "32881") &&
                string.Equals(pm.Protocol, "tcp", StringComparison.OrdinalIgnoreCase)
            );
            Assert.NotNull(result1Port80Mapping);

            Assert.NotNull(result1Empty);
            Assert.Equal(result1Empty.Count, 0);

            Assert.NotNull(result2);
            Assert.Equal(result2.Count, 2);
            var result2Port80Mapping = result2.Find(pm =>
                string.Equals(pm.ContainerPort, "80") &&
                string.Equals(pm.HostPort, "32881") &&
                string.Equals(pm.Protocol, "tcp", StringComparison.OrdinalIgnoreCase)
            );
            Assert.NotNull(result2Port80Mapping);
            var result2Port6379Mapping = result2.Find(pm =>
                string.Equals(pm.ContainerPort, "6379") &&
                string.Equals(pm.HostPort, "32882") &&
                string.Equals(pm.Protocol, "tcp", StringComparison.OrdinalIgnoreCase)
            );
            Assert.NotNull(result2Port6379Mapping);
        }
    }
}
