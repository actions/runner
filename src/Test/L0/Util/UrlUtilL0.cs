using System;
using GitHub.Runner.Sdk;
using Xunit;

namespace GitHub.Runner.Common.Tests.Util
{
    public class UrlUtilL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredentialEmbeddedUrl_NoUsernameAndPassword()
        {
            // Act.
            Uri result = UrlUtil.GetCredentialEmbeddedUrl(new Uri("https://github.com/actions/runner.git"), string.Empty, string.Empty);
            // Actual
            Assert.Equal("https://github.com/actions/runner.git", result.AbsoluteUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredentialEmbeddedUrl_NoUsername()
        {
            // Act.
            Uri result = UrlUtil.GetCredentialEmbeddedUrl(new Uri("https://github.com/actions/runner.git"), string.Empty, "password123");
            // Actual
            Assert.Equal("https://emptyusername:password123@github.com/actions/runner.git", result.AbsoluteUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredentialEmbeddedUrl_NoPassword()
        {
            // Act.
            Uri result = UrlUtil.GetCredentialEmbeddedUrl(new Uri("https://github.com/actions/runner.git"), "user123", string.Empty);
            // Actual
            Assert.Equal("https://user123@github.com/actions/runner.git", result.AbsoluteUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredentialEmbeddedUrl_HasUsernameAndPassword()
        {
            // Act.
            Uri result = UrlUtil.GetCredentialEmbeddedUrl(new Uri("https://github.com/actions/runner.git"), "user123", "password123");
            // Actual
            Assert.Equal("https://user123:password123@github.com/actions/runner.git", result.AbsoluteUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetCredentialEmbeddedUrl_UsernameAndPasswordEncoding()
        {
            // Act.
            Uri result = UrlUtil.GetCredentialEmbeddedUrl(new Uri("https://github.com/actions/runner.git"), "user 123", "password 123");
            // Actual
            Assert.Equal("https://user%20123:password%20123@github.com/actions/runner.git", result.AbsoluteUri);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetAbsoluteUrlWithPort_WithUriPort80OutputContainsPort80()
        {
            UriBuilder uriBuilder = new UriBuilder
            {
                Port = 80,
                Host = "my.proxy.com",
                Scheme = "http"
            };

            string result = UrlUtil.GetAbsoluteUrlWithPort(uriBuilder.Uri);

            Assert.Equal("http://my.proxy.com:80/", result);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetAbsoluteUrlWithPort_WithUriPort443OutputContainsPort443()
        {
            UriBuilder uriBuilder = new UriBuilder
            {
                Port = 443,
                Host = "my.proxy.com",
                Scheme = "https"
            };

            string result = UrlUtil.GetAbsoluteUrlWithPort(uriBuilder.Uri);

            Assert.Equal("https://my.proxy.com:443/", result);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetAbsoluteUrlWithPort_WithCredentialsAndPort80OutputContainsPort80()
        {
            Uri result = new Uri("https://$user123:$pass123@my.proxy.com:80");
            string resultWithPort = UrlUtil.GetAbsoluteUrlWithPort(result);

            Assert.Equal("https://%24user123:%24pass123@my.proxy.com:80/", resultWithPort);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void GetAbsoluteUrlWithPort_WithCredentialsAndPort443OutputContainsPort443()
        {
            Uri result = new Uri("https://$user123:$pass123@my.proxy.com:443");
            string resultWithPort = UrlUtil.GetAbsoluteUrlWithPort(result);

            Assert.Equal("https://%24user123:%24pass123@my.proxy.com:443/", resultWithPort);
        }
    }
}
