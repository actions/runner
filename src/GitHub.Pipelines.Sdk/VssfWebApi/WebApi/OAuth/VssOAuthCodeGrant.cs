using System;
using System.Collections.Generic;
using GitHub.Services.Common;

namespace GitHub.Services.OAuth
{
    public sealed class VssOAuthCodeGrant : VssOAuthGrant
    {
        public VssOAuthCodeGrant(String code)
            : base(VssOAuthGrantType.AuthorizationCode)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(code, nameof(code));

            m_code = code;
        }

        /// <summary>
        /// Gets the authorization code provided by the authorization server.
        /// </summary>
        public String Code
        {
            get
            {
                return m_code;
            }
        }

        protected override void SetParameters(IDictionary<String, String> parameters)
        {
            parameters[VssOAuthConstants.GrantType] = VssOAuthConstants.AuthorizationCodeGrantType;
            parameters[VssOAuthConstants.Code] = m_code;
        }

        private readonly String m_code;
    }
}
