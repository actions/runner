using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class TfsVCTeeSourceProvider : SourceProvider, ISourceProvider
    {
        public override string RepositoryType => WellKnownRepositoryTypes.TfsVersionControl;

        public async Task GetSourceAsync(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            CancellationToken cancellationToken)
        {
            // Validate args.
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(endpoint, nameof(endpoint));

            // Create the tf command manager.
            var tf = HostContext.CreateService<ITeeTFCommandManager>();
            tf.CancellationToken = cancellationToken;
            tf.Endpoint = endpoint;
            tf.ExecutionContext = executionContext;

            // Get the TEE workspaces.
            TeeWorkspace[] teeWorkspaces = await tf.WorkspacesAsync();

            // Determine the workspace name.
            AgentSettings settings = HostContext.GetService<IConfigurationStore>().GetSettings();
            string buildDirectory = executionContext.Variables.Agent_BuildDirectory;
            ArgUtil.NotNullOrEmpty(buildDirectory, nameof(buildDirectory));
            string workspaceName = $"ws_{Path.GetFileName(buildDirectory)}_{settings.AgentId}";
            executionContext.Variables.Set(Constants.Variables.Build.RepoTfvcWorkspace, workspaceName);

            // Get the definition mappings.
            TfsVCTeeWorkspaceMapping[] definitionMappings =
                JsonConvert.DeserializeObject<TfsVCTeeWorkspaceMappings>(endpoint.Data["tfvcWorkspaceMapping"])?.Mappings;

            // Determine the sources directory.
            string sourcesDirectory = executionContext.Variables.Build_SourcesDirectory;
            ArgUtil.NotNullOrEmpty(sourcesDirectory, nameof(sourcesDirectory));

            // Attempt to re-use an existing workspace if clean=false.
            TeeWorkspace existingTeeWorkspace = null;
            bool clean = endpoint.Data.ContainsKey(WellKnownEndpointData.Clean) &&
                StringUtil.ConvertToBoolean(endpoint.Data[WellKnownEndpointData.Clean], defaultValue: false);
            if (!clean)
            {
                existingTeeWorkspace = MatchExactWorkspace(
                    teeWorkspaces: teeWorkspaces,
                    name: workspaceName,
                    definitionMappings: definitionMappings,
                    sourcesDirectory: sourcesDirectory);

                // Undo any pending changes.
                // TODO: Manually delete pending adds since they do not get deleted on undo?
                if (existingTeeWorkspace != null)
                {
                    TeeStatus teeStatus = await tf.StatusAsync(workspaceName);
                    if (teeStatus?.PendingChanges?.Any() ?? false)
                    {
                        await tf.UndoAsync(sourcesDirectory);
                    }
                }
            }

            // Create a new workspace.
            if (existingTeeWorkspace == null)
            {
                // Remove any conflicting TEE workspaces.
                await RemoveConflictingWorkspacesAsync(
                    tf: tf,
                    teeWorkspaces: teeWorkspaces,
                    name: workspaceName,
                    directory: sourcesDirectory);

                // Recreate the sources directory.
                executionContext.Debug($"Deleting: '{sourcesDirectory}'.");
                IOUtil.DeleteDirectory(sourcesDirectory, cancellationToken);
                Directory.CreateDirectory(sourcesDirectory);

                // Create the TEE workspace.
                await tf.WorkspaceNewAsync(workspaceName);

                // Sort the definition mappings.
                definitionMappings =
                    (definitionMappings ?? new TfsVCTeeWorkspaceMapping[0])
                    .OrderBy(x => NormalizeServerPath(x.ServerPath)?.Length ?? 0) // By server path length.
                    .ToArray() ?? new TfsVCTeeWorkspaceMapping[0];

                // Add the definition mappings to the TEE workspace.
                foreach (TfsVCTeeWorkspaceMapping definitionMapping in definitionMappings)
                {
                    switch (definitionMapping.MappingType)
                    {
                        case TfsVCTeeMappingType.Cloak:
                            // Add the cloak.
                            await tf.WorkfoldCloakAsync(
                                workspace: workspaceName,
                                serverPath: definitionMapping.ServerPath);
                            break;
                        case TfsVCTeeMappingType.Map:
                            // Add the mapping.
                            await tf.WorkfoldMapAsync(
                                workspace: workspaceName,
                                serverPath: definitionMapping.ServerPath,
                                localPath: ResolveMappingLocalPath(definitionMapping, sourcesDirectory));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            // Get.
            await tf.GetAsync(
                version: executionContext.Variables.Build_SourceVersion,
                directory: sourcesDirectory);

            string shelvesetName = executionContext.Variables.Build_SourceTfvcShelveset;
            if (!string.IsNullOrEmpty(shelvesetName))
            {
                // Get the shelveset details.
                TeeShelveset teeShelveset = null;
                string gatedShelvesetName = executionContext.Variables.Build_GatedShelvesetName;
                if (!string.IsNullOrEmpty(gatedShelvesetName))
                {
                    teeShelveset = await tf.ShelvesetsAsync(workspace: workspaceName, shelveset: shelvesetName);
                    // The command throws if the shelveset is not found.
                    // This assertion should never fail.
                    ArgUtil.NotNull(teeShelveset, nameof(teeShelveset));
                }

                // Unshelve.
                // TODO: Confirm Get then Unshelve is OK.
                await tf.UnshelveAsync(workspace: workspaceName, shelveset: shelvesetName);

                if (!string.IsNullOrEmpty(gatedShelvesetName))
                {
                    // Create the comment file for reshelve.
                    StringBuilder comment = new StringBuilder(teeShelveset.Comment ?? string.Empty);
                    if (!(executionContext.Variables.Build_GatedRunCI ?? true))
                    {
                        if (comment.Length > 0)
                        {
                            comment.AppendLine();
                        }

                        comment.Append(Constants.Build.NoCICheckInComment);
                    }

                    string commentFile = null;
                    try
                    {
                        commentFile = Path.GetTempFileName();
                        // TODO: FIGURE OUT WHAT ENCODING TF EXPECTS
                        File.WriteAllText(path: commentFile, contents: comment.ToString());

                        // Reshelve.
                        // TODO: Work with TEE folks regarding support for associate work items, policy override comment, etc.
                        await tf.ShelveAsync(
                            directory: sourcesDirectory,
                            shelveset: gatedShelvesetName,
                            commentFile: commentFile);
                    }
                    finally
                    {
                        // Cleanup the comment file.
                        if (File.Exists(commentFile))
                        {
                            File.Delete(commentFile);
                        }
                    }
                }
            }
        }

        public Task PostJobCleanupAsync(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            return Task.CompletedTask;
        }

        public override string GetLocalPath(IExecutionContext executionContext, ServiceEndpoint endpoint, string path)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(executionContext.Variables, nameof(executionContext.Variables));
            ArgUtil.NotNullOrEmpty(executionContext.Variables.Build_RepoTfvcWorkspace, nameof(executionContext.Variables.Build_RepoTfvcWorkspace));
            ArgUtil.NotNull(endpoint, nameof(endpoint));
            path = path ?? string.Empty;
            if (path.StartsWith("$/") || path.StartsWith(@"$\"))
            {
                // Create the tf command manager.
                var tf = HostContext.CreateService<ITeeTFCommandManager>();
                tf.CancellationToken = CancellationToken.None;
                tf.Endpoint = endpoint;
                tf.ExecutionContext = executionContext;

                // Attempt to resolve the path.
                string localPath = tf.ResolvePath(
                    workspace: executionContext.Variables.Build_RepoTfvcWorkspace,
                    serverPath: path);
                if (!string.IsNullOrEmpty(localPath))
                {
                    return localPath;
                }
            }

            // Return the original path.
            return path;
        }

        private static string NormalizeServerPath(string definitionServerPath)
        {
            return RecursiveServerPath(definitionServerPath)
                ? definitionServerPath
                : definitionServerPath.Substring(0, definitionServerPath.Length - 2);
        }

        public static bool RecursiveServerPath(string definitionServerPath)
        {
            return !(definitionServerPath ?? string.Empty).EndsWith("/*");
        }

        private static string ResolveMappingLocalPath(TfsVCTeeWorkspaceMapping definitionMapping, string sourcesDirectory)
        {
            return Path.Combine(
                sourcesDirectory,
                (definitionMapping.LocalPath ?? string.Empty).Trim('/', '\\').Replace('\\', Path.DirectorySeparatorChar));
        }

        private TeeWorkspace MatchExactWorkspace(TeeWorkspace[] teeWorkspaces, string name, TfsVCTeeWorkspaceMapping[] definitionMappings, string sourcesDirectory)
        {
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            ArgUtil.NotNullOrEmpty(sourcesDirectory, nameof(sourcesDirectory));

            // Short-circuit early if the sources directory is empty.
            //
            // Consider the sources directory to be empty if it only contains a .tf directory exists. This can
            // indicate the workspace is in a corrupted state and the tf commands (e.g. status) will not return
            // reliable information. An easy way to reproduce this is to delete the workspace directory, then
            // run "tf status" on that workspace. The .tf directory will be recreated but the contents will be
            // in a corrupted state.
            if (!Directory.Exists(sourcesDirectory) ||
                !Directory.EnumerateFileSystemEntries(sourcesDirectory).Any(x => !x.EndsWith($"{Path.DirectorySeparatorChar}.tf")))
            {
                Trace.Info($"Sources directory does not exist or is empty.");
                return null;
            }

            string machineName = Environment.MachineName;
            Trace.Info("Attempting to find a matching workspace.");
            Trace.Info($"Expected workspace name '{name}', machine name '{machineName}', number of mappings '{definitionMappings?.Length ?? 0}'.");
            foreach (TeeWorkspace teeWorkspace in teeWorkspaces ?? new TeeWorkspace[0])
            {
                // Compare the works name, machine name, and number of mappings.
                Trace.Info($"Candidate workspace name '{teeWorkspace.Name}', machine name '{teeWorkspace.Computer}', number of mappings '{teeWorkspace.Mappings?.Length ?? 0}'.");
                if (!string.Equals(teeWorkspace.Name, name, StringComparison.Ordinal) ||
                    !string.Equals(teeWorkspace.Computer, machineName, StringComparison.Ordinal) ||
                    (teeWorkspace.Mappings?.Length ?? 0) != (definitionMappings?.Length ?? 0))
                {
                    continue;
                }

                // TODO: Is there such a thing as a single level cloak?
                // Sort the TEE mappings.
                List<TeeMapping> sortedTeeMappings =
                    (teeWorkspace.Mappings ?? new TeeMapping[0])
                    .OrderBy(x => !x.Cloak) // Cloaks first
                    .ThenBy(x => !x.Recursive) // Then recursive maps
                    .ThenBy(x => x.ServerPath) // Then sort by server path
                    .ToList();
                sortedTeeMappings.ForEach(x => Trace.Info($"TEE mapping: cloak '{x.Cloak}', recursive '{x.Recursive}', server path '{x.ServerPath}', local path '{x.LocalPath}'."));

                // Sort the definition mappings.
                List<TfsVCTeeWorkspaceMapping> sortedDefinitionMappings =
                    (definitionMappings ?? new TfsVCTeeWorkspaceMapping[0])
                    .OrderBy(x => x.MappingType != TfsVCTeeMappingType.Cloak) // Cloaks first
                    .ThenBy(x => !RecursiveServerPath(x.ServerPath)) // Then recursive maps
                    .ThenBy(x => NormalizeServerPath(x.ServerPath)) // Then sort by the normalized server path
                    .ToList();
                sortedDefinitionMappings.ForEach(x => Trace.Info($"Definition mapping: cloak '{x.MappingType == TfsVCTeeMappingType.Cloak}', recursive '{RecursiveServerPath(x.ServerPath)}', server path '{NormalizeServerPath(x.ServerPath)}', local path '{ResolveMappingLocalPath(x, sourcesDirectory)}'."));

                // Compare the mappings,
                bool allMatch = true;
                for (int i = 0 ; i < sortedTeeMappings.Count ; i++)
                {
                    TeeMapping teeMapping = sortedTeeMappings[i];
                    TfsVCTeeWorkspaceMapping definitionMapping = sortedDefinitionMappings[i];
                    if (teeMapping.Cloak)
                    {
                        // The TEE mapping is a cloak.

                        // Verify the definition mapping is a cloak and the server paths match.
                        if (definitionMapping.MappingType != TfsVCTeeMappingType.Cloak ||
                            !string.Equals(teeMapping.ServerPath, definitionMapping.ServerPath, StringComparison.Ordinal))
                        {
                            allMatch = false; // Mapping comparison failed.
                            break;
                        }
                    }
                    else
                    {
                        // The TEE mapping is a map.

                        // Verify the definition mapping is a map and the local paths match.
                        if (definitionMapping.MappingType != TfsVCTeeMappingType.Map ||
                            !string.Equals(teeMapping.LocalPath, ResolveMappingLocalPath(definitionMapping, sourcesDirectory), StringComparison.Ordinal))
                        {
                            allMatch = false; // Mapping comparison failed.
                            break;
                        }

                        if (teeMapping.Recursive)
                        {
                            // The TEE mapping is a recursive map.

                            // Verify the server paths match.
                            if (!string.Equals(teeMapping.ServerPath, definitionMapping.ServerPath, StringComparison.Ordinal))
                            {
                                allMatch = false; // Mapping comparison failed.
                                break;
                            }
                        }
                        else
                        {
                            // The TEE mapping is a single-level map.

                            // Verify the definition mapping is a single-level map and the normalized server paths match.
                            if (RecursiveServerPath(definitionMapping.ServerPath) ||
                                !string.Equals(teeMapping.ServerPath, NormalizeServerPath(definitionMapping.ServerPath), StringComparison.Ordinal))
                            {
                                allMatch = false; // Mapping comparison failed.
                                break;
                            }
                        }
                    }
                }

                if (allMatch)
                {
                    Trace.Info("Matching workspace found.");
                    return teeWorkspace;
                }
            }

            Trace.Info("Matching workspace not found.");
            return null;
        }

        private async Task RemoveConflictingWorkspacesAsync(ITeeTFCommandManager tf, TeeWorkspace[] teeWorkspaces, string name, string directory)
        {
            // Validate the args.
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            ArgUtil.NotNullOrEmpty(directory, nameof(directory));

            // Fixup the directory.
            directory = directory.TrimEnd('/', '\\');
            ArgUtil.NotNullOrEmpty(directory, nameof(directory));
            string directorySlash = $"{directory}{Path.DirectorySeparatorChar}";

            foreach (TeeWorkspace teeWorkspace in teeWorkspaces ?? new TeeWorkspace[0])
            {
                // Attempt to match the workspace by name.
                if (string.Equals(teeWorkspace.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    // Try deleting the workspace from the server.
                    if (!(await tf.TryWorkspaceDeleteAsync(teeWorkspace)))
                    {
                        // Otherwise fallback to deleting the workspace from the local computer.
                        await tf.WorkspacesRemoveAsync(teeWorkspace);
                    }

                    // Continue iterating over the rest of the workspaces.
                    continue;
                }

                // Attempt to match the workspace by local path.
                foreach (TeeMapping teeMapping in teeWorkspace.Mappings ?? new TeeMapping[0])
                {
                    // Skip cloaks.
                    if (teeMapping.Cloak)
                    {
                        continue;
                    }

                    // Note, this comparison assumes running on Linux/OSX.
                    if (string.Equals(teeMapping.LocalPath, directory, StringComparison.Ordinal) ||
                        (teeMapping.LocalPath ?? string.Empty).StartsWith(directorySlash, StringComparison.Ordinal))
                    {
                        // Try deleting the workspace from the server.
                        if (!(await tf.TryWorkspaceDeleteAsync(teeWorkspace)))
                        {
                            // Otherwise fallback to deleting the workspace from the local computer.
                            await tf.WorkspacesRemoveAsync(teeWorkspace);
                        }

                        // Break out of this nested for loop only.
                        // Continue iterating over the rest of the workspaces.
                        break;
                    }
                }
            }
        }
    }

    // TODO: Switch to use contracts in web api DLL instead.
    public sealed class TfsVCTeeWorkspaceMappings
    {
        public TfsVCTeeWorkspaceMapping[] Mappings { get; set; }
    }

    public sealed class TfsVCTeeWorkspaceMapping
    {
        public string LocalPath { get; set; }
        public TfsVCTeeMappingType MappingType { get; set; }
        public string ServerPath { get; set; }
    }

    public enum TfsVCTeeMappingType
    {
        Cloak,
        Map,
    }
}