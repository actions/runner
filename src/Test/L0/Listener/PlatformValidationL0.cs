using GitHub.Runner.Listener;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener
{
    public sealed class PlatformValidationL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PlatformValidation")]
        //process 2 new job messages, and one cancel message
        public void TestSupportedPlatform()
        {
            var validation = PlatformValidation.Validate(Constants.OSPlatform.Linux, platform => true);
            Assert.True(validation.IsValid);
            Assert.Null(validation.Message);
        }
        
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "PlatformValidation")]
        //process 2 new job messages, and one cancel message
        public void TestSupportedPlatformOnWrongOs()
        {
            var validation = PlatformValidation.Validate(Constants.OSPlatform.Linux, platform => false);
            Assert.False(validation.IsValid);
            Assert.Contains("Please install a correct build for your OS", validation.Message);
            Assert.Contains("This runner version is built for Linux", validation.Message);
        }
    }
}
