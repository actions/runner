using System;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.Client
{
    [Serializable]
    public class VssAadToken : IssuedToken
    {
        private string accessToken;
        private string accessTokenType;

        private AuthenticationContext authenticationContext;
        private UserCredential userCredential;
        private VssAadTokenOptions options;

        public VssAadToken(AuthenticationResult authentication)
        {
            // Prevent any attempt to store this token.
            this.FromStorage = true;

            if (!string.IsNullOrWhiteSpace(authentication.AccessToken))
            {
                this.Authenticated();
            }

            this.accessToken = authentication.AccessToken;
            this.accessTokenType = authentication.AccessTokenType;
        }

        public VssAadToken(
            string accessTokenType, 
            string accessToken)
        {
            // Prevent any attempt to store this token.
            this.FromStorage = true;

            if (!string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(accessTokenType))
            {
                this.Authenticated();
            }

            this.accessToken = accessToken;
            this.accessTokenType = accessTokenType;
        }

        public VssAadToken(
            AuthenticationContext authenticationContext, 
            UserCredential userCredential = null, 
            VssAadTokenOptions options = VssAadTokenOptions.None)
        {
            // Prevent any attempt to store this token.
            this.FromStorage = true;

            this.authenticationContext = authenticationContext;
            this.userCredential = userCredential;
            this.options = options;
        }

        protected internal override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.Aad;
            }
        }

        public AuthenticationResult AcquireToken()
        {
            if (this.authenticationContext == null)
            {
                return null;
            }

            AuthenticationResult authenticationResult = null;

            for (int index = 0; index < 3; index++)
            {
                try
                {
                    if (this.userCredential == null && !options.HasFlag(VssAadTokenOptions.AllowDialog))
                    {
                        authenticationResult = authenticationContext.AcquireTokenSilentAsync(VssAadSettings.Resource, VssAadSettings.Client).ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    else
                    {
                        authenticationResult = authenticationContext.AcquireTokenAsync(VssAadSettings.Resource, VssAadSettings.Client, this.userCredential).ConfigureAwait(false).GetAwaiter().GetResult();
                    }

                    if (authenticationResult != null)
                    {
                        break;
                    }
                }
                catch (Exception x)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to get ADFS token: " + x.ToString());
                }
            }

            return authenticationResult;
        }

        internal override void ApplyTo(IHttpRequest request)
        {
            AuthenticationResult authenticationResult = AcquireToken();
            if (authenticationResult != null)
            {
                request.Headers.SetValue(Common.Internal.HttpHeaders.Authorization, $"{authenticationResult.AccessTokenType} {authenticationResult.AccessToken}");
            }
            else if (!string.IsNullOrEmpty(this.accessTokenType) && !string.IsNullOrEmpty(this.accessToken))
            {
                request.Headers.SetValue(Common.Internal.HttpHeaders.Authorization, $"{this.accessTokenType} {this.accessToken}");
            }
        }
    }

    [Flags]
    public enum VssAadTokenOptions
    {
        None = 0,
        AllowDialog = 1
    }
}
