namespace Runner.Server.Azure.Devops
{
    public class LocalFileProvider : IFileProvider // TODO: Fix IFileProvider namespacing
    {
        private IDictionary<string, string> repos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public LocalFileProvider(string cwd)
        {
            repos["self"] = cwd;
        }

        public async Task<string> ReadFile(string repositoryAndRef, string path)
        {
            var filePath = ResolveFilePath(repositoryAndRef, path);
            return await Task.FromResult(System.IO.File.ReadAllText(filePath));
        }

        #region Helper methods for local repositories
        public void AddRepo(string repositoryAndRef, string folderPath)
        {
            if (!folderPath.EndsWith(Path.DirectorySeparatorChar))
            {
                folderPath = $"{folderPath}{Path.DirectorySeparatorChar}";
            }
            string repoName = repositoryAndRef.Split("@")[0];
            repos[repoName] = folderPath;
        }

        public void AddRepo(string projectName, string repositoryName, string folderPath)
        {
            var key = $"{projectName}/{repositoryName}".ToUpper();
            AddRepo(key, folderPath);
        }



        private string ResolveFilePath(string repositoryAndRef, string path)
        {
            var cwd = GetRepoLocalFolder(repositoryAndRef);
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }
            return Path.Combine(cwd, path);
        }

        private string GetRepoLocalFolder(string repositoryAndRef)
        {
            var repoName = (repositoryAndRef ?? "self").Split("@")[0];
            return repos[repoName];
        }
        #endregion
    }
}
