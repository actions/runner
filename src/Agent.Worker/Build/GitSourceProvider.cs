using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public class ExternalGitSourceProvider : GitSourceProvider
    {
        public override string RepositoryType => WellKnownRepositoryTypes.Git;
    }

    public sealed class GitHubSourceProvider : GitSourceProvider
    {
        public override string RepositoryType => WellKnownRepositoryTypes.GitHub;
    }

    public sealed class TfsGitSourceProvider : GitSourceProvider
    {
        public override string RepositoryType => WellKnownRepositoryTypes.TfsGit;
    }

    public abstract class GitSourceProvider : SourceProvider, ISourceProvider
    {
        // refs prefix
        // TODO: how to deal with limited refs?
        private const string _refsPrefix = "refs/heads/";
        private const string _remoteRefsPrefix = "refs/remotes/origin/";
        private const string _pullRefsPrefix = "refs/pull/";
        private const string _remotePullRefsPrefix = "refs/remotes/pull/";

        private readonly Dictionary<string, Uri> _credentialUrlCache = new Dictionary<string, Uri>();
        private readonly Dictionary<string, string> _authHeaderCache = new Dictionary<string, string>();

        private IGitCommandManager _gitCommandManager;
        private bool _selfManageGitCreds = false;

        public async Task GetSourceAsync(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            CancellationToken cancellationToken)
        {
            Trace.Entering();
            // Validate args.
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(endpoint, nameof(endpoint));

            executionContext.Output($"Syncing repository: {endpoint.Name} ({RepositoryType})");
            Uri repositoryUrl = endpoint.Url;
            if (!repositoryUrl.IsAbsoluteUri)
            {
                throw new InvalidOperationException("Repository url need to be an absolute uri.");
            }

            string targetPath = GetEndpointData(endpoint, Constants.EndpointData.SourcesDirectory);
            string sourceBranch = GetEndpointData(endpoint, Constants.EndpointData.SourceBranch);
            string sourceVersion = GetEndpointData(endpoint, Constants.EndpointData.SourceVersion);

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

            Trace.Info($"Repository url={repositoryUrl}");
            Trace.Info($"targetPath={targetPath}");
            Trace.Info($"sourceBranch={sourceBranch}");
            Trace.Info($"sourceVersion={sourceVersion}");
            Trace.Info($"clean={clean}");
            Trace.Info($"checkoutSubmodules={checkoutSubmodules}");
            Trace.Info($"exposeCred={exposeCred}");

            // Determine which git will be use
            // On windows, we prefer the built-in portable git within the agent's externals folder, set system.prefergitfrompath=true can change the behavior, 
            // agent will find git.exe from %PATH%
            // On Linux, we will always use git find in %PATH% regardless of system.prefergitfrompath
            bool preferGitFromPath = executionContext.Variables.GetBoolean(Constants.Variables.System.PreferGitFromPath) ?? false;
#if !OS_WINDOWS
            // On linux/OSX, we will always find git from %PATH%
            preferGitFromPath = true;
#endif

            // Determine do we need to provide creds to git operation
            _selfManageGitCreds = executionContext.Variables.GetBoolean(Constants.Variables.System.SelfManageGitCreds) ?? false;
            if (_selfManageGitCreds)
            {
                // Customer choose to own git creds by themselves.
                executionContext.Output(StringUtil.Loc("SelfManageGitCreds"));
            }

            // Initialize git command manager
            _gitCommandManager = HostContext.GetService<IGitCommandManager>();
            await _gitCommandManager.LoadGitExecutionInfo(executionContext, useBuiltInGit: !preferGitFromPath);

            // min git version that support add extra auth header.
            Version minGitVersionSupportAuthHeader = new Version(2, 9);

            // When the repository is a TfsGit, figure out the endpoint is hosted vsts git or on-prem tfs git
            bool? onPremTfsGit = null;
            if (RepositoryType == WellKnownRepositoryTypes.TfsGit)
            {
                // When repository type is TfsGit, set OnPremTfsGit to True, since that variable is added around TFS 2015 Qu2.
                // Old TFS AT will not send this variable to build agent, and VSTS will always send it to build agent.
                onPremTfsGit = true;
                string onPremTfsGitString;
                if (endpoint.Data.TryGetValue(WellKnownEndpointData.OnPremTfsGit, out onPremTfsGitString))
                {
                    onPremTfsGit = StringUtil.ConvertToBoolean(onPremTfsGitString);
                }

                if (!_selfManageGitCreds && onPremTfsGit.Value)
                {
                    // When repository is TFS on-prem git, and we own manage git creds, 
                    // we require git version 2.9.0 to support adding extra auth header.
                    _gitCommandManager.EnsureGitVersion(minGitVersionSupportAuthHeader, throwOnNotMatch: true);
                }
            }

            // retrieve credential from endpoint.
            string username = string.Empty;
            string password = string.Empty;
            if (!_selfManageGitCreds && endpoint.Authorization != null)
            {
                switch (endpoint.Authorization.Scheme)
                {
                    case EndpointAuthorizationSchemes.OAuth:
                        username = EndpointAuthorizationSchemes.OAuth;
                        if (!endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out password))
                        {
                            password = string.Empty;
                        }
                        break;
                    case EndpointAuthorizationSchemes.UsernamePassword:
                        if (!endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.Username, out username))
                        {
                            // leave the username as empty, the username might in the url, like: http://username@repository.git
                            username = string.Empty;
                        }
                        if (!endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.Password, out password))
                        {
                            // we have username, but no password
                            password = string.Empty;
                        }
                        break;
                    default:
                        executionContext.Warning($"Unsupport endpoint authorization schemes: {endpoint.Authorization.Scheme}");
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
                    Boolean softCleanSucceed = true;

                    // git clean -fdx
                    int exitCode_clean = await _gitCommandManager.GitClean(executionContext, targetPath);
                    if (exitCode_clean != 0)
                    {
                        executionContext.Debug($"'git clean -fdx' failed with exit code {exitCode_clean}, this normally caused by:\n    1) Path too long\n    2) Permission issue\n    3) File in use\nFor futher investigation, manually run 'git clean -fdx' on repo root: {targetPath} after each build.");
                        softCleanSucceed = false;
                    }

                    // git reset --hard HEAD
                    if (softCleanSucceed)
                    {
                        int exitCode_reset = await _gitCommandManager.GitReset(executionContext, targetPath);
                        if (exitCode_reset != 0)
                        {
                            executionContext.Debug($"'git reset --hard HEAD' failed with exit code {exitCode_reset}\nFor futher investigation, manually run 'git reset --hard HEAD' on repo root: {targetPath} after each build.");
                            softCleanSucceed = false;
                        }
                    }

                    // git clean -fdx and git reset --hard HEAD for each submodule
                    if (checkoutSubmodules)
                    {
                        if (softCleanSucceed)
                        {
                            int exitCode_submoduleclean = await _gitCommandManager.GitSubmoduleClean(executionContext, targetPath);
                            if (exitCode_submoduleclean != 0)
                            {
                                executionContext.Debug($"'git submodule foreach git clean -fdx' failed with exit code {exitCode_submoduleclean}\nFor futher investigation, manually run 'git submodule foreach git clean -fdx' on repo root: {targetPath} after each build.");
                                softCleanSucceed = false;
                            }
                        }

                        if (softCleanSucceed)
                        {
                            int exitCode_submodulereset = await _gitCommandManager.GitSubmoduleReset(executionContext, targetPath);
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

            // always remove any possible left extraheader setting from git config.
            if (await _gitCommandManager.GitConfigExist(executionContext, targetPath, $"http.{repositoryUrl.AbsoluteUri}.extraheader"))
            {
                executionContext.Debug("Remove any extraheader setting from git config.");
                await RemoveExtraHeader(executionContext, targetPath, $"http.{repositoryUrl.AbsoluteUri}.extraheader", string.Empty);
            }

            string additionalFetchArgs = string.Empty;
            if (!_selfManageGitCreds)
            {
                if (RepositoryType == WellKnownRepositoryTypes.TfsGit &&
                    (onPremTfsGit.Value || _gitCommandManager.EnsureGitVersion(minGitVersionSupportAuthHeader, throwOnNotMatch: false)))
                {
                    //  When repository is TfsGit on-prem git or vsts-git with git version support adding auth header.
                    additionalFetchArgs = $"-c http.extraheader=\"AUTHORIZATION: bearer {password}\"";
                }
                else
                {
                    // Otherwise, inject credential into fetch/push url
                    // inject credential into fetch url
                    executionContext.Debug("Inject credential into git remote url.");
                    Uri urlWithCred = null;
                    urlWithCred = GetCredentialEmbeddedRepoUrl(repositoryUrl, username, password);

                    // inject credential into fetch url
                    executionContext.Debug("Inject credential into git remote fetch url.");
                    int exitCode_seturl = await _gitCommandManager.GitRemoteSetUrl(executionContext, targetPath, "origin", urlWithCred.AbsoluteUri);
                    if (exitCode_seturl != 0)
                    {
                        throw new InvalidOperationException($"Unable to use git.exe inject credential to git remote fetch url, 'git remote set-url' failed with exit code: {exitCode_seturl}");
                    }

                    // inject credential into push url
                    executionContext.Debug("Inject credential into git remote push url.");
                    exitCode_seturl = await _gitCommandManager.GitRemoteSetPushUrl(executionContext, targetPath, "origin", urlWithCred.AbsoluteUri);
                    if (exitCode_seturl != 0)
                    {
                        throw new InvalidOperationException($"Unable to use git.exe inject credential to git remote push url, 'git remote set-url --push' failed with exit code: {exitCode_seturl}");
                    }
                }
            }

            // If this is a build for a pull request, then include
            // the pull request reference as an additional ref.
            string fetchSpec = IsPullRequest(sourceBranch) ? StringUtil.Format("+{0}:{1}", sourceBranch, GetRemoteRefName(sourceBranch)) : null;

            int exitCode_fetch = await _gitCommandManager.GitFetch(executionContext, targetPath, "origin", new List<string>() { fetchSpec }, additionalFetchArgs, cancellationToken);
            if (exitCode_fetch != 0)
            {
                throw new InvalidOperationException($"Git fetch failed with exit code: {exitCode_fetch}");
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

                string additionalSubmoduleUpdateArgs = string.Empty;
                if (!_selfManageGitCreds &&
                    RepositoryType == WellKnownRepositoryTypes.TfsGit &&
                    (onPremTfsGit.Value || _gitCommandManager.EnsureGitVersion(minGitVersionSupportAuthHeader, throwOnNotMatch: false)))
                {
                    string tfsAccountUrl = repositoryUrl.AbsoluteUri.Replace(repositoryUrl.PathAndQuery, string.Empty);
                    additionalSubmoduleUpdateArgs = $"-c http.{tfsAccountUrl}.extraheader=\"AUTHORIZATION: bearer {password}\"";
                }

                int exitCode_submoduleUpdate = await _gitCommandManager.GitSubmoduleUpdate(executionContext, targetPath, additionalSubmoduleUpdateArgs, cancellationToken);
                if (exitCode_submoduleUpdate != 0)
                {
                    throw new InvalidOperationException($"Git submodule update failed with exit code: {exitCode_submoduleUpdate}");
                }
            }

            // handle expose creds, related to 'Allow Scripts to Access OAuth Token' option
            if (!_selfManageGitCreds)
            {
                if (RepositoryType == WellKnownRepositoryTypes.TfsGit &&
                    (onPremTfsGit.Value || _gitCommandManager.EnsureGitVersion(minGitVersionSupportAuthHeader, throwOnNotMatch: false)))
                {
                    if (exposeCred)
                    {
                        string configKey = $"http.{repositoryUrl.AbsoluteUri}.extraheader";
                        string configValue = $"\"AUTHORIZATION: bearer {password}\"";
                        _authHeaderCache[configKey] = configValue.Trim('\"');
                        int exitCode_config = await _gitCommandManager.GitConfig(executionContext, targetPath, configKey, configValue);
                        if (exitCode_config != 0)
                        {
                            throw new InvalidOperationException($"Git config failed with exit code: {exitCode_config}");
                        }
                    }
                }
                else
                {
                    if (!exposeCred)
                    {
                        // remove cached credential from origin's fetch/push url.
                        await RemoveCachedCredential(executionContext, targetPath, repositoryUrl, "origin");
                    }
                }
            }
        }

        public async Task PostJobCleanupAsync(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            Trace.Entering();
            ArgUtil.NotNull(endpoint, nameof(endpoint));

            executionContext.Output($"Cleaning any cached credential from repository: {endpoint.Name} (Git)");

            Uri repositoryUrl = endpoint.Url;
            string targetPath = GetEndpointData(endpoint, Constants.EndpointData.SourcesDirectory);

            executionContext.Debug($"Repository url={repositoryUrl}");
            executionContext.Debug($"targetPath={targetPath}");

            if (!_selfManageGitCreds)
            {
                executionContext.Debug("Remove any extraheader setting from git config.");
                foreach (var header in _authHeaderCache)
                {
                    await RemoveExtraHeader(executionContext, targetPath, header.Key, header.Value);
                }

                await RemoveCachedCredential(executionContext, targetPath, repositoryUrl, "origin");
            }
        }

        public override void SetVariablesInEndpoint(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            base.SetVariablesInEndpoint(executionContext, endpoint);
            endpoint.Data.Add(Constants.EndpointData.SourceBranch, executionContext.Variables.Get(Constants.Variables.Build.SourceBranch));
        }

        private async Task<bool> IsRepositoryOriginUrlMatch(IExecutionContext context, string repositoryPath, Uri expectedRepositoryOriginUrl)
        {
            context.Debug($"Checking if the repo on {repositoryPath} matches the expected repository origin URL. expected Url: {expectedRepositoryOriginUrl.AbsoluteUri}");
            if (!Directory.Exists(Path.Combine(repositoryPath, ".git")))
            {
                // There is no repo directory
                context.Debug($"Repository is not found since '.git' directory does not exist under. {repositoryPath}");
                return false;
            }

            Uri remoteUrl;
            remoteUrl = await _gitCommandManager.GitGetFetchUrl(context, repositoryPath);

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

        private Uri GetCredentialEmbeddedRepoUrl(Uri repositoryUrl, string username, string password)
        {
            ArgUtil.NotNull(repositoryUrl, nameof(repositoryUrl));

            // retrieve cached url first.
            Uri cachedUrl;
            if (_credentialUrlCache.TryGetValue(repositoryUrl.AbsoluteUri, out cachedUrl))
            {
                return cachedUrl;
            }

            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            {
                return repositoryUrl;
            }
            else
            {
                UriBuilder credUri = new UriBuilder(repositoryUrl);

                // priority: username => uri.username => 'useranme not supplied'
                // escape chars in username for uri
                if (string.IsNullOrEmpty(username))
                {
                    if (string.IsNullOrEmpty(credUri.UserName))
                    {
                        username = "username not supplied";
                    }
                    else
                    {
                        username = credUri.UserName;
                    }
                }
                username = Uri.EscapeDataString(username);

                // priority: password => uri.password => string.empty
                // escape chars in password for uri
                if (string.IsNullOrEmpty(password))
                {
                    if (string.IsNullOrEmpty(credUri.Password))
                    {
                        password = string.Empty;
                    }
                    else
                    {
                        password = credUri.Password;
                    }
                }
                password = Uri.EscapeDataString(password);

                credUri.UserName = username;
                credUri.Password = password;

                _credentialUrlCache[credUri.Uri.AbsoluteUri] = credUri.Uri;
                return credUri.Uri;
            }
        }

        private async Task RemoveExtraHeader(IExecutionContext executionContext, string targetPath, string extraheaderConfigKey, string extraheaderConfigValue)
        {
            int exitCode_configUnset = await _gitCommandManager.GitConfigUnset(executionContext, targetPath, extraheaderConfigKey);
            if (exitCode_configUnset != 0)
            {
                // if unable to use git.exe unset http.extraheader, modify git config file on disk. make sure we don't left credential.
                if (!string.IsNullOrEmpty(extraheaderConfigValue))
                {
                    executionContext.Warning(StringUtil.Loc("AttemptRemoveCredFromConfig"));
                    string gitConfig = Path.Combine(targetPath, ".git/config");
                    if (File.Exists(gitConfig))
                    {
                        string gitConfigContent = File.ReadAllText(Path.Combine(targetPath, ".git", "config"));
                        if (gitConfigContent.Contains(extraheaderConfigKey))
                        {
                            string setting = $"extraheader = {extraheaderConfigValue}";
                            gitConfigContent = Regex.Replace(gitConfigContent, setting, string.Empty, RegexOptions.IgnoreCase);
                            File.WriteAllText(gitConfig, gitConfigContent);
                        }
                    }
                }
                else
                {
                    executionContext.Warning(StringUtil.Loc("FailToRemoveCredFromConfig", extraheaderConfigKey, targetPath));
                }
            }
        }

        private async Task RemoveCachedCredential(IExecutionContext context, string repositoryPath, Uri repositoryUrl, string remoteName)
        {
            // there is nothing cached in repository Url.
            if (_credentialUrlCache.Count == 0)
            {
                return;
            }

            //remove credential from fetch url
            context.Debug("Remove injected credential from git remote fetch url.");
            int exitCode_seturl = await _gitCommandManager.GitRemoteSetUrl(context, repositoryPath, remoteName, repositoryUrl.AbsoluteUri);

            context.Debug("Remove injected credential from git remote push url.");
            int exitCode_setpushurl = await _gitCommandManager.GitRemoteSetPushUrl(context, repositoryPath, remoteName, repositoryUrl.AbsoluteUri);

            if (exitCode_seturl != 0 || exitCode_setpushurl != 0)
            {
                // if unable to use git.exe set fetch url back, modify git config file on disk. make sure we don't left credential.
                context.Warning("Unable to use git.exe remove injected credential from git remote fetch url, modify git config file on disk to remove injected credential.");
                string gitConfig = Path.Combine(repositoryPath, ".git/config");
                if (File.Exists(gitConfig))
                {
                    string gitConfigContent = File.ReadAllText(Path.Combine(repositoryPath, ".git", "config"));
                    Uri urlWithCred;
                    if (_credentialUrlCache.TryGetValue(repositoryUrl.AbsoluteUri, out urlWithCred))
                    {
                        gitConfigContent = gitConfigContent.Replace(urlWithCred.AbsoluteUri, repositoryUrl.AbsoluteUri);
                        File.WriteAllText(gitConfig, gitConfigContent);
                    }
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