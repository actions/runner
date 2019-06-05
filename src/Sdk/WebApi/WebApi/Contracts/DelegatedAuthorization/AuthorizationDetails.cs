namespace GitHub.Services.DelegatedAuthorization
{
    public class AuthorizationDetails
    {
        public Authorization Authorization { get; set; }
        public Registration ClientRegistration { get; set; }
        public AuthorizationScopeDescription[] ScopeDescriptions { get; set; }
    }
}
