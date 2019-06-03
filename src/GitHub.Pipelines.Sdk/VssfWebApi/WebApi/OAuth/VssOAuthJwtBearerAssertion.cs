using System;
using System.Collections.Generic;
using System.Security.Claims;
using GitHub.Services.WebApi;
using GitHub.Services.WebApi.Jwt;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Represents a bearer token assertion for either JWT Bearer Token Profile for OAuth 2.0 Client Authentication 
    /// or JWT Bearer Token Grant Type Profile for OAuth 2.0.
    /// </summary>
    public class VssOAuthJwtBearerAssertion
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthJwtBearerAssertion</c> with the specified token as the assertion value.
        /// </summary>
        /// <param name="bearerToken">The <c>JsonWebToken</c> instance representing the assertion</param>
        internal VssOAuthJwtBearerAssertion(JsonWebToken bearerToken)
        {
            m_bearerToken = bearerToken;
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthJwtBearerAssertion</c> with the specified issuer, subject, audience,
        /// and signing credentials for generating a bearer token.
        /// </summary>
        /// <param name="issuer">The iss claim for the bearer token</param>
        /// <param name="subject">The sub claim for the bearer token</param>
        /// <param name="audience">The aud claim for the bearer token</param>
        /// <param name="signingCredentials">The credentials used to sign the bearer token</param>
        public VssOAuthJwtBearerAssertion(
            String issuer,
            String subject,
            String audience,
            VssSigningCredentials signingCredentials)
            : this(issuer, subject, audience, null, signingCredentials)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthJwtBearerAssertion</c> with the specified issuer, subject, audience,
        /// and signing credentials for generating a bearer token.
        /// </summary>
        /// <param name="issuer">The iss claim for the bearer token</param>
        /// <param name="subject">The sub claim for the bearer token</param>
        /// <param name="audience">The aud claim for the bearer token</param>
        /// <param name="additionalClaims">An optional list of additional claims to provide with the bearer token</param>
        /// <param name="signingCredentials">The credentials used to sign the bearer token</param>
        public VssOAuthJwtBearerAssertion(
            String issuer,
            String subject,
            String audience,
            IList<Claim> additionalClaims,
            VssSigningCredentials signingCredentials)
        {
            m_issuer = issuer;
            m_subject = subject;
            m_audience = audience;
            m_signingCredentials = signingCredentials;

            if (additionalClaims != null)
            {
                this.additionalClaims = new List<Claim>(additionalClaims);
            }
        }

        /// <summary>
        /// Gets the issuer (iss claim) for the credentials.
        /// </summary>
        public String Issuer
        {
            get
            {
                return m_issuer;
            }
        }

        /// <summary>
        /// Gets the subject (sub claim) for the credentials.
        /// </summary>
        public String Subject
        {
            get
            {
                return m_subject;
            }
        }

        /// <summary>
        /// Gets the audience (aud claim) for the credentials.
        /// </summary>
        public String Audience
        {
            get
            {
                return m_audience;
            }
        }

        /// <summary>
        /// Gets a list of additional claims provided with the credentials.
        /// </summary>
        public IList<Claim> AdditionalClaims
        {
            get
            {
                if (additionalClaims == null)
                {
                    additionalClaims = new List<Claim>();
                }
                return additionalClaims;
            }
        }

        /// <summary>
        /// Gets a <c>JsonWebToken</c> instance based on the values provided to the assertion.
        /// </summary>
        /// <returns>A signed <c>JsonWebToken</c> instance for presentation as a bearer token</returns>
        public JsonWebToken GetBearerToken()
        {
            if (m_bearerToken != null)
            {
                return m_bearerToken;
            }
            else
            {
                var additionalClaims = new List<Claim>(this.AdditionalClaims ?? new Claim[0]);
                if (!String.IsNullOrEmpty(m_subject))
                {
                    additionalClaims.Add(new Claim(JsonWebTokenClaims.Subject, m_subject));
                }

                additionalClaims.Add(new Claim(JsonWebTokenClaims.TokenId, Guid.NewGuid().ToString()));

                var nowUtc = DateTime.UtcNow;
                return JsonWebToken.Create(m_issuer, m_audience, nowUtc, nowUtc.Add(BearerTokenLifetime), additionalClaims, m_signingCredentials);
            }
        }

        private List<Claim> additionalClaims;
        private readonly String m_issuer;
        private readonly String m_subject;
        private readonly String m_audience;
        private readonly JsonWebToken m_bearerToken;
        private readonly VssSigningCredentials m_signingCredentials;
        private static readonly TimeSpan BearerTokenLifetime = TimeSpan.FromMinutes(5);
    }
}
