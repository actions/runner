using System;
using System.Net;

namespace Microsoft.VisualStudio.Services.Common
{
    public sealed class WindowsToken : IssuedToken, ICredentials
    {
        internal WindowsToken(ICredentials credentials)
        {
            this.Credentials = credentials;
        }

        public ICredentials Credentials
        {
            get;
        }

        protected internal override VssCredentialsType CredentialType
        { 
            get
            {
                return VssCredentialsType.Windows;
            }
        }

        internal override void ApplyTo(IHttpRequest request)
        {
            // Special-cased by the caller because we implement ICredentials
            throw new InvalidOperationException();
        }

        NetworkCredential ICredentials.GetCredential(
            Uri uri, 
            String authType)
        {
            return this.Credentials?.GetCredential(uri, authType);
        }
    }
}
