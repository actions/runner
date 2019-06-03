using System;
using System.Net.Http;
using System.Security;
using GitHub.Services.Common;

namespace GitHub.Services.Client
{
    /// <summary>
    /// Currently it is impossible to get whether prompting is allowed from the credential itself without reproducing the logic
    /// used by VssClientCredentials. Since this is a stop gap solution to get Windows integrated authentication to work against
    /// AAD via ADFS for now this class will only support that one, non-interactive flow. We need to assess how much we want to
    /// invest in this legacy stack rather than recommending people move to the VssConnect API for future authentication needs.
    /// </summary>
    [Serializable]
    public sealed class VssAadCredential : FederatedCredential
    {
        private string username;
        private SecureString password;

        public VssAadCredential()
            : base(null)
        {
        }

        public VssAadCredential(VssAadToken initialToken)
            : base(initialToken)
        {
        }

        public VssAadCredential(string username)
            : base(null)
        {
            this.username = username;
        }

        public VssAadCredential(string username, string password)
            : base(null)
        {
            this.username = username;

            if (password != null)
            {
                this.password = new SecureString();

                foreach (char character in password)
                {
                    this.password.AppendChar(character);
                }
            }
        }

        public VssAadCredential(string username, SecureString password)
            : base(null)
        {
            this.username = username;
            this.password = password;
        }

        public override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.Aad;
            }
        }

        internal string Username
        {
            get
            {
                return username;
            }
        }

        internal SecureString Password => password;

        public override bool IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            bool isNonAuthenticationChallenge = false;
            return VssFederatedCredential.IsVssFederatedAuthenticationChallenge(webResponse, out isNonAuthenticationChallenge) ?? false;
        }

        protected override IssuedTokenProvider OnCreateTokenProvider(
            Uri serverUrl, 
            IHttpResponse response)
        {
            if (response == null && base.InitialToken == null)
            {
                return null;
            }

            return new VssAadTokenProvider(this);
        }
    }
}
