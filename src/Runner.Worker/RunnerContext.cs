using GitHub.DistributedTask.Pipelines.ContextData;
using System;
using System.Collections.Generic;

namespace GitHub.Runner.Worker
{
    public sealed class RunnerContext : DictionaryContextData, IEnvironmentContextData
    {
        public IEnumerable<KeyValuePair<string, string>> GetRuntimeEnvironmentVariables()
        {
            foreach (var data in this)
            {
                yield return new KeyValuePair<string, string>($"RUNNER_{data.Key.ToUpperInvariant()}", data.Value as StringContextData);
            }
        }
    }
}