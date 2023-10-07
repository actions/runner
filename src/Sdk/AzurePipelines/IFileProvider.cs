using System.Threading.Tasks;

public interface IFileProvider {
    Task<string> ReadFile(string repositoryAndRef, string path);
}