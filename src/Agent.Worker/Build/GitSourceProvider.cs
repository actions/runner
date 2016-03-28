using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using System;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent.Util;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

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
        private readonly Dictionary<string, Dictionary<Version, string>> _gitCommands = new Dictionary<string, Dictionary<Version, string>>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "checkout", new Dictionary<Version, string> ()
                            {
                                { new Version(1,8), "--force {0}" },
                                { new Version(2,7), "--progress --force {0}" }
                            }
            }
        };

        private string _gitPath = null;
        private Version _gitVersion = null;

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

            // Find full path to git, get version of the installed git.
            if (!await TrySetGitInstallationInfo(executionContext))
            {
                throw new Exception(StringUtil.Loc("InstalledGitNotSupport", _minSupportGitVersion));
            }

            await SyncAndCheckout(executionContext, endpoint, targetPath, clean, sourceBranch, sourceVersion, checkoutSubmodules, exposeCred, cancellationToken);

            return;
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

        private async Task<bool> TrySetGitInstallationInfo(IExecutionContext executionContext)
        {
            //find git in %Path%
            _gitPath = IOUtil.Which("git");

#if OS_WINDOWS
            //find in %ProgramFiles(x86)%\git\cmd if platform is Windows
            if (string.IsNullOrEmpty(_gitPath))
            {
                string programFileX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                if (!string.IsNullOrEmpty(programFileX86))
                {
                    _gitPath = Path.Combine(programFileX86, "Git\\cmd\\git.exe");
                    if (!File.Exists(_gitPath))
                    {
                        _gitPath = null;
                    }
                }
            }
#endif
            if (string.IsNullOrEmpty(_gitPath))
            {
                return false;
            }
            else
            {
                executionContext.Debug($"Find git installation path: {_gitPath}.");
            }

            _gitVersion = await GitVersion(executionContext, _gitPath);
            if (_gitVersion == null || _gitVersion < _minSupportGitVersion)
            {
                return false;
            }
            else
            {
                executionContext.Debug($"The version of the installed git is: {_gitVersion}.");
            }

            return true;
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
            Int32 gitCommandExitCode;

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
                // TODO: add IO Util Delete() handle cancellation and exception
                Directory.Delete(targetPath, true);
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
                    gitCommandExitCode = await GitClean(context, targetPath);
                    if (gitCommandExitCode != 0)
                    {
                        context.Debug($"'git clean -fdx' failed with exit code {gitCommandExitCode}, this normally caused by:\n    1) Path too long\n    2) Permission issue\n    3) File in use\nFor futher investigation, manually run 'git clean -fdx' on repo root: {targetPath} after each build.");
                    }
                    else
                    {
                        gitCommandExitCode = await GitReset(context, targetPath);
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

            // if the folder contains a .git folder, it means the folder contains a git repo that matches the remote url and in a clean state.
            // we will run git fetch to update the repo.
            if (Directory.Exists(Path.Combine(targetPath, ".git")))
            {
                // If this is a build for a pull request, then include
                // the pull request reference as an additional ref.
                string fetchSpec = IsPullRequest(sourceBranch) ? StringUtil.Format("+{0}:{1}", sourceBranch, GetRemoteRefName(sourceBranch)) : null;

                context.Progress(0, "Starting fetch...");
                gitCommandExitCode = await GitFetch(context, targetPath, repositoryUrl, "origin", new List<string>() { fetchSpec }, username, password, exposeCred, cancellationToken);
                if (gitCommandExitCode != 0)
                {
                    throw new InvalidOperationException($"Git fetch failed with exit code: {gitCommandExitCode}");
                }
            }
            else
            {
                context.Progress(0, "Starting clone...");
                gitCommandExitCode = await GitClone(context, targetPath, repositoryUrl, username, password, exposeCred, cancellationToken);
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
                    gitCommandExitCode = await GitFetch(context, targetPath, repositoryUrl, "origin", new List<string>() { fetchSpec }, username, password, exposeCred, cancellationToken);
                    if (gitCommandExitCode != 0)
                    {
                        throw new InvalidOperationException($"Git fetch failed with exit code: {gitCommandExitCode}");
                    }
                }
            }

            // Checkout
            // delete the index.lock file left by previous canceled build or any operation casue git.exe crash last time.
            string lockFile = Path.Combine(targetPath, ".git\\index.lock");
            try
            {
                // TODO: IOUtil.FileDelete()
                File.Delete(lockFile);
            }
            catch (Exception ex)
            {
                context.Debug($"Unable to delete the index.lock file: {lockFile}");
                context.Debug(ex.ToString());
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
            gitCommandExitCode = await GitCheckout(context, targetPath, sourcesToBuild, cancellationToken);
            if (gitCommandExitCode != 0)
            {
                throw new InvalidOperationException($"Git checkout failed with exit code: {gitCommandExitCode}");
            }

            // Submodule update
            if (checkoutSubmodules)
            {
                context.Progress(90, "Updating submodules...");
                gitCommandExitCode = await GitSubmoduleInit(context, targetPath);
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
                gitCommandExitCode = await GitSubmoduleUpdate(context, targetPath, cancellationToken);
                if (gitCommandExitCode != 0)
                {
                    throw new InvalidOperationException($"Git submodule update failed with exit code: {gitCommandExitCode}");
                }
            }
        }

        // git clone --progress --no-checkout <URL> <LocalDir>
        private async Task<int> GitClone(IExecutionContext context, string repositoryPath, Uri repositoryUrl, string username, string password, bool exposeCred, CancellationToken cancellationToken)
        {
            context.Debug($"Clone git repository: {repositoryUrl.AbsoluteUri} into: {repositoryPath}.");

            // inject credential into fetch url
            context.Debug("Inject credential into git remote url.");
            Uri urlWithCred = null;
            urlWithCred = GetCredentialEmbeddedRepoUrl(repositoryUrl, username, password);

            string repoRootEscapeSpace = StringUtil.Format(@"""{0}""", repositoryPath.Replace(@"""", @"\"""));
            Int32 exitCode = await ExecuteGitCommandAsync(context, repositoryPath, "clone", StringUtil.Format($"--progress --no-checkout {urlWithCred.AbsoluteUri} {repoRootEscapeSpace}"), cancellationToken);

            if (!exposeCred)
            {
                // remove cached credential from origin's fetch/push url.
                await RemoveCachedCredential(context, repositoryPath, repositoryUrl, "origin");
            }

            return exitCode;
        }

        // git fetch --tags --prune --progress origin [+refs/pull/*:refs/remote/pull/*]
        private async Task<int> GitFetch(IExecutionContext context, string repositoryPath, Uri repositoryUrl, string remoteName, List<string> refSpec, string username, string password, bool exposeCred, CancellationToken cancellationToken)
        {
            if (refSpec != null && refSpec.Count > 0)
            {
                refSpec = refSpec.Where(r => !string.IsNullOrEmpty(r)).ToList();
            }

            context.Debug($"Fetch git repository: {repositoryUrl.AbsoluteUri} remote: {remoteName}.");

            // disable git auto gc
            Int32 exitCode_disableGC = await GitDisableAutoGC(context, repositoryPath);
            if (exitCode_disableGC != 0)
            {
                context.Warning("Unable turn off git auto garbage collection, git fetch operation may trigger auto garbage collection which will affect the performence of fetching.");
            }

            Uri urlWithCred = null;
            urlWithCred = GetCredentialEmbeddedRepoUrl(repositoryUrl, username, password);

            // inject credential into fetch url
            context.Debug("Inject credential into git remote fetch url.");
            Int32 exitCode_seturl = await GitRemoteSetUrl(context, repositoryPath, remoteName, urlWithCred.AbsoluteUri);
            if (exitCode_seturl != 0)
            {
                throw new InvalidOperationException($"Unable to use git.exe inject credential to git remote fetch url, 'git remote set-url' failed with exit code: {exitCode_seturl}");
            }

            // inject credential into push url
            context.Debug("Inject credential into git remote push url.");
            exitCode_seturl = await GitRemoteSetPushUrl(context, repositoryPath, remoteName, urlWithCred.AbsoluteUri);
            if (exitCode_seturl != 0)
            {
                throw new InvalidOperationException($"Unable to use git.exe inject credential to git remote push url, 'git remote set-url --push' failed with exit code: {exitCode_seturl}");
            }

            Int32 exitCode = await ExecuteGitCommandAsync(context, repositoryPath, "fetch", StringUtil.Format($"--tags --prune --progress {remoteName} {string.Join(" ", refSpec)}"), cancellationToken);

            if (!exposeCred)
            {
                // remove cached credential from origin's fetch/push url.
                await RemoveCachedCredential(context, repositoryPath, repositoryUrl, "origin");
            }

            return exitCode;
        }

        // git checkout -f --progress <commitId/branch>
        private async Task<int> GitCheckout(IExecutionContext context, string repositoryPath, string committishOrBranchSpec, CancellationToken cancellationToken = default(CancellationToken))
        {
            context.Debug($"Checkout {committishOrBranchSpec}.");
            string checkoutOption = GetCommandOption("checkout", _gitVersion);
            return await ExecuteGitCommandAsync(context, repositoryPath, "checkout", StringUtil.Format(checkoutOption, committishOrBranchSpec), cancellationToken);
        }

        // git clean -fdx
        private async Task<int> GitClean(IExecutionContext context, string repositoryPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            context.Debug($"Delete untracked files/folders for repository at {repositoryPath}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "clean", "-fdx", cancellationToken); ;
        }

        // git reset --hard HEAD
        private async Task<int> GitReset(IExecutionContext context, string repositoryPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            context.Debug($"Undo any changes to tracked files in the working tree for repository at {repositoryPath}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "reset", "--hard HEAD", cancellationToken);
        }

        // get remote set-url <origin> <url>
        private async Task<int> GitRemoteSetUrl(IExecutionContext context, string repositoryPath, string remoteName, string remoteUrl)
        {
            context.Debug($"Set git fetch url to: {remoteUrl} for remote: {remoteName}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "remote", StringUtil.Format($"set-url {remoteName} {remoteUrl}"));
        }

        // get remote set-url --push <origin> <url>
        private async Task<int> GitRemoteSetPushUrl(IExecutionContext context, string repositoryPath, string remoteName, string remoteUrl)
        {
            context.Debug($"Set git push url to: {remoteUrl} for remote: {remoteName}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "remote", StringUtil.Format($"set-url --push {remoteName} {remoteUrl}"));
        }

        // git submodule init
        private async Task<int> GitSubmoduleInit(IExecutionContext context, string repositoryPath)
        {
            context.Debug("Initialize the git submodules.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "submodule", "init");
        }

        // git submodule update -f
        private async Task<int> GitSubmoduleUpdate(IExecutionContext context, string repositoryPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            context.Debug("Update the registered git submodules.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "submodule", "update -f", cancellationToken);
        }

        // git config --get remote.origin.url
        private async Task<Uri> GitGetFetchUrl(IExecutionContext context, string repositoryPath)
        {
            context.Debug($"Inspect remote.origin.url for repository under {repositoryPath}");
            Uri fetchUrl = null;

            List<string> outputStrings = new List<string>();
            int exitCode = await ExecuteGitCommandAsync(context, repositoryPath, "config", "--get remote.origin.url", outputStrings);

            if (exitCode != 0)
            {
                context.Warning($"'git config --get remote.origin.url' failed with exit code: {exitCode}, output: '{string.Join(Environment.NewLine, outputStrings)}'");
            }
            else
            {
                // remove empty strings
                outputStrings = outputStrings.Where(o => !string.IsNullOrEmpty(o)).ToList();
                if (outputStrings.Count == 1 && !string.IsNullOrEmpty(outputStrings.First()))
                {
                    string remoteFetchUrl = outputStrings.First();
                    if (Uri.IsWellFormedUriString(remoteFetchUrl, UriKind.Absolute))
                    {
                        context.Debug($"Get remote origin fetch url from git config: {remoteFetchUrl}");
                        fetchUrl = new Uri(remoteFetchUrl);
                    }
                    else
                    {
                        context.Debug($"The Origin fetch url from git config: {remoteFetchUrl} is not a absolute well formed url.");
                    }
                }
                else
                {
                    context.Debug($"Unable capture git remote fetch uri from 'git config --get remote.origin.url' command's output, the command's output is not expected: {string.Join(Environment.NewLine, outputStrings)}.");
                }
            }

            return fetchUrl;
        }

        // git config --get-regexp submodule.*.url
        private async Task<Dictionary<string, Uri>> GitGetSubmoduleUrls(IExecutionContext context, string repoRoot)
        {
            context.Debug($"Inspect all submodule.<name>.url for submodules under {repoRoot}");

            Dictionary<string, Uri> submoduleUrls = new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);

            List<string> outputStrings = new List<string>();
            int exitCode = await ExecuteGitCommandAsync(context, repoRoot, "config", "--get-regexp submodule.?*.url", outputStrings);

            if (exitCode != 0)
            {
                context.Debug($"'git config --get-regexp submodule.?*.url' failed with exit code: {exitCode}, output: '{string.Join(Environment.NewLine, outputStrings)}'");
            }
            else
            {
                // remove empty strings
                outputStrings = outputStrings.Where(o => !string.IsNullOrEmpty(o)).ToList();
                foreach (var urlString in outputStrings)
                {
                    context.Debug($"Potential git submodule name and fetch url: {urlString}.");
                    string[] submoduleUrl = urlString.Split(new Char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (submoduleUrl.Length == 2 && Uri.IsWellFormedUriString(submoduleUrl[1], UriKind.Absolute))
                    {
                        submoduleUrls[submoduleUrl[0]] = new Uri(submoduleUrl[1]);
                    }
                    else
                    {
                        context.Debug($"Can't parse git submodule name and submodule fetch url from output: '{urlString}'.");
                    }
                }
            }

            return submoduleUrls;
        }

        // git config <key> <value>
        private async Task<int> GitUpdateSubmoduleUrls(IExecutionContext context, string repositoryPath, Dictionary<string, Uri> updateSubmoduleUrls)
        {
            context.Debug("Update all submodule.<name>.url with credential embeded url.");

            int overallExitCode = 0;
            foreach (var submodule in updateSubmoduleUrls)
            {
                Int32 exitCode = await ExecuteGitCommandAsync(context, repositoryPath, "config", StringUtil.Format($"{submodule.Key} {submodule.Value.ToString()}"));
                if (exitCode != 0)
                {
                    context.Debug($"Unable update: {submodule.Key}.");
                    overallExitCode = exitCode;
                }
            }

            return overallExitCode;
        }

        // git config gc.auto 0
        private async Task<int> GitDisableAutoGC(IExecutionContext context, string repositoryPath)
        {
            context.Debug("Disable git auto garbage collection.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "config", "gc.auto 0");
        }

        // git version
        public async Task<Version> GitVersion(IExecutionContext context, string gitPath)
        {
            context.Debug("Get git version.");
            Version version = null;
            List<string> outputStrings = new List<string>();
            int exitCode = await ExecuteGitCommandAsync(context, IOUtil.GetWorkPath(HostContext), "version", null, outputStrings);
            if (exitCode == 0)
            {
                // remove any empty line.
                outputStrings = outputStrings.Where(o => !string.IsNullOrEmpty(o)).ToList();
                if (outputStrings.Count == 1 && !string.IsNullOrEmpty(outputStrings.First()))
                {
                    string verString = outputStrings.First();
                    // we might only interested about major.minor version
                    Regex verRegex = new Regex("\\d+\\.\\d+", RegexOptions.IgnoreCase);
                    var matchResult = verRegex.Match(verString);
                    if (matchResult.Success && !string.IsNullOrEmpty(matchResult.Value))
                    {
                        if (!Version.TryParse(matchResult.Value, out version))
                        {
                            version = null;
                        }
                    }
                }
            }

            return version;
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

        private async Task<bool> IsRepositoryOriginUrlMatch(IExecutionContext context, string repositoryPath, Uri expectedRepositoryOriginUrl)
        {
            context.Debug($"Checking if the repo on {repositoryPath} matches the expected repository origin URL. expected Url: {expectedRepositoryOriginUrl.AbsoluteUri}");
            if (!Directory.Exists(repositoryPath))
            {
                // There is no repo directory
                context.Debug($"Repository is not found since directory does not exist. {repositoryPath}");
                return false;
            }

            Uri remoteUrl;
            remoteUrl = await GitGetFetchUrl(context, repositoryPath);

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

        private async Task RemoveCachedCredential(IExecutionContext context, string repositoryPath, Uri repositoryUrl, string remoteName)
        {
            //remove credential from fetch url
            context.Debug("Remove injected credential from git remote fetch url.");
            Int32 exitCode_seturl = await GitRemoteSetUrl(context, repositoryPath, remoteName, repositoryUrl.AbsoluteUri);

            context.Debug("Remove injected credential from git remote push url.");
            Int32 exitCode_setpushurl = await GitRemoteSetPushUrl(context, repositoryPath, remoteName, repositoryUrl.AbsoluteUri);

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

        private string GetCommandOption(string command, Version version)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException("command");
            }

            if (version < _minSupportGitVersion)
            {
                throw new NotSupportedException($"MinSupported git version is {_minSupportGitVersion}, your request version is {version}");
            }

            if (!_gitCommands.ContainsKey(command))
            {
                throw new NotSupportedException($"Unsupported git command: {command}");
            }

            Dictionary<Version, string> options = _gitCommands[command];
            foreach (var versionOption in options.OrderByDescending(o => o.Key))
            {
                if (version >= versionOption.Key)
                {
                    return versionOption.Value;
                }
            }

            throw new NotSupportedException($"Can't find supported git command option for command: {command}, version: {version}.");
        }

        private async Task<int> ExecuteGitCommandAsync(IExecutionContext context, string repoRoot, string command, string options, CancellationToken cancellationToken = default(CancellationToken))
        {
            string arg = StringUtil.Format($"{command} {options}").Trim();
            context.Command($"git {arg}");

            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, DataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            processInvoker.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            return await processInvoker.ExecuteAsync(repoRoot, _gitPath, arg, null, cancellationToken);
        }

        private async Task<int> ExecuteGitCommandAsync(IExecutionContext context, string repoRoot, string command, string options, IList<string> output)
        {
            string arg = StringUtil.Format($"{command} {options}").Trim();
            context.Command($"git {arg}");

            if (output == null)
            {
                output = new List<string>();
            }

            object outputLock = new object();
            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, DataReceivedEventArgs message)
            {
                lock(outputLock)
                {
                    output.Add(message.Data);
                }
            };

            processInvoker.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs message)
            {
                lock (outputLock)
                {
                    output.Add(message.Data);
                }
            };

            return await processInvoker.ExecuteAsync(repoRoot, _gitPath, arg, null, default(CancellationToken));
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