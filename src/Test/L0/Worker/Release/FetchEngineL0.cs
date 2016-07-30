using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Worker.Release;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Worker.Release
{
    public sealed class FetchEngineL0
    {
        private readonly IEnumerable<ContainerItem> mockContainerItems = new List<ContainerItem>
        {
            DummyConatinerItem1,
            DummyConatinerItem2
        };

        private readonly byte[] mockItemContent = Encoding.UTF8.GetBytes("Item Content");

        private readonly ContainerFetchEngineOptions containerFetchEngineTestOptions = new ContainerFetchEngineOptions
        {
            RetryInterval = TimeSpan.FromMilliseconds(1),
            RetryLimit = 1,
            ParallelDownloadLimit = 1,
            GetFileAsyncTimeout = TimeSpan.FromMilliseconds(1000),
        };

        private static readonly ContainerItem DummyConatinerItem1 = new ContainerItem
        {
            ItemType = ItemType.File,
            FileLength = 52,
            Path = "c:\\drop\\file1.txt"
        };

        private static readonly ContainerItem DummyConatinerItem2 = new ContainerItem
        {
            ItemType = ItemType.File,
            FileLength = 52,
            Path = "c:\\drop\\file2.txt"
        };

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void ShouldDownloadAllTheFiles()
        {
            var stubContainerProvider = new StubContainerProvider(mockContainerItems, item => mockItemContent);
            var fetchEngine = GetFetchEngine(stubContainerProvider);

            Task fetchAsync = fetchEngine.FetchAsync();
            await fetchAsync;

            Assert.Equal(1, stubContainerProvider.GetItemsAsynCounter);
            Assert.Equal(mockContainerItems, stubContainerProvider.GetFileTaskArguments);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async void ShouldNotDoParallelDownloadIfSpecified()
        {
            int concurrentAccessCount = 0;
            var stubContainerProvider = new StubContainerProvider(mockContainerItems,
                item =>
                {
                    concurrentAccessCount++;
                    Thread.Sleep(10);
                    if (concurrentAccessCount == 1)
                    {
                        concurrentAccessCount = 0;
                    }
                    return mockItemContent;
                });
            containerFetchEngineTestOptions.ParallelDownloadLimit = 1;
            ContainerFetchEngine fetchEngine = GetFetchEngine(stubContainerProvider);

            Task fetchAsync = fetchEngine.FetchAsync();
            await fetchAsync;

            Assert.Equal(0, concurrentAccessCount);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ShouldHonorRetryLimit()
        {
            int fetchCount = 0;
            var stubContainerProvider = new StubContainerProvider(
                new List<ContainerItem>
                {
                    DummyConatinerItem1
                },
                item1 =>
                {
                    fetchCount++;
                    if (fetchCount == 1)
                    {
                        throw new FileNotFoundException();
                    }
                    return mockItemContent;
                });
            containerFetchEngineTestOptions.RetryLimit = 2;
            var fetchEngine = GetFetchEngine(stubContainerProvider);

            Task fetchAsync = fetchEngine.FetchAsync();
            await fetchAsync;

            Assert.Equal(containerFetchEngineTestOptions.RetryLimit, fetchCount);
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public async Task ShouldSupportCancellation()
        {
            var stubContainerProvider = new StubContainerProvider(
                mockContainerItems,
                item1 =>
                {
                    Thread.Sleep(30);
                    return mockItemContent;
                });
            var cancellationTokenSource = new CancellationTokenSource();
            containerFetchEngineTestOptions.CancellationToken = cancellationTokenSource.Token;
            ContainerFetchEngine fetchEngine = GetFetchEngine(stubContainerProvider);
            cancellationTokenSource.Cancel();
            Task fetchAsync = fetchEngine.FetchAsync();
            await fetchAsync;

            Assert.Equal(0, stubContainerProvider.GetFileTaskArguments.Count);
        }

        private ContainerFetchEngine GetFetchEngine(StubContainerProvider stubContainerProvider)
        {
            return new ContainerFetchEngine(stubContainerProvider,
                string.Empty,
                "c:\\")
            {
                FileSystemManager = new Mock<IReleaseFileSystemManager>().Object,
                ContainerFetchEngineOptions = containerFetchEngineTestOptions
            };
        }
    }

    public class StubContainerProvider : IContainerProvider
    {
        private readonly Func<object, byte[]> getItemFunc;

        public StubContainerProvider(IEnumerable<ContainerItem> containerItems, Func<object, byte[]> itemFunc)
        {
            Items = containerItems;
            getItemFunc = itemFunc;

            GetFileTaskArguments = new List<ContainerItem>();
        }

        public int GetItemsAsynCounter { get; private set; }

        public List<ContainerItem> GetFileTaskArguments { get; private set; }

        public IEnumerable<ContainerItem> Items { get; private set; }

        public Task<IEnumerable<ContainerItem>> GetItemsAsync()
        {
            GetItemsAsynCounter++;
            return Task.Run(() => Items);
        }

        public Task<Stream> GetFileTask(ContainerItem item)
        {
            GetFileTaskArguments.Add(item);
            Task<Stream> fileTask = Task.Run(
                () =>
                {
                    Stream memoryStream = new MemoryStream(getItemFunc(item));
                    return memoryStream;
                });

            return fileTask;
        }
    }
}
