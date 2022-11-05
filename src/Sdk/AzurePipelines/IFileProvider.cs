public interface IFileProvider {
    string ReadFile(string repositoryAndRef, string path);
}