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
    public sealed class StepsContext
    {
        private static readonly Regex _propertyRegex = new Regex("^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private readonly DictionaryContextData _contextData = new DictionaryContextData();

        public void ClearScope(string scopeName)
        {
            if (_contextData.TryGetValue(scopeName, out _))
            {
                _contextData[scopeName] = new DictionaryContextData();
            } 
        }

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
            string stepName,
            string outputName,
            string value,
            out string reference)
        {
            var step = GetStep(scopeName, stepName);
            var outputs = step["outputs"].AssertDictionary("outputs");
            outputs[outputName] = new StringContextData(value);
            if (_propertyRegex.IsMatch(outputName))
            {
                reference = $"steps.{stepName}.outputs.{outputName}";
            }
            else
            {
                reference = $"steps['{stepName}']['outputs']['{outputName}']";
            }
        }

        public void SetConclusion(
            string scopeName,
            string stepName,
            ActionResult conclusion)
        {
            var step = GetStep(scopeName, stepName);
            step["conclusion"] = new StringContextData(conclusion.ToString().ToLowerInvariant());
        }

        public void SetOutcome(
            string scopeName,
            string stepName,
            ActionResult outcome)
        {
            var step = GetStep(scopeName, stepName);
            step["outcome"] = new StringContextData(outcome.ToString().ToLowerInvariant());
        }

        private DictionaryContextData GetStep(string scopeName, string stepName)
        {
            var scope = GetScope(scopeName);
            var step = default(DictionaryContextData);
            if (scope.TryGetValue(stepName, out var stepValue))
            {
                step = stepValue.AssertDictionary("step");
            }
            else
            {
                step = new DictionaryContextData
                {
                    { "outputs", new DictionaryContextData() },
                };
                scope.Add(stepName, step);
            }

            return step;
        }
    }
}
