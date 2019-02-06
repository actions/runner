// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;
using Microsoft.VisualStudio.Services.CustomerIntelligence.WebApi;
using Microsoft.VisualStudio.Services.WebPlatform;

namespace Agent.Plugins.TestResultParser.Plugin
{
    public class TelemetryDataCollector : ITelemetryDataCollector
    {
        private readonly CustomerIntelligenceHttpClient _httpClient;

        public TelemetryDataCollector(IClientFactory clientFactory)
        {
            _httpClient = clientFactory.GetClient<CustomerIntelligenceHttpClient>();
        }

        /// <inheritdoc />
        public void AddToCumulativeTelemetry(string eventArea, string eventName, object value, bool aggregate = false)
        {
            //do nothing
        }

        /// <inheritdoc />
        public Task PublishTelemetryAsync(string eventArea, string eventName, Dictionary<string, object> value)
        {
            var ciEvent = new CustomerIntelligenceEvent
            {
                Area = eventArea,
                Feature = eventName,
                Properties = value
            };

            return _httpClient.PublishEventsAsync(new[] { ciEvent });
        }
    }
}

