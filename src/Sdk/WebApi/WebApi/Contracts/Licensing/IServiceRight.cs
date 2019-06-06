namespace GitHub.Services.Licensing
{
    /// <summary>
    /// Container for service licensing rights
    /// </summary>
    public interface IServiceRight : IUsageRight
    {
        VisualStudioOnlineServiceLevel ServiceLevel { get; }
    }
}
