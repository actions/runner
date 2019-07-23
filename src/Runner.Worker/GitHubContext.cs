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
                if (!data.Key.Equals("token") && data.Value is StringContextData value)
                {
                    var camelKey = data.Key;
		            var snakeKey = camelKey.Aggregate("", (result, ch) =>
                        result + (result.Length > 0 && char.IsUpper(ch) ?
						    "_" + ch.ToString().ToUpperInvariant()
							: ch.ToString().ToUpperInvariant()));
                    yield return new KeyValuePair<string, string>($"GITHUB_{snakeKey}", value);
                }
            }
        }
    }
}
