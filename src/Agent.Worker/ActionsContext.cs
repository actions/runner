using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class ActionsContext : IReadOnlyDictionary<string, object>
    {
        private static readonly Regex _propertyRegex = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private readonly Dictionary<String, Object> _dictionary = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

        public Int32 Count => _dictionary.Count;

        public IEnumerable<string> Keys => _dictionary.Keys;

        public IEnumerable<object> Values => _dictionary.Values;

        public object this[string key] => _dictionary[key];

        public Boolean ContainsKey(string key) => _dictionary.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

        public Boolean TryGetValue(
            string key,
            out object value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public void SetOutput(
            string stepName,
            string key,
            string value,
            out string reference)
        {
            var action = GetAction(stepName);
            var outputs = action["outputs"] as Dictionary<String, String>;
            outputs[key] = value;
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
            action["result"] = result;
        }

        private Dictionary<String, Object> GetAction(string stepName)
        {
            if (_dictionary.TryGetValue(stepName, out var actionObject))
            {
                return actionObject as Dictionary<String, Object>;
            }

            var action = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);
            action.Add("result", null);
            action.Add("outputs", new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase));
            _dictionary.Add(stepName, action);
            return action;
        }
    }
}
