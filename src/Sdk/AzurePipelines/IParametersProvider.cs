using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace Runner.Server.Azure.Devops {
    public interface IParametersProvider {
        Task<TemplateToken> GetParameter(string name, string type, System.Collections.Generic.IEnumerable<string> enumerable, TemplateToken defaultValue);
        Task ReportInvalidParameterValue(string name, string type, string message);
    }
}