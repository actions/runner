using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public class ExternalGitSourceProvider : GitSourceProvider
    {
        public override string RepositoryType => WellKnownRepositoryTypes.Git;

        // external git repository won't use auth header cmdline arg, since we don't know the auth scheme.
        public override bool UseAuthHeaderCmdlineArg => false;

        public override void RequirementCheck(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            // no-opt for external git repo, there is no additional requirements.
        }

        public override string GenerateAuthHeader(string username, string password)
        {
            // can't generate auth header for external git. 
            throw new NotSupportedException(nameof(ExternalGitSourceProvider.GenerateAuthHeader));
        }
    }

    public sealed class GitHubSourceProvider : GitSourceProvider
    {
        public override string RepositoryType => WellKnownRepositoryTypes.GitHub;

        public override bool UseAuthHeaderCmdlineArg
        {
            get
            {
                // v2.9 git exist use auth header for github repository.
                ArgUtil.NotNull(_gitCommandManager, nameof(_gitCommandManager));
                return _gitCommandManager.EnsureGitVersion(_minGitVersionSupportAuthHeader, throwOnNotMatch: false);
            }
        }

        public override void RequirementCheck(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            // no-opt for github repo, there is no additional requirements.
        }

        public override string GenerateAuthHeader(string username, string password)
        {
            // github use basic auth header with username:password in base64encoding. 
            string authHeader = $"{username ?? string.Empty}:{password ?? string.Empty}";
            string base64encodedAuthHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader));

            // add base64 encoding auth header into secretMasker.
            var secretMasker = HostContext.GetService<ISecretMasker>();
            secretMasker.AddValue(base64encodedAuthHeader);
            return $"basic {base64encodedAuthHeader}";
        }
    }

    public sealed class TfsGitSourceProvider : GitSourceProvider
    {
        public override string RepositoryType => WellKnownRepositoryTypes.TfsGit;

        public override bool UseAuthHeaderCmdlineArg
        {
            get
            {
                // v2.9 git exist use auth header for tfsgit repository.
                ArgUtil.NotNull(_gitCommandManager, nameof(_gitCommandManager));
                return _gitCommandManager.EnsureGitVersion(_minGitVersionSupportAuthHeader, throwOnNotMatch: false);
            }
        }

        // When the repository is a TfsGit, figure out the endpoint is hosted vsts git or on-prem tfs git
        // if repository is on-prem tfs git, make sure git version greater than 2.9
        // we have to use http.extraheader option to provide auth header for on-prem tfs git
        public override void RequirementCheck(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            ArgUtil.NotNull(_gitCommandManager, nameof(_gitCommandManager));
            var selfManageGitCreds = executionContext.Variables.GetBoolean(Constants.Variables.System.SelfManageGitCreds) ?? false;
            if (selfManageGitCreds)
            {
                // Customer choose to own git creds by themselves, we don't have to worry about git version anymore.
                return;
            }

            // Since that variable is added around TFS 2015 Qu2.
            // Old TFS AT will not send this variable to build agent, and VSTS will always send it to build agent.
            bool? onPremTfsGit = true;
            string onPremTfsGitString;
            if (endpoint.Data.TryGetValue(WellKnownEndpointData.OnPremTfsGit, out onPremTfsGitString))
            {
                onPremTfsGit = StringUtil.ConvertToBoolean(onPremTfsGitString);
            }

            // only ensure git version for on-prem tfsgit.
            if (onPremTfsGit.Value)
            {
                _gitCommandManager.EnsureGitVersion(_minGitVersionSupportAuthHeader, throwOnNotMatch: true);
            }
        }

        public override string GenerateAuthHeader(string username, string password)
        {
            // tfsgit use bearer auth header with JWToken from systemconnection.
            ArgUtil.NotNullOrEmpty(password, nameof(password));
            return $"bearer {password}";
        }
    }

    public abstract class GitSourceProvider : SourceProvider, ISourceProvider
    {
        // refs prefix
        // TODO: how to deal with limited refs?
        private const string _refsPrefix = "refs/heads/";
        private const string _remoteRefsPrefix = "refs/remotes/origin/";
        private const string _pullRefsPrefix = "refs/pull/";
        private const string _remotePullRefsPrefix = "refs/remotes/pull/";
        private readonly Dictionary<string, string> _configModifications = new Dictionary<string, string>();
        private bool _selfManageGitCreds = false;
        private Uri _repositoryUrlWithCred = null;
        private Uri _proxyUrlWithCred = null;
        private string _proxyUrlWithCredString = null;
        private Uri _gitLfsUrlWithCred = null;

        protected IGitCommandManager _gitCommandManager;

        // min git version that support add extra auth header.
        protected Version _minGitVersionSupportAuthHeader = new Version(2, 9);

        public abstract bool UseAuthHeaderCmdlineArg { get; }
        public abstract void RequirementCheck(IExecutionContext executionContext, ServiceEndpoint endpoint);
        public abstract string GenerateAuthHeader(string username, string password);

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

            int fetchDepth = 0;
            if (endpoint.Data.ContainsKey("fetchDepth") &&
                (!int.TryParse(endpoint.Data["fetchDepth"], out fetchDepth) || fetchDepth < 0))
            {
                fetchDepth = 0;
            }
            // prefer feature variable over endpoint data
            fetchDepth = executionContext.Variables.GetInt(Constants.Variables.Features.GitShallowDepth) ?? fetchDepth;

            bool gitLfsSupport = false;
            if (endpoint.Data.ContainsKey("GitLfsSupport"))
            {
                gitLfsSupport = StringUtil.ConvertToBoolean(endpoint.Data["GitLfsSupport"]);
            }
            // prefer feature variable over endpoint data
            gitLfsSupport = executionContext.Variables.GetBoolean(Constants.Variables.Features.GitLfsSupport) ?? gitLfsSupport;

            bool exposeCred = executionContext.Variables.GetBoolean(Constants.Variables.System.EnableAccessToken) ?? false;

            Trace.Info($"Repository url={repositoryUrl}");
            Trace.Info($"targetPath={targetPath}");
            Trace.Info($"sourceBranch={sourceBranch}");
            Trace.Info($"sourceVersion={sourceVersion}");
            Trace.Info($"clean={clean}");
            Trace.Info($"checkoutSubmodules={checkoutSubmodules}");
            Trace.Info($"exposeCred={exposeCred}");
            Trace.Info($"fetchDepth={fetchDepth}");
            Trace.Info($"gitLfsSupport={gitLfsSupport}");

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

            // Make sure the build machine met all requirements for the git repository
            // For now, the only requirement we have is git version greater than 2.9 for on-prem tfsgit
            RequirementCheck(executionContext, endpoint);

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

            // prepare credentail embedded urls
            _repositoryUrlWithCred = UrlUtil.GetCredentialEmbeddedUrl(repositoryUrl, username, password);
            if (!string.IsNullOrEmpty(executionContext.Variables.Agent_ProxyUrl))
            {
                _proxyUrlWithCred = UrlUtil.GetCredentialEmbeddedUrl(new Uri(executionContext.Variables.Agent_ProxyUrl), executionContext.Variables.Agent_ProxyUsername, executionContext.Variables.Agent_ProxyPassword);

                // uri.absoluteuri will not contains port info if the scheme is http/https and the port is 80/443
                // however, git.exe always require you provide port info, if nothing passed in, it will use 1080 as default
                // as result, we need prefer the uri.originalstring when it's different than uri.absoluteuri.
                if (string.Equals(_proxyUrlWithCred.AbsoluteUri, _proxyUrlWithCred.OriginalString, StringComparison.OrdinalIgnoreCase))
                {
                    _proxyUrlWithCredString = _proxyUrlWithCred.AbsoluteUri;
                }
                else
                {
                    _proxyUrlWithCredString = _proxyUrlWithCred.OriginalString;
                }
            }

            if (gitLfsSupport)
            {
                // Construct git-lfs url
                UriBuilder gitLfsUrl = new UriBuilder(_repositoryUrlWithCred);
                if (gitLfsUrl.Path.EndsWith(".git"))
                {
                    gitLfsUrl.Path = gitLfsUrl.Path + "/info/lfs";
                }
                else
                {
                    gitLfsUrl.Path = gitLfsUrl.Path + ".git/info/lfs";
                }

                _gitLfsUrlWithCred = gitLfsUrl.Uri;
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
                await RemoveGitConfig(executionContext, targetPath, $"http.{repositoryUrl.AbsoluteUri}.extraheader", string.Empty);
            }

            // always remove any possible left proxy setting from git config, the proxy setting may contains credential
            if (await _gitCommandManager.GitConfigExist(executionContext, targetPath, $"http.proxy"))
            {
                executionContext.Debug("Remove any proxy setting from git config.");
                await RemoveGitConfig(executionContext, targetPath, $"http.proxy", string.Empty);
            }

            List<string> additionalFetchArgs = new List<string>();
            if (!_selfManageGitCreds)
            {
                // v2.9 git support provide auth header as cmdline arg. 
                // as long 2.9 git exist, VSTS repo, TFS repo and Github repo will use this to handle auth challenge. 
                if (UseAuthHeaderCmdlineArg)
                {
                    additionalFetchArgs.Add($"-c http.extraheader=\"AUTHORIZATION: {GenerateAuthHeader(username, password)}\"");
                }
                else
                {
                    // Otherwise, inject credential into fetch/push url
                    // inject credential into fetch url
                    executionContext.Debug("Inject credential into git remote url.");
                    ArgUtil.NotNull(_repositoryUrlWithCred, nameof(_repositoryUrlWithCred));

                    // inject credential into fetch url
                    executionContext.Debug("Inject credential into git remote fetch url.");
                    int exitCode_seturl = await _gitCommandManager.GitRemoteSetUrl(executionContext, targetPath, "origin", _repositoryUrlWithCred.AbsoluteUri);
                    if (exitCode_seturl != 0)
                    {
                        throw new InvalidOperationException($"Unable to use git.exe inject credential to git remote fetch url, 'git remote set-url' failed with exit code: {exitCode_seturl}");
                    }

                    // inject credential into push url
                    executionContext.Debug("Inject credential into git remote push url.");
                    exitCode_seturl = await _gitCommandManager.GitRemoteSetPushUrl(executionContext, targetPath, "origin", _repositoryUrlWithCred.AbsoluteUri);
                    if (exitCode_seturl != 0)
                    {
                        throw new InvalidOperationException($"Unable to use git.exe inject credential to git remote push url, 'git remote set-url --push' failed with exit code: {exitCode_seturl}");
                    }
                }

                // Prepare proxy config for fetch.
                if (!string.IsNullOrEmpty(executionContext.Variables.Agent_ProxyUrl))
                {
                    executionContext.Debug($"Config proxy server '{executionContext.Variables.Agent_ProxyUrl}' for git fetch.");
                    ArgUtil.NotNullOrEmpty(_proxyUrlWithCredString, nameof(_proxyUrlWithCredString));
                    additionalFetchArgs.Add($"-c http.proxy=\"{_proxyUrlWithCredString}\"");
                }

                // Prepare gitlfs url for fetch and checkout
                if (gitLfsSupport)
                {
                    // Initialize git lfs by execute 'git lfs install'
                    executionContext.Debug("Setup the global Git hooks for Git LFS.");
                    int exitCode_lfsInstall = await _gitCommandManager.GitLFSInstall(executionContext, targetPath);
                    if (exitCode_lfsInstall != 0)
                    {
                        throw new InvalidOperationException($"Git-lfs installation failed with exit code: {exitCode_lfsInstall}");
                    }

                    // Inject credential into lfs fetch/push url
                    executionContext.Debug("Inject credential into git-lfs remote url.");
                    ArgUtil.NotNull(_gitLfsUrlWithCred, nameof(_gitLfsUrlWithCred));

                    // inject credential into fetch url
                    executionContext.Debug("Inject credential into git-lfs remote fetch url.");
                    _configModifications["remote.origin.lfsurl"] = _gitLfsUrlWithCred.AbsoluteUri;
                    int exitCode_configlfsurl = await _gitCommandManager.GitConfig(executionContext, targetPath, "remote.origin.lfsurl", _gitLfsUrlWithCred.AbsoluteUri);
                    if (exitCode_configlfsurl != 0)
                    {
                        throw new InvalidOperationException($"Git config failed with exit code: {exitCode_configlfsurl}");
                    }

                    // inject credential into push url
                    executionContext.Debug("Inject credential into git-lfs remote push url.");
                    _configModifications["remote.origin.lfspushurl"] = _gitLfsUrlWithCred.AbsoluteUri;
                    int exitCode_configlfspushurl = await _gitCommandManager.GitConfig(executionContext, targetPath, "remote.origin.lfspushurl", _gitLfsUrlWithCred.AbsoluteUri);
                    if (exitCode_configlfspushurl != 0)
                    {
                        throw new InvalidOperationException($"Git config failed with exit code: {exitCode_configlfspushurl}");
                    }
                }
            }

            // If this is a build for a pull request, then include
            // the pull request reference as an additional ref.
            List<string> additionalFetchSpecs = new List<string>();
            if (IsPullRequest(sourceBranch))
            {
                additionalFetchSpecs.Add("+refs/heads/*:refs/remotes/origin/*");
                additionalFetchSpecs.Add(StringUtil.Format("+{0}:{1}", sourceBranch, GetRemoteRefName(sourceBranch)));
            }

            int exitCode_fetch = await _gitCommandManager.GitFetch(executionContext, targetPath, "origin", fetchDepth, additionalFetchSpecs, string.Join(" ", additionalFetchArgs), cancellationToken);
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
                // local repository is shallow repository, checkout may fail due to lack of commits history.
                // this will happen when the checkout commit is older than tip -> fetchDepth
                if (fetchDepth > 0)
                {
                    executionContext.Warning(StringUtil.Loc("ShallowCheckoutFail", fetchDepth, sourcesToBuild));
                }

                throw new InvalidOperationException($"Git checkout failed with exit code: {exitCode_checkout}");
            }

            // Submodule update
            if (checkoutSubmodules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                executionContext.Progress(90, "Updating submodules...");
                List<string> additionalSubmoduleUpdateArgs = new List<string>();
                if (!_selfManageGitCreds)
                {
                    if (UseAuthHeaderCmdlineArg)
                    {
                        string authorityUrl = repositoryUrl.AbsoluteUri.Replace(repositoryUrl.PathAndQuery, string.Empty);
                        additionalSubmoduleUpdateArgs.Add($"-c http.{authorityUrl}.extraheader=\"AUTHORIZATION: {GenerateAuthHeader(username, password)}\"");
                    }

                    // Prepare proxy config for submodule update.
                    if (!string.IsNullOrEmpty(executionContext.Variables.Agent_ProxyUrl))
                    {
                        executionContext.Debug($"Config proxy server '{executionContext.Variables.Agent_ProxyUrl}' for git submodule update.");
                        ArgUtil.NotNullOrEmpty(_proxyUrlWithCredString, nameof(_proxyUrlWithCredString));
                        additionalSubmoduleUpdateArgs.Add($"-c http.proxy=\"{_proxyUrlWithCredString}\"");
                    }
                }

                int exitCode_submoduleUpdate = await _gitCommandManager.GitSubmoduleUpdate(executionContext, targetPath, string.Join(" ", additionalSubmoduleUpdateArgs), cancellationToken);
                if (exitCode_submoduleUpdate != 0)
                {
                    throw new InvalidOperationException($"Git submodule update failed with exit code: {exitCode_submoduleUpdate}");
                }
            }

            // handle expose creds, related to 'Allow Scripts to Access OAuth Token' option
            if (!_selfManageGitCreds)
            {
                if (UseAuthHeaderCmdlineArg && exposeCred)
                {
                    string configKey = $"http.{repositoryUrl.AbsoluteUri}.extraheader";
                    string configValue = $"\"AUTHORIZATION: {GenerateAuthHeader(username, password)}\"";
                    _configModifications[configKey] = configValue.Trim('\"');
                    int exitCode_config = await _gitCommandManager.GitConfig(executionContext, targetPath, configKey, configValue);
                    if (exitCode_config != 0)
                    {
                        throw new InvalidOperationException($"Git config failed with exit code: {exitCode_config}");
                    }
                }

                if (!UseAuthHeaderCmdlineArg && !exposeCred)
                {
                    // remove cached credential from origin's fetch/push url.
                    await RemoveCachedCredential(executionContext, targetPath, repositoryUrl, "origin");
                }

                if (exposeCred)
                {
                    // save proxy setting to git config.
                    if (!string.IsNullOrEmpty(executionContext.Variables.Agent_ProxyUrl))
                    {
                        executionContext.Debug($"Save proxy config for proxy server '{executionContext.Variables.Agent_ProxyUrl}' into git config.");
                        ArgUtil.NotNullOrEmpty(_proxyUrlWithCredString, nameof(_proxyUrlWithCredString));

                        string proxyConfigKey = "http.proxy";
                        string proxyConfigValue = $"\"{_proxyUrlWithCredString}\"";
                        _configModifications[proxyConfigKey] = proxyConfigValue.Trim('\"');

                        int exitCode_proxyconfig = await _gitCommandManager.GitConfig(executionContext, targetPath, proxyConfigKey, proxyConfigValue);
                        if (exitCode_proxyconfig != 0)
                        {
                            throw new InvalidOperationException($"Git config failed with exit code: {exitCode_proxyconfig}");
                        }
                    }
                }

                if (gitLfsSupport && !exposeCred)
                {
                    executionContext.Debug("Remove git-lfs fetch and push url setting from git config.");
                    await RemoveGitConfig(executionContext, targetPath, "remote.origin.lfsurl", _gitLfsUrlWithCred.AbsoluteUri);
                    _configModifications.Remove("remote.origin.lfsurl");
                    await RemoveGitConfig(executionContext, targetPath, "remote.origin.lfspushurl", _gitLfsUrlWithCred.AbsoluteUri);
                    _configModifications.Remove("remote.origin.lfspushurl");
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
                executionContext.Debug("Remove any extraheader and proxy setting from git config.");
                foreach (var config in _configModifications)
                {
                    await RemoveGitConfig(executionContext, targetPath, config.Key, config.Value);
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

        private async Task RemoveGitConfig(IExecutionContext executionContext, string targetPath, string configKey, string configValue)
        {
            int exitCode_configUnset = await _gitCommandManager.GitConfigUnset(executionContext, targetPath, configKey);
            if (exitCode_configUnset != 0)
            {
                // if unable to use git.exe unset http.extraheader or http.proxy, modify git config file on disk. make sure we don't left credential.
                if (!string.IsNullOrEmpty(configValue))
                {
                    executionContext.Warning(StringUtil.Loc("AttemptRemoveCredFromConfig"));
                    string gitConfig = Path.Combine(targetPath, ".git/config");
                    if (File.Exists(gitConfig))
                    {
                        string gitConfigContent = File.ReadAllText(Path.Combine(targetPath, ".git", "config"));
                        if (gitConfigContent.Contains(configKey))
                        {
                            string setting = $"extraheader = {configValue}";
                            gitConfigContent = Regex.Replace(gitConfigContent, setting, string.Empty, RegexOptions.IgnoreCase);

                            setting = $"proxy = {configValue}";
                            gitConfigContent = Regex.Replace(gitConfigContent, setting, string.Empty, RegexOptions.IgnoreCase);

                            File.WriteAllText(gitConfig, gitConfigContent);
                        }
                    }
                }
                else
                {
                    executionContext.Warning(StringUtil.Loc("FailToRemoveGitConfig", configKey, configKey, targetPath));
                }
            }
        }

        private async Task RemoveCachedCredential(IExecutionContext context, string repositoryPath, Uri repositoryUrl, string remoteName)
        {
            // there is nothing cached in repository Url.
            if (_repositoryUrlWithCred == null)
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
                    gitConfigContent = gitConfigContent.Replace(_repositoryUrlWithCred.AbsoluteUri, repositoryUrl.AbsoluteUri);
                    File.WriteAllText(gitConfig, gitConfigContent);
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