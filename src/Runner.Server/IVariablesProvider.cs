using System.Collections.Generic;

public interface IVariablesProvider {
    IDictionary<string, string> GetVariablesForEnvironment(string name = null);
}
