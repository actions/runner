public class DefaultFileProvider : IFileProvider
{
    public string ReadFile(string repositoryAndRef, string path)
    {
        return System.IO.File.ReadAllText(path);
    }
}