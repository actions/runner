using System.Collections.Generic;
using Xunit;


namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class ConstantGenerationL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Agent")]
        public void BuildConstantGenerateSucceed()
        {
            List<string> validPackageNames = new List<string>()
            {
                "win7-x64",
                "ubuntu.14.04-x64",
                "ubuntu.16.04-x64",
                "centos.7-x64",
                "rhel.7.2-x64",
                "osx.10.11-x64"
            };

            Assert.True(BuildConstants.Source.CommitHash.Length == 40, $"CommitHash should be SHA-1 hash {BuildConstants.Source.CommitHash}");
            Assert.True(validPackageNames.Contains(BuildConstants.AgentPackage.PackageName), $"PackageName should be one of the following '{string.Join(", ", validPackageNames)}', current PackageName is '{BuildConstants.AgentPackage.PackageName}'");
        }
    }
}
