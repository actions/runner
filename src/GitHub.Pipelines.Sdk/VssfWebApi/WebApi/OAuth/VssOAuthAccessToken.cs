using System;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Jwt;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Provides encapsulation for opaque access tokens in OAuth token exchanges.
    /// </summary>
    public sealed class VssOAuthAccessToken : IssuedToken
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthAccessToken</c> instance with the specified value.
        /// </summary>
        /// <param name="value">The value of the access token, encoded as a string</param>
        public VssOAuthAccessToken(String value)
            : this(value, DateTime.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthAccessToken</c> instance with the specified value and expiration time.
        /// </summary>
        /// <param name="value">The value of the access token, encoded as a string</param>
        /// <param name="validTo">The date and time when this token is no longer valid</param>
        public VssOAuthAccessToken(
            String value, 
            DateTime validTo)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(value, nameof(value));
            m_value = value;
            m_validTo = validTo;
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthAccessToken</c> instance with the specified JWT.
        /// </summary>
        /// <param name="value">The value of the access token, encoded as a JsonWebToken</param>
        public VssOAuthAccessToken(JsonWebToken value)
        {
            ArgumentUtility.CheckForNull(value, nameof(value));
            m_value = value.EncodedToken;
            m_validTo = value.ValidTo;
        }

        /// <summary>
        /// Gets the date and time at which this token expires.
        /// </summary>
        public DateTime ValidTo
        {
            get
            {
                return m_validTo;
            }
        }

        /// <summary>
        /// Gets the value of the current token.
        /// </summary>
        public String Value
        {
            get
            {
                return m_value;
            }
        }

        protected internal override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.OAuth;
            }
        }

        internal override void ApplyTo(IHttpRequest request)
        {
            request.Headers.SetValue(Common.Internal.HttpHeaders.Authorization, $"Bearer {m_value}");
        }

        private readonly String m_value;
        private readonly DateTime m_validTo;
    }
}
