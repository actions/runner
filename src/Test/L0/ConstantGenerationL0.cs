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
            List<string> validPackageNames = new()
            {
                "win-x64",
                "win-x86",
                "win-arm64",
                "linux-x64",
                "linux-arm",
                "linux-arm64",
                "osx-x64",
                "osx-arm64",
                ""
            };

            Assert.Equal(40, BuildConstants.Source.CommitHash.Length);
            Assert.True(validPackageNames.Contains(BuildConstants.RunnerPackage.PackageName), $"PackageName should be one of the following '{string.Join(", ", validPackageNames)}', current PackageName is '{BuildConstants.RunnerPackage.PackageName}'");
        }
    }
}
