using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GitHub.Services.Common.Internal
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpHeaders
    {
        public const String ActivityId = "ActivityId";
        public const String TfsServiceError = "X-TFS-ServiceError";
        public const String TfsSessionHeader = "X-TFS-Session";
        public const String TfsFedAuthRealm = "X-TFS-FedAuthRealm";
        public const String TfsFedAuthIssuer = "X-TFS-FedAuthIssuer";
        public const String TfsFedAuthRedirect = "X-TFS-FedAuthRedirect";
        public const String VssE2EID = "X-VSS-E2EID";

        public const String VssUserData = "X-VSS-UserData";
        public const String VssAgentHeader = "X-VSS-Agent";
        public const String VssAuthenticateError = "X-VSS-AuthenticateError";

        public const String VssRateLimitDelay = "X-RateLimit-Delay";
        public const String VssRateLimitReset = "X-RateLimit-Reset";
        
        public const String VssHostOfflineError = "X-VSS-HostOfflineError";

        public const string VssRequestPriority = "X-VSS-RequestPriority";

        public const string Authorization = "Authorization";
        public const string ProxyAuthenticate = "Proxy-Authenticate";
        public const string WwwAuthenticate = "WWW-Authenticate";

        public const string AfdResponseRef = "X-MSEdge-Ref";
    }
}
