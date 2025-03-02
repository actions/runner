using System.Collections.Generic;

namespace Runner.Server.Services
{

    public class AzurePipelinesVariablesProvider : IVariablesProvider
    {
        private readonly ISecretsProvider parent;
        private readonly IDictionary<string, string> rootVars;

        public AzurePipelinesVariablesProvider(ISecretsProvider parent, IDictionary<string, string> rootVars){
            this.parent = parent;
            this.rootVars = rootVars;
        }
    
        public IDictionary<string, string> GetVariablesForEnvironment(string name = null)
        {
            if(string.IsNullOrEmpty(name)) {
                return rootVars;
            }
            return parent.GetVariablesForEnvironment(name);
        }
    }
}
