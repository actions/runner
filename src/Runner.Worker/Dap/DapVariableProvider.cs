using System;
using System.Collections.Generic;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Maps runner execution context data to DAP scopes and variables.
    ///
    /// This is the single point where runner context values are materialized
    /// for the debugger. All string values pass through the runner's existing
    /// <see cref="GitHub.DistributedTask.Logging.ISecretMasker"/> so the DAP
    /// surface never exposes anything beyond what a normal CI log would show.
    ///
    /// The secrets scope is intentionally opaque: keys are visible but every
    /// value is replaced with a constant redaction marker.
    ///
    /// Designed to be reusable by future DAP features (evaluate, hover, REPL)
    /// so that masking policy is never duplicated.
    /// </summary>
    internal sealed class DapVariableProvider
    {
        // Well-known scope names that map to top-level expression contexts.
        // Order matters: the index determines the stable variablesReference ID.
        internal static readonly string[] ScopeNames =
        {
            "github", "env", "runner", "job", "steps",
            "secrets", "inputs", "vars", "matrix", "needs"
        };

        // Scope references occupy the range [1, ScopeReferenceMax].
        private const int ScopeReferenceBase = 1;
        private const int ScopeReferenceMax = 100;

        // Dynamic (nested) variable references start above the scope range.
        private const int DynamicReferenceBase = 101;

        internal const string RedactedValue = "***";

        private readonly IHostContext _hostContext;

        // Maps dynamic variable reference IDs to the backing data and its
        // dot-separated path (e.g. "github.event.pull_request").
        private readonly Dictionary<int, (PipelineContextData Data, string Path)> _variableReferences = new();
        private int _nextVariableReference = DynamicReferenceBase;

        public DapVariableProvider(IHostContext hostContext)
        {
            _hostContext = hostContext ?? throw new ArgumentNullException(nameof(hostContext));
        }

        /// <summary>
        /// Clears all dynamic variable references.
        /// Call this whenever the paused execution context changes (e.g. new step)
        /// so that stale nested references are not served to the client.
        /// </summary>
        public void Reset()
        {
            _variableReferences.Clear();
            _nextVariableReference = DynamicReferenceBase;
        }

        /// <summary>
        /// Returns the list of DAP scopes for the given execution context.
        /// Each scope corresponds to a well-known runner expression context
        /// (github, env, secrets, …) and carries a stable variablesReference
        /// that the client can use to drill into variables.
        /// </summary>
        public List<Scope> GetScopes(IExecutionContext context)
        {
            var scopes = new List<Scope>();

            if (context?.ExpressionValues == null)
            {
                return scopes;
            }

            for (int i = 0; i < ScopeNames.Length; i++)
            {
                var scopeName = ScopeNames[i];
                if (!context.ExpressionValues.TryGetValue(scopeName, out var value) || value == null)
                {
                    continue;
                }

                var scope = new Scope
                {
                    Name = scopeName,
                    VariablesReference = ScopeReferenceBase + i,
                    Expensive = false,
                    PresentationHint = scopeName == "secrets" ? "registers" : null
                };

                if (value is DictionaryContextData dict)
                {
                    scope.NamedVariables = dict.Count;
                }
                else if (value is CaseSensitiveDictionaryContextData csDict)
                {
                    scope.NamedVariables = csDict.Count;
                }

                scopes.Add(scope);
            }

            return scopes;
        }

        /// <summary>
        /// Returns the child variables for a given variablesReference.
        /// The reference may point at a top-level scope (1–100) or a
        /// dynamically registered nested container (101+).
        /// </summary>
        public List<Variable> GetVariables(IExecutionContext context, int variablesReference)
        {
            var variables = new List<Variable>();

            if (context?.ExpressionValues == null)
            {
                return variables;
            }

            PipelineContextData data = null;
            string basePath = null;
            bool isSecretsScope = false;

            if (variablesReference >= ScopeReferenceBase && variablesReference <= ScopeReferenceMax)
            {
                var scopeIndex = variablesReference - ScopeReferenceBase;
                if (scopeIndex < ScopeNames.Length)
                {
                    var scopeName = ScopeNames[scopeIndex];
                    isSecretsScope = scopeName == "secrets";
                    if (context.ExpressionValues.TryGetValue(scopeName, out data))
                    {
                        basePath = scopeName;
                    }
                }
            }
            else if (_variableReferences.TryGetValue(variablesReference, out var refData))
            {
                data = refData.Data;
                basePath = refData.Path;
                isSecretsScope = basePath?.StartsWith("secrets", StringComparison.OrdinalIgnoreCase) == true;
            }

            if (data == null)
            {
                return variables;
            }

            ConvertToVariables(data, basePath, isSecretsScope, variables);
            return variables;
        }

        /// <summary>
        /// Applies the runner's secret masker to the given value.
        /// This is the single masking entry-point for all DAP-visible strings
        /// and is intentionally public so future DAP features (evaluate, REPL)
        /// can reuse it without duplicating policy.
        /// </summary>
        public string MaskSecrets(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            return _hostContext.SecretMasker.MaskSecrets(value);
        }

        #region Private helpers

        private void ConvertToVariables(
            PipelineContextData data,
            string basePath,
            bool isSecretsScope,
            List<Variable> variables)
        {
            switch (data)
            {
                case DictionaryContextData dict:
                    foreach (var pair in dict)
                    {
                        variables.Add(CreateVariable(pair.Key, pair.Value, basePath, isSecretsScope));
                    }
                    break;

                case CaseSensitiveDictionaryContextData csDict:
                    foreach (var pair in csDict)
                    {
                        variables.Add(CreateVariable(pair.Key, pair.Value, basePath, isSecretsScope));
                    }
                    break;

                case ArrayContextData array:
                    for (int i = 0; i < array.Count; i++)
                    {
                        var variable = CreateVariable($"[{i}]", array[i], basePath, isSecretsScope);
                        variables.Add(variable);
                    }
                    break;
            }
        }

        private Variable CreateVariable(
            string name,
            PipelineContextData value,
            string basePath,
            bool isSecretsScope)
        {
            var childPath = string.IsNullOrEmpty(basePath) ? name : $"{basePath}.{name}";
            var variable = new Variable
            {
                Name = name,
                EvaluateName = $"${{{{ {childPath} }}}}"
            };

            if (value == null)
            {
                variable.Value = "null";
                variable.Type = "null";
                variable.VariablesReference = 0;
                return variable;
            }

            switch (value)
            {
                case StringContextData str:
                    variable.Value = isSecretsScope ? RedactedValue : MaskSecrets(str.Value);
                    variable.Type = "string";
                    variable.VariablesReference = 0;
                    break;

                case NumberContextData num:
                    variable.Value = num.Value.ToString("G15", System.Globalization.CultureInfo.InvariantCulture);
                    variable.Type = "number";
                    variable.VariablesReference = 0;
                    break;

                case BooleanContextData boolVal:
                    variable.Value = boolVal.Value ? "true" : "false";
                    variable.Type = "boolean";
                    variable.VariablesReference = 0;
                    break;

                case DictionaryContextData dict:
                    variable.Value = $"Object ({dict.Count} properties)";
                    variable.Type = "object";
                    variable.VariablesReference = RegisterVariableReference(dict, childPath);
                    variable.NamedVariables = dict.Count;
                    break;

                case CaseSensitiveDictionaryContextData csDict:
                    variable.Value = $"Object ({csDict.Count} properties)";
                    variable.Type = "object";
                    variable.VariablesReference = RegisterVariableReference(csDict, childPath);
                    variable.NamedVariables = csDict.Count;
                    break;

                case ArrayContextData array:
                    variable.Value = $"Array ({array.Count} items)";
                    variable.Type = "array";
                    variable.VariablesReference = RegisterVariableReference(array, childPath);
                    variable.IndexedVariables = array.Count;
                    break;

                default:
                    var rawValue = value.ToJToken()?.ToString() ?? "unknown";
                    variable.Value = isSecretsScope ? RedactedValue : MaskSecrets(rawValue);
                    variable.Type = value.GetType().Name;
                    variable.VariablesReference = 0;
                    break;
            }

            return variable;
        }

        private int RegisterVariableReference(PipelineContextData data, string path)
        {
            var reference = _nextVariableReference++;
            _variableReferences[reference] = (data, path);
            return reference;
        }

        #endregion
    }
}
