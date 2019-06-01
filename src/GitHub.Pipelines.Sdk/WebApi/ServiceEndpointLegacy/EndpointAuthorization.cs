using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    public sealed class EndpointAuthorizationSchemes
    {
        public const String AzureStorage = "AzureStorage";
        public const String OAuth = "OAuth";
        public const String OAuth2 = "OAuth2";
        public const String OAuthWrap = "OAuthWrap";
        public const String Certificate = "Certificate";
        public const String UsernamePassword = "UsernamePassword";
        public const String Token = "Token";
        public const String PersonalAccessToken = "PersonalAccessToken";
        public const String ServicePrincipal = "ServicePrincipal";
        public const String None = "None";
        public const String Jwt = "JWT";
        public const String InstallationToken = "InstallationToken";
    }

    public sealed class EndpointAuthorizationParameters
    {
        public const String Username = "Username";
        public const String Password = "Password";
        public const String Certificate = "Certificate";
        public const String AccessToken = "AccessToken";
        public const String ApiToken = "ApiToken";
        public const String RefreshToken = "RefreshToken";
        public const String ServicePrincipalId = "ServicePrincipalId";
        public const String ServicePrincipalKey = "ServicePrincipalKey";
        public const String TenantId = "TenantId";
        public const String RealmName = "RealmName";
        public const String IdToken = "IdToken";
        public const String Nonce = "nonce";
        public const String Scope = "Scope";
        public const String Role = "Role";
        public const String ServerCertThumbprint = "ServerCertThumbprint";
        public const String CompleteCallbackPayload = "CompleteCallbackPayload";
        public const String ClientMail = "ClientMail";
        public const String PrivateKey = "PrivateKey";
        public const String Issuer = "Issuer";
        public const String Audience = "Audience";
        public const String StorageAccountName = "StorageAccountName";
        public const String StorageAccessKey = "StorageAccessKey";
        public const String AccessTokenType = "AccessTokenType";
        public const String Signature = "Signature";
    }

    [DataContract]
    public sealed class EndpointAuthorization
    {
        public EndpointAuthorization()
        {
        }

        private EndpointAuthorization(EndpointAuthorization authorizationToClone)
        {
            this.Scheme = authorizationToClone.Scheme;
            if (authorizationToClone.m_parameters != null && authorizationToClone.m_parameters.Count > 0)
            {
                m_parameters = new Dictionary<String, String>(authorizationToClone.m_parameters, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets or sets the scheme used for service endpoint authentication.
        /// </summary>
        [DataMember]
        public String Scheme
        {
            get;
            set;
        }

        public IDictionary<String, String> Parameters
        {
            get
            {
                if (m_parameters == null)
                {
                    m_parameters = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_parameters;
            }
        }

        public EndpointAuthorization Clone()
        {
            return new EndpointAuthorization(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedParameters, ref m_parameters, StringComparer.OrdinalIgnoreCase, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_parameters, ref m_serializedParameters, StringComparer.OrdinalIgnoreCase);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedParameters = null;
        }

        private IDictionary<String, String> m_parameters;

        /// <summary>
        /// Gets or sets the parameters for the selected authorization scheme.
        /// </summary>
        [DataMember(Name = "Parameters", EmitDefaultValue = false)]
        private IDictionary<String, String> m_serializedParameters;
    }
}
