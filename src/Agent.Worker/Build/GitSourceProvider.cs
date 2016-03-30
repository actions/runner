using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public class GitSourceProvider : SourceProvider, ISourceProvider
    {
        // refs prefix
        // TODO: how to deal with limited refs?
        private const string _refsPrefix = "refs/heads/";
        private const string _remoteRefsPrefix = "refs/remotes/origin/";
        private const string _pullRefsPrefix = "refs/pull/";
        private const string _remotePullRefsPrefix = "refs/remotes/pull/";

        private static Version _minSupportGitVersion = new Version(1, 8);
        private readonly Dictionary<string, Uri> _credentialUrlCache = new Dictionary<string, Uri>();
        private IGitCommandManager _gitCommandManager;

        public override string RepositoryType => WellKnownRepositoryTypes.Git;

        public async Task GetSourceAsync(IExecutionContext executionContext, ServiceEndpoint endpoint, CancellationToken cancellationToken)
        {
            Trace.Entering();
            ArgUtil.NotNull(endpoint, nameof(endpoint));

            executionContext.Output($"Syncing repository: {endpoint.Name} (Git)");

            string targetPath = executionContext.Variables.Get(Constants.Variables.Build.SourceFolder);
            string sourceBranch = executionContext.Variables.Get(Constants.Variables.Build.SourceBranch);
            string sourceVersion = executionContext.Variables.Get(Constants.Variables.Build.SourceVersion);

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

            // ensure find full path to git exist, the version of the installed git is what we supported.
            string gitPath = null;
            if (!TryGetGitLocation(executionContext, out gitPath))
            {
                throw new Exception(StringUtil.Loc("GitNotInstalled"));
            }
            Trace.Info($"Git path={gitPath}");

            _gitCommandManager = HostContext.GetService<IGitCommandManager>();
            _gitCommandManager.GitPath = gitPath;

            Version gitVersion = await _gitCommandManager.GitVersion(executionContext);
            if (gitVersion < _minSupportGitVersion)
            {
                throw new Exception(StringUtil.Loc("InstalledGitNotSupport", _minSupportGitVersion));
            }
            Trace.Info($"Git version={gitVersion}");
            _gitCommandManager.Version = gitVersion;

            // sync source
            await SyncAndCheckout(executionContext, endpoint, targetPath, clean, sourceBranch, sourceVersion, checkoutSubmodules, exposeCred, cancellationToken);
        }

        public async Task PostJobCleanupAsync(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            Trace.Entering();
            ArgUtil.NotNull(endpoint, nameof(endpoint));

            executionContext.Output($"Cleaning embeded credential from repository: {endpoint.Name} (Git)");

            Uri repositoryUrl = endpoint.Url;
            string targetPath = executionContext.Variables.Get(Constants.Variables.Build.SourceFolder);

            executionContext.Debug($"Repository url={endpoint.Url}");
            executionContext.Debug($"targetPath={targetPath}");

            await RemoveCachedCredential(executionContext, targetPath, repositoryUrl, "origin");
        }

        public string GetLocalPath(ServiceEndpoint endpoint, string path)
        {
            // For git repositories, we don't do anything
            // We expect the path to be a relative path within the Repository
            return path;
        }

        private bool TryGetGitLocation(IExecutionContext executionContext, out string gitPath)
        {
            //find git in %Path%
            var whichTool = HostContext.GetService<IWhichUtil>();
            gitPath = whichTool.Which("git");

#if OS_WINDOWS
            //find in %ProgramFiles(x86)%\git\cmd if platform is Windows
            if (string.IsNullOrEmpty(gitPath))
            {
                string programFileX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                if (!string.IsNullOrEmpty(programFileX86))
                {
                    gitPath = Path.Combine(programFileX86, "Git\\cmd\\git.exe");
                    if (!File.Exists(gitPath))
                    {
                        gitPath = null;
                    }
                }
            }
#endif
            if (string.IsNullOrEmpty(gitPath))
            {
                return false;
            }
            else
            {
                executionContext.Debug($"Find git installation path: {gitPath}.");
                return true;
            }
        }

        private async Task SyncAndCheckout(
            IExecutionContext context,
            ServiceEndpoint endpoint,
            string targetPath,
            bool clean,
            string sourceBranch,
            string sourceVersion,
            bool checkoutSubmodules,
            bool exposeCred,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            Trace.Entering();
            cancellationToken.ThrowIfCancellationRequested();
            int gitCommandExitCode;

            // retrieve credential from endpoint.
            Uri repositoryUrl = endpoint.Url;
            if (!repositoryUrl.IsAbsoluteUri)
            {
                throw new InvalidOperationException("Repository url need to be an absolute uri.");
            }

            string username = string.Empty;
            string password = string.Empty;
            if (endpoint.Authorization != null)
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
                        context.Warning($"Unsupport endpoint authorization schemes: {endpoint.Authorization.Scheme}");
                        break;
                }
            }

            // Check the current contents of the root folder to see if there is already a repo
            // If there is a repo, see if it matches the one we are expecting to be there based on the remote fetch url
            // if the repo is not what we expect, remove the folder
            if (!await IsRepositoryOriginUrlMatch(context, targetPath, repositoryUrl))
            {
                // Delete source folder
                IOUtil.DeleteDirectory(targetPath, cancellationToken);
            }
            else
            {
                // When repo.clean is selected for a git repo, execute git clean -fdx and git reset --hard HEAD on the current repo.
                // This will help us save the time to reclone the entire repo.
                // If any git commands exit with non-zero return code or any exception happened during git.exe invoke, fall back to delete the repo folder.
                if (clean)
                {
                    Boolean softClean = false;
                    // git clean -fdx
                    // git reset --hard HEAD
                    gitCommandExitCode = await _gitCommandManager.GitClean(context, targetPath);
                    if (gitCommandExitCode != 0)
                    {
                        context.Debug($"'git clean -fdx' failed with exit code {gitCommandExitCode}, this normally caused by:\n    1) Path too long\n    2) Permission issue\n    3) File in use\nFor futher investigation, manually run 'git clean -fdx' on repo root: {targetPath} after each build.");
                    }
                    else
                    {
                        gitCommandExitCode = await _gitCommandManager.GitReset(context, targetPath);
                        if (gitCommandExitCode != 0)
                        {
                            context.Debug($"'git reset --hard HEAD' failed with exit code {gitCommandExitCode}\nFor futher investigation, manually run 'git reset --hard HEAD' on repo root: {targetPath} after each build.");
                        }
                        else
                        {
                            softClean = true;
                        }
                    }

                    if (!softClean)
                    {
                        //fall back
                        context.Warning("Unable to run \"git clean -fdx\" and \"git reset --hard HEAD\" successfully, delete source folder instead.");
                        // TODO: Util.DirectoryDelete()
                        Directory.Delete(targetPath, true);
                    }
                }
            }

            // if the folder is missing, create it
            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // inject credential into fetch url
            context.Debug("Inject credential into git remote url.");
            Uri urlWithCred = null;
            urlWithCred = GetCredentialEmbeddedRepoUrl(repositoryUrl, username, password);

            // if the folder contains a .git folder, it means the folder contains a git repo that matches the remote url and in a clean state.
            // we will run git fetch to update the repo.
            if (Directory.Exists(Path.Combine(targetPath, ".git")))
            {
                // disable git auto gc
                int exitCode_disableGC = await _gitCommandManager.GitDisableAutoGC(context, targetPath);
                if (exitCode_disableGC != 0)
                {
                    context.Warning("Unable turn off git auto garbage collection, git fetch operation may trigger auto garbage collection which will affect the performence of fetching.");
                }

                // inject credential into fetch url
                context.Debug("Inject credential into git remote fetch url.");
                int exitCode_seturl = await _gitCommandManager.GitRemoteSetUrl(context, targetPath, "origin", urlWithCred.AbsoluteUri);
                if (exitCode_seturl != 0)
                {
                    throw new InvalidOperationException($"Unable to use git.exe inject credential to git remote fetch url, 'git remote set-url' failed with exit code: {exitCode_seturl}");
                }

                // inject credential into push url
                context.Debug("Inject credential into git remote push url.");
                exitCode_seturl = await _gitCommandManager.GitRemoteSetPushUrl(context, targetPath, "origin", urlWithCred.AbsoluteUri);
                if (exitCode_seturl != 0)
                {
                    throw new InvalidOperationException($"Unable to use git.exe inject credential to git remote push url, 'git remote set-url --push' failed with exit code: {exitCode_seturl}");
                }

                // If this is a build for a pull request, then include
                // the pull request reference as an additional ref.
                string fetchSpec = IsPullRequest(sourceBranch) ? StringUtil.Format("+{0}:{1}", sourceBranch, GetRemoteRefName(sourceBranch)) : null;

                context.Progress(0, "Starting fetch...");
                gitCommandExitCode = await _gitCommandManager.GitFetch(context, targetPath, "origin", new List<string>() { fetchSpec }, username, password, exposeCred, cancellationToken);
                if (gitCommandExitCode != 0)
                {
                    throw new InvalidOperationException($"Git fetch failed with exit code: {gitCommandExitCode}");
                }
            }
            else
            {
                context.Progress(0, "Starting clone...");
                gitCommandExitCode = await _gitCommandManager.GitClone(context, targetPath, urlWithCred, username, password, exposeCred, cancellationToken);
                if (gitCommandExitCode != 0)
                {
                    throw new InvalidOperationException($"Git clone failed with exit code: {gitCommandExitCode}");
                }

                if (IsPullRequest(sourceBranch))
                {
                    // Clone doesn't pull the refs/pull namespace so we need to Fetch the appropriate ref
                    string fetchSpec = StringUtil.Format("+{0}:{1}", sourceBranch, GetRemoteRefName(sourceBranch));

                    context.Progress(76, $"Starting fetch pull request ref... {fetchSpec}");
                    context.Output("Starting fetch pull request ref");
                    gitCommandExitCode = await _gitCommandManager.GitFetch(context, targetPath, "origin", new List<string>() { fetchSpec }, username, password, exposeCred, cancellationToken);
                    if (gitCommandExitCode != 0)
                    {
                        throw new InvalidOperationException($"Git fetch failed with exit code: {gitCommandExitCode}");
                    }
                }
            }

            if (!exposeCred)
            {
                // remove cached credential from origin's fetch/push url.
                await RemoveCachedCredential(context, targetPath, repositoryUrl, "origin");
            }

            // Checkout
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
                    context.Debug($"Unable to delete the index.lock file: {lockFile}");
                    context.Debug(ex.ToString());
                }
            }

            // sourceToBuild is used for checkout
            // if sourceBranch is a PR branch or sourceVersion is null, make sure branch name is a remote branch. we need checkout to detached head. 
            // (change refs/heads to refs/remotes/origin, refs/pull to refs/remotes/pull, or leava it as it when the branch name doesn't contain refs/...)
            // if sourceVersion provide, just use that for checkout, since when you checkout a commit, it will end up in detached head.
            context.Progress(80, "Starting checkout...");
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
            gitCommandExitCode = await _gitCommandManager.GitCheckout(context, targetPath, sourcesToBuild, cancellationToken);
            if (gitCommandExitCode != 0)
            {
                throw new InvalidOperationException($"Git checkout failed with exit code: {gitCommandExitCode}");
            }

            // Submodule update
            if (checkoutSubmodules)
            {
                context.Progress(90, "Updating submodules...");
                gitCommandExitCode = await _gitCommandManager.GitSubmoduleInit(context, targetPath);
                if (gitCommandExitCode != 0)
                {
                    throw new InvalidOperationException($"Git submodule init failed with exit code: {gitCommandExitCode}");
                }

                // we can use the following code if we want to inject different credential for different submodules.
                // inject credentials for each submodule
                // Dictionary<string, Uri> submoduleUrls = GitGetSubmoduleUrls(m_rootPath);
                // inject credentials into submoduleUrls
                // GitUpdateSubmoduleUrls(m_rootPath, submoduleUrls);

                context.Command("git submodule update");
                gitCommandExitCode = await _gitCommandManager.GitSubmoduleUpdate(context, targetPath, cancellationToken);
                if (gitCommandExitCode != 0)
                {
                    throw new InvalidOperationException($"Git submodule update failed with exit code: {gitCommandExitCode}");
                }
            }
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

        private async Task RemoveCachedCredential(IExecutionContext context, string repositoryPath, Uri repositoryUrl, string remoteName)
        {
            //remove credential from fetch url
            context.Debug("Remove injected credential from git remote fetch url.");
            int exitCode_seturl = await _gitCommandManager.GitRemoteSetUrl(context, repositoryPath, remoteName, repositoryUrl.AbsoluteUri);

            context.Debug("Remove injected credential from git remote push url.");
            int exitCode_setpushurl = await _gitCommandManager.GitRemoteSetPushUrl(context, repositoryPath, remoteName, repositoryUrl.AbsoluteUri);

            if (exitCode_seturl != 0 || exitCode_setpushurl != 0)
            {
                // if unable to use git.exe set fetch url back, modify git config file on disk. make sure we don't left credential.
                context.Debug("Unable to use git.exe remove injected credential from git remote fetch url, modify git config file on disk to remove injected credential.");
                string gitConfig = Path.Combine(repositoryPath, ".git/config");
                if (File.Exists(gitConfig))
                {
                    // TODO: async read/write ?
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