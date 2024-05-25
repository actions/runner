using System.Threading.Tasks;

namespace Runner.Server.Azure.Devops {

    public class DefaultFileProvider : IFileProvider
    {
        public Task<string> ReadFile(string repositoryAndRef, string path)
        {
            return Task.FromResult(System.IO.File.ReadAllText(path));
        }
    }
}