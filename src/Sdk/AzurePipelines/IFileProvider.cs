using System.Threading.Tasks;

namespace Runner.Server.Azure.Devops {

    public interface IFileProvider {
        Task<string> ReadFile(string repositoryAndRef, string path);
    }
}