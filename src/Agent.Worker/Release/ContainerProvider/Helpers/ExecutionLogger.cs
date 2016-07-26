using Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerProvider.Helpers
{
    public class ExecutionLogger : IConatinerFetchEngineLogger
    {
        private readonly IExecutionContext _executionContext;

        public ExecutionLogger(IExecutionContext executionContext)
        {
            this._executionContext = executionContext;
        }

        public void Warning(string message)
        {
            this._executionContext.Warning(message);
        }

        public void Output(string message)
        {
            this._executionContext.Output(message);
        }

        public void Info(string message)
        {
            this._executionContext.Debug(message);
        }
    }
}