using System;
using System.Collections.Generic;

namespace Agent.Plugins.Log.TestFilePublisher
{
    public class PipelineConfig
    {
        public Guid ProjectGuid { get; set; }

        public string ProjectName { get; set; }

        public int BuildId { get; set; }

        public string BuildUri { get; set; }

        public IList<string> SearchFolders { get; } = new List<string>();

        public IList<string> Patterns { get; } = new List<string>();
    }
}
