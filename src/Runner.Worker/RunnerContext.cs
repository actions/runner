using GitHub.DistributedTask.Pipelines.ContextData;
using System;
using System.Collections.Generic;

public sealed class RunnerContext : DictionaryContextData, IEnvironmentContextData
    {
        public IEnumerable<KeyValuePair<string, string>> GetRuntimeEnvironmentVariables()
        {
            foreach (var data in this)
            {
                // Change to RUNNER_ after the new action toolkits released
                yield return new KeyValuePair<string, string>($"AGENT_{data.Key.ToUpperInvariant()}", data.Value as StringContextData);
            }
        }
    }