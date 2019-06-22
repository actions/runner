namespace GitHub.Services.Content.Common.Telemetry
{
    public interface ITelemetrySender
    {
        void StartSender();

        void StopSender();

        void SendActionTelemetry(ActionTelemetryRecord actionTelemetry);

        void SendErrorTelemetry(ErrorTelemetryRecord errorTelemetry);

        void SendRecord(TelemetryRecord record);
    }
}
