using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class Variables
    {
        private readonly ConcurrentDictionary<string, string> _store = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly IHostContext _hostContext;
        private readonly Tracing _trace;

        public Variables(IHostContext hostContext, IDictionary<string, string> copy, out List<string> warnings)
        {
            // Store/Validate args.
            _hostContext = hostContext;
            _trace = _hostContext.GetTrace(nameof(Variables));
            ArgUtil.NotNull(hostContext, nameof(hostContext));
            ArgUtil.NotNull(copy, nameof(copy));
            foreach (string key in copy.Keys)
            {
                _store[key] = copy[key] ?? string.Empty;
            }

            // Recursively expand the variables.
            RecursivelyExpand(out warnings);
        }

        public BuildCleanOption? Build_Clean { get { return GetEnum<BuildCleanOption>(Constants.Variables.Build.Clean); } }
        public string Build_DefinitionName { get { return Get(Constants.Variables.Build.DefinitionName); } }
        public bool? Build_SyncSources { get { return GetBoolean(Constants.Variables.Build.SyncSources); } }
        public string System_CollectionId { get { return Get(Constants.Variables.System.CollectionId); } }
        public string System_DefinitionId { get { return Get(Constants.Variables.System.DefinitionId); } }
        public string System_HostType { get { return Get(Constants.Variables.System.HostType); } }
        public string System_TFCollectionUrl { get { return Get(WellKnownDistributedTaskVariables.TFCollectionUrl);  } }
        public bool? System_EnableAccessToken { get { return GetBoolean(Constants.Variables.System.EnableAccessToken); } }

        public void ExpandValues(IDictionary<string, string> target)
        {
            _trace.Entering();
            target = target ?? new Dictionary<string, string>();

            // This algorithm does not perform recursive replacement.

            // Process each key in the target dictionary.
            foreach (string targetKey in target.Keys.ToArray())
            {
                _trace.Verbose($"Expanding key: '{targetKey}'");
                int startIndex = 0;
                int prefixIndex;
                int suffixIndex;
                string targetValue = target[targetKey] ?? string.Empty;

                // Find the next macro within the target value.
                while (startIndex < targetValue.Length &&
                    (prefixIndex = targetValue.IndexOf(Constants.Variables.MacroPrefix, startIndex, StringComparison.Ordinal)) >= 0 &&
                    (suffixIndex = targetValue.IndexOf(Constants.Variables.MacroSuffix, prefixIndex + Constants.Variables.MacroPrefix.Length, StringComparison.Ordinal)) >= 0)
                {
                    // A candidate was found.
                    string variableKey = targetValue.Substring(
                        startIndex: prefixIndex + Constants.Variables.MacroPrefix.Length,
                        length: suffixIndex - prefixIndex - Constants.Variables.MacroPrefix.Length);
                    _trace.Verbose($"Found macro candidate: '{variableKey}'");
                    string variableValue;
                    if (!string.IsNullOrEmpty(variableKey) &&
                        TryGetValue(variableKey, out variableValue))
                    {
                        // A matching variable was found.
                        // Update the target value.
                        _trace.Verbose("Macro found.");
                        targetValue = string.Concat(
                            targetValue.Substring(0, prefixIndex),
                            variableValue ?? string.Empty,
                            targetValue.Substring(suffixIndex + Constants.Variables.MacroSuffix.Length));

                        // Bump the start index to prevent recursive replacement.
                        startIndex = prefixIndex + (variableValue ?? string.Empty).Length;
                    }
                    else
                    {
                        // A matching variable was not found.
                        _trace.Verbose("Macro not found.");
                        startIndex = prefixIndex + 1;
                    }
                }

                target[targetKey] = targetValue ?? string.Empty;
            }
        }

        public string Get(string name)
        {
            string val;
            _store.TryGetValue(name, out val);
            _trace.Verbose($"Get '{name}': '{val}'");
            return val;
        }

        public bool? GetBoolean(string name)
        {
            bool val;
            if (bool.TryParse(Get(name), out val))
            {
                return val;
            }

            return null;
        }

        public T? GetEnum<T>(string name) where T : struct
        {
            T val;
            if (Enum.TryParse(Get(name), ignoreCase: true, result: out val))
            {
                return val;
            }

            return null;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _store.GetEnumerator();
        }

        public void Set(string name, string val)
        {
            //TODO: Determine if variable should be added to SecretMasker

            _trace.Verbose($"Set '{name}' = '{val}'");
            _store[name] = val ?? string.Empty;
        }

        public bool TryGetValue(string name, out string val)
        {
            if (_store.TryGetValue(name, out val))
            {
                _trace.Verbose($"Get '{name}': '{val}'");
                return true;
            }

            _trace.Verbose($"Get '{name}' (not found)");
            return false;
        }

        private void RecursivelyExpand(out List<string> warnings)
        {
            // TODO: Should tracing be omitted when expanding secret vales?
            const int MaxDepth = 50;
            // TODO: Max size?
            _trace.Entering();
            warnings = new List<string>();

            // Make a copy of the original dictionary so the expansion results are predictable. Otherwise,
            // depending on the order of expansion, an actual max depth restriction may not be encountered.
            var original = new Dictionary<string, string>(_store, StringComparer.OrdinalIgnoreCase);

            // Process each variable in the dictionary.
            foreach (string key in original.Keys)
            {
                _trace.Verbose($"Expanding variable: '{key}'");

                // This algorithm handles recursive replacement using a stack.
                // 1) Max depth is enforced by leveraging the stack count.
                // 2) Cyclical references are detected by walking the stack.
                // 3) Additional call frames are avoided.
                bool exceedsMaxDepth = false;
                bool hasCycle = false;
                var stack = new Stack<RecursionState>();
                RecursionState state = new RecursionState(key: key, value: original[key] ?? string.Empty);

                // The outer while loop is used to manage popping items from the stack (of state objects).
                while (true)
                {
                    // The inner while loop is used to manage replacement within the current state object.

                    // Find the next macro within the current value.
                    while (state.StartIndex < state.Value.Length &&
                        (state.PrefixIndex = state.Value.IndexOf(Constants.Variables.MacroPrefix, state.StartIndex, StringComparison.Ordinal)) >= 0 &&
                        (state.SuffixIndex = state.Value.IndexOf(Constants.Variables.MacroSuffix, state.PrefixIndex + Constants.Variables.MacroPrefix.Length, StringComparison.Ordinal)) >= 0)
                    {
                        // A candidate was found.
                        string nestedKey = state.Value.Substring(
                            startIndex: state.PrefixIndex + Constants.Variables.MacroPrefix.Length,
                            length: state.SuffixIndex - state.PrefixIndex - Constants.Variables.MacroPrefix.Length);
                        _trace.Verbose($"Found macro candidate: '{nestedKey}'");
                        string nestedValue;
                        if (!string.IsNullOrEmpty(nestedKey) &&
                            original.TryGetValue(nestedKey, out nestedValue))
                        {
                            // A matching variable was found.
                            // Push the current state onto the stack.
                            _trace.Verbose("Macro found.");

                            // Check for max depth.
                            int currentDepth = stack.Count + 1; // Add 1 since the current state isn't on the stack.
                            if (currentDepth == MaxDepth)
                            {
                                // Warn and break out of the while loops.
                                _trace.Warning("Exceeds max depth.");
                                exceedsMaxDepth = true;
                                warnings.Add(StringUtil.Loc("Variable0ExceedsMaxDepth1", key, MaxDepth));
                                break;
                            }
                            // Check for a cyclical reference.
                            else if (string.Equals(state.Key, nestedKey, StringComparison.OrdinalIgnoreCase) ||
                                stack.Any(x => string.Equals(x.Key, nestedKey, StringComparison.OrdinalIgnoreCase)))
                            {
                                // Warn and break out of the while loops.
                                _trace.Warning("Cyclical reference detected.");
                                hasCycle = true;
                                warnings.Add(StringUtil.Loc("Variable0ContainsCyclicalReference", key));
                                break;
                            }
                            else
                            {
                                // Push the current state and start a new state. There is no need to break out
                                // of the inner while loop. It will continue processing the new current state.
                                _trace.Verbose($"Expanding nested variable: '{nestedKey}'");
                                stack.Push(state);
                                state = new RecursionState(key: nestedKey, value: nestedValue ?? string.Empty);
                            }
                        }
                        else
                        {
                            // A matching variable was not found.
                            _trace.Verbose("Macro not found.");
                            state.StartIndex = state.PrefixIndex + 1;
                        }
                    } // End of inner while loop for processing the variable.

                    // No replacement is performed if something went wrong.
                    if (exceedsMaxDepth || hasCycle)
                    {
                        break;
                    }

                    // Check if finished processing the stack.
                    if (stack.Count == 0)
                    {
                        // Store the final value and break out of the outer while loop.
                        Set(state.Key, state.Value);
                        break;
                    }

                    // Adjust and pop the parent state.
                    _trace.Verbose("Popping recursion state.");
                    RecursionState parent = stack.Pop();
                    parent.Value = string.Concat(
                        parent.Value.Substring(0, parent.PrefixIndex),
                        state.Value,
                        parent.Value.Substring(parent.SuffixIndex + Constants.Variables.MacroSuffix.Length));
                    parent.StartIndex = parent.PrefixIndex + (state.Value).Length;
                    state = parent;
                    _trace.Verbose($"Intermediate state '{state.Key}': '{state.Value}'");
                } // End of outer while loop for recursively processing the variable.
            } // End of foreach loop over each key in the dictionary.
        }

        private sealed class RecursionState
        {
            public RecursionState(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; private set; }
            public string Value { get; set; }
            public int StartIndex { get; set; }
            public int PrefixIndex { get; set; }
            public int SuffixIndex { get; set; }
        }
    }
}