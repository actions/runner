using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GitHub.Services.DelegatedAuthorization
{
    public class AuthorizationDecision
    {
        public AuthorizationGrant AuthorizationGrant { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public AuthorizationError AuthorizationError { get; set; }

        public Authorization Authorization { get; set; }

        public bool IsAuthorized => AuthorizationGrant != null;

        public bool HasError => AuthorizationError != AuthorizationError.None;
    }
}
