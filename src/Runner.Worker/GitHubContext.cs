using GitHub.DistributedTask.Pipelines.ContextData;
using System;
using System.Collections.Generic;

namespace GitHub.Runner.Worker
{
    public sealed class GitHubContext : DictionaryContextData, IEnvironmentContextData
    {
        private readonly HashSet<string> _contextEnvAllowlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "action_path",
            "action_ref",
            "action_repository",
            "action",
            "actor",
            "api_url",
            "base_ref",
            "env",
            "event_name",
            "event_path",
            "graphql_url",
            "head_ref",
            "job",
            "job_name",
            "path",
            "ref_name",
            "ref_protected",
            "ref_type",
            "ref",
            "repository_owner",
            "repository",
            "retention_days",
            "run_attempt",
            "run_id",
            "run_number",
            "server_url",
            "sha",
            "step_summary",
            "triggering_actor",
            "workflow",
            "workspace",
        };

        public IEnumerable<KeyValuePair<string, string>> GetRuntimeEnvironmentVariables()
        {
            foreach (var data in this)
            {
                if (_contextEnvAllowlist.Contains(data.Key))
                {
                    if (data.Value is StringContextData value)
                    {
                        yield return new KeyValuePair<string, string>($"GITHUB_{data.Key.ToUpperInvariant()}", value);
                    }
                    else if (data.Value is BooleanContextData booleanValue)
                    {
                        yield return new KeyValuePair<string, string>($"GITHUB_{data.Key.ToUpperInvariant()}", booleanValue.ToString());
                    }
                }
            }
        }

        public GitHubContext ShallowCopy()
        {
            var copy = new GitHubContext();

            foreach (var pair in this)
            {
                copy[pair.Key] = pair.Value;
            }

            return copy;
        }
    }
}
