using Agent.Plugins.Log.TestResultParser.Contracts;

namespace Agent.Plugins.Log.TestResultParser.Plugin
{
    public class TelemetryDataWrapper
    {
        public TelemetryDataWrapper(ITelemetryDataCollector telemetry, string telemetryEventName, string telemetrySubArea = null)
        {
            telemetryDataCollector = telemetry;
            this.telemetryEventName = telemetryEventName;
            this.telemetrySubArea = telemetrySubArea;
        }

        public void AddAndAggregate(object value)
        {
            telemetryDataCollector.AddAndAggregate(telemetryEventName, value, telemetrySubArea);
        }

        private string telemetrySubArea;

        public string telemetryEventName;

        public ITelemetryDataCollector telemetryDataCollector;
    }
}
