namespace GitHub.Services.Content.Common.Telemetry
{
    /// <summary>
    /// Level of detail sent in telemetry. More details are sent for first party customers.
    /// </summary>
    public enum TelemetryInformationLevel
    {
        ThirdParty = 1,
        FirstParty = 2,
        Debug = 3
    }
}
