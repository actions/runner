using System;
using System.Linq;
using Agent.Plugins.Log.TestResultParser.Plugin;
using Xunit;

namespace Test.L0.Plugin.TestResultParser
{
    public class EnumerableExtensionL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public void ListShouldBeBatchedAsPerRequestedSize()
        {
            const int listSize = 2500;
            var rnd = new Random();
            var randomList = Enumerable.Range(1, listSize).OrderBy(e => rnd.Next()).ToList();

            var batchedList = randomList.Batch(1000).ToArray();
            Assert.True(batchedList.Length == 3);
            Assert.True(batchedList[0].Count() == 1000);
            Assert.True(batchedList[1].Count() == 1000);
            Assert.True(batchedList[2].Count() == 500);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public void ListShouldBeBatchedIfSizeIsLessThanBatch()
        {
            const int listSize = 50;
            var rnd = new Random();
            var randomList = Enumerable.Range(1, listSize).OrderBy(e => rnd.Next()).ToList();

            var batchedList = randomList.Batch(100).ToArray();
            
            Assert.True(batchedList.Length == 1);
            Assert.True(batchedList[0].Count() == 50);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Plugin")]
        public void ListShouldBeBatchedForAnEmptyList()
        {
            var rnd = new Random();
            var randomList = Enumerable.Empty<int>();

            var batchedList = randomList.Batch(100).ToArray();
            Assert.True(batchedList.Length == 0);
        }
    }
}
