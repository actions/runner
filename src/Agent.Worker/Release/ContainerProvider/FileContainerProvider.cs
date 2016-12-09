using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerFetchEngine;
using Microsoft.VisualStudio.Services.FileContainer;
using Microsoft.VisualStudio.Services.FileContainer.Client;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.ContainerProvider.Helpers
{
    public class FileContainerProvider : IContainerProvider
    {
        private readonly AsyncLazy<VssConnection> _connection;
        private readonly AsyncLazy<IEnumerable<FileContainerItem>> _items;
        private readonly IExecutionContext _executionContext;

        public FileContainerProvider(
            long containerId,
            string projectId,
            string rootItemPath,
            Uri tfsUrl,
            string accessToken,
            DelegatingHandler httpRetryOnTimeoutMessageHandler,
            IExecutionContext executionContext,
            bool includeDownloadTickets = false)
        {
            this._executionContext = executionContext;

            // Needs to be async since we don't want to wait for connection creation and lazy since we don't want to create multiple connections.
            this._connection = new AsyncLazy<VssConnection>(
                async () =>
                {
                    var data = await VssConnectionFactory.GetVssConnectionAsync(
                        tfsUrl,
                        accessToken,
                        httpRetryOnTimeoutMessageHandler)
                        .ConfigureAwait(false);
                    return data;
                });

            // Even though current code items is fetch only once we cannot assume it won't be called mutiple times, so making it AsyncLazy
            _items = new AsyncLazy<IEnumerable<FileContainerItem>>(
                async delegate
                {
                    var vssConnection = await GetVssConnection();
                    var client = vssConnection.GetClient<FileContainerHttpClient>();

                    if (string.IsNullOrEmpty(rootItemPath))
                    {
                        executionContext.Output(StringUtil.Loc("RMCachingAllItems"));
                    }
                    else
                    {
                        executionContext.Output(StringUtil.Loc("RMCachingContainerItems", rootItemPath));
                    }

                    Stopwatch watch = Stopwatch.StartNew();

                    List<FileContainerItem> items =
                        await
                            client.QueryContainerItemsAsync(
                                containerId,
                                new Guid(projectId),
                                rootItemPath,
                                includeDownloadTickets: includeDownloadTickets,
                                cancellationToken: executionContext.CancellationToken)
                                .ConfigureAwait(false);

                    watch.Stop();

                    executionContext.Output(StringUtil.Loc("RMCachingComplete", watch.ElapsedMilliseconds));

                    return items;
                });
        }

        public async Task<Stream> GetFileTask(ContainerItem ticketedItem, CancellationToken cancellationToken)
        {
            VssConnection vssConnection = await GetVssConnection();
            var fileContainer = vssConnection.GetClient<FileContainerHttpClient>();

            Stream stream = await fileContainer.DownloadFileAsync(
                ticketedItem.ContainerId,
                ticketedItem.Path,
                cancellationToken,
                scopeIdentifier: ticketedItem.ScopeIdentifier);

            return stream;
        }

        public async Task<IEnumerable<ContainerItem>> GetItemsAsync()
        {
            IEnumerable<FileContainerItem> fileContainerItems = await _items;
            return fileContainerItems.Select(ConvertToContainerItem);
        }

        private static ContainerItem ConvertToContainerItem(FileContainerItem x)
        {
            return new ContainerItem
            {
                ItemType = (ItemType) (int) x.ItemType,
                Path = x.Path,
                FileLength = x.FileLength,
                ContainerId = x.ContainerId,
                ScopeIdentifier = x.ScopeIdentifier
            };
        }

        private async Task<VssConnection> GetVssConnection()
        {
            return await _connection;
        }
    }
}
