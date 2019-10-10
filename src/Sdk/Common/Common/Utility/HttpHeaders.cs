using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GitHub.Services.Common.Internal
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HttpHeaders
    {
        public const String ActivityId = "ActivityId";
        public const String ETag = "ETag";
        public const String TfsVersion = "X-TFS-Version";
        public const String TfsRedirect = "X-TFS-Redirect";
        public const String TfsException = "X-TFS-Exception";
        public const String TfsServiceError = "X-TFS-ServiceError";
        public const String TfsSessionHeader = "X-TFS-Session";
        public const String TfsSoapException = "X-TFS-SoapException";
        public const String TfsFedAuthRealm = "X-TFS-FedAuthRealm";
        public const String TfsFedAuthIssuer = "X-TFS-FedAuthIssuer";
        public const String TfsFedAuthRedirect = "X-TFS-FedAuthRedirect";
        public const String VssAuthorizationEndpoint = "X-VSS-AuthorizationEndpoint";
        public const String VssPageHandlers = "X-VSS-PageHandlers";
        public const String VssE2EID = "X-VSS-E2EID";
        public const String VssOrchestrationId = "X-VSS-OrchestrationId";
        public const String AuditCorrelationId = "X-VSS-Audit-CorrelationId";
        public const String VssOriginUserAgent = "X-VSS-OriginUserAgent";

        // Internal Headers that we use in our client.
        public const string TfsInstanceHeader = "X-TFS-Instance";
        public const string TfsVersionOneHeader = "X-VersionControl-Instance";

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Tfs")]
        public const string TfsImpersonate = "X-TFS-Impersonate";
        public const string TfsSubjectDescriptorImpersonate = "X-TFS-SubjectDescriptorImpersonate";

        public const string MsContinuationToken = "X-MS-ContinuationToken";
        public const String VssUserData = "X-VSS-UserData";
        public const String VssAgentHeader = "X-VSS-Agent";
        public const String VssAuthenticateError = "X-VSS-AuthenticateError";
        public const string VssReauthenticationAction = "X-VSS-ReauthenticationAction";
        public const string RequestedWith = "X-Requested-With";

        public const String VssRateLimitResource = "X-RateLimit-Resource";
        public const String VssRateLimitDelay = "X-RateLimit-Delay";
        public const String VssRateLimitLimit = "X-RateLimit-Limit";
        public const String VssRateLimitRemaining = "X-RateLimit-Remaining";
        public const String VssRateLimitReset = "X-RateLimit-Reset";
        public const String RetryAfter = "Retry-After";

        public const String VssGlobalMessage = "X-VSS-GlobalMessage";

        public const String VssRequestRouted = "X-VSS-RequestRouted";
        public const String VssUseRequestRouting = "X-VSS-UseRequestRouting";

        public const string VssResourceTenant = "X-VSS-ResourceTenant";
        public const String VssOverridePrompt = "X-VSS-OverridePrompt";

        public const String VssOAuthS2STargetService = "X-VSS-S2STargetService";
        public const String VssHostOfflineError = "X-VSS-HostOfflineError";

        public const string VssForceMsaPassThrough = "X-VSS-ForceMsaPassThrough";
        public const string VssRequestPriority = "X-VSS-RequestPriority";

        // This header represents set of ';' delimited mappings (usually one) that are considered by DetermineAccessMapping API
        public const string VssClientAccessMapping = "X-VSS-ClientAccessMapping";

        // This header is used to download artifacts anonymously.
        // N.B. Some resources secured with download tickets (e.g. TFVC files) are still retrieved with the download 
        // ticket in the query string.
        public const string VssDownloadTicket = "X-VSS-DownloadTicket";

        public const string IfModifiedSince = "If-Modified-Since";
        public const string Authorization = "Authorization";
        public const string Location = "Location";
        public const string ProxyAuthenticate = "Proxy-Authenticate";
        public const string WwwAuthenticate = "WWW-Authenticate";

        public const string AfdIncomingRouteKey = "X-FD-RouteKey";
        public const string AfdOutgoingRouteKey = "X-AS-RouteKey";
        public const string AfdIncomingEndpointList = "X-FD-RouteKeyApplicationEndpointList";
        public const string AfdOutgoingEndpointList = "X-AS-RouteKeyApplicationEndpointList";
        public const string AfdResponseRef = "X-MSEdge-Ref";
        public const string AfdIncomingClientIp = "X-FD-ClientIP";
        public const string AfdIncomingSocketIp = "X-FD-SocketIP";
        public const string AfdIncomingRef = "X-FD-Ref";
        public const string AfdIncomingEventId = "X-FD-EventId";
        public const string AfdIncomingEdgeEnvironment = "X-FD-EdgeEnvironment";
        public const string AfdOutgoingQualityOfResponse = "X-AS-QualityOfResponse";
        public const string AfdOutgoingClientIp = "X-MSEdge-ClientIP";
    }
}
