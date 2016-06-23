using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts.Definition;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release.Artifacts
{
    // TODO: Add test for this
    public class FileShareArtifact
    {
        // This is only used by build artifact. This isn't a officially supported artifact type in RM
        public async Task DownloadArtifactAsync(IExecutionContext executionContext, IHostContext hostContext, ArtifactDefinition artifactDefinition, string dropLocation, string localFolderPath)
        {
            ArgUtil.NotNull(artifactDefinition, nameof(artifactDefinition));
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNullOrEmpty(localFolderPath, nameof(localFolderPath));
            ArgUtil.NotNullOrEmpty(dropLocation, nameof(dropLocation));

            var trimChars = new[] { '\\', '/' };
            var relativePath = artifactDefinition.Details.RelativePath;

            // If user has specified a relative folder in the drop, change the drop location itself. 
            dropLocation = Path.Combine(dropLocation.TrimEnd(trimChars), relativePath.Trim(trimChars));

            var fileSystemManager = hostContext.CreateService<IReleaseFileSystemManager>();
            List<string> filePaths =
                new DirectoryInfo(dropLocation).EnumerateFiles("*", SearchOption.AllDirectories)
                    .Select(path => path.FullName)
                    .ToList();

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
    }
}