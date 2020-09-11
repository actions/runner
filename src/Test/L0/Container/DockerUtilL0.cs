using System;
using System.Collections.Generic;
using GitHub.Runner.Worker.Container;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker.Container
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
            Assert.Equal(0, result0.Count);

            Assert.NotNull(result1);
            Assert.Equal(1, result1.Count);
            var result1Port80Mapping = result1.Find(pm =>
                string.Equals(pm.ContainerPort, "80") &&
                string.Equals(pm.HostPort, "32881") &&
                string.Equals(pm.Protocol, "tcp", StringComparison.OrdinalIgnoreCase)
            );
            Assert.NotNull(result1Port80Mapping);

            Assert.NotNull(result1Empty);
            Assert.Equal(0, result1Empty.Count);

            Assert.NotNull(result2);
            Assert.Equal(2, result2.Count);
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

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void RegexParsesPathFromDockerConfigEnv()
        {
            // Arrange
            var configOutput0 = new List<string>
            {
                "PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
                "MY_VAR=test"
            };
            var configOutput1 = new List<string>
            {
                "PATH=/bad idea:/really,bad,idea:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
                "MY_VAR=test"
            };
            var configOutput2 = new List<string>();
            var configOutput3 = new List<string>
            {
                "NOT_A_PATH=/bad idea:/really,bad,idea:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
                "MY_VAR=test"
            };
            var configOutput4 = new List<string>
            {
                "PATH",
                "PATH="
            };
            var configOutput5 = new List<string>
            {
                "PATH=/foo/bar:/baz",
                "Path=/no/where"
            };

            // Act
            var result0 = DockerUtil.ParsePathFromConfigEnv(configOutput0);
            var result1 = DockerUtil.ParsePathFromConfigEnv(configOutput1);
            var result2 = DockerUtil.ParsePathFromConfigEnv(configOutput2);
            var result3 = DockerUtil.ParsePathFromConfigEnv(configOutput3);
            var result4 = DockerUtil.ParsePathFromConfigEnv(configOutput4);
            var result5 = DockerUtil.ParsePathFromConfigEnv(configOutput5);

            // Assert
            Assert.NotNull(result0);
            Assert.Equal("/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin", result0);

            Assert.NotNull(result1);
            Assert.Equal("/bad idea:/really,bad,idea:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin", result1);

            Assert.NotNull(result2);
            Assert.Equal("", result2);

            Assert.NotNull(result3);
            Assert.Equal("", result3);

            Assert.NotNull(result4);
            Assert.Equal("", result4);

            Assert.NotNull(result5);
            Assert.Equal("/foo/bar:/baz", result5);
        }

        [Theory]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        [InlineData("dockerhub/repo", "")]
        [InlineData("localhost/doesnt_work", "")]
        [InlineData("localhost:port/works", "localhost:port")]
        [InlineData("host.tld/works", "host.tld")]
        [InlineData("ghcr.io/owner/image", "ghcr.io")]
        [InlineData("gcr.io/project/image", "gcr.io")]
        [InlineData("myregistry.azurecr.io/namespace/image", "myregistry.azurecr.io")]
        [InlineData("account.dkr.ecr.region.amazonaws.com/image", "account.dkr.ecr.region.amazonaws.com")]
        [InlineData("docker.pkg.github.com/owner/repo/image", "docker.pkg.github.com")]
        public void ParseRegistryHostnameFromImageName(string input, string expected)
        {
            var actual = DockerUtil.ParseRegistryHostnameFromImageName(input);
            Assert.Equal(expected, actual);
        }
    }
}
