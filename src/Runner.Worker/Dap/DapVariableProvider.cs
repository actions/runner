using System;
using System.Collections.Generic;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Provides DAP variable information from the execution context.
    /// Maps workflow contexts (github, env, runner, job, steps, secrets) to DAP scopes and variables.
    /// </summary>
    public sealed class DapVariableProvider
    {
        // Well-known scope names that map to top-level contexts
        private static readonly string[] ScopeNames = { "github", "env", "runner", "job", "steps", "secrets", "inputs", "vars", "matrix", "needs" };

        // Reserved variable reference ranges for scopes (1-100)
        private const int ScopeReferenceBase = 1;
        private const int ScopeReferenceMax = 100;

        // Dynamic variable references start after scope range
        private const int DynamicReferenceBase = 101;

        private readonly IHostContext _hostContext;
        private readonly Dictionary<int, (PipelineContextData Data, string Path)> _variableReferences = new();
        private int _nextVariableReference = DynamicReferenceBase;

        public DapVariableProvider(IHostContext hostContext)
        {
            _hostContext = hostContext;
        }

        /// <summary>
        /// Resets the variable reference state. Call this when the execution context changes.
        /// </summary>
        public void Reset()
        {
            _variableReferences.Clear();
            _nextVariableReference = DynamicReferenceBase;
        }

        /// <summary>
        /// Gets the list of scopes for a given execution context.
        /// Each scope represents a top-level context like 'github', 'env', etc.
        /// </summary>
        public List<Scope> GetScopes(IExecutionContext context, int frameId)
        {
            var scopes = new List<Scope>();

            if (context?.ExpressionValues == null)
            {
                return scopes;
            }

            for (int i = 0; i < ScopeNames.Length; i++)
            {
                var scopeName = ScopeNames[i];
                if (context.ExpressionValues.TryGetValue(scopeName, out var value) && value != null)
                {
                    var variablesRef = ScopeReferenceBase + i;
                    var scope = new Scope
                    {
                        Name = scopeName,
                        VariablesReference = variablesRef,
                        Expensive = false,
                        // Secrets get a special presentation hint
                        PresentationHint = scopeName == "secrets" ? "registers" : null
                    };

                    // Count named variables if it's a dictionary
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
            }

            return scopes;
        }

        /// <summary>
        /// Gets variables for a given variable reference.
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

            // Check if this is a scope reference (1-100)
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
            // Check dynamic references
            else if (_variableReferences.TryGetValue(variablesReference, out var refData))
            {
                data = refData.Data;
                basePath = refData.Path;
                // Check if we're inside the secrets scope
                isSecretsScope = basePath?.StartsWith("secrets", StringComparison.OrdinalIgnoreCase) == true;
            }

            if (data == null)
            {
                return variables;
            }

            // Convert the data to variables
            ConvertToVariables(data, basePath, isSecretsScope, variables);

            return variables;
        }

        /// <summary>
        /// Converts PipelineContextData to DAP Variable objects.
        /// </summary>
        private void ConvertToVariables(PipelineContextData data, string basePath, bool isSecretsScope, List<Variable> variables)
        {
            switch (data)
            {
                case DictionaryContextData dict:
                    ConvertDictionaryToVariables(dict, basePath, isSecretsScope, variables);
                    break;

                case CaseSensitiveDictionaryContextData csDict:
                    ConvertCaseSensitiveDictionaryToVariables(csDict, basePath, isSecretsScope, variables);
                    break;

                case ArrayContextData array:
                    ConvertArrayToVariables(array, basePath, isSecretsScope, variables);
                    break;

                default:
                    // Scalar value - shouldn't typically get here for a container
                    break;
            }
        }

        private void ConvertDictionaryToVariables(DictionaryContextData dict, string basePath, bool isSecretsScope, List<Variable> variables)
        {
            foreach (var pair in dict)
            {
                var variable = CreateVariable(pair.Key, pair.Value, basePath, isSecretsScope);
                variables.Add(variable);
            }
        }

        private void ConvertCaseSensitiveDictionaryToVariables(CaseSensitiveDictionaryContextData dict, string basePath, bool isSecretsScope, List<Variable> variables)
        {
            foreach (var pair in dict)
            {
                var variable = CreateVariable(pair.Key, pair.Value, basePath, isSecretsScope);
                variables.Add(variable);
            }
        }

        private void ConvertArrayToVariables(ArrayContextData array, string basePath, bool isSecretsScope, List<Variable> variables)
        {
            for (int i = 0; i < array.Count; i++)
            {
                var item = array[i];
                var variable = CreateVariable($"[{i}]", item, basePath, isSecretsScope);
                variable.Name = $"[{i}]";
                variables.Add(variable);
            }
        }

        private Variable CreateVariable(string name, PipelineContextData value, string basePath, bool isSecretsScope)
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
                    if (isSecretsScope)
                    {
                        // Always mask secrets regardless of value
                        variable.Value = "[REDACTED]";
                    }
                    else
                    {
                        // Mask any secret values that might be in non-secret contexts
                        variable.Value = MaskSecrets(str.Value);
                    }
                    variable.Type = "string";
                    variable.VariablesReference = 0;
                    break;

                case NumberContextData num:
                    variable.Value = num.ToString();
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
                    // Unknown type - convert to string representation
                    var rawValue = value.ToJToken()?.ToString() ?? "unknown";
                    variable.Value = MaskSecrets(rawValue);
                    variable.Type = value.GetType().Name;
                    variable.VariablesReference = 0;
                    break;
            }

            return variable;
        }

        /// <summary>
        /// Registers a nested variable reference and returns its ID.
        /// </summary>
        private int RegisterVariableReference(PipelineContextData data, string path)
        {
            var reference = _nextVariableReference++;
            _variableReferences[reference] = (data, path);
            return reference;
        }

        /// <summary>
        /// Masks any secret values in the string using the host context's secret masker.
        /// </summary>
        private string MaskSecrets(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            return _hostContext.SecretMasker.MaskSecrets(value);
        }
    }
}
