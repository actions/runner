namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine
{
    public class NullExecutionLogger : IConatinerFetchEngineLogger
    {
        public void Warning(string message)
        {
        }

        public void Output(string message)
        {
        }

        public void Debug(string message)
        {
        }
    }
}