using System.Collections.Generic;

namespace Runner.Server.Services
{
    public class ScheduleSecretsProvider : ISecretsProvider
    {
        public IDictionary<string, IDictionary<string, string>> SecretsEnvironments { get; set; }
        public IDictionary<string, IDictionary<string, string>> VarEnvironments { get; set; }
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
