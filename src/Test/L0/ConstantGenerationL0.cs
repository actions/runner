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
                "win-x64",
                "linux-x64",
                "osx-x64"
            };

            Assert.True(BuildConstants.Source.CommitHash.Length == 40, $"CommitHash should be SHA-1 hash {BuildConstants.Source.CommitHash}");
            Assert.True(validPackageNames.Contains(BuildConstants.AgentPackage.PackageName), $"PackageName should be one of the following '{string.Join(", ", validPackageNames)}', current PackageName is '{BuildConstants.AgentPackage.PackageName}'");
        }
    }
}
