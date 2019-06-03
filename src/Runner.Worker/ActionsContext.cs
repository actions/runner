using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using Runner.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Runner.Common.Worker
{
    public sealed class ActionsContext : DictionaryContextData
    {
        private static readonly Regex _propertyRegex = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        public void SetOutput(
            string stepName,
            string key,
            string value,
            out string reference)
        {
            var action = GetAction(stepName);
            var outputs = action["outputs"].AssertDictionary("outputs");
            outputs[key] = new StringContextData(value);
            if (_propertyRegex.IsMatch(key))
            {
                reference = $"actions.{stepName}.outputs.{key}";
            }
            else
            {
                reference = $"actions['{stepName}']['outputs']['{key}']";
            }
        }

        public void SetResult(
            string stepName,
            string result)
        {
            var action = GetAction(stepName);
            action["result"] = new StringContextData(result);
        }

        private DictionaryContextData GetAction(string stepName)
        {
            if (TryGetValue(stepName, out var actionObject))
            {
                return actionObject.AssertDictionary("action");
            }

            var action = new DictionaryContextData
            {
                {
                    "result",
                    null
                },
                {
                    "outputs",
                    new DictionaryContextData()
                }
            };
            Add(stepName, action);
            return action;
        }
    }

    public interface IEnvironmentContextData
    {
        IEnumerable<KeyValuePair<String, String>> GetRuntimeEnvironmentVariables();
    }

    public sealed class RunnerContext : DictionaryContextData, IEnvironmentContextData
    {
        public IEnumerable<KeyValuePair<String, String>> GetRuntimeEnvironmentVariables()
        {
            foreach (var data in this)
            {
                // Change to RUNNER_ after the new action toolkits released
                yield return new KeyValuePair<String, String>($"AGENT_{data.Key.ToUpperInvariant()}", data.Value as StringContextData);
            }
        }
    }

    public sealed class GitHubContext : DictionaryContextData, IEnvironmentContextData
    {
        public IEnumerable<KeyValuePair<String, String>> GetRuntimeEnvironmentVariables()
        {
            foreach (var data in this)
            {
                yield return new KeyValuePair<String, String>($"GITHUB_{data.Key.ToUpperInvariant()}", data.Value as StringContextData);
            }
        }
    }
}
