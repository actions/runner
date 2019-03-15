using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Agent.Plugins.Log.TestFilePublisher
{
    public class TestFileFinder : ITestFileFinder
    {
        private readonly IList<string> _searchFolders;

        public TestFileFinder(IList<string> searchFolders)
        {
            _searchFolders = searchFolders;
        }

        public async Task<IEnumerable<string>> FindAsync(IList<string> patterns)
        {
            return await Task.Run(() => Find(patterns));
        }

        protected virtual IEnumerable<string> GetFiles(string path, string[] searchPatterns, SearchOption searchOption = SearchOption.AllDirectories)
        {
            return searchPatterns.AsParallel()
                .SelectMany(searchPattern =>
                    Directory.EnumerateFiles(path, searchPattern, searchOption));
        }

        protected IEnumerable<string> Find(IList<string> patterns)
        {
            var files = Enumerable.Empty<string>();
            if (!_searchFolders.Any() || !patterns.Any())
            {
                return files;
            }

            var testResultFiles = Enumerable.Empty<string>();

            testResultFiles = _searchFolders.AsParallel().Aggregate(testResultFiles, (current, folder) => current.Union(GetFiles(folder, patterns.ToArray())));

            return testResultFiles;
        }
    }
}
