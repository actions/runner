using System.Collections.Generic;

namespace Runner.Server.Services
{
    public interface ISecretsProvider {
        IDictionary<string, string> GetSecretsForEnvironment(GitHub.DistributedTask.ObjectTemplating.ITraceWriter traceWriter, string name = null);
        IDictionary<string, string> GetVariablesForEnvironment(string name = null);
        IDictionary<string, string> GetReservedSecrets();
    }
}