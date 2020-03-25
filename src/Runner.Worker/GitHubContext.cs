using GitHub.DistributedTask.Pipelines.ContextData;
using System;
using System.Collections.Generic;

namespace GitHub.Runner.Worker
{
    public sealed class GitHubContext : DictionaryContextData, IEnvironmentContextData
    {
        private readonly HashSet<string> _contextEnvWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "action",
            "actor",
            "api_url", // temp for GHES alpha release
            "base_ref",
            "event_name",
            "event_path",
            "head_ref",
            "job",
            "ref",
            "repository",
            "repository_owner",
            "run_id",
            "run_number",
            "sha",
            "url", // temp for GHES alpha release
            "workflow",
            "workspace",
        };

        public IEnumerable<KeyValuePair<string, string>> GetRuntimeEnvironmentVariables()
        {
            foreach (var data in this)
            {
                if (_contextEnvWhitelist.Contains(data.Key) && data.Value is StringContextData value)
                {
                    yield return new KeyValuePair<string, string>($"GITHUB_{data.Key.ToUpperInvariant()}", value);
                }
            }
        }
    }
}