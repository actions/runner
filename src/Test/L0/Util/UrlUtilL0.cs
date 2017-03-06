using System;
using Microsoft.VisualStudio.Services.Agent.Util;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Util
{
    public class UrlUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredentialEmbeddedUrl_NoUsernameAndPassword()
        {
            // Act.
            Uri result = UrlUtil.GetCredentialEmbeddedUrl(new Uri("https://github.com/Microsoft/vsts-agent.git"), string.Empty, string.Empty);
            // Actual
            Assert.Equal("https://github.com/Microsoft/vsts-agent.git", result.AbsoluteUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredentialEmbeddedUrl_NoUsername()
        {
            // Act.
            Uri result = UrlUtil.GetCredentialEmbeddedUrl(new Uri("https://github.com/Microsoft/vsts-agent.git"), string.Empty, "password123");
            // Actual
            Assert.Equal("https://emptyusername:password123@github.com/Microsoft/vsts-agent.git", result.AbsoluteUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredentialEmbeddedUrl_NoPassword()
        {
            // Act.
            Uri result = UrlUtil.GetCredentialEmbeddedUrl(new Uri("https://github.com/Microsoft/vsts-agent.git"), "user123", string.Empty);
            // Actual
            Assert.Equal("https://user123@github.com/Microsoft/vsts-agent.git", result.AbsoluteUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredentialEmbeddedUrl_HasUsernameAndPassword()
        {
            // Act.
            Uri result = UrlUtil.GetCredentialEmbeddedUrl(new Uri("https://github.com/Microsoft/vsts-agent.git"), "user123", "password123");
            // Actual
            Assert.Equal("https://user123:password123@github.com/Microsoft/vsts-agent.git", result.AbsoluteUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredentialEmbeddedUrl_UsernameAndPasswordEncoding()
        {
            // Act.
            Uri result = UrlUtil.GetCredentialEmbeddedUrl(new Uri("https://github.com/Microsoft/vsts-agent.git"), "user 123", "password 123");
            // Actual
            Assert.Equal("https://user%20123:password%20123@github.com/Microsoft/vsts-agent.git", result.AbsoluteUri);
        }
    }
}