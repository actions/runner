using System;
using System.Linq;
using System.Net;
using GitHub.Services.Common.Internal;

namespace GitHub.Services.Common
{
    /// <summary>
    /// Provides a common implementation for federated credentials.
    /// </summary>
    [Serializable]
    public abstract class FederatedCredential : IssuedTokenCredential
    {
        protected FederatedCredential(IssuedToken initialToken)
            : base(initialToken)
        { 
        }

        public override bool IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            if (webResponse == null)
            {
                return false;
            }

            if (webResponse.StatusCode == HttpStatusCode.Found ||
                webResponse.StatusCode == HttpStatusCode.Redirect)
            {
                return webResponse.Headers.GetValues(HttpHeaders.TfsFedAuthRealm).Any();
            }

            return webResponse.StatusCode == HttpStatusCode.Unauthorized;
        }
    }
}
