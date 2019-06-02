using System;
using System.Globalization;
using System.Net;

namespace Microsoft.VisualStudio.Services.Common
{
    internal sealed class WindowsTokenProvider : IssuedTokenProvider
    {
        public WindowsTokenProvider(
            WindowsCredential credential,
            Uri serverUrl) 
            : base(credential, serverUrl, serverUrl)
        {
        }

        protected override String AuthenticationScheme
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3}", AuthenticationSchemes.Negotiate, AuthenticationSchemes.Ntlm, AuthenticationSchemes.Digest, AuthenticationSchemes.Basic);
            }
        }

        public new WindowsCredential Credential
        {
            get
            {
                return (WindowsCredential)base.Credential;
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
