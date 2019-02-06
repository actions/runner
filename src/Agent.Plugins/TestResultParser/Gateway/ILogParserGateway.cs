using System.Threading.Tasks;
using Agent.Plugins.Log.TestResultParser.Contracts;

namespace Agent.Plugins.TestResultParser.Plugin
{
    public interface ILogParserGateway
    {
        /// <summary>
        /// Register all parsers which needs to parse the task console stream
        /// </summary>
        Task InitializeAsync(IClientFactory clientFactory, IPipelineConfig pipelineConfig, ITraceLogger traceLogger);

        /// <summary>
        /// Process the task output data
        /// </summary>
        Task ProcessDataAsync(string data);

        /// <summary>
        /// Complete parsing the data
        /// </summary>
        Task CompleteAsync();
    }
}
