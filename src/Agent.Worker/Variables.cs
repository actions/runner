using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class Variables
    {
        private readonly ConcurrentDictionary<string, Variable> _store = new ConcurrentDictionary<string, Variable>(StringComparer.OrdinalIgnoreCase);
        private readonly IHostContext _hostContext;
        private readonly ISecretMasker _secretMasker;
        private readonly Tracing _trace;

        public IEnumerable<KeyValuePair<string, string>> Public
        {
            get
            {
                return _store.Values
                    .Where(x => !x.Secret)
                    .Select(x => new KeyValuePair<string, string>(x.Name, x.Value));
            }
        }

        public Variables(IHostContext hostContext, IDictionary<string, string> copy, IList<MaskHint> maskHints, out List<string> warnings)
        {
            // Store/Validate args.
            _hostContext = hostContext;
            _secretMasker = _hostContext.GetService<ISecretMasker>();
            _trace = _hostContext.GetTrace(nameof(Variables));
            ArgUtil.NotNull(hostContext, nameof(hostContext));

            // Validate the dictionary.
            ArgUtil.NotNull(copy, nameof(copy));
            foreach (string variableName in copy.Keys)
            {
                ArgUtil.NotNullOrEmpty(variableName, nameof(variableName));
            }

            // Filter/validate the mask hints.
            ArgUtil.NotNull(maskHints, nameof(maskHints));
            MaskHint[] variableMaskHints = maskHints.Where(x => x.Type == MaskType.Variable).ToArray();
            foreach (MaskHint maskHint in variableMaskHints)
            {
                string maskHintValue = maskHint.Value;
                ArgUtil.NotNullOrEmpty(maskHintValue, nameof(maskHintValue));
            }

            // Initialize the variable dictionary.
            IEnumerable<Variable> variables =
                from string name in copy.Keys
                join MaskHint maskHint in variableMaskHints // Join the variable names with the variable mask hints.
                on name.ToUpperInvariant() equals maskHint.Value.ToUpperInvariant()
                into maskHintGrouping
                select new Variable(
                    name: name,
                    value: copy[name] ?? string.Empty,
                    secret: maskHintGrouping.Any());
            foreach (Variable variable in variables)
            {
                // Store the variable. The initial secret values have already been
                // registered by the Worker class.
                _store[variable.Name] = variable;
            }

            // Recursively expand the variables.
            RecursivelyExpand(out warnings);
        }

        public string Agent_BuildDirectory { get { return Get(Constants.Variables.Agent.BuildDirectory); } }
        public int? Build_BuildId { get { return GetInt(WellKnownBuildVariables.BuildId); } }
        public BuildCleanOption? Build_Clean { get { return GetEnum<BuildCleanOption>(Constants.Variables.Build.Clean); } }
        public long? Build_ContainerId { get { return GetLong(WellKnownBuildVariables.ContainerId); } }
        public string Build_DefinitionName { get { return Get(Constants.Variables.Build.DefinitionName); } }
        public string Build_RepoTfvcWorkspace { get { return Get(Constants.Variables.Build.RepoTfvcWorkspace); } }
        public string Build_SourcesDirectory { get { return Get(Constants.Variables.Build.SourcesDirectory); } }
        public string Build_SourceVersion { get { return Get(Constants.Variables.Build.SourceVersion); } }
        public bool? Build_SyncSources { get { return GetBoolean(Constants.Variables.Build.SyncSources); } }
        public string System_CollectionId { get { return Get(Constants.Variables.System.CollectionId); } }
        public bool? System_Debug { get { return GetBoolean(Constants.Variables.System.Debug); } }
        public string System_DefinitionId { get { return Get(Constants.Variables.System.DefinitionId); } }
        public bool? System_EnableAccessToken { get { return GetBoolean(Constants.Variables.System.EnableAccessToken); } }
        public string System_HostType { get { return Get(Constants.Variables.System.HostType); } }
        public Guid? System_TeamProjectId { get { return GetGuid(WellKnownBuildVariables.TeamProjectId); } }
        public string System_TFCollectionUrl { get { return Get(WellKnownDistributedTaskVariables.TFCollectionUrl);  } }

        public void ExpandValues(IDictionary<string, string> target)
        {
            _trace.Entering();
            target = target ?? new Dictionary<string, string>();

            // This algorithm does not perform recursive replacement.

            // Process each key in the target dictionary.
            foreach (string targetKey in target.Keys.ToArray())
            {
                _trace.Verbose($"Processing expansion for: '{targetKey}'");
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
            Variable variable;
            if (_store.TryGetValue(name, out variable))
            {
                _trace.Verbose($"Get '{name}': '{variable.Value}'");
                return variable.Value;
            }

            _trace.Verbose($"Get '{name}' (not found)");
            return null;
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

        public Guid? GetGuid(string name)
        {
            Guid val;
            if (Guid.TryParse(Get(name), out val))
            {
                return val;
            }

            return null;
        }

        public int? GetInt(string name)
        {
            int val;
            if (int.TryParse(Get(name), out val))
            {
                return val;
            }

            return null;
        }

        public long? GetLong(string name)
        {
            long val;
            if (long.TryParse(Get(name), out val))
            {
                return val;
            }

            return null;
        }

        public void Set(string name, string val, bool secret = false)
        {
            // Validate the args.
            ArgUtil.NotNullOrEmpty(name, nameof(name));

            // Add or update the variable.
            _store.AddOrUpdate(
                key: name,
                addValueFactory: (string key) =>
                {
                    var variable = new Variable(name, val, secret);
                    if (variable.Secret && !string.IsNullOrEmpty(variable.Value))
                    {
                        _secretMasker.AddValue(variable.Value);
                    }

                    return variable;
                },
                updateValueFactory: (string key, Variable existing) =>
                {
                    var variable = new Variable(name, val, existing.Secret || secret);
                    if (variable.Secret && !string.IsNullOrEmpty(variable.Value))
                    {
                        _secretMasker.AddValue(variable.Value);
                    }

                    return variable;
                });
            _trace.Verbose($"Set '{name}' = '{val}'");
        }

        public bool TryGetValue(string name, out string val)
        {
            Variable variable;
            if (_store.TryGetValue(name, out variable))
            {
                val = variable.Value;
                _trace.Verbose($"Get '{name}': '{val}'");
                return true;
            }

            val = null;
            _trace.Verbose($"Get '{name}' (not found)");
            return false;
        }

        private void RecursivelyExpand(out List<string> warnings)
        {
            const int MaxDepth = 50;
            // TODO: Max size?
            _trace.Entering();
            warnings = new List<string>();

            // Make a copy of the original dictionary so the expansion results are predictable. Otherwise,
            // depending on the order of expansion, an actual max depth restriction may not be encountered.
            var original = new Dictionary<string, Variable>(_store, StringComparer.OrdinalIgnoreCase);

            // Process each variable in the dictionary.
            foreach (string name in original.Keys)
            {
                bool secret = original[name].Secret;
                _trace.Verbose($"Processing expansion for variable: '{name}'");

                // This algorithm handles recursive replacement using a stack.
                // 1) Max depth is enforced by leveraging the stack count.
                // 2) Cyclical references are detected by walking the stack.
                // 3) Additional call frames are avoided.
                bool exceedsMaxDepth = false;
                bool hasCycle = false;
                var stack = new Stack<RecursionState>();
                RecursionState state = new RecursionState(name: name, value: original[name].Value ?? string.Empty);

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
                        string nestedName = state.Value.Substring(
                            startIndex: state.PrefixIndex + Constants.Variables.MacroPrefix.Length,
                            length: state.SuffixIndex - state.PrefixIndex - Constants.Variables.MacroPrefix.Length);
                        if (!secret)
                        {
                            _trace.Verbose($"Found macro candidate: '{nestedName}'");
                        }

                        Variable nestedVariable;
                        if (!string.IsNullOrEmpty(nestedName) &&
                            original.TryGetValue(nestedName, out nestedVariable))
                        {
                            // A matching variable was found.

                            // Check for max depth.
                            int currentDepth = stack.Count + 1; // Add 1 since the current state isn't on the stack.
                            if (currentDepth == MaxDepth)
                            {
                                // Warn and break out of the while loops.
                                _trace.Warning("Exceeds max depth.");
                                exceedsMaxDepth = true;
                                warnings.Add(StringUtil.Loc("Variable0ExceedsMaxDepth1", name, MaxDepth));
                                break;
                            }
                            // Check for a cyclical reference.
                            else if (string.Equals(state.Name, nestedName, StringComparison.OrdinalIgnoreCase) ||
                                stack.Any(x => string.Equals(x.Name, nestedName, StringComparison.OrdinalIgnoreCase)))
                            {
                                // Warn and break out of the while loops.
                                _trace.Warning("Cyclical reference detected.");
                                hasCycle = true;
                                warnings.Add(StringUtil.Loc("Variable0ContainsCyclicalReference", name));
                                break;
                            }
                            else
                            {
                                // Push the current state and start a new state. There is no need to break out
                                // of the inner while loop. It will continue processing the new current state.
                                secret = secret || nestedVariable.Secret;
                                if (!secret)
                                {
                                    _trace.Verbose($"Processing expansion for nested variable: '{nestedName}'");
                                }

                                stack.Push(state);
                                state = new RecursionState(name: nestedName, value: nestedVariable.Value ?? string.Empty);
                            }
                        }
                        else
                        {
                            // A matching variable was not found.
                            if (!secret)
                            {
                                _trace.Verbose("Macro not found.");
                            }

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
                        if (!string.Equals(state.Value, original[name].Value, StringComparison.Ordinal))
                        {
                            Set(state.Name, state.Value, secret);
                        }

                        break;
                    }

                    // Adjust and pop the parent state.
                    if (!secret)
                    {
                        _trace.Verbose("Popping recursion state.");
                    }

                    RecursionState parent = stack.Pop();
                    parent.Value = string.Concat(
                        parent.Value.Substring(0, parent.PrefixIndex),
                        state.Value,
                        parent.Value.Substring(parent.SuffixIndex + Constants.Variables.MacroSuffix.Length));
                    parent.StartIndex = parent.PrefixIndex + (state.Value).Length;
                    state = parent;
                    if (!secret)
                    {
                        _trace.Verbose($"Intermediate state '{state.Name}': '{state.Value}'");
                    }
                } // End of outer while loop for recursively processing the variable.
            } // End of foreach loop over each key in the dictionary.
        }

        private sealed class RecursionState
        {
            public RecursionState(string name, string value)
            {
                Name = name;
                Value = value;
            }

            public string Name { get; private set; }
            public string Value { get; set; }
            public int StartIndex { get; set; }
            public int PrefixIndex { get; set; }
            public int SuffixIndex { get; set; }
        }
    }

    public sealed class Variable
    {
        public string Name { get; private set; }
        public bool Secret { get; private set; }
        public string Value { get; private set; }

        public Variable(string name, string value, bool secret)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            Name = name;
            Value = value ?? string.Empty;
            Secret = secret;
        }
    }
}