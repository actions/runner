using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
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
    public sealed class TfsVCSourceProvider : SourceProvider, ISourceProvider
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
            var tf = HostContext.CreateService<ITfsVCCommandManager>();
            tf.CancellationToken = cancellationToken;
            tf.Endpoint = endpoint;
            tf.ExecutionContext = executionContext;

            // Check if the administrator accepted the license terms of the TEE EULA when configuring the agent.
            AgentSettings settings = HostContext.GetService<IConfigurationStore>().GetSettings();
            if (tf.Features.HasFlag(TfsVCFeatures.Eula) && settings.AcceptTeeEula)
            {
                // Check if the "tf eula -accept" command needs to be run for the current user.
                bool skipEula = false;
                try
                {
                    skipEula = tf.TestEulaAccepted();
                }
                catch (Exception ex)
                {
                    Trace.Error("Unexpected exception while testing whether the TEE EULA has been accepted for the current security user.");
                    Trace.Error(ex);
                }

                if (!skipEula)
                {
                    // Run the command "tf eula -accept".
                    try
                    {
                        await tf.EulaAsync();
                    }
                    catch (Exception ex)
                    {
                        Trace.Error(ex);
                        executionContext.Warning(ex.Message);
                    }
                }
            }

            // Get the workspaces.
            ITfsVCWorkspace[] tfWorkspaces = await tf.WorkspacesAsync();

            // Determine the workspace name.
            string buildDirectory = executionContext.Variables.Agent_BuildDirectory;
            ArgUtil.NotNullOrEmpty(buildDirectory, nameof(buildDirectory));
            string workspaceName = $"ws_{Path.GetFileName(buildDirectory)}_{settings.AgentId}";
            executionContext.Variables.Set(Constants.Variables.Build.RepoTfvcWorkspace, workspaceName);

            // Get the definition mappings.
            DefinitionWorkspaceMapping[] definitionMappings =
                JsonConvert.DeserializeObject<DefinitionWorkspaceMappings>(endpoint.Data[WellKnownEndpointData.TfvcWorkspaceMapping])?.Mappings;

            // Determine the sources directory.
            string sourcesDirectory = executionContext.Variables.Build_SourcesDirectory;
            ArgUtil.NotNullOrEmpty(sourcesDirectory, nameof(sourcesDirectory));

            // Attempt to re-use an existing workspace if the command manager supports scorch
            // or if clean is not specified.
            ITfsVCWorkspace existingTFWorkspace = null;
            bool clean = endpoint.Data.ContainsKey(WellKnownEndpointData.Clean) &&
                StringUtil.ConvertToBoolean(endpoint.Data[WellKnownEndpointData.Clean], defaultValue: false);
            if (tf.Features.HasFlag(TfsVCFeatures.Scorch) || !clean)
            {
                existingTFWorkspace = MatchExactWorkspace(
                    tfWorkspaces: tfWorkspaces,
                    name: workspaceName,
                    definitionMappings: definitionMappings,
                    sourcesDirectory: sourcesDirectory);
                if (existingTFWorkspace != null)
                {
                    if (tf.Features.HasFlag(TfsVCFeatures.GetFromUnmappedRoot))
                    {
                        // Undo pending changes.
                        ITfsVCStatus tfStatus = await tf.StatusAsync(localPath: sourcesDirectory);
                        if (tfStatus?.HasPendingChanges ?? false)
                        {
                            await tf.UndoAsync(localPath: sourcesDirectory);

                            // Cleanup remaining files/directories from pend adds.
                            tfStatus.AllAdds
                                .OrderByDescending(x => x.LocalItem) // Sort descending so nested items are deleted before their parent is deleted.
                                .ToList()
                                .ForEach(x =>
                                {
                                    executionContext.Output(StringUtil.Loc("Deleting", x.LocalItem));
                                    IOUtil.Delete(x.LocalItem, cancellationToken);
                                });
                        }
                    }
                    else
                    {
                        // Perform "undo" for each map.
                        foreach (DefinitionWorkspaceMapping definitionMapping in definitionMappings ?? new DefinitionWorkspaceMapping[0])
                        {
                            if (definitionMapping.MappingType == DefinitionMappingType.Map)
                            {
                                // Check the status.
                                string localPath = ResolveMappingLocalPath(definitionMapping, sourcesDirectory);
                                ITfsVCStatus tfStatus = await tf.StatusAsync(localPath: localPath);
                                if (tfStatus?.HasPendingChanges ?? false)
                                {
                                    // Undo.
                                    await tf.UndoAsync(localPath: localPath);

                                    // Cleanup remaining files/directories from pend adds.
                                    tfStatus.AllAdds
                                        .OrderByDescending(x => x.LocalItem) // Sort descending so nested items are deleted before their parent is deleted.
                                        .ToList()
                                        .ForEach(x =>
                                        {
                                            executionContext.Output(StringUtil.Loc("Deleting", x.LocalItem));
                                            IOUtil.Delete(x.LocalItem, cancellationToken);
                                        });
                                }
                            }
                        }
                    }

                    // Scorch.
                    if (clean)
                    {
                        await tf.ScorchAsync();
                    }
                }
            }

            // Create a new workspace.
            if (existingTFWorkspace == null)
            {
                // Remove any conflicting workspaces.
                await RemoveConflictingWorkspacesAsync(
                    tf: tf,
                    tfWorkspaces: tfWorkspaces,
                    name: workspaceName,
                    directory: sourcesDirectory);

                // Recreate the sources directory.
                executionContext.Debug($"Deleting: '{sourcesDirectory}'.");
                IOUtil.DeleteDirectory(sourcesDirectory, cancellationToken);
                Directory.CreateDirectory(sourcesDirectory);

                // Create the workspace.
                await tf.WorkspaceNewAsync();

                // Remove the default mapping.
                if (tf.Features.HasFlag(TfsVCFeatures.DefaultWorkfoldMap))
                {
                    await tf.WorkfoldUnmapAsync("$/");
                }

                // Sort the definition mappings.
                definitionMappings =
                    (definitionMappings ?? new DefinitionWorkspaceMapping[0])
                    .OrderBy(x => x.NormalizedServerPath?.Length ?? 0) // By server path length.
                    .ToArray() ?? new DefinitionWorkspaceMapping[0];

                // Add the definition mappings to the workspace.
                foreach (DefinitionWorkspaceMapping definitionMapping in definitionMappings)
                {
                    switch (definitionMapping.MappingType)
                    {
                        case DefinitionMappingType.Cloak:
                            // Add the cloak.
                            await tf.WorkfoldCloakAsync(serverPath: definitionMapping.ServerPath);
                            break;
                        case DefinitionMappingType.Map:
                            // Add the mapping.
                            await tf.WorkfoldMapAsync(
                                serverPath: definitionMapping.ServerPath,
                                localPath: ResolveMappingLocalPath(definitionMapping, sourcesDirectory));
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            if (tf.Features.HasFlag(TfsVCFeatures.GetFromUnmappedRoot))
            {
                // Get.
                await tf.GetAsync(localPath: sourcesDirectory);
            }
            else
            {
                // Perform "get" for each map.
                foreach (DefinitionWorkspaceMapping definitionMapping in definitionMappings ?? new DefinitionWorkspaceMapping[0])
                {
                    if (definitionMapping.MappingType == DefinitionMappingType.Map)
                    {
                        await tf.GetAsync(localPath: ResolveMappingLocalPath(definitionMapping, sourcesDirectory));
                    }
                }
            }

            string shelvesetName = executionContext.Variables.Build_SourceTfvcShelveset;
            if (!string.IsNullOrEmpty(shelvesetName))
            {
                // Get the shelveset details.
                ITfsVCShelveset tfShelveset = null;
                string gatedShelvesetName = executionContext.Variables.Build_GatedShelvesetName;
                if (!string.IsNullOrEmpty(gatedShelvesetName))
                {
                    tfShelveset = await tf.ShelvesetsAsync(shelveset: shelvesetName);
                    // The command throws if the shelveset is not found.
                    // This assertion should never fail.
                    ArgUtil.NotNull(tfShelveset, nameof(tfShelveset));
                }

                // Unshelve.
                await tf.UnshelveAsync(shelveset: shelvesetName);

                if (!string.IsNullOrEmpty(gatedShelvesetName))
                {
                    // Create the comment file for reshelve.
                    StringBuilder comment = new StringBuilder(tfShelveset.Comment ?? string.Empty);
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
                        await tf.ShelveAsync(shelveset: gatedShelvesetName, commentFile: commentFile);
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
            ArgUtil.NotNull(endpoint, nameof(endpoint));
            path = path ?? string.Empty;
            if (path.StartsWith("$/") || path.StartsWith(@"$\"))
            {
                // Create the tf command manager.
                var tf = HostContext.CreateService<ITfsVCCommandManager>();
                tf.CancellationToken = CancellationToken.None;
                tf.Endpoint = endpoint;
                tf.ExecutionContext = executionContext;

                // Attempt to resolve the path.
                string localPath = tf.ResolvePath(serverPath: path);
                if (!string.IsNullOrEmpty(localPath))
                {
                    return localPath;
                }
            }

            // Return the original path.
            return path;
        }

        private static string ResolveMappingLocalPath(DefinitionWorkspaceMapping definitionMapping, string sourcesDirectory)
        {
            string relativePath =
                (definitionMapping.LocalPath ?? string.Empty)
                .Trim('/', '\\');
            if (Path.DirectorySeparatorChar == '\\')
            {
                relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
            }
            else
            {
                relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
            }

            return Path.Combine(sourcesDirectory, relativePath);
        }

        private ITfsVCWorkspace MatchExactWorkspace(ITfsVCWorkspace[] tfWorkspaces, string name, DefinitionWorkspaceMapping[] definitionMappings, string sourcesDirectory)
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
            foreach (ITfsVCWorkspace tfWorkspace in tfWorkspaces ?? new ITfsVCWorkspace[0])
            {
                // Compare the works name, machine name, and number of mappings.
                Trace.Info($"Candidate workspace name '{tfWorkspace.Name}', machine name '{tfWorkspace.Computer}', number of mappings '{tfWorkspace.Mappings?.Length ?? 0}'.");
                if (!string.Equals(tfWorkspace.Name, name, StringComparison.Ordinal) ||
                    !string.Equals(tfWorkspace.Computer, machineName, StringComparison.Ordinal) ||
                    (tfWorkspace.Mappings?.Length ?? 0) != (definitionMappings?.Length ?? 0))
                {
                    continue;
                }

                // TODO: Is there such a thing as a single level cloak?
                // Sort the TF mappings.
                List<ITfsVCMapping> sortedTFMappings =
                    (tfWorkspace.Mappings ?? new ITfsVCMapping[0])
                    .OrderBy(x => !x.Cloak) // Cloaks first
                    .ThenBy(x => !x.Recursive) // Then recursive maps
                    .ThenBy(x => x.ServerPath) // Then sort by server path
                    .ToList();
                sortedTFMappings.ForEach(x => Trace.Info($"TF mapping: cloak '{x.Cloak}', recursive '{x.Recursive}', server path '{x.ServerPath}', local path '{x.LocalPath}'."));

                // Sort the definition mappings.
                List<DefinitionWorkspaceMapping> sortedDefinitionMappings =
                    (definitionMappings ?? new DefinitionWorkspaceMapping[0])
                    .OrderBy(x => x.MappingType != DefinitionMappingType.Cloak) // Cloaks first
                    .ThenBy(x => !x.Recursive) // Then recursive maps
                    .ThenBy(x => x.NormalizedServerPath) // Then sort by the normalized server path
                    .ToList();
                sortedDefinitionMappings.ForEach(x => Trace.Info($"Definition mapping: cloak '{x.MappingType == DefinitionMappingType.Cloak}', recursive '{x.Recursive}', server path '{x.NormalizedServerPath}', local path '{ResolveMappingLocalPath(x, sourcesDirectory)}'."));

                // Compare the mappings,
                bool allMatch = true;
                for (int i = 0 ; i < sortedTFMappings.Count ; i++)
                {
                    ITfsVCMapping tfMapping = sortedTFMappings[i];
                    DefinitionWorkspaceMapping definitionMapping = sortedDefinitionMappings[i];
                    if (tfMapping.Cloak)
                    {
                        // The TF mapping is a cloak.

                        // Verify the definition mapping is a cloak and the server paths match.
                        if (definitionMapping.MappingType != DefinitionMappingType.Cloak ||
                            !string.Equals(tfMapping.ServerPath, definitionMapping.ServerPath, StringComparison.Ordinal))
                        {
                            allMatch = false; // Mapping comparison failed.
                            break;
                        }
                    }
                    else
                    {
                        // The TF mapping is a map.

                        // Verify the definition mapping is a map and the local paths match.
                        if (definitionMapping.MappingType != DefinitionMappingType.Map ||
                            !string.Equals(tfMapping.LocalPath, ResolveMappingLocalPath(definitionMapping, sourcesDirectory), StringComparison.Ordinal))
                        {
                            allMatch = false; // Mapping comparison failed.
                            break;
                        }

                        if (tfMapping.Recursive)
                        {
                            // The TF mapping is a recursive map.

                            // Verify the server paths match.
                            if (!string.Equals(tfMapping.ServerPath, definitionMapping.ServerPath, StringComparison.Ordinal))
                            {
                                allMatch = false; // Mapping comparison failed.
                                break;
                            }
                        }
                        else
                        {
                            // The TF mapping is a single-level map.

                            // Verify the definition mapping is a single-level map and the normalized server paths match.
                            if (definitionMapping.Recursive ||
                                !string.Equals(tfMapping.ServerPath, definitionMapping.NormalizedServerPath, StringComparison.Ordinal))
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
                    return tfWorkspace;
                }
            }

            Trace.Info("Matching workspace not found.");
            return null;
        }

        private async Task RemoveConflictingWorkspacesAsync(ITfsVCCommandManager tf, ITfsVCWorkspace[] tfWorkspaces, string name, string directory)
        {
            // Validate the args.
            ArgUtil.NotNullOrEmpty(name, nameof(name));
            ArgUtil.NotNullOrEmpty(directory, nameof(directory));

            // Fixup the directory.
            directory = directory.TrimEnd('/', '\\');
            ArgUtil.NotNullOrEmpty(directory, nameof(directory));
            string directorySlash = $"{directory}{Path.DirectorySeparatorChar}";

            foreach (ITfsVCWorkspace tfWorkspace in tfWorkspaces ?? new ITfsVCWorkspace[0])
            {
                // Attempt to match the workspace by name.
                if (string.Equals(tfWorkspace.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    // Try deleting the workspace from the server.
                    if (!(await tf.TryWorkspaceDeleteAsync(tfWorkspace)))
                    {
                        // Otherwise fallback to deleting the workspace from the local computer.
                        await tf.WorkspacesRemoveAsync(tfWorkspace);
                    }

                    // Continue iterating over the rest of the workspaces.
                    continue;
                }

                // Attempt to match the workspace by local path.
                foreach (ITfsVCMapping tfMapping in tfWorkspace.Mappings ?? new ITfsVCMapping[0])
                {
                    // Skip cloaks.
                    if (tfMapping.Cloak)
                    {
                        continue;
                    }

                    if (string.Equals(tfMapping.LocalPath, directory, StringComparison.Ordinal) ||
                        (tfMapping.LocalPath ?? string.Empty).StartsWith(directorySlash, StringComparison.Ordinal))
                    {
                        // Try deleting the workspace from the server.
                        if (!(await tf.TryWorkspaceDeleteAsync(tfWorkspace)))
                        {
                            // Otherwise fallback to deleting the workspace from the local computer.
                            await tf.WorkspacesRemoveAsync(tfWorkspace);
                        }

                        // Break out of this nested for loop only.
                        // Continue iterating over the rest of the workspaces.
                        break;
                    }
                }
            }
        }

        public sealed class DefinitionWorkspaceMappings
        {
            public DefinitionWorkspaceMapping[] Mappings { get; set; }
        }

        public sealed class DefinitionWorkspaceMapping
        {
            public string LocalPath { get; set; }

            public DefinitionMappingType MappingType { get; set; }

            // Remove the trailing "/*" from the single-level mapping server path.
            public string NormalizedServerPath => Recursive ? ServerPath : ServerPath.Substring(0, ServerPath.Length - 2);

            // Returns true if the path does not end with "/*".
            public bool Recursive => !(ServerPath ?? string.Empty).EndsWith("/*");

            public string ServerPath { get; set; }
        }

        public enum DefinitionMappingType
        {
            Cloak,
            Map,
        }
    }
}