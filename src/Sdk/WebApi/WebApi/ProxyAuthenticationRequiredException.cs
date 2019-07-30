using System;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi
{
    [ExceptionMapping("0.0", "3.0", "ProxyAuthenticationRequiredException", "GitHub.Services.WebApi.ProxyAuthenticationRequiredException, GitHub.Services.WebApi, Version=14.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public class ProxyAuthenticationRequiredException : VssException
    {
        public ProxyAuthenticationRequiredException()
            : base(WebApiResources.ProxyAuthenticationRequired())
        {
            this.HelpLink = HelpLinkUrl;
        }

        public ProxyAuthenticationRequiredException(string message, Exception innerException)
            : base(message, innerException)
        {
            this.HelpLink = HelpLinkUrl;
        }

        public ProxyAuthenticationRequiredException(string message)
            : base(message)
        {
            this.HelpLink = HelpLinkUrl;
        }

        private const string HelpLinkUrl = "";
    }
}
