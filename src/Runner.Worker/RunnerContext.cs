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
                if (data.Value is StringContextData stringData)
                {
                    yield return new KeyValuePair<string, string>($"RUNNER_{data.Key.ToUpperInvariant()}", stringData);
                }
            }
        }
    }
}
