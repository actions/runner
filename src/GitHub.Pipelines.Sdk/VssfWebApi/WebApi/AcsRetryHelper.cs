using System;
using System.ComponentModel;
using System.Net;
using GitHub.Services.Common;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// A helper class that that implements a retry strategy for ACS requests.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AcsRetryHelper : HttpRetryHelper
    {
        /// <summary>
        /// Initializes a new instance of the AcsRetryHelper class.
        /// </summary>
        /// <param name="maxRetries"></param>
        public AcsRetryHelper(int maxRetries)
            : base(maxRetries, CanRetryOnException)
        {
        }

        /// <summary>
        /// This method determines if request to ACS should be retried. 
        /// See http://msdn.microsoft.com/en-us/library/jj878112.aspx for more details.
        /// </summary>
        public static bool CanRetryOnException(Exception ex)
        {
            WebException webEx = ex as WebException;
            HttpStatusCode statusCode = (HttpStatusCode)0;
            HttpWebResponse webResponse = webEx != null ? webEx.Response as HttpWebResponse : null;

            if (webResponse != null)
            {
                statusCode = webResponse.StatusCode;
            }

            /* ACS recommends retrying on the following errors:
            ** 
            **  429 - Too many requests
            **  500 - Internal Server Error
            **  502 - Bad Gateway
            **  503 - Service Unavailable
            **  504 – Gateway Timeout
            **
            **  http://msdn.microsoft.com/en-us/library/jj878112.aspx
            */
            return statusCode == VssNetworkHelper.TooManyRequests ||
                statusCode == HttpStatusCode.InternalServerError ||
                statusCode == HttpStatusCode.BadGateway ||
                statusCode == HttpStatusCode.ServiceUnavailable ||
                statusCode == HttpStatusCode.GatewayTimeout;
        }

    }
}
