using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using GitHub.Services.Common;

namespace GitHub.Services.Client
{
    internal sealed class VssAadTokenProvider : IssuedTokenProvider
    {
        public VssAadTokenProvider(VssAadCredential credential)
            : base(credential, null, null)
        {
        }

        public override bool GetTokenIsInteractive
        {
            get
            {
                return false;
            }
        }

        private VssAadToken GetVssAadToken()
        {
            AuthenticationContext authenticationContext = new AuthenticationContext(string.Concat(VssAadSettings.AadInstance, VssAadSettings.CommonTenant));
            UserCredential userCredential = null;

            VssAadCredential credential = this.Credential as VssAadCredential;

            if (credential?.Username != null)
            {
#if  NETSTANDARD
                // UserPasswordCredential does not currently exist for ADAL 3.13.5 for any non-desktop build.
                userCredential = new UserCredential(credential.Username);
#else
                if (credential.Password != null)
                {
                    userCredential = new UserPasswordCredential(credential.Username, credential.Password);

                }
                else
                {
                    userCredential = new UserCredential(credential.Username);
                }
#endif
            }
            else
            {
                userCredential = new UserCredential();
            }

            return new VssAadToken(authenticationContext, userCredential);            
        }

        /// <summary>
        /// Temporary implementation since we don't have a good configuration story here at the moment.
        /// </summary>
        protected override Task<IssuedToken> OnGetTokenAsync(IssuedToken failedToken, CancellationToken cancellationToken)
        {
            // If we have already tried to authenticate with an AAD token retrieved from Windows integrated authentication and it is not working, clear out state.
            if (failedToken != null && failedToken.CredentialType == VssCredentialsType.Aad && failedToken.IsAuthenticated)
            {
                this.CurrentToken = null;
                return Task.FromResult<IssuedToken>(null);
            }

            try
            {
                return Task.FromResult<IssuedToken>(GetVssAadToken());
            }
            catch
            { }

            return Task.FromResult<IssuedToken>(null);
        }
    }
}
