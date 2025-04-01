using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace Runner.Server.Azure.Devops {
    public interface IRequiredParametersProvider {
        Task<TemplateToken> GetRequiredParameter(string name, string type, System.Collections.Generic.IEnumerable<string> enumerable);
        Task ReportInvalidParameterValue(string name, string type, string message);
    }
}