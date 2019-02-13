using System;
using Agent.Plugins.Log.TestResultParser.Contracts;

namespace Agent.Plugins.Log.TestResultParser.Plugin
{
    public class PipelineConfig : IPipelineConfig
    {
        public Guid Project { get; set; }

        public int BuildId { get; set; }
    }
}
