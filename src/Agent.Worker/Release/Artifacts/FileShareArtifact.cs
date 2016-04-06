using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Agent.Worker.Release.Artifacts.Definition;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    // TODO: Implement serviceLocator pattern to have custom attribute as we have different type of artifacts
    [ServiceLocator(Default = typeof(FileShareArtifact))]
    public interface IFileShareArtifact : IAgentService
    {
        Task Download(
            ArtifactDefinition artifactDefinition,
            IExecutionContext executionContext,
            string localFolderPath);
    }

    public class FileShareArtifact : AgentService, IFileShareArtifact
    {
        public async Task Download(ArtifactDefinition artifactDefinition, IExecutionContext executionContext, string localFolderPath)
        {
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNullOrEmpty(localFolderPath, nameof(localFolderPath));

            var dropLocation = GetFileShareDropLocation(artifactDefinition.Details.RelativePath, artifactDefinition.Version);
            await DownloadArtifact(artifactDefinition, executionContext, dropLocation, localFolderPath);
        }

        public async Task DownloadArtifact(ArtifactDefinition artifactDefinition, IExecutionContext executionContext, string dropLocation, string localFolderPath)
        {
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNullOrEmpty(localFolderPath, nameof(localFolderPath));
            ArgUtil.NotNullOrEmpty(dropLocation, nameof(dropLocation));

            var trimChars = new[] { '\\', '/' };
            var relativePath = artifactDefinition.Details.RelativePath;

            // If user has specified a relative folder in the drop, change the drop location itself. 
            dropLocation = Path.Combine(dropLocation.TrimEnd(trimChars), relativePath.Trim(trimChars));

            var fileSystemManager = HostContext.CreateService<IReleaseFileSystemManager>();
            List<string> filePaths = fileSystemManager.GetFiles(dropLocation, SearchOption.AllDirectories).Select(path => path.FullName).ToList();

            if (filePaths.Any())
            {
                foreach (var filePath in filePaths)
                {
                    var filePathRelativeToDrop = filePath.Replace(dropLocation, string.Empty).Trim(trimChars);
                    using (var fileReader = fileSystemManager.GetFileReader(filePath))
                    {
                        await fileSystemManager.WriteStreamToFile(fileReader.BaseStream, Path.Combine(localFolderPath, filePathRelativeToDrop));
                    }
                }
            }
            else
            {
                executionContext.Warning(StringUtil.Loc("RMNoArtifactsFound", relativePath));
            }
        }

        private static string GetFileShareDropLocation(string buildExternallyPackageLocation, string buildNumber)
        {
            if (string.IsNullOrWhiteSpace(buildExternallyPackageLocation))
            {
                return null;
            }

            var externallyPackageLocation = StringUtil.Format(
                "{0}\\{1}",
                buildExternallyPackageLocation.TrimEnd(new[] { '\\' }),
                buildNumber).TrimEnd(new[] { '\\' });

            return externallyPackageLocation;
        }
    }
}