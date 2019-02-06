using Agent.Plugins.Log.TestResultParser.Contracts;
using Agent.Sdk;

namespace Agent.Plugins.TestResultParser.Plugin
{
    public class TraceLogger : ITraceLogger
    {
        public TraceLogger(IAgentLogPluginContext context)
        {
            _context = context;
        }

        #region interface implementation

        /// <inheritdoc />
        void ITraceLogger.Warning(string text)
        {
            _context.Output($"Warning: {text}");
        }

        /// <inheritdoc />
        void ITraceLogger.Error(string error)
        {
            _context.Output($"Error: {error}");
        }

        /// <inheritdoc />
        void ITraceLogger.Verbose(string text)
        {
            _context.Output($"Debug: {text}");
        }

        /// <inheritdoc />
        void ITraceLogger.Info(string text)
        {
            _context.Output(text);
        }

        #endregion

        private readonly IAgentLogPluginContext _context;
    }
}
