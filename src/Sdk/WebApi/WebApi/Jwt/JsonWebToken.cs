using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text;
using GitHub.Services.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.Services.WebApi.Jwt
{
    //while the spec defined other possible algorithms
    //in practice these are the only two that are used
    [DataContract]
    public enum JWTAlgorithm
    {
        [EnumMember]
        None,

        [EnumMember]
        HS256,

        [EnumMember]
        RS256
    }

    //JsonWebToken is marked as DataContract so
    //it can me nested in the Payload as an actor token
    //note that the only member serialized is the
    //EncodedToken property, and the OnDeserialized method
    //decodes everything back out
    [DataContract]
    [JsonConverter(typeof(JsonWebTokenConverter))]
    public sealed class JsonWebToken : IssuedToken
    {
        //Default lifetime for a JsonWebToken set to 300 seconds (5 minutes)
        private const int DefaultLifetime = 300;

        #region Factory Methods
        //We chose factory methods for creation, because creation
        //generally involves signing the token, which is a bigger operation
        //than just using a constructor implies

        //this method is used to instantiate the "self-signed" token for obtaining
        //the access token
        public static JsonWebToken Create(string issuer, string audience, DateTime validFrom, DateTime validTo, VssSigningCredentials credentials)
        {
            return Create(issuer, audience, validFrom, validTo, default(DateTime), null, null, null, credentials, allowExpiredCertificate: false);
        }

        //use this method to instantiate the token with user information
        public static JsonWebToken Create(string issuer, string audience, DateTime validFrom, DateTime validTo, IEnumerable<Claim> additionalClaims, JsonWebToken actor)
        {
            //if you are calling this version of the method additionalClaims and actor cannot be null
            ArgumentUtility.CheckForNull(additionalClaims, nameof(additionalClaims));
            ArgumentUtility.CheckForNull(actor, nameof(actor));

            return Create(issuer, audience, validFrom, validTo, default(DateTime), additionalClaims, actor, null, null, allowExpiredCertificate: false);
        }

        //use this method to instantiate the token with user information
        public static JsonWebToken Create(string issuer, string audience, DateTime validFrom, DateTime validTo, IEnumerable<Claim> additionalClaims, string actorToken)
        {
            //if you are calling this version of the method additionalClaims and actor cannot be null
            ArgumentUtility.CheckForNull(additionalClaims, nameof(additionalClaims));
            ArgumentUtility.CheckStringForNullOrEmpty(actorToken, nameof(actorToken));

            return Create(issuer, audience, validFrom, validTo, default(DateTime), additionalClaims, null, actorToken, null, allowExpiredCertificate: false);
        }

        public static JsonWebToken Create(string issuer, string audience, DateTime validFrom, DateTime validTo, IEnumerable<Claim> additionalClaims, VssSigningCredentials credentials)
        {
            //if you are calling this version claims can't be null
            ArgumentUtility.CheckForNull(additionalClaims, nameof(additionalClaims));

            return Create(issuer, audience, validFrom, validTo, default(DateTime), additionalClaims, null, null, credentials, allowExpiredCertificate: false);
        }

        public static JsonWebToken Create(string issuer, string audience, DateTime validFrom, DateTime validTo, IEnumerable<Claim> additionalClaims, VssSigningCredentials credentials, bool allowExpiredCertificate)
        {
            //if you are calling this version claims can't be null
            ArgumentUtility.CheckForNull(additionalClaims, nameof(additionalClaims));

            return Create(issuer, audience, validFrom, validTo, default(DateTime), additionalClaims, null, null, credentials, allowExpiredCertificate);
        }

        public static JsonWebToken Create(string issuer, string audience, DateTime validFrom, DateTime validTo, DateTime issuedAt, IEnumerable<Claim> additionalClaims, VssSigningCredentials credentials)
        {
            //if you are calling this version claims can't be null
            ArgumentUtility.CheckForNull(additionalClaims, nameof(additionalClaims));

            return Create(issuer, audience, validFrom, validTo, issuedAt, additionalClaims, null, null, credentials, allowExpiredCertificate: false);
        }

        private static JsonWebToken Create(string issuer, string audience, DateTime validFrom, DateTime validTo, DateTime issuedAt, IEnumerable<Claim> additionalClaims, JsonWebToken actor, string actorToken, VssSigningCredentials credentials, bool allowExpiredCertificate)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(issuer, nameof(issuer));
            ArgumentUtility.CheckStringForNullOrEmpty(audience, nameof(audience)); // Audience isn't actually required...

            validFrom = validFrom == default(DateTime) ? DateTime.UtcNow : validFrom.ToUniversalTime();
            validTo = validTo == default(DateTime) ? DateTime.UtcNow + TimeSpan.FromSeconds(DefaultLifetime) : validTo.ToUniversalTime();
            //issuedAt is optional, and breaks certain scenarios if it is present, and breaks others if it is not.
            //so only include it if it is explicitly set.
            issuedAt = issuedAt == default(DateTime) ? default(DateTime) : issuedAt.ToUniversalTime();

            JWTHeader header = GetHeader(credentials, allowExpiredCertificate);
            JWTPayload payload = new JWTPayload(additionalClaims) { Issuer = issuer, Audience = audience, ValidFrom = validFrom, ValidTo = validTo, IssuedAt = issuedAt };

            if (actor != null)
            {
                payload.Actor = actor;
            }
            else if (actorToken != null)
            {
                payload.ActorToken = actorToken;
            }

            byte[] signature = GetSignature(header, payload, header.Algorithm, credentials);

            return new JsonWebToken(header, payload, signature);
        }

        public static JsonWebToken Create(string jwtEncodedString)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(jwtEncodedString, nameof(jwtEncodedString));

            JValue value = new JValue(jwtEncodedString);

            return value.ToObject<JsonWebToken>();
        }
        #endregion

        #region .ctors
        private JsonWebToken() { }

        private JsonWebToken(JWTHeader header, JWTPayload payload, byte[] signature)
        {
            ArgumentUtility.CheckForNull(header, nameof(header));
            ArgumentUtility.CheckForNull(payload, nameof(payload));
            //signature allowed to be null
            _header = header;
            _payload = payload;
            _signature = signature;
        }
        #endregion

        #region Base Class Overrides
        protected internal override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.S2S;
            }
        }

        internal override void ApplyTo(IHttpRequest request)
        {
            request.Headers.SetValue(Common.Internal.HttpHeaders.Authorization, $"Bearer {this.EncodedToken}");
        }
        #endregion

        #region Private Fields
        JWTHeader _header;
        JWTPayload _payload;
        byte[] _signature;
        string _encodedToken;
        #endregion

        #region Public Properties
        public string TokenType => _header.Type;

        public JWTAlgorithm Algorithm => _header.Algorithm;

        public string CertificateThumbprint => _header.CertificateThumbprint;

        public string Audience => _payload.Audience;

        public string Issuer => _payload.Issuer;

        public string Subject => _payload.Subject;

        public string NameIdentifier => _payload.NameIdentifier;

        public string IdentityProvider => _payload.IdentityProvider;

        public DateTime ValidTo => _payload.ValidTo;

        public DateTime ValidFrom => _payload.ValidFrom;

        public DateTime IssuedAt => _payload.IssuedAt;

        public bool TrustedForDelegation => _payload.TrustedForDelegation;

        public JsonWebToken Actor => _payload.Actor;

        public string ApplicationIdentifier => _payload.ApplicationIdentifier;

        public string EncodedToken
        {
            get
            {
                if (string.IsNullOrEmpty(_encodedToken))
                {
                    _encodedToken = this.Encode();
                }
                return _encodedToken;
            }
            private set
            {
                this._encodedToken = value;
            }
        }

        public string Scopes => _payload.Scopes;

        #endregion

        #region Public \ Protected Overrides
        public override string ToString()
        {
            return string.Format("{0}.{1}",
                this._header.ToString(), this._payload.ToString());
        }
        #endregion

        #region Internal Properties
        internal IDictionary<string, object> Header => this._header;

        internal IDictionary<string, object> Payload => this._payload;

        internal byte[] Signature => this._signature;

        #endregion

        #region Private Helpers
        private static JWTHeader GetHeader(VssSigningCredentials credentials, bool allowExpired)
        {
            //note credentials are allowed to be null here, see ValidateSigningCredentials
            JWTHeader header = new JWTHeader();

            JWTAlgorithm alg = JsonWebTokenUtilities.ValidateSigningCredentials(credentials, allowExpired);

            header.Algorithm = alg;

            if (alg != JWTAlgorithm.None)
            {
                // Some signing credentials may need to set headers for the JWT
                var jwtHeaderProvider = credentials as IJsonWebTokenHeaderProvider;
                if (jwtHeaderProvider != null)
                {
                    jwtHeaderProvider.SetHeaders(header);
                }
            }

            return header;
        }

        private static byte[] GetSignature(JWTHeader header, JWTPayload payload, VssSigningCredentials credentials, bool allowExpired)
        {
            JWTAlgorithm alg = JsonWebTokenUtilities.ValidateSigningCredentials(credentials, allowExpired);

            return GetSignature(header, payload, alg, credentials);
        }

        //if we alread have the alg, we assume that the creds have been validated already,
        //to save the expense of validating twice in the create function...
        private static byte[] GetSignature(JWTHeader header, JWTPayload payload, JWTAlgorithm alg, VssSigningCredentials signingCredentials)
        {
            if (alg == JWTAlgorithm.None)
            {
                return null;
            }

            ArgumentUtility.CheckForNull(header, nameof(header));
            ArgumentUtility.CheckForNull(payload, nameof(payload));

            string encoding = string.Format("{0}.{1}", header.JsonEncode(), payload.JsonEncode());

            byte[] bytes = Encoding.UTF8.GetBytes(encoding);

            switch (alg)
            {
                case JWTAlgorithm.HS256:
                case JWTAlgorithm.RS256:
                    return signingCredentials.SignData(bytes);

                default:
                    throw new InvalidOperationException();
            }
        }

        private string Encode()
        {
            string encodedHeader = JsonWebTokenUtilities.JsonEncode(this._header);
            string encodedPayload = JsonWebTokenUtilities.JsonEncode(this._payload);
            string encodedSignature = null;
            if (this._signature != null)
            {
                encodedSignature = this._signature.ToBase64StringNoPadding();
            }

            return string.Format("{0}.{1}.{2}", encodedHeader, encodedPayload, encodedSignature);
        }

        //OnDeserialized never gets called by serializer because we have a custom converter, so call this
        //from there...
        //[OnDeserialized]
        private void OnDeserialized(/*StreamingContext context*/)
        {
            if (string.IsNullOrEmpty(this._encodedToken))
                throw new JsonWebTokenDeserializationException();

            string[] fields = this._encodedToken.Split('.');

            if (fields.Length != 3)
                throw new JsonWebTokenDeserializationException();

            this._header = JsonWebTokenUtilities.JsonDecode<JWTHeader>(fields[0]);
            this._payload = JsonWebTokenUtilities.JsonDecode<JWTPayload>(fields[1]);
            if(!string.IsNullOrEmpty(fields[2]))
            {
                this._signature = fields[2].FromBase64StringNoPadding();
            }
        }
        #endregion

        #region Nested Types
        [JsonDictionary]
        private abstract class JWTSectionBase : Dictionary<string, object>
        {
            public override string ToString()
            {
                return JsonConvert.SerializeObject(this, JsonWebTokenUtilities.DefaultSerializerSettings);
            }

            protected T TryGetValueOrDefault<T>(string key)
            {
                object ret;
                if(TryGetValue(key, out ret))
                {
                    //we have to special case DateTime
                    if (typeof(T) == typeof(DateTime))
                    {
                        return (T)(object)ConvertDateTime(ret);
                    }
                    if (typeof(T).GetTypeInfo().IsEnum && ret is string)
                    {
                        return (T)Enum.Parse(typeof(T), (string)ret);
                    }
                    return (T)Convert.ChangeType(ret, typeof(T));
                }

                return default(T);
            }

            protected System.DateTime ConvertDateTime(object obj)
            {
                if(obj is DateTime)
                {
                    return (DateTime)obj;
                }
                else
                {
                    //try to convert to a long, then
                    //convert from there, we expect it
                    //to be a Unix time
                    long longVal = Convert.ToInt64(obj);

                    return longVal.FromUnixEpochTime();
                }

            }
        }

        //these nested types comprise the header and the payload
        //of the JWT, they are [DataContracts] so we can use JSON.NET
        //to produce the JSON

        private class JWTHeader : JWTSectionBase
        {
            public JWTHeader() : base()
            {
                this.Type = "JWT";
            }

            internal string Type
            {
                get
                {
                    return TryGetValueOrDefault<string>(JsonWebTokenHeaderParameters.Type);
                }
                set
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        this.Remove(JsonWebTokenHeaderParameters.Type);
                    }
                    else
                    {
                        this[JsonWebTokenHeaderParameters.Type] = value;
                    }
                }
            }

            internal JWTAlgorithm Algorithm
            {
                get
                {
                    return TryGetValueOrDefault<JWTAlgorithm>(JsonWebTokenHeaderParameters.Algorithm);
                }
                set
                {
                    this[JsonWebTokenHeaderParameters.Algorithm] = value;
                }
            }

            internal string CertificateThumbprint
            {
                get
                {
                    return TryGetValueOrDefault<string>(JsonWebTokenHeaderParameters.X509CertificateThumbprint);
                }
                set
                {
                    if(string.IsNullOrEmpty(value))
                    {
                        this.Remove(JsonWebTokenHeaderParameters.X509CertificateThumbprint);
                    }
                    else
                    {
                        this[JsonWebTokenHeaderParameters.X509CertificateThumbprint] = value;
                    }
                }
            }
        }

        private class JWTPayload : JWTSectionBase
        {
            public JWTPayload() { }

            internal JWTPayload(IEnumerable<Claim> claims)
            {
                this.AddRange(JsonWebTokenUtilities.TranslateToJwtClaims(claims.AsEmptyIfNull()));
            }

            internal string Audience
            {
                get
                {
                    return TryGetValueOrDefault<string>(JsonWebTokenClaims.Audience);
                }
                set
                {
                    ArgumentUtility.CheckStringForNullOrEmpty(value, nameof(Audience));

                    this[JsonWebTokenClaims.Audience] = value;
                }
            }

            internal string Issuer
            {
                get
                {
                    return TryGetValueOrDefault<string>(JsonWebTokenClaims.Issuer);
                }
                set
                {
                    ArgumentUtility.CheckStringForNullOrEmpty(value, nameof(Issuer));

                    this[JsonWebTokenClaims.Issuer] = value;
                }
            }

            internal string Subject
            {
                get
                {
                    return TryGetValueOrDefault<string>(JsonWebTokenClaims.Subject);
                }
                set
                {
                    ArgumentUtility.CheckStringForNullOrEmpty(value, nameof(Subject));

                    this[JsonWebTokenClaims.Subject] = value;
                }
            }

            internal string NameIdentifier
            {
                get
                {
                    return TryGetValueOrDefault<string>(JsonWebTokenClaims.NameId);
                }
                set
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        this.Remove(JsonWebTokenClaims.NameId);
                    }
                    else
                    {
                        this[JsonWebTokenClaims.NameId] = value;
                    }
                }
            }

            internal string IdentityProvider
            {
                get
                {
                    return TryGetValueOrDefault<string>(JsonWebTokenClaims.IdentityProvider);
                }
                set
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        this.Remove(JsonWebTokenClaims.IdentityProvider);
                    }
                    else
                    {
                        this[JsonWebTokenClaims.IdentityProvider] = value;
                    }
                }
            }

            internal DateTime ValidTo
            {
                get
                {
                    return TryGetValueOrDefault<DateTime>(JsonWebTokenClaims.ValidTo);
                }
                set
                {
                    this[JsonWebTokenClaims.ValidTo] = value;
                }
            }

            internal DateTime ValidFrom
            {
                get
                {
                    return TryGetValueOrDefault<DateTime>(JsonWebTokenClaims.ValidFrom);
                }
                set
                {
                    this[JsonWebTokenClaims.ValidFrom] = value;
                }
            }

            internal DateTime IssuedAt
            {
                get
                {
                    return TryGetValueOrDefault<DateTime>(JsonWebTokenClaims.IssuedAt);
                }
                set
                {
                    if (value == default(DateTime))
                    {
                        this.Remove(JsonWebTokenClaims.IssuedAt);
                    }
                    else
                    {
                        this[JsonWebTokenClaims.IssuedAt] = value;
                    }
                }
            }


            internal bool TrustedForDelegation
            {
                get
                {
                    return TryGetValueOrDefault<bool>(JsonWebTokenClaims.TrustedForDelegation);
                }
                set
                {
                    this[JsonWebTokenClaims.TrustedForDelegation] = value;
                }
            }

            internal string ApplicationIdentifier
            {
                get
                {
                    return TryGetValueOrDefault<string>(JsonWebTokenClaims.AppId);
                }
                set
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        this.Remove(JsonWebTokenClaims.AppId);
                    }
                    else
                    {
                        this[JsonWebTokenClaims.AppId] = value;
                    }
                }
            }

            internal JsonWebToken Actor
            {
                get
                {
                    if (_actorToken == null && TryGetValueOrDefault<string>(JsonWebTokenClaims.ActorToken) != null)
                    {
                        _actorToken = JsonConvert.DeserializeObject<JsonWebToken>((string)this[JsonWebTokenClaims.ActorToken], JsonWebTokenUtilities.DefaultSerializerSettings);
                    }
                    return _actorToken;
                }
                set
                {
                    if (value == null)
                    {
                        this.Remove(JsonWebTokenClaims.ActorToken);
                    }
                    else
                    {
                        this[JsonWebTokenClaims.ActorToken] = JsonConvert.SerializeObject(value);
                    }
                }
            }

            internal string ActorToken
            {
                get
                {
                    return TryGetValueOrDefault<string>(JsonWebTokenClaims.ActorToken);
                }
                set
                {
                    this[JsonWebTokenClaims.ActorToken] = value;
                }
            }

            internal string Scopes
            {
                get
                {
                    return TryGetValueOrDefault<string>(JsonWebTokenClaims.Scopes);
                }
                set
                {
                    this[JsonWebTokenClaims.Scopes] = value;
                }
            }

            private JsonWebToken _actorToken;
        }

        //this coverter converts back and forth from the JWT encoded string
        //and this full type
        internal class JsonWebTokenConverter : VssSecureJsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return (objectType == typeof(JsonWebToken));
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                else if (reader.TokenType == JsonToken.String)
                {
                    var ret = new JsonWebToken { EncodedToken = (string)reader.Value };
                    ret.OnDeserialized();
                    return ret;
                }
                else
                    throw new JsonWebTokenDeserializationException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                base.WriteJson(writer, value, serializer);
                if (!(value is JsonWebToken)) throw new JsonWebTokenSerializationException();

                writer.WriteValue(((JsonWebToken)value).EncodedToken);
            }
        }
        #endregion
    }
}
