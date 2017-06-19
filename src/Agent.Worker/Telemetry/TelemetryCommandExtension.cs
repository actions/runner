using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebPlatform;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Telemetry
{
    public class TelemetryCommandExtension : AgentService, IWorkerCommandExtension
    {
        private string _area;
        private string _feature;
        public HostTypes SupportedHostTypes => HostTypes.All;

        public void ProcessCommand(IExecutionContext context, Command command)
        {
            if (string.Equals(command.Event, WellKnownEventTrackCommand.Publish, StringComparison.OrdinalIgnoreCase))
            {
                ProcessPublishTelemetryCommand(context, command.Properties, command.Data);
            }
            else
            {
                throw new Exception(StringUtil.Loc("TelemetryCommandNotFound", command.Event));
            }
        }

        public Type ExtensionType
        {
            get
            {
                return typeof(IWorkerCommandExtension);
            }
        }

        public string CommandArea
        {
            get
            {
                return "telemetry";
            }
        }

        private void ProcessPublishTelemetryCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            ArgUtil.NotNull(context, nameof(context));

            LoadTelemetryInputs(eventProperties);

            var ciService = HostContext.GetService<ICustomerIntelligenceServer>();
            var vssConnection = WorkerUtilies.GetVssConnection(context);
            ciService.Initialize(vssConnection);

            var commandContext = HostContext.CreateService<IAsyncCommandContext>();
            commandContext.InitializeCommandContext(context, StringUtil.Loc("Telemetry"));
            commandContext.Task = ciService.PublishEventsAsync(new CustomerIntelligenceEvent[] { PopulateCustomerIntelligenceData(context, data) });
            context.AsyncCommands.Add(commandContext);
        }

        private void LoadTelemetryInputs(Dictionary<string, string> eventProperties)
        {
            eventProperties.TryGetValue(WellKnownEventTrackProperties.Area, out _area);
            if (string.IsNullOrEmpty(_area))
            {
                throw new ArgumentException(StringUtil.Loc("ArgumentNeeded", "Area"));
            }

            eventProperties.TryGetValue(WellKnownEventTrackProperties.Feature, out _feature);
            if (string.IsNullOrEmpty(_feature))
            {
                throw new ArgumentException(StringUtil.Loc("ArgumentNeeded", "Feature"));
            }
        }

        private CustomerIntelligenceEvent PopulateCustomerIntelligenceData(IExecutionContext context, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException(StringUtil.Loc("ArgumentNeeded", "EventTrackerData"));
            }

            try
            {
                var ciProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
                return new CustomerIntelligenceEvent
                {
                    Area = _area,
                    Feature = _feature,
                    Properties = ciProperties
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException(StringUtil.Loc("TelemetryCommandDataError", data, ex.Message));
            }
        }

        internal static class WellKnownEventTrackCommand
        {
            internal static readonly string Publish = "publish";
        }

        internal static class WellKnownEventTrackProperties
        {
            internal static readonly string Area = "area";
            internal static readonly string Feature = "feature";
        }
    }
}