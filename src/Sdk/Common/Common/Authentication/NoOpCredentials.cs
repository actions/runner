using System;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Services.Common
{
    // Set of classes used to bypass token operations
    // Results Service and External services follow a different auth model but
    // we are required to pass in a credentials object to create a RawHttpMessageHandler
    public class NoOpCredentials : FederatedCredential
    {
        public NoOpCredentials(IssuedToken initialToken) : base(initialToken)
        {
        }

        public override VssCredentialsType CredentialType { get; }
        protected override IssuedTokenProvider OnCreateTokenProvider(Uri serverUrl, IHttpResponse response)
        {
            return null;
        }
    }
}
