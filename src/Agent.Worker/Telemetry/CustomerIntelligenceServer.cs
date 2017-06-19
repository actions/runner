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
    [ServiceLocator(Default = typeof(CustomerIntelligenceServer))]
    public interface ICustomerIntelligenceServer : IAgentService
    {
        void Initialize(VssConnection connection);
        Task PublishEventsAsync(CustomerIntelligenceEvent[] ciEvents);
    }

    // This service is used for tracking task events which are applicable for VSTS internal tasks
    public class CustomerIntelligenceServer : AgentService, ICustomerIntelligenceServer
    {
        private CustomerIntelligenceHttpClient _ciClient;

        public void Initialize(VssConnection connection)
        {
            _ciClient = connection.GetClient<CustomerIntelligenceHttpClient>();
        }

        public Task PublishEventsAsync(CustomerIntelligenceEvent[] ciEvents)
        {
            return _ciClient.PublishEventsAsync(events: ciEvents);
        }
    }
}
