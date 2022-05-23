using System.Collections.Generic;
using GitHub.Runner.Sdk;
using Xunit;


namespace GitHub.Runner.Common.Tests
{
    public sealed class ConstantGenerationL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Runner")]
        public void BuildConstantGenerateSucceed()
        {
            List<string> validPackageNames = new List<string>()
            {
                "win-x64",
                "win-x86",
                "linux-x64",
                "linux-arm",
                "linux-arm64",
                "osx-x64",
                "osx-arm64"
            };

            Assert.True(BuildConstants.Source.CommitHash.Length == 40, $"CommitHash should be SHA-1 hash {BuildConstants.Source.CommitHash}");
            Assert.True(validPackageNames.Contains(BuildConstants.RunnerPackage.PackageName), $"PackageName should be one of the following '{string.Join(", ", validPackageNames)}', current PackageName is '{BuildConstants.RunnerPackage.PackageName}'");
        }
    }
}
