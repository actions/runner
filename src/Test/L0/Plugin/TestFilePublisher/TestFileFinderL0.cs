using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Agent.Plugins.Log.TestFilePublisher;
using Xunit;

namespace Test.L0.Plugin.TestFilePublisher
{
    public class TestFileFinderL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFileFinder_FindFilesWithMatchingPattern()
        {
            var finder = new MockTestFileFinder(new List<string>
            {
                "/tmp"
            });

            var files = await finder.FindAsync(new List<string> { "test-*.xml" });

            Assert.True(files.Count() == 2);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFileFinder_FindFilesWithEmptySearchFolders()
        {
            var finder = new MockTestFileFinder(new List<string>());

            var files = await finder.FindAsync(new List<string> { "test-*.xml" });

            Assert.True(!files.Any());
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public async Task TestFileFinder_FindFilesWithEmptyPattern()
        {
            var finder = new MockTestFileFinder(new List<string>
            {
                "/tmp"
            });

            var files = await finder.FindAsync(new List<string>());

            Assert.True(!files.Any());
        }
    }

    public class MockTestFileFinder : TestFileFinder
    {
        public MockTestFileFinder(IList<string> searchFolders) : base(searchFolders)
        {
        }

        protected override IEnumerable<string> GetFiles(string path, string[] searchPatterns, SearchOption searchOption = SearchOption.AllDirectories)
        {
            return new List<string>
            {
                "/tmp/test-1.xml",
                "/tmp/test-2.xml",
                "/tmp/test-1.xml"
        };
        }
    }
}
