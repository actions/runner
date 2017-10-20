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

            string area;
            if (!eventProperties.TryGetValue(WellKnownEventTrackProperties.Area, out area) || string.IsNullOrEmpty(area))
            {
                throw new ArgumentException(StringUtil.Loc("ArgumentNeeded", "Area"));
            }

            string feature;
            if (!eventProperties.TryGetValue(WellKnownEventTrackProperties.Feature, out feature) || string.IsNullOrEmpty(feature))
            {
                throw new ArgumentException(StringUtil.Loc("ArgumentNeeded", "Feature"));
            }

            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentException(StringUtil.Loc("ArgumentNeeded", "EventTrackerData"));
            }

            CustomerIntelligenceEvent ciEvent;
            try
            {
                var ciProperties = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
                ciEvent = new CustomerIntelligenceEvent()
                {
                    Area = area,
                    Feature = feature,
                    Properties = ciProperties
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException(StringUtil.Loc("TelemetryCommandDataError", data, ex.Message));
            }

            var ciService = HostContext.GetService<ICustomerIntelligenceServer>();
            var vssConnection = WorkerUtilities.GetVssConnection(context);
            ciService.Initialize(vssConnection);

            var commandContext = HostContext.CreateService<IAsyncCommandContext>();
            commandContext.InitializeCommandContext(context, StringUtil.Loc("Telemetry"));
            commandContext.Task = ciService.PublishEventsAsync(new CustomerIntelligenceEvent[] { ciEvent });
            context.AsyncCommands.Add(commandContext);
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