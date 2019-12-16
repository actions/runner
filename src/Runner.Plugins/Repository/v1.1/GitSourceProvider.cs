using Pipelines = GitHub.DistributedTask.Pipelines;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;
using GitHub.Runner.Sdk;
using System.Linq;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Plugins.Repository.v1_1
{
    public sealed class GitHubSourceProvider
    {
        // refs prefix
        private const string _refsPrefix = "refs/heads/";
        private const string _remoteRefsPrefix = "refs/remotes/origin/";
        private const string _pullRefsPrefix = "refs/pull/";
        private const string _remotePullRefsPrefix = "refs/remotes/pull/";
        private const string _tagRefsPrefix = "refs/tags/";

        // min git version that support add extra auth header.
        private Version _minGitVersionSupportAuthHeader = new Version(2, 9);

#if OS_WINDOWS
        // min git version that support override sslBackend setting.
        private Version _minGitVersionSupportSSLBackendOverride = new Version(2, 14, 2);
#endif

        // min git-lfs version that support add extra auth header.
        private Version _minGitLfsVersionSupportAuthHeader = new Version(2, 1);

        public static string ProblemMatcher => @"    
{
    ""problemMatcher"": [
        {
            ""owner"": ""checkout-git"",
            ""pattern"": [
                {
                    ""regexp"": ""^(fatal|error): (.*)$"",
                    ""message"": 2
                }
            ]
        }
    ]
}";

        public async Task GetSourceAsync(
            RunnerActionPluginExecutionContext executionContext,
            string repositoryPath,
            string repoFullName,
            string sourceBranch,
            string sourceVersion,
            bool clean,
            string submoduleInput,
            int fetchDepth,
            bool gitLfsSupport,
            string accessToken,
            CancellationToken cancellationToken)
        {
            // Validate args.
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            Dictionary<string, string> configModifications = new Dictionary<string, string>();
            executionContext.Output($"Syncing repository: {repoFullName}");
            Uri repositoryUrl = new Uri($"https://github.com/{repoFullName}");
            if (!repositoryUrl.IsAbsoluteUri)
            {
                throw new InvalidOperationException("Repository url need to be an absolute uri.");
            }

            string targetPath = repositoryPath;

            // input Submodules can be ['', true, false, recursive]
            // '' or false indicate don't checkout submodules
            // true indicate checkout top level submodules
            // recursive indicate checkout submodules recursively 
            bool checkoutSubmodules = false;
            bool checkoutNestedSubmodules = false;
            if (!string.IsNullOrEmpty(submoduleInput))
            {
                if (string.Equals(submoduleInput, Pipelines.PipelineConstants.CheckoutTaskInputs.SubmodulesOptions.Recursive, StringComparison.OrdinalIgnoreCase))
                {
                    checkoutSubmodules = true;
                    checkoutNestedSubmodules = true;
                }
                else
                {
                    checkoutSubmodules = StringUtil.ConvertToBoolean(submoduleInput);
                }
            }

            executionContext.Debug($"repository url={repositoryUrl}");
            executionContext.Debug($"targetPath={targetPath}");
            executionContext.Debug($"sourceBranch={sourceBranch}");
            executionContext.Debug($"sourceVersion={sourceVersion}");
            executionContext.Debug($"clean={clean}");
            executionContext.Debug($"checkoutSubmodules={checkoutSubmodules}");
            executionContext.Debug($"checkoutNestedSubmodules={checkoutNestedSubmodules}");
            executionContext.Debug($"fetchDepth={fetchDepth}");
            executionContext.Debug($"gitLfsSupport={gitLfsSupport}");

            // Initialize git command manager with additional environment variables.
            Dictionary<string, string> gitEnv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Disable git prompt
            gitEnv["GIT_TERMINAL_PROMPT"] = "0";

            // Disable prompting for git credential manager
            gitEnv["GCM_INTERACTIVE"] = "Never";

            // Git-lfs will try to pull down asset if any of the local/user/system setting exist.
            // If customer didn't enable `LFS` in their pipeline definition, we will use ENV to disable LFS fetch/checkout.
            if (!gitLfsSupport)
            {
                gitEnv["GIT_LFS_SKIP_SMUDGE"] = "1";
            }

            // Add the public variables.
            foreach (var variable in executionContext.Variables)
            {
                // Add the variable using the formatted name.
                string formattedKey = (variable.Key ?? string.Empty).Replace('.', '_').Replace(' ', '_').ToUpperInvariant();
                gitEnv[formattedKey] = variable.Value?.Value ?? string.Empty;
            }

            GitCliManager gitCommandManager = new GitCliManager(gitEnv);
            await gitCommandManager.LoadGitExecutionInfo(executionContext);

            // Make sure the build machine met all requirements for the git repository
            // For now, the requirement we have are:
            // 1. git version greater than 2.9 since we need to use auth header.
            // 2. git-lfs version greater than 2.1 since we need to use auth header.
            // 3. git version greater than 2.14.2 if use SChannel for SSL backend (Windows only)
            RequirementCheck(executionContext, gitCommandManager, gitLfsSupport);

            // prepare askpass for client cert private key, if the repository's endpoint url match the runner config url
            var systemConnection = executionContext.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));

            // Check the current contents of the root folder to see if there is already a repo
            // If there is a repo, see if it matches the one we are expecting to be there based on the remote fetch url
            // if the repo is not what we expect, remove the folder
            if (!await IsRepositoryOriginUrlMatch(executionContext, gitCommandManager, targetPath, repositoryUrl))
            {
                // Delete source folder
                IOUtil.DeleteDirectory(targetPath, cancellationToken);
            }
            else
            {
                // delete the index.lock file left by previous canceled build or any operation cause git.exe crash last time.
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

                // delete the shallow.lock file left by previous canceled build or any operation cause git.exe crash last time.		
                string shallowLockFile = Path.Combine(targetPath, ".git\\shallow.lock");
                if (File.Exists(shallowLockFile))
                {
                    try
                    {
                        File.Delete(shallowLockFile);
                    }
                    catch (Exception ex)
                    {
                        executionContext.Debug($"Unable to delete the shallow.lock file: {shallowLockFile}");
                        executionContext.Debug(ex.ToString());
                    }
                }

                // When repo.clean is selected for a git repo, execute git clean -ffdx and git reset --hard HEAD on the current repo.
                // This will help us save the time to reclone the entire repo.
                // If any git commands exit with non-zero return code or any exception happened during git.exe invoke, fall back to delete the repo folder.
                if (clean)
                {
                    Boolean softCleanSucceed = true;

                    // git clean -ffdx
                    int exitCode_clean = await gitCommandManager.GitClean(executionContext, targetPath);
                    if (exitCode_clean != 0)
                    {
                        executionContext.Debug($"'git clean -ffdx' failed with exit code {exitCode_clean}, this normally caused by:\n    1) Path too long\n    2) Permission issue\n    3) File in use\nFor futher investigation, manually run 'git clean -ffdx' on repo root: {targetPath} after each build.");
                        softCleanSucceed = false;
                    }

                    // git reset --hard HEAD
                    if (softCleanSucceed)
                    {
                        int exitCode_reset = await gitCommandManager.GitReset(executionContext, targetPath);
                        if (exitCode_reset != 0)
                        {
                            executionContext.Debug($"'git reset --hard HEAD' failed with exit code {exitCode_reset}\nFor futher investigation, manually run 'git reset --hard HEAD' on repo root: {targetPath} after each build.");
                            softCleanSucceed = false;
                        }
                    }

                    // git clean -ffdx and git reset --hard HEAD for each submodule
                    if (checkoutSubmodules)
                    {
                        if (softCleanSucceed)
                        {
                            int exitCode_submoduleclean = await gitCommandManager.GitSubmoduleClean(executionContext, targetPath);
                            if (exitCode_submoduleclean != 0)
                            {
                                executionContext.Debug($"'git submodule foreach git clean -ffdx' failed with exit code {exitCode_submoduleclean}\nFor futher investigation, manually run 'git submodule foreach git clean -ffdx' on repo root: {targetPath} after each build.");
                                softCleanSucceed = false;
                            }
                        }

                        if (softCleanSucceed)
                        {
                            int exitCode_submodulereset = await gitCommandManager.GitSubmoduleReset(executionContext, targetPath);
                            if (exitCode_submodulereset != 0)
                            {
                                executionContext.Debug($"'git submodule foreach git reset --hard HEAD' failed with exit code {exitCode_submodulereset}\nFor futher investigation, manually run 'git submodule foreach git reset --hard HEAD' on repo root: {targetPath} after each build.");
                                softCleanSucceed = false;
                            }
                        }
                    }

                    if (!softCleanSucceed)
                    {
                        //fall back
                        executionContext.Warning("Unable to run \"git clean -ffdx\" and \"git reset --hard HEAD\" successfully, delete source folder instead.");
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
                int exitCode_init = await gitCommandManager.GitInit(executionContext, targetPath);
                if (exitCode_init != 0)
                {
                    throw new InvalidOperationException($"Unable to use git.exe init repository under {targetPath}, 'git init' failed with exit code: {exitCode_init}");
                }

                int exitCode_addremote = await gitCommandManager.GitRemoteAdd(executionContext, targetPath, "origin", repositoryUrl.AbsoluteUri);
                if (exitCode_addremote != 0)
                {
                    throw new InvalidOperationException($"Unable to use git.exe add remote 'origin', 'git remote add' failed with exit code: {exitCode_addremote}");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // disable git auto gc
            int exitCode_disableGC = await gitCommandManager.GitDisableAutoGC(executionContext, targetPath);
            if (exitCode_disableGC != 0)
            {
                executionContext.Warning("Unable turn off git auto garbage collection, git fetch operation may trigger auto garbage collection which will affect the performance of fetching.");
            }

            // always remove any possible left extraheader setting from git config.
            if (await gitCommandManager.GitConfigExist(executionContext, targetPath, $"http.{repositoryUrl.AbsoluteUri}.extraheader"))
            {
                executionContext.Debug("Remove any extraheader setting from git config.");
                await RemoveGitConfig(executionContext, gitCommandManager, targetPath, $"http.{repositoryUrl.AbsoluteUri}.extraheader", string.Empty);
            }

            List<string> additionalFetchArgs = new List<string>();
            List<string> additionalLfsFetchArgs = new List<string>();

            // Add http.https://github.com.extraheader=... to gitconfig
            // accessToken as basic auth header to handle any auth challenge from github.com 
            string configKey = $"http.https://github.com/.extraheader";
            string configValue = $"\"AUTHORIZATION: {GenerateBasicAuthHeader(executionContext, accessToken)}\"";
            configModifications[configKey] = configValue.Trim('\"');
            int exitCode_config = await gitCommandManager.GitConfig(executionContext, targetPath, configKey, configValue);
            if (exitCode_config != 0)
            {
                throw new InvalidOperationException($"Git config failed with exit code: {exitCode_config}");
            }

            // Prepare gitlfs url for fetch and checkout
            if (gitLfsSupport)
            {
                // Initialize git lfs by execute 'git lfs install'
                executionContext.Debug("Setup the local Git hooks for Git LFS.");
                int exitCode_lfsInstall = await gitCommandManager.GitLFSInstall(executionContext, targetPath);
                if (exitCode_lfsInstall != 0)
                {
                    throw new InvalidOperationException($"Git-lfs installation failed with exit code: {exitCode_lfsInstall}");
                }
            }

            List<string> additionalFetchSpecs = new List<string>();
            additionalFetchSpecs.Add("+refs/heads/*:refs/remotes/origin/*");

            if (IsPullRequest(sourceBranch))
            {
                additionalFetchSpecs.Add($"+{sourceBranch}:{GetRemoteRefName(sourceBranch)}");
            }

            int exitCode_fetch = await gitCommandManager.GitFetch(executionContext, targetPath, "origin", fetchDepth, additionalFetchSpecs, string.Join(" ", additionalFetchArgs), cancellationToken);
            if (exitCode_fetch != 0)
            {
                throw new InvalidOperationException($"Git fetch failed with exit code: {exitCode_fetch}");
            }

            // Checkout
            // sourceToBuild is used for checkout
            // if sourceBranch is a PR branch or sourceVersion is null, make sure branch name is a remote branch. we need checkout to detached head. 
            // (change refs/heads to refs/remotes/origin, refs/pull to refs/remotes/pull, or leave it as it when the branch name doesn't contain refs/...)
            // if sourceVersion provide, just use that for checkout, since when you checkout a commit, it will end up in detached head.
            cancellationToken.ThrowIfCancellationRequested();
            string sourcesToBuild;
            if (IsPullRequest(sourceBranch) || string.IsNullOrEmpty(sourceVersion))
            {
                sourcesToBuild = GetRemoteRefName(sourceBranch);
            }
            else
            {
                sourcesToBuild = sourceVersion;
            }

            // fetch lfs object upfront, this will avoid fetch lfs object during checkout which cause checkout taking forever
            // since checkout will fetch lfs object 1 at a time, while git lfs fetch will fetch lfs object in parallel.
            if (gitLfsSupport)
            {
                int exitCode_lfsFetch = await gitCommandManager.GitLFSFetch(executionContext, targetPath, "origin", sourcesToBuild, string.Join(" ", additionalLfsFetchArgs), cancellationToken);
                if (exitCode_lfsFetch != 0)
                {
                    // local repository is shallow repository, lfs fetch may fail due to lack of commits history.
                    // this will happen when the checkout commit is older than tip -> fetchDepth
                    if (fetchDepth > 0)
                    {
                        executionContext.Warning($"Git lfs fetch failed on shallow repository, this might because of git fetch with depth '{fetchDepth}' doesn't include the lfs fetch commit '{sourcesToBuild}'.");
                    }

                    // git lfs fetch failed, get lfs log, the log is critical for debug.
                    int exitCode_lfsLogs = await gitCommandManager.GitLFSLogs(executionContext, targetPath);
                    throw new InvalidOperationException($"Git lfs fetch failed with exit code: {exitCode_lfsFetch}. Git lfs logs returned with exit code: {exitCode_lfsLogs}.");
                }
            }

            // Finally, checkout the sourcesToBuild (if we didn't find a valid git object this will throw)
            int exitCode_checkout = await gitCommandManager.GitCheckout(executionContext, targetPath, sourcesToBuild, cancellationToken);
            if (exitCode_checkout != 0)
            {
                // local repository is shallow repository, checkout may fail due to lack of commits history.
                // this will happen when the checkout commit is older than tip -> fetchDepth
                if (fetchDepth > 0)
                {
                    executionContext.Warning($"Git checkout failed on shallow repository, this might because of git fetch with depth '{fetchDepth}' doesn't include the checkout commit '{sourcesToBuild}'.");
                }

                throw new InvalidOperationException($"Git checkout failed with exit code: {exitCode_checkout}");
            }

            // Submodule update
            if (checkoutSubmodules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int exitCode_submoduleSync = await gitCommandManager.GitSubmoduleSync(executionContext, targetPath, checkoutNestedSubmodules, cancellationToken);
                if (exitCode_submoduleSync != 0)
                {
                    throw new InvalidOperationException($"Git submodule sync failed with exit code: {exitCode_submoduleSync}");
                }

                List<string> additionalSubmoduleUpdateArgs = new List<string>();

                int exitCode_submoduleUpdate = await gitCommandManager.GitSubmoduleUpdate(executionContext, targetPath, fetchDepth, string.Join(" ", additionalSubmoduleUpdateArgs), checkoutNestedSubmodules, cancellationToken);
                if (exitCode_submoduleUpdate != 0)
                {
                    throw new InvalidOperationException($"Git submodule update failed with exit code: {exitCode_submoduleUpdate}");
                }
            }

            // Set intra-task variable for post job cleanup
            executionContext.SetIntraActionState("repositoryPath", targetPath);
            executionContext.SetIntraActionState("modifiedgitconfig", JsonUtility.ToString(configModifications.Keys));
            foreach (var config in configModifications)
            {
                executionContext.SetIntraActionState(config.Key, config.Value);
            }
        }

        public async Task CleanupAsync(RunnerActionPluginExecutionContext executionContext)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            var repositoryPath = Environment.GetEnvironmentVariable("STATE_repositoryPath");
            ArgUtil.NotNullOrEmpty(repositoryPath, nameof(repositoryPath));
            executionContext.Output($"Cleanup cached git credential from {repositoryPath}.");

            // Initialize git command manager
            GitCliManager gitCommandManager = new GitCliManager();
            await gitCommandManager.LoadGitExecutionInfo(executionContext);

            executionContext.Debug("Remove any extraheader setting from git config.");
            var configKeys = JsonUtility.FromString<List<string>>(Environment.GetEnvironmentVariable("STATE_modifiedgitconfig"));
            if (configKeys?.Count > 0)
            {
                foreach (var config in configKeys)
                {
                    var configValue = Environment.GetEnvironmentVariable($"STATE_{config}");
                    if (!string.IsNullOrEmpty(configValue))
                    {
                        await RemoveGitConfig(executionContext, gitCommandManager, repositoryPath, config, configValue);
                    }
                }
            }
        }

        private void RequirementCheck(RunnerActionPluginExecutionContext executionContext, GitCliManager gitCommandManager, bool checkGitLfs)
        {
            // v2.9 git exist use auth header.
            gitCommandManager.EnsureGitVersion(_minGitVersionSupportAuthHeader, throwOnNotMatch: true);

#if OS_WINDOWS
            // check git version for SChannel SSLBackend (Windows Only)
            bool schannelSslBackend = StringUtil.ConvertToBoolean(executionContext.GetRunnerContext("gituseschannel"));
            if (schannelSslBackend)
            {
                gitCommandManager.EnsureGitVersion(_minGitVersionSupportSSLBackendOverride, throwOnNotMatch: true);
            }
#endif
            if (checkGitLfs)
            {
                // v2.1 git-lfs exist use auth header.
                gitCommandManager.EnsureGitLFSVersion(_minGitLfsVersionSupportAuthHeader, throwOnNotMatch: true);
            }
        }

        private string GenerateBasicAuthHeader(RunnerActionPluginExecutionContext executionContext, string accessToken)
        {
            // use basic auth header with username:password in base64encoding. 
            string authHeader = $"x-access-token:{accessToken}";
            string base64encodedAuthHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader));

            // add base64 encoding auth header into secretMasker.
            executionContext.AddMask(base64encodedAuthHeader);
            return $"basic {base64encodedAuthHeader}";
        }

        private async Task<bool> IsRepositoryOriginUrlMatch(RunnerActionPluginExecutionContext context, GitCliManager gitCommandManager, string repositoryPath, Uri expectedRepositoryOriginUrl)
        {
            context.Debug($"Checking if the repo on {repositoryPath} matches the expected repository origin URL. expected Url: {expectedRepositoryOriginUrl.AbsoluteUri}");
            if (!Directory.Exists(Path.Combine(repositoryPath, ".git")))
            {
                // There is no repo directory
                context.Debug($"Repository is not found since '.git' directory does not exist under. {repositoryPath}");
                return false;
            }

            Uri remoteUrl;
            remoteUrl = await gitCommandManager.GitGetFetchUrl(context, repositoryPath);

            if (remoteUrl == null)
            {
                // origin fetch url not found.
                context.Debug("Repository remote origin fetch url is empty.");
                return false;
            }

            context.Debug($"Repository remote origin fetch url is {remoteUrl}");
            // compare the url passed in with the remote url found
            if (expectedRepositoryOriginUrl.Equals(remoteUrl))
            {
                context.Debug("URLs match.");
                return true;
            }
            else
            {
                context.Debug($"The remote.origin.url of the repository under root folder '{repositoryPath}' doesn't matches source repository url.");
                return false;
            }
        }

        private async Task RemoveGitConfig(RunnerActionPluginExecutionContext executionContext, GitCliManager gitCommandManager, string targetPath, string configKey, string configValue)
        {
            int exitCode_configUnset = await gitCommandManager.GitConfigUnset(executionContext, targetPath, configKey);
            if (exitCode_configUnset != 0)
            {
                // if unable to use git.exe unset http.extraheader or core.askpass, modify git config file on disk. make sure we don't left credential.
                if (!string.IsNullOrEmpty(configValue))
                {
                    executionContext.Warning("An unsuccessful attempt was made using git command line to remove \"http.extraheader\" from the git config. Attempting to modify the git config file directly to remove the credential.");
                    string gitConfig = Path.Combine(targetPath, ".git/config");
                    if (File.Exists(gitConfig))
                    {
                        List<string> safeGitConfig = new List<string>();
                        var gitConfigContents = File.ReadAllLines(gitConfig);
                        foreach (var line in gitConfigContents)
                        {
                            if (!line.Contains(configValue))
                            {
                                safeGitConfig.Add(line);
                            }
                        }

                        File.WriteAllLines(gitConfig, safeGitConfig);
                    }
                }
                else
                {
                    executionContext.Warning($"Unable to remove \"{configKey}\" from the git config. To remove the credential, execute \"git config --unset - all {configKey}\" from the repository root \"{targetPath}\".");
                }
            }
        }

        private bool IsPullRequest(string sourceBranch)
        {
            return !string.IsNullOrEmpty(sourceBranch) &&
                (sourceBranch.StartsWith(_pullRefsPrefix, StringComparison.OrdinalIgnoreCase) ||
                 sourceBranch.StartsWith(_remotePullRefsPrefix, StringComparison.OrdinalIgnoreCase));
        }

        private string GetRemoteRefName(string refName)
        {
            if (string.IsNullOrEmpty(refName))
            {
                // If the refName is empty return the remote name for master
                refName = _remoteRefsPrefix + "master";
            }
            else if (refName.Equals("master", StringComparison.OrdinalIgnoreCase))
            {
                // If the refName is master return the remote name for master
                refName = _remoteRefsPrefix + refName;
            }
            else if (refName.StartsWith(_refsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // If the refName is refs/heads change it to the remote version of the name
                refName = _remoteRefsPrefix + refName.Substring(_refsPrefix.Length);
            }
            else if (refName.StartsWith(_pullRefsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // If the refName is refs/pull change it to the remote version of the name
                refName = refName.Replace(_pullRefsPrefix, _remotePullRefsPrefix);
            }

            return refName;
        }
    }
}
