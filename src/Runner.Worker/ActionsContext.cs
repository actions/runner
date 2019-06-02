using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    public sealed class ActionsContext
    {
        private static readonly Regex _propertyRegex = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private readonly DictionaryContextData _contextData = new DictionaryContextData();

        public DictionaryContextData GetScope(string scopeName)
        {
            if (scopeName == null)
            {
                scopeName = string.Empty;
            }

            var scope = default(DictionaryContextData);
            if (_contextData.TryGetValue(scopeName, out var scopeValue))
            {
                scope = scopeValue.AssertDictionary("scope");
            }
            else
            {
                scope = new DictionaryContextData();
                _contextData.Add(scopeName, scope);
            }

            return scope;
        }

        public void SetOutput(
            string scopeName,
            string actionName,
            string outputName,
            string value,
            out string reference)
        {
            var action = GetAction(scopeName, actionName);
            var outputs = action["outputs"].AssertDictionary("outputs");
            outputs[outputName] = new StringContextData(value);
            if (_propertyRegex.IsMatch(outputName))
            {
                reference = $"actions.{actionName}.outputs.{outputName}";
            }
            else
            {
                reference = $"actions['{actionName}']['outputs']['{outputName}']";
            }
        }

        public void SetResult(
            string scopeName,
            string actionName,
            string result)
        {
            var action = GetAction(scopeName, actionName);
            action["result"] = new StringContextData(result);
        }

        private DictionaryContextData GetAction(string scopeName, string actionName)
        {
            var scope = GetScope(scopeName);
            var action = default(DictionaryContextData);
            if (scope.TryGetValue(actionName, out var actionValue))
            {
                action = actionValue.AssertDictionary("action");
            }
            else
            {
                action = new DictionaryContextData
                {
                    { "outputs", new DictionaryContextData() },
                };
                scope.Add(actionName, action);
            }

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
