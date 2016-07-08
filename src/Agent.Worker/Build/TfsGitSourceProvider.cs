using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class TfsGitSourceProvider : GitSourceProvider, ISourceProvider
    {
        private readonly Dictionary<string, string> _authHeaderCache = new Dictionary<string, string>();

        public override string RepositoryType => WellKnownRepositoryTypes.TfsGit;

        public override async Task GetSourceAsync(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            CancellationToken cancellationToken)
        {
            Trace.Entering();
            // Validate args.
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(endpoint, nameof(endpoint));

            executionContext.Output($"Syncing repository: {endpoint.Name} (TfsGit)");
            _gitCommandManager = HostContext.GetService<IGitCommandManager>();
            await _gitCommandManager.LoadGitExecutionInfo(executionContext);

            string targetPath;
            string sourceBranch;
            string sourceVersion;
            endpoint.Data.TryGetValue(Constants.Variables.Build.SourcesDirectory, out targetPath);
            endpoint.Data.TryGetValue(Constants.Variables.Build.SourceBranch, out sourceBranch);
            endpoint.Data.TryGetValue(Constants.Variables.Build.SourceVersion, out sourceVersion);

            bool clean = false;
            if (endpoint.Data.ContainsKey(WellKnownEndpointData.Clean))
            {
                clean = StringUtil.ConvertToBoolean(endpoint.Data[WellKnownEndpointData.Clean]);
            }

            bool checkoutSubmodules = false;
            if (endpoint.Data.ContainsKey(WellKnownEndpointData.CheckoutSubmodules))
            {
                checkoutSubmodules = StringUtil.ConvertToBoolean(endpoint.Data[WellKnownEndpointData.CheckoutSubmodules]);
            }

            bool exposeCred = executionContext.Variables.GetBoolean(Constants.Variables.System.EnableAccessToken) ?? false;

            Trace.Info($"Repository url={endpoint.Url}");
            Trace.Info($"targetPath={targetPath}");
            Trace.Info($"sourceBranch={sourceBranch}");
            Trace.Info($"sourceVersion={sourceVersion}");
            Trace.Info($"clean={clean}");
            Trace.Info($"checkoutSubmodules={checkoutSubmodules}");
            Trace.Info($"exposeCred={exposeCred}");

            // retrieve credential from endpoint.
            Uri repositoryUrl = endpoint.Url;
            if (!repositoryUrl.IsAbsoluteUri)
            {
                throw new InvalidOperationException("Repository url need to be an absolute uri.");
            }

            string authToken = string.Empty;
            if (endpoint.Authorization != null)
            {
                switch (endpoint.Authorization.Scheme)
                {
                    case EndpointAuthorizationSchemes.OAuth:
                        if (!endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out authToken))
                        {
                            authToken = string.Empty;
                        }
                        break;
                    default:
                        executionContext.Warning($"Unsupport endpoint authorization schemes for TfsGit: {endpoint.Authorization.Scheme}");
                        break;
                }
            }

            // Check the current contents of the root folder to see if there is already a repo
            // If there is a repo, see if it matches the one we are expecting to be there based on the remote fetch url
            // if the repo is not what we expect, remove the folder
            if (!await IsRepositoryOriginUrlMatch(executionContext, targetPath, repositoryUrl))
            {
                // Delete source folder
                IOUtil.DeleteDirectory(targetPath, cancellationToken);
            }
            else
            {
                // delete the index.lock file left by previous canceled build or any operation casue git.exe crash last time.
                string lockFile = Path.Combine(targetPath, ".git\\index.lock");
                if (File.Exists(lockFile))
                {
                    try
                    {
                        File.Delete(lockFile);
                    }
                    catch (Exception ex)
                    {
                        executionContext.Debug($"Unable to delete the index.lock file: {lockFile}");
                        executionContext.Debug(ex.ToString());
                    }
                }

                // When repo.clean is selected for a git repo, execute git clean -fdx and git reset --hard HEAD on the current repo.
                // This will help us save the time to reclone the entire repo.
                // If any git commands exit with non-zero return code or any exception happened during git.exe invoke, fall back to delete the repo folder.
                if (clean)
                {
                    Boolean softClean = false;
                    // git clean -fdx
                    // git reset --hard HEAD
                    int exitCode_clean = await _gitCommandManager.GitClean(executionContext, targetPath);
                    if (exitCode_clean != 0)
                    {
                        executionContext.Debug($"'git clean -fdx' failed with exit code {exitCode_clean}, this normally caused by:\n    1) Path too long\n    2) Permission issue\n    3) File in use\nFor futher investigation, manually run 'git clean -fdx' on repo root: {targetPath} after each build.");
                    }
                    else
                    {
                        int exitCode_reset = await _gitCommandManager.GitReset(executionContext, targetPath);
                        if (exitCode_reset != 0)
                        {
                            executionContext.Debug($"'git reset --hard HEAD' failed with exit code {exitCode_reset}\nFor futher investigation, manually run 'git reset --hard HEAD' on repo root: {targetPath} after each build.");
                        }
                        else
                        {
                            softClean = true;
                        }
                    }

                    if (!softClean)
                    {
                        //fall back
                        executionContext.Warning("Unable to run \"git clean -fdx\" and \"git reset --hard HEAD\" successfully, delete source folder instead.");
                        IOUtil.DeleteDirectory(targetPath, cancellationToken);
                    }
                }
            }

            // if the folder is missing, create it
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // if the folder contains a .git folder, it means the folder contains a git repo that matches the remote url and in a clean state.
            // we will run git fetch to update the repo.
            if (!Directory.Exists(Path.Combine(targetPath, ".git")))
            {
                // init git repository
                int exitCode_init = await _gitCommandManager.GitInit(executionContext, targetPath);
                if (exitCode_init != 0)
                {
                    throw new InvalidOperationException($"Unable to use git.exe init repository under {targetPath}, 'git init' failed with exit code: {exitCode_init}");
                }

                int exitCode_addremote = await _gitCommandManager.GitRemoteAdd(executionContext, targetPath, "origin", repositoryUrl.AbsoluteUri);
                if (exitCode_addremote != 0)
                {
                    throw new InvalidOperationException($"Unable to use git.exe add remote 'origin', 'git remote add' failed with exit code: {exitCode_addremote}");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            executionContext.Progress(0, "Starting fetch...");

            // disable git auto gc
            int exitCode_disableGC = await _gitCommandManager.GitDisableAutoGC(executionContext, targetPath);
            if (exitCode_disableGC != 0)
            {
                executionContext.Warning("Unable turn off git auto garbage collection, git fetch operation may trigger auto garbage collection which will affect the performence of fetching.");
            }


            // If this is a build for a pull request, then include
            // the pull request reference as an additional ref.
            string fetchSpec = IsPullRequest(sourceBranch) ? StringUtil.Format("+{0}:{1}", sourceBranch, GetRemoteRefName(sourceBranch)) : null;
            string fetchAdditionalCommandline = $"-c http.extraheader=\"AUTHORIZATION: bearer {authToken}\"";
            int exitCode_fetch = await _gitCommandManager.GitFetch(executionContext, targetPath, "origin", new List<string>() { fetchSpec }, fetchAdditionalCommandline, cancellationToken);
            if (exitCode_fetch != 0)
            {
                throw new InvalidOperationException($"Git fetch failed with exit code: {exitCode_fetch}");
            }

            if (exposeCred)
            {
                string configKey = $"http.{repositoryUrl.AbsoluteUri}.extraheader";
                string configValue = $"\"AUTHORIZATION: bearer {authToken}\"";
                _authHeaderCache[configKey] = configValue.Trim('\"');
                int exitCode_config = await _gitCommandManager.GitConfig(executionContext, targetPath, configKey, configValue);
                if (exitCode_config != 0)
                {
                    throw new InvalidOperationException($"Git config failed with exit code: {exitCode_config}");
                }
            }

            // Checkout
            // sourceToBuild is used for checkout
            // if sourceBranch is a PR branch or sourceVersion is null, make sure branch name is a remote branch. we need checkout to detached head. 
            // (change refs/heads to refs/remotes/origin, refs/pull to refs/remotes/pull, or leava it as it when the branch name doesn't contain refs/...)
            // if sourceVersion provide, just use that for checkout, since when you checkout a commit, it will end up in detached head.
            cancellationToken.ThrowIfCancellationRequested();
            executionContext.Progress(80, "Starting checkout...");
            string sourcesToBuild;
            if (IsPullRequest(sourceBranch) || string.IsNullOrEmpty(sourceVersion))
            {
                sourcesToBuild = GetRemoteRefName(sourceBranch);
            }
            else
            {
                sourcesToBuild = sourceVersion;
            }

            // Finally, checkout the sourcesToBuild (if we didn't find a valid git object this will throw)
            int exitCode_checkout = await _gitCommandManager.GitCheckout(executionContext, targetPath, sourcesToBuild, cancellationToken);
            if (exitCode_checkout != 0)
            {
                throw new InvalidOperationException($"Git checkout failed with exit code: {exitCode_checkout}");
            }

            // Submodule update
            if (checkoutSubmodules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                executionContext.Progress(90, "Updating submodules...");
                int exitCode_submoduleInit = await _gitCommandManager.GitSubmoduleInit(executionContext, targetPath);
                if (exitCode_submoduleInit != 0)
                {
                    throw new InvalidOperationException($"Git submodule init failed with exit code: {exitCode_submoduleInit}");
                }

                string submoduleUpdateAdditionalCommandline = null;
                string tfsAccountUrl = repositoryUrl.AbsoluteUri.Replace(repositoryUrl.PathAndQuery, string.Empty);
                submoduleUpdateAdditionalCommandline = $"-c http.{tfsAccountUrl}.extraheader=\"AUTHORIZATION: bearer {authToken}\"";

                int exitCode_submoduleUpdate = await _gitCommandManager.GitSubmoduleUpdate(executionContext, targetPath, submoduleUpdateAdditionalCommandline, cancellationToken);
                if (exitCode_submoduleUpdate != 0)
                {
                    throw new InvalidOperationException($"Git submodule update failed with exit code: {exitCode_submoduleUpdate}");
                }
            }
        }

        public override async Task PostJobCleanupAsync(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            Trace.Entering();
            ArgUtil.NotNull(endpoint, nameof(endpoint));
            executionContext.Output($"Cleaning extra http auth header from repository: {endpoint.Name} (TfsGit)");

            string targetPath;
            endpoint.Data.TryGetValue(Constants.Variables.Build.SourcesDirectory, out targetPath);

            executionContext.Debug($"Repository url={endpoint.Url}");
            executionContext.Debug($"targetPath={targetPath}");

            executionContext.Debug("Remove any extraheader setting from git config.");
            foreach (var header in _authHeaderCache)
            {
                int exitCode_configUnset = await _gitCommandManager.GitConfigUnset(executionContext, targetPath, header.Key);
                if (exitCode_configUnset != 0)
                {
                    // if unable to use git.exe unset http.extraheader, modify git config file on disk. make sure we don't left credential.
                    executionContext.Warning("Unable to use git.exe unset http.extraheader, modify git config file on disk to remove leaking credential.");
                    string gitConfig = Path.Combine(targetPath, ".git/config");
                    if (File.Exists(gitConfig))
                    {
                        string gitConfigContent = File.ReadAllText(Path.Combine(targetPath, ".git", "config"));
                        if (gitConfigContent.Contains(header.Key))
                        {
                            string setting = $"extraheader = {header.Key}";
                            gitConfigContent = Regex.Replace(gitConfigContent, setting, string.Empty, RegexOptions.IgnoreCase);
                            File.WriteAllText(gitConfig, gitConfigContent);
                        }
                    }
                }
            }
        }
    }
}