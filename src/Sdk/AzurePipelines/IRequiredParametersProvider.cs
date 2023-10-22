using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Tokens;

namespace Runner.Server.Azure.Devops {
    public interface IRequiredParametersProvider {
        Task<TemplateToken> GetRequiredParameter(string name);
    }
}