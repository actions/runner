using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.CustomerIntelligence.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebPlatform;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Telemetry
{
    [ServiceLocator(Default = typeof(CustomerIntelligenceService))]
    public interface ICustomerIntelligenceService : IAgentService, IThrottlingReporter
    {
        void Initialize(VssConnection connection);
        event EventHandler<ThrottlingEventArgs> CustomerIntelligenceQueueThrottling;
        Task PublishEventsAsync(CustomerIntelligenceEvent[] ciEvents);
    }

    // This service is used for tracking task events which are applicable for VSTS internal tasks
    public class CustomerIntelligenceService : AgentService, ICustomerIntelligenceService
    {
        private CustomerIntelligenceHttpClient _ciClient;

        public event EventHandler<ThrottlingEventArgs> CustomerIntelligenceQueueThrottling;

        public void Initialize(VssConnection connection)
        {
            _ciClient = connection.GetClient<CustomerIntelligenceHttpClient>();
        }

        public Task PublishEventsAsync(CustomerIntelligenceEvent[] ciEvents)
        {
            return _ciClient.PublishEventsAsync(events: ciEvents);
        }

        public void ReportThrottling(TimeSpan delay, DateTime expiration)
        {
            Trace.Info($"Receive server throttling report, expect delay {delay} milliseconds till {expiration}");
            var throttlingEvent = CustomerIntelligenceQueueThrottling;
            if (throttlingEvent != null)
            {
                throttlingEvent(this, new ThrottlingEventArgs(delay, expiration));
            }
        }
    }
}
