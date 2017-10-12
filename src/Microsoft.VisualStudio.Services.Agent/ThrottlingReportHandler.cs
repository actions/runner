using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common.Internal;

namespace Microsoft.VisualStudio.Services.Agent
{
    public class ThrottlingEventArgs : EventArgs
    {
        public ThrottlingEventArgs(TimeSpan delay, DateTime expiration)
        {
            Delay = delay;
            Expiration = expiration;
        }

        public TimeSpan Delay { get; private set; }
        public DateTime Expiration { get; private set; }
    }

    public interface IThrottlingReporter
    {
        void ReportThrottling(TimeSpan delay, DateTime expiration);
    }

    public class ThrottlingReportHandler : DelegatingHandler
    {
        private IThrottlingReporter _throttlingReporter;

        public ThrottlingReportHandler(IThrottlingReporter throttlingReporter)
            : base()
        {
            ArgUtil.NotNull(throttlingReporter, nameof(throttlingReporter));
            _throttlingReporter = throttlingReporter;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Call the inner handler.
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Inspect whether response has throttling information
            IEnumerable<string> vssRequestDelayed = null;
            IEnumerable<string> vssRequestQuotaReset = null;
            if (response.Headers.TryGetValues(HttpHeaders.VssRateLimitDelay, out vssRequestDelayed) &&
                response.Headers.TryGetValues(HttpHeaders.VssRateLimitReset, out vssRequestQuotaReset) &&
                !string.IsNullOrEmpty(vssRequestDelayed.FirstOrDefault()) &&
                !string.IsNullOrEmpty(vssRequestQuotaReset.FirstOrDefault()))
            {
                TimeSpan delay = TimeSpan.FromMilliseconds(int.Parse(vssRequestDelayed.First()));
                DateTime expiration = DateTime.Parse(vssRequestQuotaReset.First());
                _throttlingReporter.ReportThrottling(delay, expiration);
            }

            return response;
        }
    }
}