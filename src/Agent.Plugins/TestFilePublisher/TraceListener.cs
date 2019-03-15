using System.Diagnostics;
using Agent.Sdk;

namespace Agent.Plugins.Log.TestFilePublisher
{
    public class TestFileTraceListener : TraceListener
    {
        private readonly IAgentLogPluginContext _context;

        public TestFileTraceListener(IAgentLogPluginContext context)
        {
            _context = context;
        }

        public override void Write(string message)
        {
            //ignoring this as this contains trash info
        }

        public override void WriteLine(string message)
        {
            _context.Output(message);
        }
    }
}
