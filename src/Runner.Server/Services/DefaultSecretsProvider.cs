using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Runner.Server.Services
{
    public class DefaultSecretsProvider : ISecretsProvider
    {
        private Dictionary<string, Dictionary<string, string>> SecretsEnvironments { get; set; }
        private Dictionary<string, Dictionary<string, string>> VarEnvironments { get; set; }
        public DefaultSecretsProvider(IConfiguration configuration) {
            var secenvs = (configuration.GetSection("Runner.Server:Environments").Get<Dictionary<string, Dictionary<string, string>>>() ?? new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)).ToArray();
            SecretsEnvironments = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach(var secenv in secenvs) {
                SecretsEnvironments[secenv.Key] = new Dictionary<string, string>(secenv.Value, StringComparer.OrdinalIgnoreCase);
            }
            var globalSecrets = configuration.GetSection("Runner.Server:Secrets")?.Get<List<Secret>>() ?? new List<Secret>();                
            if(!SecretsEnvironments.ContainsKey("")) {
                SecretsEnvironments[""] = globalSecrets.ToDictionary(sec => sec.Name, sec => sec.Value, StringComparer.OrdinalIgnoreCase);
            }
            var varenvs = (configuration.GetSection("Runner.Server:VarEnvironments").Get<Dictionary<string, Dictionary<string, string>>>() ?? new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)).ToArray();
            VarEnvironments = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach(var varenv in varenvs) {
                VarEnvironments[varenv.Key] = new Dictionary<string, string>(varenv.Value, StringComparer.OrdinalIgnoreCase);
            }

        }
        public IDictionary<string, string> GetSecretsForEnvironment(GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter, string name = null)
        {
            return (SecretsEnvironments.TryGetValue("", out var def) ? def : null).Merge(name != null && SecretsEnvironments.TryGetValue(name, out var val) ? val : null);
        }

        public IDictionary<string, string> GetReservedSecrets()
        {
            return SecretHelper.WithReservedSecrets(null, SecretsEnvironments.TryGetValue("", out var def) ? def : null);
        }

        public IDictionary<string, string> GetVariablesForEnvironment(string name = null)
        {
            return (VarEnvironments.TryGetValue("", out var def) ? def : null).Merge(name != null && VarEnvironments.TryGetValue(name, out var val) ? val : null);
        }
    }
}
