using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.Commerce.Client
{
    /// <summary>
    /// Class that represents methods communicating with the platform service via REST controller.
    /// </summary>
    [ResourceArea(CommerceResourceIds.AreaId)]
    public class ReportingHttpClient : VssHttpClientBase
    {
        #region Constructors

        public ReportingHttpClient(Uri baseUrl, VssCredentials credentials)
            : base(baseUrl, credentials)
        { }

        public ReportingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
            : base(baseUrl, credentials, settings)
        {
        }

        public ReportingHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, handlers)
        {
        }

        public ReportingHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
            : base(baseUrl, credentials, settings, handlers)
        {
        }

        public ReportingHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
            : base(baseUrl, pipeline, disposeHandler)
        {
        }

        #endregion

        /// <summary>
        /// Returns commerce reporting events within time range.
        /// </summary>
        /// <param name="startTime">Start time of events range</param>
        /// <param name="endTime">End time of events range</param>
        /// <param name="filter">OData filter on event properties (optional)</param>
        /// <returns>Commerce events</returns>
        public virtual async Task<IEnumerable<ICommerceEvent>> GetCommerceEvents(string viewName, string resourceName, DateTime startTime, DateTime endTime, string filter = null, 
            object userState = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (new OperationScope(CommerceResourceIds.AreaName, "GetReportingEvents"))
            {
                List<KeyValuePair<String, String>> queryParameters = new List<KeyValuePair<String, String>>();
                queryParameters.Add("startTime", startTime.ToString("u"));
                queryParameters.Add("endTime", endTime.ToString("u"));

                if (!string.IsNullOrEmpty(filter))
                {
                    queryParameters.Add("filter", filter);
                }

                return await SendAsync<IEnumerable<CommerceEvent>>(
                    method: HttpMethod.Get,
                    locationId: CommerceResourceIds.ReportingEventLocationId,
                    routeValues: new { viewName = viewName, resourceName = resourceName },
                    queryParameters: queryParameters,
                    userState: userState,
                    version: new ApiResourceVersion(previewApiVersion, CommerceResourceVersions.ReportingV1Resources),
                    cancellationToken: cancellationToken
                    ).ConfigureAwait(false);
            }
        }

        [ExcludeFromCodeCoverage]
        protected override IDictionary<string, Type> TranslatedExceptions
        {
            get { return s_translatedExceptions; }
        }

        protected static readonly Version previewApiVersion = new Version(3, 2);

        internal static readonly Dictionary<string, Type> s_translatedExceptions = new Dictionary<string, Type>
        {
            {"ReportingViewNotSupportedException", typeof(ReportingViewNotSupportedException)},

            {"ReportingViewInvalidFilterException", typeof(ReportingViewInvalidFilterException)},
        };
    }
}
