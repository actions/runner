using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

            bool disableRobocopy = executionContext.Variables.GetBoolean(Constants.Variables.Release.DisableRobocopy) ?? false;
            if (disableRobocopy == false)
            {
                await DownloadArtifactUsingRobocopyAsync(executionContext, hostContext, artifactDefinition, dropLocation, localFolderPath);
            }
            else
            {
                await DownloadArtifactUsingFileSystemManagerAsync(executionContext, hostContext, artifactDefinition, dropLocation, localFolderPath);
            }
        }

        private async Task DownloadArtifactUsingFileSystemManagerAsync(IExecutionContext executionContext, IHostContext hostContext, ArtifactDefinition artifactDefinition, string dropLocation, string localFolderPath)
        {
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
                int bufferSize = executionContext.Variables.Release_Download_BufferSize ?? DefaultBufferSize;

                foreach (var filePath in filePaths)
                {
                    string fullPath = Path.GetFullPath(filePath);
                    if (File.Exists(fullPath))
                    {
                        string filePathRelativeToDrop = filePath.Replace(dropLocation, string.Empty).Trim(trimChars);
                        using (StreamReader fileReader = fileSystemManager.GetFileReader(filePath))
                        {
                            await
                                fileSystemManager.WriteStreamToFile(
                                    fileReader.BaseStream,
                                    Path.Combine(localFolderPath, filePathRelativeToDrop),
                                    bufferSize,
                                    executionContext.CancellationToken);
                        }
                    }
                    else
                    {
                        executionContext.Warning(StringUtil.Loc("FileNotFound", fullPath));
                    }
                }
            }
            else
            {
                executionContext.Warning(StringUtil.Loc("RMArtifactEmpty"));
            }
        }

        private async Task DownloadArtifactUsingRobocopyAsync(IExecutionContext executionContext, IHostContext hostContext, ArtifactDefinition artifactDefinition, string dropLocation, string downloadFolderPath)
        {
            int? robocopyMT = executionContext.Variables.GetInt(Constants.Variables.Release.RobocopyMT);
            bool verbose = executionContext.Variables.GetBoolean(Constants.Variables.System.Debug) ?? false;

            if (robocopyMT != null)
            {
                if (robocopyMT < 1)
                {
                    robocopyMT = 1;
                }
                else if (robocopyMT > 128)
                {
                    robocopyMT = 128;
                }
            }

            executionContext.Output(StringUtil.Loc("RMDownloadingArtifactUsingRobocopy"));
            using (var processInvoker = hostContext.CreateService<IProcessInvoker>())
            {
                // Save STDOUT from worker, worker will use STDOUT report unhandle exception.
                processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stdout)
                {
                    if (!string.IsNullOrEmpty(stdout.Data))
                    {
                        executionContext.Output(stdout.Data);
                    }
                };

                // Save STDERR from worker, worker will use STDERR on crash.
                processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs stderr)
                {
                    if (!string.IsNullOrEmpty(stderr.Data))
                    {
                        executionContext.Error(stderr.Data);
                    }
                };

                var trimChars = new[] { '\\', '/' };
                var relativePath = artifactDefinition.Details.RelativePath;

                dropLocation = Path.Combine(dropLocation.TrimEnd(trimChars), relativePath.Trim(trimChars));

                string robocopyArguments = "\"" + dropLocation + "\" \"" + downloadFolderPath + "\" /E /Z /NP /R:3";
                if (verbose != true)
                {
                    robocopyArguments = robocopyArguments + " /NDL /NFL";
                }

                if (robocopyMT != null)
                {
                    robocopyArguments = robocopyArguments + " /MT:" + robocopyMT;
                }

                int exitCode = await processInvoker.ExecuteAsync(
                        workingDirectory: "",
                        fileName: "robocopy",
                        arguments: robocopyArguments,
                        environment: null,
                        requireExitCodeZero: false,
                        outputEncoding: null,
                        killProcessOnCancel: true,
                        cancellationToken: executionContext.CancellationToken);

                executionContext.Output(StringUtil.Loc("RMRobocopyBasedArtifactDownloadExitCode", exitCode));

                if (exitCode >= 8)
                {
                    throw new ArtifactDownloadException(StringUtil.Loc("RMRobocopyBasedArtifactDownloadFailed", exitCode));
                }
            }
        }

        private const int DefaultBufferSize = 8192;
    }
}