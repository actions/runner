using Microsoft.VisualStudio.Services.Agent.Configuration;
using System;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class ArgumentValidatorTestsL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ArgumentValidator")]
        public void ServerUrlValidator()
        {
            using (TestHostContext hc = new TestHostContext(nameof(ConfigurationManagerL0)))
            {
                Assert.True(Validators.ServerUrlValidator("http://servername"));
                Assert.False(Validators.ServerUrlValidator("Fail"));
                Assert.False(Validators.ServerUrlValidator("ftp://servername"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ArgumentValidator")]
        public void AuthSchemeValidator()
        {
            using (TestHostContext hc = new TestHostContext(nameof(ConfigurationManagerL0)))
            {
                Assert.True(Validators.AuthSchemeValidator("pat"));
                Assert.False(Validators.AuthSchemeValidator("Fail"));
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "ArgumentValidator")]
        public void NonEmptyValidator()
        {
            using (TestHostContext hc = new TestHostContext(nameof(ConfigurationManagerL0)))
            {
                Assert.True(Validators.NonEmptyValidator("test"));
                Assert.False(Validators.NonEmptyValidator(String.Empty));
            }
        }
    }
}