namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    [ServiceLocator(Default = typeof(NullExecutionLogger))]
    // NOTE: FetchEngine specific interface shouldn't take dependency on Agent code.
    public interface IConatinerFetchEngineLogger
    {
        void Warning(string message);
        void Output(string message);
        void Debug(string message);
    }
}