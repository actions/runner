public class DefaultFileProvider : IFileProvider
{
    public string ReadFile(string path)
    {
        return System.IO.File.ReadAllText(path);
    }
}