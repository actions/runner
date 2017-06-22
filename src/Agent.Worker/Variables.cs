using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BuildWebApi = Microsoft.TeamFoundation.Build.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class Variables
    {
        private readonly IHostContext _hostContext;
        private readonly ConcurrentDictionary<string, Variable> _nonexpanded = new ConcurrentDictionary<string, Variable>(StringComparer.OrdinalIgnoreCase);
        private readonly ISecretMasker _secretMasker;
        private readonly object _setLock = new object();
        private readonly Tracing _trace;
        private ConcurrentDictionary<string, Variable> _expanded;

        public IEnumerable<KeyValuePair<string, string>> Public
        {
            get
            {
                return _expanded.Values
                    .Where(x => !x.Secret)
                    .Select(x => new KeyValuePair<string, string>(x.Name, x.Value));
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Private
        {
            get
            {
                return _expanded.Values
                    .Where(x => x.Secret)
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

            // Validate the dictionary, rmeove any variable with empty variable name.
            ArgUtil.NotNull(copy, nameof(copy));
            if (copy.Keys.Any(k => string.IsNullOrWhiteSpace(k)))
            {
                _trace.Info($"Remove {copy.Keys.Count(k => string.IsNullOrWhiteSpace(k))} variables with empty variable name.");
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
                where !string.IsNullOrWhiteSpace(name)
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
                _nonexpanded[variable.Name] = variable;
            }

            // Recursively expand the variables.
            RecalculateExpanded(out warnings);
        }

        public string Agent_BuildDirectory => Get(Constants.Variables.Agent.BuildDirectory);

        public TaskResult? Agent_JobStatus
        {
            get
            {
                return GetEnum<TaskResult>(Constants.Variables.Agent.JobStatus);
            }

            set
            {
                Set(Constants.Variables.Agent.JobStatus, $"{value}");
            }
        }

        public string Agent_ProxyUrl => Get(Constants.Variables.Agent.ProxyUrl);

        public string Agent_ProxyUsername => Get(Constants.Variables.Agent.ProxyUsername);

        public string Agent_ProxyPassword => Get(Constants.Variables.Agent.ProxyPassword);

        public string Agent_ServerOMDirectory => Get(Constants.Variables.Agent.ServerOMDirectory);

        public string Agent_TempDirectory => Get(Constants.Variables.Agent.TempDirectory);

        public string Agent_ToolsDirectory => Get(Constants.Variables.Agent.ToolsDirectory);

        public bool? Agent_UseNode5 => GetBoolean(Constants.Variables.Agent.UseNode5);

        public string Agent_WorkFolder => Get(Constants.Variables.Agent.WorkFolder);

        public int? Build_BuildId => GetInt(BuildWebApi.WellKnownBuildVariables.BuildId);

        public string Build_BuildUri => Get(BuildWebApi.WellKnownBuildVariables.BuildUri);

        public BuildCleanOption? Build_Clean => GetEnum<BuildCleanOption>(Constants.Variables.Features.BuildDirectoryClean) ?? GetEnum<BuildCleanOption>(Constants.Variables.Build.Clean);

        public long? Build_ContainerId => GetLong(BuildWebApi.WellKnownBuildVariables.ContainerId);

        public string Build_DefinitionName => Get(Constants.Variables.Build.DefinitionName);

        public bool? Build_GatedRunCI => GetBoolean(Constants.Variables.Build.GatedRunCI);

        public string Build_GatedShelvesetName => Get(Constants.Variables.Build.GatedShelvesetName);

        public string Build_RepoTfvcWorkspace => Get(Constants.Variables.Build.RepoTfvcWorkspace);

        public string Build_RequestedFor => Get((BuildWebApi.WellKnownBuildVariables.RequestedFor));

        public string Build_SourceBranch => Get(Constants.Variables.Build.SourceBranch);

        public string Build_SourcesDirectory => Get(Constants.Variables.Build.SourcesDirectory);

        public string Build_SourceTfvcShelveset => Get(Constants.Variables.Build.SourceTfvcShelveset);

        public string Build_SourceVersion => Get(Constants.Variables.Build.SourceVersion);

        public bool? Build_SyncSources => GetBoolean(Constants.Variables.Build.SyncSources);

        public string Release_ArtifactsDirectory => Get(Constants.Variables.Release.ArtifactsDirectory);

        public string Release_ReleaseEnvironmentUri => Get(Constants.Variables.Release.ReleaseEnvironmentUri);

        public string Release_ReleaseUri => Get(Constants.Variables.Release.ReleaseUri);

        public int? Release_Download_BufferSize => GetInt(Constants.Variables.Release.ReleaseDownloadBufferSize);

        public int? Release_Parallel_Download_Limit => GetInt(Constants.Variables.Release.ReleaseParallelDownloadLimit);

        public string System_CollectionId => Get(Constants.Variables.System.CollectionId);

        public bool? System_Debug => GetBoolean(Constants.Variables.System.Debug);

        public string System_DefaultWorkingDirectory => Get(Constants.Variables.System.DefaultWorkingDirectory);

        public string System_DefinitionId => Get(Constants.Variables.System.DefinitionId);

        public bool? System_EnableAccessToken => GetBoolean(Constants.Variables.System.EnableAccessToken);

        public HostTypes System_HostType => GetEnum<HostTypes>(Constants.Variables.System.HostType) ?? HostTypes.None;

        public string System_TaskDefinitionsUri => Get(WellKnownDistributedTaskVariables.TaskDefinitionsUrl);

        public string System_TeamProject => Get(BuildWebApi.WellKnownBuildVariables.TeamProject);

        public Guid? System_TeamProjectId => GetGuid(BuildWebApi.WellKnownBuildVariables.TeamProjectId);

        public string System_TFCollectionUrl => Get(WellKnownDistributedTaskVariables.TFCollectionUrl);

        public void ExpandValues(IDictionary<string, string> target)
        {
            _trace.Entering();
            var source = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Variable variable in _expanded.Values)
            {
                source[variable.Name] = variable.Value;
            }

            VarUtil.ExpandValues(_hostContext, source, target);
        }

        public string Get(string name)
        {
            Variable variable;
            if (_expanded.TryGetValue(name, out variable))
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
            return EnumUtil.TryParse<T>(Get(name));
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
            lock (_setLock)
            {
                // Determine whether the value should be a secret. The approach taken here is somewhat
                // conservative. If the previous expanded variable is a secret, then assume the new
                // value should be a secret as well.
                //
                // Keep in mind, the two goals of flagging variables as secret:
                // 1) Mask secrets from the logs.
                // 2) Keep secrets out of environment variables for tasks. Secrets must be passed into
                //    tasks via inputs. It's better to take a conservative approach when determining
                //    whether a variable should be marked secret. Otherwise nested secret values may
                //    inadvertantly end up in public environment variables.
                secret = secret || (_expanded.ContainsKey(name) && _expanded[name].Secret);

                // Register the secret. Secret masker handles duplicates gracefully.
                if (secret && !string.IsNullOrEmpty(val))
                {
                    _secretMasker.AddValue(val);
                }

                // Store the value as-is to the expanded dictionary and the non-expanded dictionary.
                // It is not expected that the caller needs to store an non-expanded value and then
                // retrieve the expanded value in the same context.
                var variable = new Variable(name, val, secret);
                _expanded[name] = variable;
                _nonexpanded[name] = variable;
                _trace.Verbose($"Set '{name}' = '{val}'");
            }
        }

        public bool TryGetValue(string name, out string val)
        {
            Variable variable;
            if (_expanded.TryGetValue(name, out variable))
            {
                val = variable.Value;
                _trace.Verbose($"Get '{name}': '{val}'");
                return true;
            }

            val = null;
            _trace.Verbose($"Get '{name}' (not found)");
            return false;
        }

        public void RecalculateExpanded(out List<string> warnings)
        {
            // TODO: A performance improvement could be made by short-circuiting if the non-expanded values are not dirty. It's unclear whether it would make a significant difference.

            // Take a lock to prevent the variables from changing while expansion is being processed.
            lock (_setLock)
            {
                const int MaxDepth = 50;
                // TODO: Validate max size? No limit on *nix. Max of 32k per env var on Windows https://msdn.microsoft.com/en-us/library/windows/desktop/ms682653%28v=vs.85%29.aspx
                _trace.Entering();
                warnings = new List<string>();

                // Create a new expanded instance.
                var expanded = new ConcurrentDictionary<string, Variable>(_nonexpanded, StringComparer.OrdinalIgnoreCase);

                // Process each variable in the dictionary.
                foreach (string name in _nonexpanded.Keys)
                {
                    bool secret = _nonexpanded[name].Secret;
                    _trace.Verbose($"Processing expansion for variable: '{name}'");

                    // This algorithm handles recursive replacement using a stack.
                    // 1) Max depth is enforced by leveraging the stack count.
                    // 2) Cyclical references are detected by walking the stack.
                    // 3) Additional call frames are avoided.
                    bool exceedsMaxDepth = false;
                    bool hasCycle = false;
                    var stack = new Stack<RecursionState>();
                    RecursionState state = new RecursionState(name: name, value: _nonexpanded[name].Value ?? string.Empty);

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
                                _nonexpanded.TryGetValue(nestedName, out nestedVariable))
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
                            if (!string.Equals(state.Value, _nonexpanded[name].Value, StringComparison.Ordinal))
                            {
                                // Register the secret.
                                if (secret && !string.IsNullOrEmpty(state.Value))
                                {
                                    _secretMasker.AddValue(state.Value);
                                }

                                // Set the expanded value.
                                expanded[state.Name] = new Variable(state.Name, state.Value, secret);
                                _trace.Verbose($"Set '{state.Name}' = '{state.Value}'");
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

                _expanded = expanded;
            } // End of critical section.
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