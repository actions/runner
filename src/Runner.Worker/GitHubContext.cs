using GitHub.DistributedTask.Pipelines.ContextData;
using System;
using System.Collections.Generic;

namespace GitHub.Runner.Worker
{
    public sealed class GitHubContext : DictionaryContextData, IEnvironmentContextData
    {
        public IEnumerable<KeyValuePair<string, string>> GetRuntimeEnvironmentVariables()
        {
            foreach (var data in this)
            {
                if (!data.Key.Equals("token"))
                    yield return new KeyValuePair<string, string>($"GITHUB_{data.Key.ToUpperInvariant()}", data.Value as StringContextData);
            }
        }
    }
}