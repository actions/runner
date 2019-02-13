using Agent.Plugins.Log.TestResultParser.Contracts;

namespace Agent.Plugins.Log.TestResultParser.Plugin
{
    public class PipelineTestRun : TestRun
    {
        public PipelineTestRun(string parserUri, string runNamePrefix, int testRunId, int tcmTestRunId) : base(parserUri, runNamePrefix, testRunId)
        {
            TcmTestRunId = tcmTestRunId;
        }

        public int TcmTestRunId { get; }
    }
}
