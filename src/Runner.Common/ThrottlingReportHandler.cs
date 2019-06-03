using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Services.Common.Internal;

namespace GitHub.Runner.Common
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
                TimeSpan delay = TimeSpan.FromSeconds(double.Parse(vssRequestDelayed.First()));
                int expirationEpoch = int.Parse(vssRequestQuotaReset.First());
                DateTime expiration = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(expirationEpoch);

                _throttlingReporter.ReportThrottling(delay, expiration);
            }

            return response;
        }
    }
}
