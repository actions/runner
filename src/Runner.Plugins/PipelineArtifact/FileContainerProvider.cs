using GitHub.Build.WebApi;
using GitHub.Services.BlobStore.Common;
using GitHub.Services.Content.Common;
using GitHub.Services.Content.Common.Tracing;
using GitHub.Services.FileContainer;
using GitHub.Services.FileContainer.Client;
using GitHub.Services.WebApi;
using Minimatch;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GitHub.Runner.Plugins.PipelineArtifact
{
    internal class FileContainerProvider : IArtifactProvider
    {
        private readonly FileContainerHttpClient containerClient;
        private readonly CallbackAppTraceSource tracer;

        public FileContainerProvider(VssConnection connection, CallbackAppTraceSource tracer)
        {
            containerClient = connection.GetClient<FileContainerHttpClient>();
            this.tracer = tracer;
        }

        public async Task DownloadSingleArtifactAsync(PipelineArtifactDownloadParameters downloadParameters, BuildArtifact buildArtifact, CancellationToken cancellationToken)
        {
            await this.DownloadFileContainerAsync(downloadParameters.ProjectId, buildArtifact, downloadParameters.TargetDirectory, downloadParameters.MinimatchFilters, cancellationToken);
        }

        public async Task DownloadMultipleArtifactsAsync(PipelineArtifactDownloadParameters downloadParameters, IEnumerable<BuildArtifact> buildArtifacts, CancellationToken cancellationToken)
        {
            await this.DownloadFileContainersAsync(downloadParameters.ProjectId, buildArtifacts, downloadParameters.TargetDirectory, downloadParameters.MinimatchFilters, cancellationToken);
        }

        public async Task DownloadFileContainersAsync(Guid projectId, IEnumerable<BuildArtifact> buildArtifacts, string targetDirectory, IEnumerable<string> minimatchFilters, CancellationToken cancellationToken)
        {
            foreach (var buildArtifact in buildArtifacts)
            {
                var dirPath = Path.Combine(targetDirectory, buildArtifact.Name);
                await DownloadFileContainerAsync(projectId, buildArtifact, dirPath, minimatchFilters, cancellationToken, isSingleArtifactDownload: false);
            }
        }

        private (long, string) ParseContainerId(string resourceData)
        {
            // Example of resourceData: "#/7029766/artifacttool-alpine-x64-Debug"
            string[] segments = resourceData.Split('/');
            long containerId;

            if (segments.Length < 3)
            {
                throw new ArgumentException($"Resource data value '{resourceData}' is invalid.");
            }

            if (segments.Length >= 3 && segments[0] == "#" && long.TryParse(segments[1], out containerId))
            {
                var artifactName = String.Join('/', segments, 2, segments.Length - 2);
                return (
                        containerId,
                        artifactName
                        );
            }
            else
            {
                var message = $"Resource data value '{resourceData}' is invalid.";
                throw new ArgumentException(message, "resourceData");
            }
        }

        private async Task DownloadFileContainerAsync(Guid projectId, BuildArtifact artifact, string rootPath, IEnumerable<string> minimatchPatterns, CancellationToken cancellationToken, bool isSingleArtifactDownload = true)
        {
            var containerIdAndRoot = ParseContainerId(artifact.Resource.Data);

            var items = await containerClient.QueryContainerItemsAsync(containerIdAndRoot.Item1, projectId, containerIdAndRoot.Item2);

            tracer.Info($"Start downloading FCS artifact- {artifact.Name}");
            IEnumerable<Func<string, bool>> minimatcherFuncs = MinimatchHelper.GetMinimatchFuncs(minimatchPatterns, tracer);

            if (minimatcherFuncs != null && minimatcherFuncs.Count() != 0)
            {
                items = this.GetFilteredItems(items, minimatcherFuncs);
            }

            if (!isSingleArtifactDownload && items.Any())
            {
                Directory.CreateDirectory(rootPath);
            }

            var folderItems = items.Where(i => i.ItemType == ContainerItemType.Folder);
            Parallel.ForEach(folderItems, (folder) =>
            {
                var targetPath = ResolveTargetPath(rootPath, folder, containerIdAndRoot.Item2);
                Directory.CreateDirectory(targetPath);
            });

            var fileItems = items.Where(i => i.ItemType == ContainerItemType.File);

            var downloadBlock = NonSwallowingActionBlock.Create<FileContainerItem>(
                async item =>
                {
                    var targetPath = ResolveTargetPath(rootPath, item, containerIdAndRoot.Item2);
                    var directory = Path.GetDirectoryName(targetPath);
                    Directory.CreateDirectory(directory);
                    await AsyncHttpRetryHelper.InvokeVoidAsync(
                       async () =>
                       {
                           using (var sourceStream = await this.DownloadFileAsync(containerIdAndRoot, projectId, containerClient, item, cancellationToken))
                           {
                               tracer.Info($"Downloading: {targetPath}");
                               using (var targetStream = new FileStream(targetPath, FileMode.Create))
                               {
                                   await sourceStream.CopyToAsync(targetStream);
                               }
                           }
                       },
                        maxRetries: 5,
                        cancellationToken: cancellationToken,
                        tracer: tracer,
                        continueOnCapturedContext: false,
                        canRetryDelegate: exception => exception is IOException,
                        context: null
                        );
                },
                new ExecutionDataflowBlockOptions()
                {
                    BoundedCapacity = 5000,
                    MaxDegreeOfParallelism = 8,
                    CancellationToken = cancellationToken,
                });

            await downloadBlock.SendAllAndCompleteSingleBlockNetworkAsync(fileItems, cancellationToken);
        }

        private async Task<Stream> DownloadFileAsync(
            (long, string) containerIdAndRoot,
            Guid scopeIdentifier,
            FileContainerHttpClient containerClient,
            FileContainerItem item,
            CancellationToken cancellationToken)
        {
            Stream responseStream = await AsyncHttpRetryHelper.InvokeAsync(
                async () =>
                {
                    Stream internalResponseStream = await containerClient.DownloadFileAsync(containerIdAndRoot.Item1, item.Path, cancellationToken, scopeIdentifier);
                    return internalResponseStream;
                },
                maxRetries: 5,
                cancellationToken: cancellationToken,
                tracer: this.tracer,
                continueOnCapturedContext: false
                );

            return responseStream;
        }

        private string ResolveTargetPath(string rootPath, FileContainerItem item, string artifactName)
        {
            //Example of item.Path&artifactName: item.Path = "drop3", "drop3/HelloWorld.exe"; artifactName = "drop3"
            string tempArtifactName;
            if (item.Path.Length == artifactName.Length)
            {
                tempArtifactName = artifactName;
            }
            else if (item.Path.Length > artifactName.Length)
            {
                tempArtifactName = artifactName + "/";
            }
            else
            {
                throw new ArgumentException($"Item path {item.Path} cannot be smaller than artifact {artifactName}");
            }

            var itemPathWithoutDirectoryPrefix = item.Path.Replace(tempArtifactName, String.Empty);
            var absolutePath = Path.Combine(rootPath, itemPathWithoutDirectoryPrefix);
            return absolutePath;
        }

        private List<FileContainerItem> GetFilteredItems(List<FileContainerItem> items, IEnumerable<Func<string, bool>> minimatchFuncs)
        {
            List<FileContainerItem> filteredItems = new List<FileContainerItem>();
            foreach (FileContainerItem item in items)
            {
                if (minimatchFuncs.Any(match => match(item.Path)))
                {
                    filteredItems.Add(item);
                }
            }
            var excludedItems = items.Except(filteredItems);
            foreach (FileContainerItem item in excludedItems)
            {
                tracer.Info($"Item excluded: {item.Path}");
            }
            return filteredItems;
        }
    }
}
