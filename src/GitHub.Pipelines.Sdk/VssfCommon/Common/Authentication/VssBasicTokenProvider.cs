using System;
using System.Net;

namespace Microsoft.VisualStudio.Services.Common
{
    internal sealed class BasicAuthTokenProvider : IssuedTokenProvider
    {
        public BasicAuthTokenProvider(
            VssBasicCredential credential,
            Uri serverUrl) 
            : base(credential, serverUrl, serverUrl)
        {
        }

        protected override String AuthenticationScheme
        {
            get
            {
                return "Basic";
            }
        }

        public new VssBasicCredential Credential
        {
            get
            {
                return (VssBasicCredential)base.Credential;
            }
        }

        public override Boolean GetTokenIsInteractive
        {
            get
            {
                return base.CurrentToken == null;
            }
        }
    }
}
