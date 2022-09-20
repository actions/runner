using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.WebApi;
using GitHub.DistributedTask.Logging;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    public sealed class Variables
    {
        private readonly IHostContext _hostContext;
        private readonly ConcurrentDictionary<string, Variable> _variables = new ConcurrentDictionary<string, Variable>(StringComparer.OrdinalIgnoreCase);
        private readonly ISecretMasker _secretMasker;
        private readonly object _setLock = new object();
        private readonly Tracing _trace;

        public IEnumerable<Variable> AllVariables
        {
            get
            {
                return _variables.Values;
            }
        }

        public Variables(IHostContext hostContext, IDictionary<string, VariableValue> copy)
        {
            // Store/Validate args.
            _hostContext = hostContext;
            _secretMasker = _hostContext.SecretMasker;
            _trace = _hostContext.GetTrace(nameof(Variables));
            ArgUtil.NotNull(hostContext, nameof(hostContext));

            // Validate the dictionary, remove any variable with empty variable name.
            ArgUtil.NotNull(copy, nameof(copy));
            if (copy.Keys.Any(k => string.IsNullOrWhiteSpace(k)))
            {
                _trace.Info($"Remove {copy.Keys.Count(k => string.IsNullOrWhiteSpace(k))} variables with empty variable name.");
            }

            // Initialize the variable dictionary.
            List<Variable> variables = new List<Variable>();
            foreach (var variable in copy)
            {
                if (!string.IsNullOrWhiteSpace(variable.Key))
                {
                    variables.Add(new Variable(variable.Key, variable.Value.Value, variable.Value.IsSecret));
                }
            }

            foreach (Variable variable in variables)
            {
                // Store the variable. The initial secret values have already been
                // registered by the Worker class.
                _variables[variable.Name] = variable;
            }
        }

        // DO NOT add file path variable to here.
        // All file path variables needs to be retrive and set through ExecutionContext, so it can handle container file path translation.
        public string Build_Number => Get(SdkConstants.Variables.Build.BuildNumber);

#if OS_WINDOWS
        public bool Retain_Default_Encoding => false;
#else
        public bool Retain_Default_Encoding => true;
#endif

        public bool? Step_Debug => GetBoolean(Constants.Variables.Actions.StepDebug);

        public string System_PhaseDisplayName => Get(Constants.Variables.System.PhaseDisplayName);

        public string Get(string name)
        {
            Variable variable;
            if (_variables.TryGetValue(name, out variable))
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

        public bool TryGetValue(string name, out string val)
        {
            Variable variable;
            if (_variables.TryGetValue(name, out variable))
            {
                val = variable.Value;
                _trace.Verbose($"Get '{name}': '{val}'");
                return true;
            }

            val = null;
            _trace.Verbose($"Get '{name}' (not found)");
            return false;
        }

        public DictionaryContextData ToSecretsContext()
        {
            var result = new DictionaryContextData();
            foreach (var variable in _variables.Values)
            {
                if (variable.Secret &&
                    !string.Equals(variable.Name, Constants.Variables.System.AccessToken, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(variable.Name, "system.github.token", StringComparison.OrdinalIgnoreCase))
                {
                    result[variable.Name] = new StringContextData(variable.Value);
                }
            }
            return result;
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
