using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    public class AuthorizationDescription
    {
        public Registration ClientRegistration { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public InitiationError InitiationError { get; set; }

        public AuthorizationScopeDescription[] ScopeDescriptions { get; set; }

        public bool HasError => InitiationError != InitiationError.None;
    }
}
