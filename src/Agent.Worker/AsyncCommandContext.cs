using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(AsyncCommandContext))]
    public interface IAsyncCommandContext : IAgentService
    {
        string Name { get; }
        Task Task { get; set; }
        void InitializeCommandContext(IExecutionContext context, string name);
        void Output(string message);
        void Debug(string message);
        Task WaitAsync();
    }

    public class AsyncCommandContext : AgentService, IAsyncCommandContext
    {
        private class OutputMessage
        {
            public OutputMessage(OutputType type, string message)
            {
                Type = type;
                Message = message;
            }

            public OutputType Type { get; }
            public String Message { get; }
        }

        private enum OutputType
        {
            Info,
            Debug,
        }

        private IExecutionContext _executionContext;
        private readonly ConcurrentQueue<OutputMessage> _outputQueue = new ConcurrentQueue<OutputMessage>();

        public string Name { get; private set; }
        public Task Task { get; set; }

        public void InitializeCommandContext(IExecutionContext context, string name)
        {
            _executionContext = context;
            Name = name;
        }

        public void Output(string message)
        {
            _outputQueue.Enqueue(new OutputMessage(OutputType.Info, message));
        }

        public void Debug(string message)
        {
            _outputQueue.Enqueue(new OutputMessage(OutputType.Debug, message));
        }

        public async Task WaitAsync()
        {
            Trace.Entering();
            // start flushing output queue
            Trace.Info("Start flush buffered output.");
            _executionContext.Section($"Async Command Start: {Name}");
            OutputMessage output;
            while (!this.Task.IsCompleted)
            {
                while (_outputQueue.TryDequeue(out output))
                {
                    switch (output.Type)
                    {
                        case OutputType.Info:
                            _executionContext.Output(output.Message);
                            break;
                        case OutputType.Debug:
                            _executionContext.Debug(output.Message);
                            break;
                    }
                }

                await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(500)), this.Task);
            }

            // Dequeue one more time make sure all outputs been flush out.
            Trace.Verbose("Command task has finished, flush out all remaining buffered output.");
            while (_outputQueue.TryDequeue(out output))
            {
                switch (output.Type)
                {
                    case OutputType.Info:
                        _executionContext.Output(output.Message);
                        break;
                    case OutputType.Debug:
                        _executionContext.Debug(output.Message);
                        break;
                }
            }

            _executionContext.Section($"Async Command End: {Name}");
            Trace.Info("Finsh flush buffered output.");

            // wait for the async command task
            Trace.Info("Wait till async command task to finish.");
            await Task;
        }
    }
}
