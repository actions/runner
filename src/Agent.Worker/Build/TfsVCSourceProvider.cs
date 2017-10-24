using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
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
        private bool _undoShelvesetPendingChanges = false;

        public override string RepositoryType => WellKnownRepositoryTypes.TfsVersionControl;

        public async Task GetSourceAsync(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            CancellationToken cancellationToken)
        {
            Trace.Entering();
            // Validate args.
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(endpoint, nameof(endpoint));

#if OS_WINDOWS
            // Validate .NET Framework 4.6 or higher is installed.
            var netFrameworkUtil = HostContext.GetService<INetFrameworkUtil>();
            if (!netFrameworkUtil.Test(new Version(4, 6)))
            {
                throw new Exception(StringUtil.Loc("MinimumNetFramework46"));
            }
#endif

            // Create the tf command manager.
            var tf = HostContext.CreateService<ITfsVCCommandManager>();
            tf.CancellationToken = cancellationToken;
            tf.Endpoint = endpoint;
            tf.ExecutionContext = executionContext;

            // Setup proxy.
            var agentProxy = HostContext.GetService<IVstsAgentWebProxy>();
            if (!string.IsNullOrEmpty(executionContext.Variables.Agent_ProxyUrl) && !agentProxy.IsBypassed(endpoint.Url))
            {
                executionContext.Debug($"Configure '{tf.FilePath}' to work through proxy server '{executionContext.Variables.Agent_ProxyUrl}'.");
                tf.SetupProxy(executionContext.Variables.Agent_ProxyUrl, executionContext.Variables.Agent_ProxyUsername, executionContext.Variables.Agent_ProxyPassword);
            }

            // Setup client certificate.
            var agentCertManager = HostContext.GetService<IAgentCertificateManager>();
            var configUrl = new Uri(HostContext.GetService<IConfigurationStore>().GetSettings().ServerUrl);
            if (!string.IsNullOrEmpty(agentCertManager.ClientCertificateFile) &&
                Uri.Compare(endpoint.Url, configUrl, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
            {
                executionContext.Debug($"Configure '{tf.FilePath}' to work with client cert '{agentCertManager.ClientCertificateFile}'.");
                tf.SetupClientCertificate(agentCertManager.ClientCertificateFile, agentCertManager.ClientCertificatePrivateKeyFile, agentCertManager.ClientCertificateArchiveFile, agentCertManager.ClientCertificatePassword);
            }

            // Add TF to the PATH.
            string tfPath = tf.FilePath;
            ArgUtil.File(tfPath, nameof(tfPath));
            var varUtil = HostContext.GetService<IVarUtil>();
            executionContext.Output(StringUtil.Loc("Prepending0WithDirectoryContaining1", Constants.PathVariable, Path.GetFileName(tfPath)));
            varUtil.PrependPath(Path.GetDirectoryName(tfPath));
            executionContext.Debug($"{Constants.PathVariable}: '{Environment.GetEnvironmentVariable(Constants.PathVariable)}'");

#if OS_WINDOWS
            // Set TFVC_BUILDAGENT_POLICYPATH
            string policyDllPath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.ServerOM), "Microsoft.TeamFoundation.VersionControl.Controls.dll");
            ArgUtil.File(policyDllPath, nameof(policyDllPath));
            const string policyPathEnvKey = "TFVC_BUILDAGENT_POLICYPATH";
            executionContext.Output(StringUtil.Loc("SetEnvVar", policyPathEnvKey));
            Environment.SetEnvironmentVariable(policyPathEnvKey, policyDllPath);
#endif

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
                    executionContext.Debug("Unexpected exception while testing whether the TEE EULA has been accepted for the current user.");
                    executionContext.Debug(ex.ToString());
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
                        executionContext.Debug(ex.ToString());
                        executionContext.Warning(ex.Message);
                    }
                }
            }

            // Get the workspaces.
            executionContext.Output(StringUtil.Loc("QueryingWorkspaceInfo"));
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
            string sourcesDirectory = GetEndpointData(endpoint, Constants.EndpointData.SourcesDirectory);
            ArgUtil.NotNullOrEmpty(sourcesDirectory, nameof(sourcesDirectory));

            // Attempt to re-use an existing workspace if the command manager supports scorch
            // or if clean is not specified.
            ITfsVCWorkspace existingTFWorkspace = null;
            bool clean = endpoint.Data.ContainsKey(WellKnownEndpointData.Clean) &&
                StringUtil.ConvertToBoolean(endpoint.Data[WellKnownEndpointData.Clean], defaultValue: false);
            if (tf.Features.HasFlag(TfsVCFeatures.Scorch) || !clean)
            {
                existingTFWorkspace = WorkspaceUtil.MatchExactWorkspace(
                    executionContext: executionContext,
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
                                string localPath = definitionMapping.GetRootedLocalPath(sourcesDirectory);
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
                        // Try to scorch.
                        try
                        {
                            await tf.ScorchAsync();
                        }
                        catch (ProcessExitCodeException ex)
                        {
                            // Scorch failed.
                            // Warn, drop the folder, and re-clone.
                            executionContext.Warning(ex.Message);
                            existingTFWorkspace = null;
                        }
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

                // Remove any conflicting workspace from a different computer.
                // This is primarily a hosted scenario where a registered hosted
                // agent can land on a different computer each time.
                tfWorkspaces = await tf.WorkspacesAsync(matchWorkspaceNameOnAnyComputer: true);
                foreach (ITfsVCWorkspace tfWorkspace in tfWorkspaces ?? new ITfsVCWorkspace[0])
                {
                    await tf.WorkspaceDeleteAsync(tfWorkspace);
                }

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
                                localPath: definitionMapping.GetRootedLocalPath(sourcesDirectory));
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
                        await tf.GetAsync(localPath: definitionMapping.GetRootedLocalPath(sourcesDirectory));
                    }
                }
            }

            // Steps for shelveset/gated.
            string shelvesetName = GetEndpointData(endpoint, Constants.EndpointData.SourceTfvcShelveset);
            if (!string.IsNullOrEmpty(shelvesetName))
            {
                // Steps for gated.
                ITfsVCShelveset tfShelveset = null;
                string gatedShelvesetName = GetEndpointData(endpoint, Constants.EndpointData.GatedShelvesetName);
                if (!string.IsNullOrEmpty(gatedShelvesetName))
                {
                    // Clean the last-saved-checkin-metadata for existing workspaces.
                    //
                    // A better long term fix is to add a switch to "tf unshelve" that completely overwrites
                    // the last-saved-checkin-metadata, instead of merging associated work items.
                    //
                    // The targeted workaround for now is to create a trivial change and "tf shelve /move",
                    // which will delete the last-saved-checkin-metadata.
                    if (existingTFWorkspace != null)
                    {
                        executionContext.Output("Cleaning last saved checkin metadata.");

                        // Find a local mapped directory.
                        string firstLocalDirectory =
                            (definitionMappings ?? new DefinitionWorkspaceMapping[0])
                            .Where(x => x.MappingType == DefinitionMappingType.Map)
                            .Select(x => x.GetRootedLocalPath(sourcesDirectory))
                            .FirstOrDefault(x => Directory.Exists(x));
                        if (firstLocalDirectory == null)
                        {
                            executionContext.Warning("No mapped folder found. Unable to clean last-saved-checkin-metadata.");
                        }
                        else
                        {
                            // Create a trival change and "tf shelve /move" to clear the
                            // last-saved-checkin-metadata.
                            string cleanName = "__tf_clean_wksp_metadata";
                            string tempCleanFile = Path.Combine(firstLocalDirectory, cleanName);
                            try
                            {
                                File.WriteAllText(path: tempCleanFile, contents: "clean last-saved-checkin-metadata", encoding: Encoding.UTF8);
                                await tf.AddAsync(tempCleanFile);
                                await tf.ShelveAsync(shelveset: cleanName, commentFile: tempCleanFile, move: true);
                            }
                            catch (Exception ex)
                            {
                                executionContext.Warning($"Unable to clean last-saved-checkin-metadata. {ex.Message}");
                                try
                                {
                                    await tf.UndoAsync(tempCleanFile);
                                }
                                catch (Exception ex2)
                                {
                                    executionContext.Warning($"Unable to undo '{tempCleanFile}'. {ex2.Message}");
                                }

                            }
                            finally
                            {
                                IOUtil.DeleteFile(tempCleanFile);
                            }
                        }
                    }

                    // Get the shelveset metadata.
                    tfShelveset = await tf.ShelvesetsAsync(shelveset: shelvesetName);
                    // The above command throws if the shelveset is not found,
                    // so the following assertion should never fail.
                    ArgUtil.NotNull(tfShelveset, nameof(tfShelveset));
                }

                // Unshelve.
                await tf.UnshelveAsync(shelveset: shelvesetName);

                // Ensure we undo pending changes for shelveset build at the end.
                _undoShelvesetPendingChanges = true;

                if (!string.IsNullOrEmpty(gatedShelvesetName))
                {
                    // Create the comment file for reshelve.
                    StringBuilder comment = new StringBuilder(tfShelveset.Comment ?? string.Empty);
                    string runCi = GetEndpointData(endpoint, Constants.EndpointData.GatedRunCI);
                    bool gatedRunCi = StringUtil.ConvertToBoolean(runCi, true);
                    if (!gatedRunCi)
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
                        File.WriteAllText(path: commentFile, contents: comment.ToString(), encoding: Encoding.UTF8);

                        // Reshelve.
                        await tf.ShelveAsync(shelveset: gatedShelvesetName, commentFile: commentFile, move: false);
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

            // Cleanup proxy settings.
            if (!string.IsNullOrEmpty(executionContext.Variables.Agent_ProxyUrl) && !agentProxy.IsBypassed(endpoint.Url))
            {
                executionContext.Debug($"Remove proxy setting for '{tf.FilePath}' to work through proxy server '{executionContext.Variables.Agent_ProxyUrl}'.");
                tf.CleanupProxySetting();
            }
        }

        public async Task PostJobCleanupAsync(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            if (_undoShelvesetPendingChanges)
            {
                string shelvesetName = GetEndpointData(endpoint, Constants.EndpointData.SourceTfvcShelveset);
                executionContext.Debug($"Undo pending changes left by shelveset '{shelvesetName}'.");

                // Create the tf command manager.
                var tf = HostContext.CreateService<ITfsVCCommandManager>();
                tf.CancellationToken = executionContext.CancellationToken;
                tf.Endpoint = endpoint;
                tf.ExecutionContext = executionContext;

                // Get the definition mappings.
                DefinitionWorkspaceMapping[] definitionMappings =
                    JsonConvert.DeserializeObject<DefinitionWorkspaceMappings>(endpoint.Data[WellKnownEndpointData.TfvcWorkspaceMapping])?.Mappings;

                // Determine the sources directory.
                string sourcesDirectory = GetEndpointData(endpoint, Constants.EndpointData.SourcesDirectory);
                ArgUtil.NotNullOrEmpty(sourcesDirectory, nameof(sourcesDirectory));

                try
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
                                    IOUtil.Delete(x.LocalItem, executionContext.CancellationToken);
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
                                string localPath = definitionMapping.GetRootedLocalPath(sourcesDirectory);
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
                                            IOUtil.Delete(x.LocalItem, executionContext.CancellationToken);
                                        });
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // We can't undo pending changes, log a warning and continue.
                    executionContext.Debug(ex.ToString());
                    executionContext.Warning(ex.Message);
                }
            }
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

        public override void SetVariablesInEndpoint(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            base.SetVariablesInEndpoint(executionContext, endpoint);
            endpoint.Data.Add(Constants.EndpointData.SourceTfvcShelveset, executionContext.Variables.Get(Constants.Variables.Build.SourceTfvcShelveset));
            endpoint.Data.Add(Constants.EndpointData.GatedShelvesetName, executionContext.Variables.Get(Constants.Variables.Build.GatedShelvesetName));
            endpoint.Data.Add(Constants.EndpointData.GatedRunCI, executionContext.Variables.Get(Constants.Variables.Build.GatedRunCI));
        }

        public sealed override bool TestOverrideBuildDirectory()
        {
            var configurationStore = HostContext.GetService<IConfigurationStore>();
            AgentSettings settings = configurationStore.GetSettings();
            return settings.IsHosted;
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

                    if (string.Equals(tfMapping.LocalPath, directory, StringComparison.CurrentCultureIgnoreCase) ||
                        (tfMapping.LocalPath ?? string.Empty).StartsWith(directorySlash, StringComparison.CurrentCultureIgnoreCase))
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

        public static class WorkspaceUtil
        {
            public static ITfsVCWorkspace MatchExactWorkspace(
                IExecutionContext executionContext,
                ITfsVCWorkspace[] tfWorkspaces,
                string name,
                DefinitionWorkspaceMapping[] definitionMappings,
                string sourcesDirectory)
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
                    executionContext.Debug("Sources directory does not exist or is empty.");
                    return null;
                }

                string machineName = Environment.MachineName;
                executionContext.Debug($"Attempting to find a workspace: '{name}'");
                foreach (ITfsVCWorkspace tfWorkspace in tfWorkspaces ?? new ITfsVCWorkspace[0])
                {
                    // Compare the workspace name.
                    if (!string.Equals(tfWorkspace.Name, name, StringComparison.Ordinal))
                    {
                        executionContext.Debug($"Skipping workspace: '{tfWorkspace.Name}'");
                        continue;
                    }

                    executionContext.Debug($"Candidate workspace: '{tfWorkspace.Name}'");

                    // Compare the machine name.
                    if (!string.Equals(tfWorkspace.Computer, machineName, StringComparison.Ordinal))
                    {
                        executionContext.Debug($"Expected computer name: '{machineName}'. Actual: '{tfWorkspace.Computer}'");
                        continue;
                    }

                    // Compare the number of mappings.
                    if ((tfWorkspace.Mappings?.Length ?? 0) != (definitionMappings?.Length ?? 0))
                    {
                        executionContext.Debug($"Expected number of mappings: '{definitionMappings?.Length ?? 0}'. Actual: '{tfWorkspace.Mappings?.Length ?? 0}'");
                        continue;
                    }

                    // Sort the definition mappings.
                    List<DefinitionWorkspaceMapping> sortedDefinitionMappings =
                        (definitionMappings ?? new DefinitionWorkspaceMapping[0])
                        .OrderBy(x => x.MappingType != DefinitionMappingType.Cloak) // Cloaks first
                        .ThenBy(x => !x.Recursive) // Then recursive maps
                        .ThenBy(x => x.NormalizedServerPath) // Then sort by the normalized server path
                        .ToList();
                    for (int i = 0; i < sortedDefinitionMappings.Count; i++)
                    {
                        DefinitionWorkspaceMapping mapping = sortedDefinitionMappings[i];
                        executionContext.Debug($"Definition mapping[{i}]: cloak '{mapping.MappingType == DefinitionMappingType.Cloak}', recursive '{mapping.Recursive}', server path '{mapping.NormalizedServerPath}', local path '{mapping.GetRootedLocalPath(sourcesDirectory)}'");
                    }

                    // Sort the TF mappings.
                    List<ITfsVCMapping> sortedTFMappings =
                        (tfWorkspace.Mappings ?? new ITfsVCMapping[0])
                        .OrderBy(x => !x.Cloak) // Cloaks first
                        .ThenBy(x => !x.Recursive) // Then recursive maps
                        .ThenBy(x => x.ServerPath) // Then sort by server path
                        .ToList();
                    for (int i = 0; i < sortedTFMappings.Count; i++)
                    {
                        ITfsVCMapping mapping = sortedTFMappings[i];
                        executionContext.Debug($"Found mapping[{i}]: cloak '{mapping.Cloak}', recursive '{mapping.Recursive}', server path '{mapping.ServerPath}', local path '{mapping.LocalPath}'");
                    }

                    // Compare the mappings.
                    bool allMatch = true;
                    for (int i = 0; i < sortedTFMappings.Count; i++)
                    {
                        ITfsVCMapping tfMapping = sortedTFMappings[i];
                        DefinitionWorkspaceMapping definitionMapping = sortedDefinitionMappings[i];

                        // Compare the cloak flag.
                        bool expectedCloak = definitionMapping.MappingType == DefinitionMappingType.Cloak;
                        if (tfMapping.Cloak != expectedCloak)
                        {
                            executionContext.Debug($"Expected mapping[{i}] cloak: '{expectedCloak}'. Actual: '{tfMapping.Cloak}'");
                            allMatch = false;
                            break;
                        }

                        // Compare the recursive flag.
                        if (!expectedCloak && tfMapping.Recursive != definitionMapping.Recursive)
                        {
                            executionContext.Debug($"Expected mapping[{i}] recursive: '{definitionMapping.Recursive}'. Actual: '{tfMapping.Recursive}'");
                            allMatch = false;
                            break;
                        }

                        // Compare the server path. Normalize the expected server path for a single-level map.
                        string expectedServerPath = definitionMapping.NormalizedServerPath;
                        if (!string.Equals(tfMapping.ServerPath, expectedServerPath, StringComparison.Ordinal))
                        {
                            executionContext.Debug($"Expected mapping[{i}] server path: '{expectedServerPath}'. Actual: '{tfMapping.ServerPath}'");
                            allMatch = false;
                            break;
                        }

                        // Compare the local path.
                        if (!expectedCloak)
                        {
                            string expectedLocalPath = definitionMapping.GetRootedLocalPath(sourcesDirectory);
                            if (!string.Equals(tfMapping.LocalPath, expectedLocalPath, StringComparison.Ordinal))
                            {
                                executionContext.Debug($"Expected mapping[{i}] local path: '{expectedLocalPath}'. Actual: '{tfMapping.LocalPath}'");
                                allMatch = false;
                                break;
                            }
                        }
                    }

                    if (allMatch)
                    {
                        executionContext.Debug("Matching workspace found.");
                        return tfWorkspace;
                    }
                }

                executionContext.Debug("Matching workspace not found.");
                return null;
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

            /// <summary>
            /// Remove the trailing "/*" from the single-level mapping server path.
            /// If the ServerPath is "$/*", then the normalized path is returned
            /// as "$/" rather than "$".
            /// </summary>
            public string NormalizedServerPath
            {
                get
                {
                    string path;
                    if (!Recursive)
                    {
                        // Trim the last two characters (i.e. "/*") from the single-level
                        // mapping server path.
                        path = ServerPath.Substring(0, ServerPath.Length - 2);

                        // Check if trimmed too much. This is important when comparing
                        // against workspaces on disk.
                        if (string.Equals(path, "$", StringComparison.Ordinal))
                        {
                            path = "$/";
                        }
                    }
                    else
                    {
                        path = ServerPath ?? string.Empty;
                    }

                    return path;
                }
            }

            /// <summary>
            /// Returns true if the path does not end with "/*".
            /// </summary>
            public bool Recursive => !(ServerPath ?? string.Empty).EndsWith("/*");

            public string ServerPath { get; set; }

            /// <summary>
            /// Gets the rooted local path and normalizes slashes.
            /// </summary>
            public string GetRootedLocalPath(string sourcesDirectory)
            {
                // TEE normalizes all slashes in a workspace mapping to match the OS. It is not
                // possible on OSX/Linux to have a workspace mapping with a backslash, even though
                // backslash is a legal file name character.
                string relativePath =
                    (LocalPath ?? string.Empty)
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Trim(Path.DirectorySeparatorChar);
                return Path.Combine(sourcesDirectory, relativePath);
            }
        }

        public enum DefinitionMappingType
        {
            Cloak,
            Map,
        }
    }
}