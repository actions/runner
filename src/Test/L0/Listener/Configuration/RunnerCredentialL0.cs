using System.Collections.Generic;
using System.Security.Cryptography;
using GitHub.Runner.Listener;
using GitHub.Runner.Listener.Configuration;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using Moq;
using Xunit;

namespace GitHub.Runner.Common.Tests.Listener.Configuration
{
    public class TestRunnerCredential : CredentialProvider
    {
        public TestRunnerCredential() : base("TEST") { }
        public override VssCredentials GetVssCredentials(IHostContext context, bool allowAuthUrlV2)
        {
            Tracing trace = context.GetTrace("OAuthAccessToken");
            trace.Info("GetVssCredentials()");

            var loginCred = new VssOAuthAccessTokenCredential("sometoken");
            VssCredentials creds = new(loginCred);
            trace.Verbose("cred created");

            return creds;
        }
        public override void EnsureCredential(IHostContext context, CommandSettings command, string serverUrl)
        {
        }
    }

    public class OAuthCredentialTestsL0
    {
        private Mock<IRSAKeyManager> _rsaKeyManager = new Mock<IRSAKeyManager>();

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "OAuthCredential")]
        public void NotUseAuthV2Url()
        {
            using (TestHostContext hc = new(this))
            {
                // Arrange.
                var oauth = new OAuthCredential();
                oauth.CredentialData = new CredentialData()
                {
                    Scheme = Constants.Configuration.OAuth
                };
                oauth.CredentialData.Data.Add("clientId", "someClientId");
                oauth.CredentialData.Data.Add("authorizationUrl", "http://myserver/");
                oauth.CredentialData.Data.Add("authorizationUrlV2", "http://myserverv2/");

                _rsaKeyManager.Setup(x => x.GetKey()).Returns(RSA.Create(2048));
                hc.SetSingleton<IRSAKeyManager>(_rsaKeyManager.Object);

                // Act.
                var cred = oauth.GetVssCredentials(hc, false); // not allow auth v2

                var cred2 = oauth.GetVssCredentials(hc, true); // use auth v2 but hostcontext doesn't

                hc.EnableAuthMigration("L0Test");
                var cred3 = oauth.GetVssCredentials(hc, false); // not use auth v2 but hostcontext does

                oauth.CredentialData.Data.Remove("authorizationUrlV2");
                var cred4 = oauth.GetVssCredentials(hc, true); // v2 url is not there

                // Assert.
                Assert.Equal("http://myserver/", (cred.Federated as VssOAuthCredential).AuthorizationUrl.AbsoluteUri);
                Assert.Equal("someClientId", (cred.Federated as VssOAuthCredential).ClientCredential.ClientId);

                Assert.Equal("http://myserver/", (cred2.Federated as VssOAuthCredential).AuthorizationUrl.AbsoluteUri);
                Assert.Equal("someClientId", (cred2.Federated as VssOAuthCredential).ClientCredential.ClientId);

                Assert.Equal("http://myserver/", (cred3.Federated as VssOAuthCredential).AuthorizationUrl.AbsoluteUri);
                Assert.Equal("someClientId", (cred3.Federated as VssOAuthCredential).ClientCredential.ClientId);

                Assert.Equal("http://myserver/", (cred4.Federated as VssOAuthCredential).AuthorizationUrl.AbsoluteUri);
                Assert.Equal("someClientId", (cred4.Federated as VssOAuthCredential).ClientCredential.ClientId);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "OAuthCredential")]
        public void UseAuthV2Url()
        {
            using (TestHostContext hc = new(this))
            {
                // Arrange.
                var oauth = new OAuthCredential();
                oauth.CredentialData = new CredentialData()
                {
                    Scheme = Constants.Configuration.OAuth
                };
                oauth.CredentialData.Data.Add("clientId", "someClientId");
                oauth.CredentialData.Data.Add("authorizationUrl", "http://myserver/");
                oauth.CredentialData.Data.Add("authorizationUrlV2", "http://myserverv2/");

                _rsaKeyManager.Setup(x => x.GetKey()).Returns(RSA.Create(2048));
                hc.SetSingleton<IRSAKeyManager>(_rsaKeyManager.Object);

                // Act.
                hc.EnableAuthMigration("L0Test");
                var cred = oauth.GetVssCredentials(hc, true);

                // Assert.
                Assert.Equal("http://myserverv2/", (cred.Federated as VssOAuthCredential).AuthorizationUrl.AbsoluteUri);
                Assert.Equal("someClientId", (cred.Federated as VssOAuthCredential).ClientCredential.ClientId);
            }
        }
    }
}
