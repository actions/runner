using GitHub.DistributedTask.ObjectTemplating;
using Xunit.Abstractions;

namespace Runner.Server.Azure.Devops
{
    class xUnitTraceWriter : ITraceWriter
    {
        private ITestOutputHelper output;

        public xUnitTraceWriter(ITestOutputHelper output)
        {
            this.output = output;
        }

        public void Error(string format, params object[] args)
        {
            output.WriteLine($"ERROR: {format}", args);
        }

        public void Info(string format, params object[] args)
        {
            output.WriteLine(format, args);
        }

        public void Verbose(string format, params object[] args)
        {
            output.WriteLine($"DEBUG: {format}", args);
        }
    }
}
