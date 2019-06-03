using System;
using System.Collections.Generic;
using System.Net.Http.Formatting;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Jwt;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Encapsulates the data used in an OAuth 2.0 token request.
    /// </summary>
    public class VssOAuthTokenRequest
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthTokenRequest</c> instance with the specified grant and client credential.
        /// </summary>
        /// <param name="grant">The authorization grant to use for the token request</param>
        /// <param name="clientCredential">The client credential to use for the token request</param>
        public VssOAuthTokenRequest(
            VssOAuthGrant grant,
            VssOAuthClientCredential clientCredential)
            : this(grant, clientCredential, null)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthTokenRequest</c> instance with the specified grant and client credential. 
        /// Additional parameters specified by the token parameters will be provided in the token request.
        /// </summary>
        /// <param name="grant">The authorization grant to use for the token request</param>
        /// <param name="clientCredential">The client credential to use for the token request</param>
        /// <param name="tokenParameters">An optional set of additional parameters for the token request</param>
        public VssOAuthTokenRequest(
            VssOAuthGrant grant,
            VssOAuthClientCredential clientCredential,
            VssOAuthTokenParameters tokenParameters)
        {
            ArgumentUtility.CheckForNull(grant, nameof(grant));

            m_grant = grant;
            m_clientCredential = clientCredential;
            m_tokenParameters = tokenParameters;
        }

        /// <summary>
        /// Gets the authorization grant for this token request.
        /// </summary>
        public VssOAuthGrant Grant
        {
            get
            {
                return m_grant;
            }
        }

        /// <summary>
        /// Gets the client credential for this token request. Depending on the grant ype used, this value may be null.
        /// </summary>
        public VssOAuthClientCredential ClientCredential
        {
            get
            {
                return m_clientCredential;
            }
        }

        /// <summary>
        /// Gets the optional set of additional parameters for this token request.
        /// </summary>
        public VssOAuthTokenParameters Parameters
        {
            get
            {
                if (m_tokenParameters == null)
                {
                    m_tokenParameters = new VssOAuthTokenParameters();
                }
                return m_tokenParameters;
            }
        }

#if !NETSTANDARD
        /// <summary>
        /// Initializes a new <c>VssOAuthTokenRequest</c> instance from the specified form input.
        /// </summary>
        /// <param name="form">The input which should be parsed into a token request</param>
        /// <returns>A new <c>VssOAuthTokenRequest</c> instance representative of the provided form input</returns>
        public static VssOAuthTokenRequest FromFormInput(FormDataCollection form)
        {
            var parsedParameters = new HashSet<String>();
            var grant = CreateGrantFromFormInput(form, parsedParameters);
            var clientCredential = CreateClientCredentialFromFormInput(form, parsedParameters);

            var tokenParameters = new VssOAuthTokenParameters();
            foreach (var parameter in form)
            {
                // Only include parameters in the extended set if we didn't already read them in the grant and 
                // credentials parsing logic.
                if (parsedParameters.Add(parameter.Key))
                {
                    tokenParameters.Add(parameter.Key, parameter.Value);
                }
            }

            return new VssOAuthTokenRequest(grant, clientCredential, tokenParameters);
        }

        private static VssOAuthGrant CreateGrantFromFormInput(
            FormDataCollection form,
            ISet<String> parsedParameters)
        {
            ArgumentUtility.CheckForNull(form, nameof(form));

            var grantType = GetRequiredValue(form, VssOAuthConstants.GrantType, VssOAuthErrorCodes.InvalidRequest);
            switch (grantType)
            {
                case VssOAuthConstants.AuthorizationCodeGrantType:
                    var codeValue = GetRequiredValue(form, VssOAuthConstants.Code, VssOAuthErrorCodes.InvalidRequest);
                    parsedParameters.Add(VssOAuthConstants.Code);
                    return new VssOAuthCodeGrant(codeValue);

                case VssOAuthConstants.ClientCredentialsGrantType:
                    return VssOAuthGrant.ClientCredentials;

                case VssOAuthConstants.JwtBearerAuthorizationGrantType:
                    var assertionValue = GetRequiredValue(form, VssOAuthConstants.Assertion, VssOAuthErrorCodes.InvalidRequest);
                    parsedParameters.Add(VssOAuthConstants.Assertion);
                    var assertion = JsonWebToken.Create(assertionValue);
                    return new VssOAuthJwtBearerGrant(new VssOAuthJwtBearerAssertion(assertion));

                case VssOAuthConstants.RefreshTokenGrantType:
                    var refreshTokenValue = GetRequiredValue(form, VssOAuthConstants.RefreshToken, VssOAuthErrorCodes.InvalidRequest);
                    parsedParameters.Add(VssOAuthConstants.RefreshToken);
                    return new VssOAuthRefreshTokenGrant(refreshTokenValue);

                default:
                    // The OAuth 2.0 spec explicitly allows only ASCII characters in the error description
                    throw new VssOAuthTokenRequestException($"{VssOAuthConstants.GrantType} {grantType} is not supported")
                    {
                        Error = VssOAuthErrorCodes.UnsupportedGrantType,
                    };
            }
        }

        private static VssOAuthClientCredential CreateClientCredentialFromFormInput(
            FormDataCollection form,
            ISet<String> parsedParameters)
        {
            // https://tools.ietf.org/html/rfc7521#section-4.2
            // See the above document for rules on processing client assertions w.r.t other credential types. 
            var clientId = form[VssOAuthConstants.ClientId];
            var clientAssertionType = form[VssOAuthConstants.ClientAssertionType];
            if (clientAssertionType == VssOAuthConstants.JwtBearerClientAssertionType)
            {
                var clientAssertionValue = GetRequiredValue(form, VssOAuthConstants.ClientAssertion, VssOAuthErrorCodes.InvalidClient);
                JsonWebToken clientAssertion = null;
                try
                {
                    clientAssertion = JsonWebToken.Create(clientAssertionValue);
                }
                catch (JsonWebTokenDeserializationException ex)
                {
                    // The OAuth 2.0 spec explicitly allows only ASCII characters in the error description
                    throw new VssOAuthTokenRequestException($"{VssOAuthConstants.ClientAssertion} is not in the correct format", ex)
                    {
                        Error = VssOAuthErrorCodes.InvalidClient
                    };
                }

                // If the client id parameter is present when client assertions are used then it must match exactly
                // the subject claim of the token.
                if (!String.IsNullOrEmpty(clientId))
                {
                    if (clientId.Equals(clientAssertion.Subject, StringComparison.Ordinal))
                    {
                        parsedParameters.Add(VssOAuthConstants.ClientId);
                    }
                    else
                    {
                        // The OAuth 2.0 spec explicitly allows only ASCII characters in the error description
                        throw new VssOAuthTokenRequestException($"{VssOAuthConstants.ClientId} {clientId} does not match {VssOAuthConstants.ClientAssertion} subject {clientAssertion.Subject}")
                        {
                            Error = VssOAuthErrorCodes.InvalidClient,
                        };
                    }
                }
                else
                {
                    clientId = clientAssertion.Subject;
                }

                parsedParameters.Add(VssOAuthConstants.ClientAssertion);
                parsedParameters.Add(VssOAuthConstants.ClientAssertionType);
                return new VssOAuthJwtBearerClientCredential(clientId, new VssOAuthJwtBearerAssertion(clientAssertion));
            }

            if (!String.IsNullOrEmpty(clientId))
            {
                parsedParameters.Add(VssOAuthConstants.ClientId);

                var clientSecret = form[VssOAuthConstants.ClientSecret];
                if (!String.IsNullOrEmpty(clientSecret))
                {
                    parsedParameters.Add(VssOAuthConstants.ClientSecret);
                    return new VssOAuthPasswordClientCredential(clientId, clientSecret);
                }
            }

            return null;
        }

        private static String GetRequiredValue(
            FormDataCollection form,
            String parameterName, 
            String error)
        {
            var value = form[parameterName];
            if (String.IsNullOrEmpty(value))
            {
                throw new VssOAuthTokenRequestException($"{parameterName} is required") { Error = error };
            }

            return value;
        }
#endif

        private VssOAuthTokenParameters m_tokenParameters;

        private readonly VssOAuthGrant m_grant;
        private readonly VssOAuthClientCredential m_clientCredential;
    }
}
