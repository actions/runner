using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebPlatform;
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
        private long _totalThrottlingDelayInMilliseconds = 0;
        private bool _throttlingReported = false;
        private string _area;
        private string _feature;
        public HostTypes SupportedHostTypes => HostTypes.Build;

        public void ProcessCommand(IExecutionContext context, Command command)
        {
            if (string.Equals(command.Event, WellKnownEventTrackCommand.TrackEvent, StringComparison.OrdinalIgnoreCase))
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

            var ciService = HostContext.GetService<ICustomerIntelligenceService>();
            var vssConnection = WorkerUtilies.GetVssConnection(context, new DelegatingHandler[] { new ThrottlingReportHandler(ciService) });
            ciService.Initialize(vssConnection);

            var commandContext = HostContext.CreateService<IAsyncCommandContext>();
            commandContext.InitializeCommandContext(context, StringUtil.Loc("Telemetry"));
            commandContext.Task = ciService.PublishEventsAsync(new CustomerIntelligenceEvent[] { PopulateCustomerIntelligenceData(context, data) });
            context.AsyncCommands.Add(commandContext);

            // Hook up Throttling event, we will log warning on server tarpit.
            ciService.CustomerIntelligenceQueueThrottling += ServiceThrottling_EventReceived;
            if (_throttlingReported)
            {
                context.Warning(StringUtil.Loc("ServerTarpit"));
            }
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

            // Expected format is key1=Value1;key2=Value2;Key3=Value3
            // TODO escape ; if value contains something like this ke\;y2
            var ciProperties = new Dictionary<string, object>();
            foreach (var ciProp in Split(data, ";", '\\'))
            {
                var keyValuePair = ciProp.Split(new char[] { '=' }, count: 2);
                object value;
                if (keyValuePair.Length == 2 && !ciProperties.TryGetValue(keyValuePair[0], out value))
                {
                    ciProperties.Add(keyValuePair[0], keyValuePair[1]);
                }
                else
                {
                    context.Debug("Ignoring the data as the key is duplicated: " + keyValuePair);
                }
            }

            return new CustomerIntelligenceEvent
            {
                Area = _area,
                Feature = _feature,
                Properties = ciProperties
            };
        }

        private void ServiceThrottling_EventReceived(object sender, ThrottlingEventArgs data)
        {
            Interlocked.Add(ref _totalThrottlingDelayInMilliseconds, Convert.ToInt64(data.Delay.TotalMilliseconds));
            if (!_throttlingReported)
            {
                _throttlingReported = true;
            }
        }

        /* Split string functionality with escape functionality
            Ex: abc;de\;fg;xyz  split by ";" escape by "\" => abc de;fg xyz
        */
        private static IEnumerable<string> Split(string input, string separator, char escapeCharacter)
        {
            var op = new List<string>();
            var startOfSegment = 0;
            var index = 0;
            while (index < input.Length)
            {
                index = input.IndexOf(separator, index, StringComparison.Ordinal);
                if (index > 0 && input[index - 1] == escapeCharacter)
                {
                    index += separator.Length;
                    continue;
                }
                if (index == -1)
                {
                    break;
                }
                op.Add(input.Substring(startOfSegment, index - startOfSegment).Replace(escapeCharacter + separator, separator));
                index += separator.Length;
                startOfSegment = index;
            }
            op.Add(input.Substring(startOfSegment).Replace(escapeCharacter + separator, separator));

            return op;
        }

        internal static class WellKnownEventTrackCommand
        {
            internal static readonly string TrackEvent = "trackevent";
        }

        internal static class WellKnownEventTrackProperties
        {
            internal static readonly string Area = "area";
            internal static readonly string Feature = "feature";
        }
    }
}