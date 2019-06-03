using System.ComponentModel;

namespace GitHub.Services.WebApi.Internal
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class VssConnectionParameterKeys
    {
        public const string User = "user";
        public const string AccessToken = "accessToken";
        public const string VssConnectionMode = "vssConnectionMode";
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class VssConnectionParameterOverrideKeys
    {
        public const string AadInstance = "AadInstance";
        public const string AadNativeClientIdentifier = "AadClientIdentifier";
        public const string AadNativeClientRedirect = "AadNativeClientRedirect";
        public const string AadApplicationTenant = "AadApplicationTenant";
        public const string ConnectedUserRoot = "ConnectedUser";
        public const string FederatedAuthenticationMode = "FederatedAuthenticationMode";
        public const string FederatedAuthenticationUser = "FederatedAuthenticationUser";
        public const string UseAadWindowsIntegrated = "UseAadWindowsIntegrated";
    }
}
