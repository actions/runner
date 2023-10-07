using System.Threading.Tasks;

public class DefaultFileProvider : IFileProvider
{
    public Task<string> ReadFile(string repositoryAndRef, string path)
    {
        return Task.FromResult(System.IO.File.ReadAllText(path));
    }
}