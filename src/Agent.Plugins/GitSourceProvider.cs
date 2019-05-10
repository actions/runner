using Microsoft.TeamFoundation.Build.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;
using Agent.Sdk;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent;

namespace Agent.Plugins.Repository
{
    public class ExternalGitSourceProvider : GitSourceProvider
    {
        public override bool GitSupportsFetchingCommitBySha1Hash
        {
            get
            {
                return false;
            }
        }

        // external git repository won't use auth header cmdline arg, since we don't know the auth scheme.
        public override bool GitSupportUseAuthHeader(AgentTaskPluginExecutionContext executionContext, GitCliManager gitCommandManager)
        {
            return false;
        }

        public override bool GitLfsSupportUseAuthHeader(AgentTaskPluginExecutionContext executionContext, GitCliManager gitCommandManager)
        {
            return false;
        }

        public override void RequirementCheck(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository, GitCliManager gitCommandManager)
        {
#if OS_WINDOWS
            // check git version for SChannel SSLBackend (Windows Only)
            bool schannelSslBackend = StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("agent.gituseschannel")?.Value);
            if (schannelSslBackend)
            {
                gitCommandManager.EnsureGitVersion(_minGitVersionSupportSSLBackendOverride, throwOnNotMatch: true);
            }
#endif
        }

        public override string GenerateAuthHeader(AgentTaskPluginExecutionContext executionContext, string username, string password)
        {
            // can't generate auth header for external git. 
            throw new NotSupportedException(nameof(ExternalGitSourceProvider.GenerateAuthHeader));
        }
    }

    public abstract class AuthenticatedGitSourceProvider : GitSourceProvider
    {
        public override bool GitSupportUseAuthHeader(AgentTaskPluginExecutionContext executionContext, GitCliManager gitCommandManager)
        {
            // v2.9 git exist use auth header.
            return gitCommandManager.EnsureGitVersion(_minGitVersionSupportAuthHeader, throwOnNotMatch: false);
        }

        public override bool GitLfsSupportUseAuthHeader(AgentTaskPluginExecutionContext executionContext, GitCliManager gitCommandManager)
        {
            // v2.1 git-lfs exist use auth header.
            return gitCommandManager.EnsureGitLFSVersion(_minGitLfsVersionSupportAuthHeader, throwOnNotMatch: false);
        }

        public override void RequirementCheck(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository, GitCliManager gitCommandManager)
        {
#if OS_WINDOWS
            // check git version for SChannel SSLBackend (Windows Only)
            bool schannelSslBackend = StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("agent.gituseschannel")?.Value);
            if (schannelSslBackend)
            {
                gitCommandManager.EnsureGitVersion(_minGitVersionSupportSSLBackendOverride, throwOnNotMatch: true);
            }
#endif
        }

        public override string GenerateAuthHeader(AgentTaskPluginExecutionContext executionContext, string username, string password)
        {
            // use basic auth header with username:password in base64encoding. 
            string authHeader = $"{username ?? string.Empty}:{password ?? string.Empty}";
            string base64encodedAuthHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(authHeader));

            // add base64 encoding auth header into secretMasker.
            executionContext.SetSecret(base64encodedAuthHeader);
            return $"basic {base64encodedAuthHeader}";
        }
    }

    public sealed class BitbucketGitSourceProvider : AuthenticatedGitSourceProvider
    {
        public override bool GitSupportsFetchingCommitBySha1Hash
        {
            get
            {
                return true;
            }
        }
    }

    public sealed class GitHubSourceProvider : AuthenticatedGitSourceProvider
    {
        public override bool GitSupportsFetchingCommitBySha1Hash
        {
            get
            {
                return false;
            }
        }
    }

    public sealed class TfsGitSourceProvider : GitSourceProvider
    {
        public override bool GitSupportsFetchingCommitBySha1Hash
        {
            get
            {
                return true;
            }
        }

        public override bool GitSupportUseAuthHeader(AgentTaskPluginExecutionContext executionContext, GitCliManager gitCommandManager)
        {
            // v2.9 git exist use auth header for tfsgit repository.
            return gitCommandManager.EnsureGitVersion(_minGitVersionSupportAuthHeader, throwOnNotMatch: false);
        }

        public override bool GitLfsSupportUseAuthHeader(AgentTaskPluginExecutionContext executionContext, GitCliManager gitCommandManager)
        {
            // v2.1 git-lfs exist use auth header for github repository.
            return gitCommandManager.EnsureGitLFSVersion(_minGitLfsVersionSupportAuthHeader, throwOnNotMatch: false);
        }

        // When the repository is a TfsGit, figure out the endpoint is hosted vsts git or on-prem tfs git
        // if repository is on-prem tfs git, make sure git version greater than 2.9
        // we have to use http.extraheader option to provide auth header for on-prem tfs git
        public override void RequirementCheck(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository, GitCliManager gitCommandManager)
        {
            var selfManageGitCreds = StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("system.selfmanagegitcreds")?.Value);
            if (selfManageGitCreds)
            {
                // Customer choose to own git creds by themselves, we don't have to worry about git version anymore.
                return;
            }

            // Since that variable is added around TFS 2015 Qu2.
            // Old TFS AT will not send this variable to build agent, and VSTS will always send it to build agent.
            bool onPremTfsGit = !String.Equals(executionContext.Variables.GetValueOrDefault(WellKnownDistributedTaskVariables.ServerType)?.Value, "Hosted", StringComparison.OrdinalIgnoreCase);

            // ensure git version and git-lfs version for on-prem tfsgit.
            if (onPremTfsGit)
            {
                gitCommandManager.EnsureGitVersion(_minGitVersionSupportAuthHeader, throwOnNotMatch: true);

                bool gitLfsSupport = StringUtil.ConvertToBoolean(executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs));
                // prefer feature variable over endpoint data
                if (executionContext.Variables.GetValueOrDefault("agent.source.git.lfs") != null)
                {
                    gitLfsSupport = StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("agent.source.git.lfs")?.Value);
                }

                if (gitLfsSupport)
                {
                    gitCommandManager.EnsureGitLFSVersion(_minGitLfsVersionSupportAuthHeader, throwOnNotMatch: true);
                }
            }

#if OS_WINDOWS
            // check git version for SChannel SSLBackend (Windows Only)
            bool schannelSslBackend = StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("agent.gituseschannel")?.Value);
            if (schannelSslBackend)
            {
                gitCommandManager.EnsureGitVersion(_minGitVersionSupportSSLBackendOverride, throwOnNotMatch: true);
            }
#endif
        }

        public override string GenerateAuthHeader(AgentTaskPluginExecutionContext executionContext, string username, string password)
        {
            // tfsgit use bearer auth header with JWToken from systemconnection.
            ArgUtil.NotNullOrEmpty(password, nameof(password));
            return $"bearer {password}";
        }
    }

    public abstract class GitSourceProvider : ISourceProvider
    {
        // refs prefix
        // TODO: how to deal with limited refs?
        private const string _refsPrefix = "refs/heads/";
        private const string _remoteRefsPrefix = "refs/remotes/origin/";
        private const string _pullRefsPrefix = "refs/pull/";
        private const string _remotePullRefsPrefix = "refs/remotes/pull/";

        // min git version that support add extra auth header.
        protected Version _minGitVersionSupportAuthHeader = new Version(2, 9);

#if OS_WINDOWS
        // min git version that support override sslBackend setting.
        protected Version _minGitVersionSupportSSLBackendOverride = new Version(2, 14, 2);
#endif

        // min git-lfs version that support add extra auth header.
        protected Version _minGitLfsVersionSupportAuthHeader = new Version(2, 1);

        public abstract bool GitSupportUseAuthHeader(AgentTaskPluginExecutionContext executionContext, GitCliManager gitCommandManager);
        public abstract bool GitLfsSupportUseAuthHeader(AgentTaskPluginExecutionContext executionContext, GitCliManager gitCommandManager);
        public abstract void RequirementCheck(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository, GitCliManager gitCommandManager);
        public abstract string GenerateAuthHeader(AgentTaskPluginExecutionContext executionContext, string username, string password);

        public abstract bool GitSupportsFetchingCommitBySha1Hash { get; }

        public async Task GetSourceAsync(
            AgentTaskPluginExecutionContext executionContext,
            Pipelines.RepositoryResource repository,
            CancellationToken cancellationToken)
        {
            // Validate args.
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(repository, nameof(repository));

            Dictionary<string, string> configModifications = new Dictionary<string, string>();
            bool selfManageGitCreds = false;
            Uri repositoryUrlWithCred = null;
            Uri proxyUrlWithCred = null;
            string proxyUrlWithCredString = null;
            Uri gitLfsUrlWithCred = null;
            bool useSelfSignedCACert = false;
            bool useClientCert = false;
            string clientCertPrivateKeyAskPassFile = null;
            bool acceptUntrustedCerts = false;

            executionContext.Output($"Syncing repository: {repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Name)} ({repository.Type})");
            Uri repositoryUrl = repository.Url;
            if (!repositoryUrl.IsAbsoluteUri)
            {
                throw new InvalidOperationException("Repository url need to be an absolute uri.");
            }

            string targetPath = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
            string sourceBranch = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Ref);
            string sourceVersion = repository.Version;

            bool clean = StringUtil.ConvertToBoolean(executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Clean));

            // input Submodules can be ['', true, false, recursive]
            // '' or false indicate don't checkout submodules
            // true indicate checkout top level submodules
            // recursive indicate checkout submodules recursively 
            bool checkoutSubmodules = false;
            bool checkoutNestedSubmodules = false;
            string submoduleInput = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules);
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

            // retrieve credential from endpoint.
            ServiceEndpoint endpoint = null;
            if (repository.Endpoint != null)
            {
                // the endpoint should either be the SystemVssConnection (id = guild.empty, name = SystemVssConnection)
                // or a real service endpoint to external service which has a real id
                endpoint = executionContext.Endpoints.Single(
                    x => (repository.Endpoint.Id != Guid.Empty && x.Id == repository.Endpoint.Id) ||
                    (repository.Endpoint.Id == Guid.Empty && string.Equals(x.Name, repository.Endpoint.Name.ToString(), StringComparison.OrdinalIgnoreCase)));
            }

            if (endpoint != null && endpoint.Data.TryGetValue(EndpointData.AcceptUntrustedCertificates, out string endpointAcceptUntrustedCerts))
            {
                acceptUntrustedCerts = StringUtil.ConvertToBoolean(endpointAcceptUntrustedCerts);
            }

            var agentCert = executionContext.GetCertConfiguration();
            acceptUntrustedCerts = acceptUntrustedCerts || (agentCert?.SkipServerCertificateValidation ?? false);

            int fetchDepth = 0;
            if (!int.TryParse(executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth), out fetchDepth) || fetchDepth < 0)
            {
                fetchDepth = 0;
            }

            // prefer feature variable over endpoint data
            if (int.TryParse(executionContext.Variables.GetValueOrDefault("agent.source.git.shallowFetchDepth")?.Value, out int fetchDepthOverwrite) && fetchDepthOverwrite >= 0)
            {
                fetchDepth = fetchDepthOverwrite;
            }

            bool gitLfsSupport = StringUtil.ConvertToBoolean(executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs));
            // prefer feature variable over endpoint data
            if (executionContext.Variables.GetValueOrDefault("agent.source.git.lfs") != null)
            {
                gitLfsSupport = StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("agent.source.git.lfs")?.Value);
            }

            bool exposeCred = StringUtil.ConvertToBoolean(executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials));

            // Read 'disable fetch by commit' value from the execution variable first, then from the environment variable if the first one is not set
            bool fetchByCommit = !StringUtil.ConvertToBoolean(
                executionContext.Variables.GetValueOrDefault("VSTS.DisableFetchByCommit")?.Value ??
                System.Environment.GetEnvironmentVariable("VSTS_DISABLEFETCHBYCOMMIT"), false);

            executionContext.Debug($"repository url={repositoryUrl}");
            executionContext.Debug($"targetPath={targetPath}");
            executionContext.Debug($"sourceBranch={sourceBranch}");
            executionContext.Debug($"sourceVersion={sourceVersion}");
            executionContext.Debug($"clean={clean}");
            executionContext.Debug($"checkoutSubmodules={checkoutSubmodules}");
            executionContext.Debug($"checkoutNestedSubmodules={checkoutNestedSubmodules}");
            executionContext.Debug($"exposeCred={exposeCred}");
            executionContext.Debug($"fetchDepth={fetchDepth}");
            executionContext.Debug($"gitLfsSupport={gitLfsSupport}");
            executionContext.Debug($"acceptUntrustedCerts={acceptUntrustedCerts}");

            bool preferGitFromPath;
#if OS_WINDOWS
            bool schannelSslBackend = StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("agent.gituseschannel")?.Value);
            executionContext.Debug($"schannelSslBackend={schannelSslBackend}");

            // Determine which git will be use
            // On windows, we prefer the built-in portable git within the agent's externals folder, 
            // set system.prefergitfrompath=true can change the behavior, agent will find git.exe from %PATH%
            var definitionSetting = executionContext.Variables.GetValueOrDefault("system.prefergitfrompath");
            if (definitionSetting != null)
            {
                preferGitFromPath = StringUtil.ConvertToBoolean(definitionSetting.Value);
            }
            else
            {
                bool.TryParse(Environment.GetEnvironmentVariable("system.prefergitfrompath"), out preferGitFromPath);
            }
#else
            // On Linux, we will always use git find in %PATH% regardless of system.prefergitfrompath
            preferGitFromPath = true;
#endif
            
            // we don't package git with for github action
            if (BuildConstants.AgentPackage.Product == "Github")
            {
                preferGitFromPath = true;
            }

            // Determine do we need to provide creds to git operation
            selfManageGitCreds = StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("system.selfmanagegitcreds")?.Value);
            if (selfManageGitCreds)
            {
                // Customer choose to own git creds by themselves.
                executionContext.Output(StringUtil.Loc("SelfManageGitCreds"));
            }

            // Initialize git command manager with additional environment variables.
            Dictionary<string, string> gitEnv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
            await gitCommandManager.LoadGitExecutionInfo(executionContext, useBuiltInGit: !preferGitFromPath);

            bool gitSupportAuthHeader = GitSupportUseAuthHeader(executionContext, gitCommandManager);

            // Make sure the build machine met all requirements for the git repository
            // For now, the requirement we have are:
            // 1. git version greater than 2.9  and git-lfs version greater than 2.1 for on-prem tfsgit
            // 2. git version greater than 2.14.2 if use SChannel for SSL backend (Windows only)
            RequirementCheck(executionContext, repository, gitCommandManager);

            string username = string.Empty;
            string password = string.Empty;
            if (!selfManageGitCreds && endpoint != null && endpoint.Authorization != null)
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
                    case EndpointAuthorizationSchemes.PersonalAccessToken:
                        username = EndpointAuthorizationSchemes.PersonalAccessToken;
                        if (!endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out password))
                        {
                            password = string.Empty;
                        }
                        break;
                    case EndpointAuthorizationSchemes.Token:
                        username = "x-access-token";
                        if (!endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out password))
                        {
                            username = EndpointAuthorizationSchemes.Token;
                            if (!endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.ApiToken, out password))
                            {
                                password = string.Empty;
                            }
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
            repositoryUrlWithCred = UrlUtil.GetCredentialEmbeddedUrl(repositoryUrl, username, password);
            var agentProxy = executionContext.GetProxyConfiguration();
            if (agentProxy != null && !string.IsNullOrEmpty(agentProxy.ProxyAddress) && !agentProxy.WebProxy.IsBypassed(repositoryUrl))
            {
                proxyUrlWithCred = UrlUtil.GetCredentialEmbeddedUrl(new Uri(agentProxy.ProxyAddress), agentProxy.ProxyUsername, agentProxy.ProxyPassword);

                // uri.absoluteuri will not contains port info if the scheme is http/https and the port is 80/443
                // however, git.exe always require you provide port info, if nothing passed in, it will use 1080 as default
                // as result, we need prefer the uri.originalstring when it's different than uri.absoluteuri.
                if (string.Equals(proxyUrlWithCred.AbsoluteUri, proxyUrlWithCred.OriginalString, StringComparison.OrdinalIgnoreCase))
                {
                    proxyUrlWithCredString = proxyUrlWithCred.AbsoluteUri;
                }
                else
                {
                    proxyUrlWithCredString = proxyUrlWithCred.OriginalString;
                }
            }

            // prepare askpass for client cert private key, if the repository's endpoint url match the TFS/VSTS url
            var systemConnection = executionContext.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            if (agentCert != null && Uri.Compare(repositoryUrl, systemConnection.Url, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (!string.IsNullOrEmpty(agentCert.CACertificateFile))
                {
                    useSelfSignedCACert = true;
                }

                if (!string.IsNullOrEmpty(agentCert.ClientCertificateFile) &&
                    !string.IsNullOrEmpty(agentCert.ClientCertificatePrivateKeyFile))
                {
                    useClientCert = true;

                    // prepare askpass for client cert password
                    if (!string.IsNullOrEmpty(agentCert.ClientCertificatePassword))
                    {
                        clientCertPrivateKeyAskPassFile = Path.Combine(executionContext.Variables["agent.tempdirectory"].Value, $"{Guid.NewGuid()}.sh");
                        List<string> askPass = new List<string>();
                        askPass.Add("#!/bin/sh");
                        askPass.Add($"echo \"{agentCert.ClientCertificatePassword}\"");
                        File.WriteAllLines(clientCertPrivateKeyAskPassFile, askPass);

#if !OS_WINDOWS
                        string toolPath = WhichUtil.Which("chmod", true);
                        string argLine = $"775 {clientCertPrivateKeyAskPassFile}";
                        executionContext.Command($"chmod {argLine}");

                        var processInvoker = new ProcessInvoker(executionContext);
                        processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs args) =>
                        {
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                executionContext.Output(args.Data);
                            }
                        };
                        processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs args) =>
                        {
                            if (!string.IsNullOrEmpty(args.Data))
                            {
                                executionContext.Output(args.Data);
                            }
                        };

                        await processInvoker.ExecuteAsync(executionContext.Variables.GetValueOrDefault("system.defaultworkingdirectory")?.Value, toolPath, argLine, null, true, CancellationToken.None);
#endif
                    }
                }
            }

            if (gitLfsSupport)
            {
                // Construct git-lfs url
                UriBuilder gitLfsUrl = new UriBuilder(repositoryUrlWithCred);
                if (gitLfsUrl.Path.EndsWith(".git"))
                {
                    gitLfsUrl.Path = gitLfsUrl.Path + "/info/lfs";
                }
                else
                {
                    gitLfsUrl.Path = gitLfsUrl.Path + ".git/info/lfs";
                }

                gitLfsUrlWithCred = gitLfsUrl.Uri;
            }

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
            executionContext.Progress(0, "Starting fetch...");

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

            // always remove any possible left proxy setting from git config, the proxy setting may contains credential
            if (await gitCommandManager.GitConfigExist(executionContext, targetPath, $"http.proxy"))
            {
                executionContext.Debug("Remove any proxy setting from git config.");
                await RemoveGitConfig(executionContext, gitCommandManager, targetPath, $"http.proxy", string.Empty);
            }

            List<string> additionalFetchArgs = new List<string>();
            List<string> additionalLfsFetchArgs = new List<string>();
            if (!selfManageGitCreds)
            {
                // v2.9 git support provide auth header as cmdline arg. 
                // as long 2.9 git exist, VSTS repo, TFS repo and Github repo will use this to handle auth challenge. 
                if (gitSupportAuthHeader)
                {
                    additionalFetchArgs.Add($"-c http.extraheader=\"AUTHORIZATION: {GenerateAuthHeader(executionContext, username, password)}\"");
                }
                else
                {
                    // Otherwise, inject credential into fetch/push url
                    // inject credential into fetch url
                    executionContext.Debug("Inject credential into git remote url.");
                    ArgUtil.NotNull(repositoryUrlWithCred, nameof(repositoryUrlWithCred));

                    // inject credential into fetch url
                    executionContext.Debug("Inject credential into git remote fetch url.");
                    int exitCode_seturl = await gitCommandManager.GitRemoteSetUrl(executionContext, targetPath, "origin", repositoryUrlWithCred.AbsoluteUri);
                    if (exitCode_seturl != 0)
                    {
                        throw new InvalidOperationException($"Unable to use git.exe inject credential to git remote fetch url, 'git remote set-url' failed with exit code: {exitCode_seturl}");
                    }

                    // inject credential into push url
                    executionContext.Debug("Inject credential into git remote push url.");
                    exitCode_seturl = await gitCommandManager.GitRemoteSetPushUrl(executionContext, targetPath, "origin", repositoryUrlWithCred.AbsoluteUri);
                    if (exitCode_seturl != 0)
                    {
                        throw new InvalidOperationException($"Unable to use git.exe inject credential to git remote push url, 'git remote set-url --push' failed with exit code: {exitCode_seturl}");
                    }
                }

                // Prepare proxy config for fetch.
                if (agentProxy != null && !string.IsNullOrEmpty(agentProxy.ProxyAddress) && !agentProxy.WebProxy.IsBypassed(repositoryUrl))
                {
                    executionContext.Debug($"Config proxy server '{agentProxy.ProxyAddress}' for git fetch.");
                    ArgUtil.NotNullOrEmpty(proxyUrlWithCredString, nameof(proxyUrlWithCredString));
                    additionalFetchArgs.Add($"-c http.proxy=\"{proxyUrlWithCredString}\"");
                    additionalLfsFetchArgs.Add($"-c http.proxy=\"{proxyUrlWithCredString}\"");
                }

                // Prepare ignore ssl cert error config for fetch.
                if (acceptUntrustedCerts)
                {
                    additionalFetchArgs.Add($"-c http.sslVerify=false");
                    additionalLfsFetchArgs.Add($"-c http.sslVerify=false");
                }

                // Prepare self-signed CA cert config for fetch from TFS.
                if (useSelfSignedCACert)
                {
                    executionContext.Debug($"Use self-signed certificate '{agentCert.CACertificateFile}' for git fetch.");
                    additionalFetchArgs.Add($"-c http.sslcainfo=\"{agentCert.CACertificateFile}\"");
                    additionalLfsFetchArgs.Add($"-c http.sslcainfo=\"{agentCert.CACertificateFile}\"");
                }

                // Prepare client cert config for fetch from TFS.
                if (useClientCert)
                {
                    executionContext.Debug($"Use client certificate '{agentCert.ClientCertificateFile}' for git fetch.");

                    if (!string.IsNullOrEmpty(clientCertPrivateKeyAskPassFile))
                    {
                        additionalFetchArgs.Add($"-c http.sslcert=\"{agentCert.ClientCertificateFile}\" -c http.sslkey=\"{agentCert.ClientCertificatePrivateKeyFile}\" -c http.sslCertPasswordProtected=true -c core.askpass=\"{clientCertPrivateKeyAskPassFile}\"");
                        additionalLfsFetchArgs.Add($"-c http.sslcert=\"{agentCert.ClientCertificateFile}\" -c http.sslkey=\"{agentCert.ClientCertificatePrivateKeyFile}\" -c http.sslCertPasswordProtected=true -c core.askpass=\"{clientCertPrivateKeyAskPassFile}\"");
                    }
                    else
                    {
                        additionalFetchArgs.Add($"-c http.sslcert=\"{agentCert.ClientCertificateFile}\" -c http.sslkey=\"{agentCert.ClientCertificatePrivateKeyFile}\"");
                        additionalLfsFetchArgs.Add($"-c http.sslcert=\"{agentCert.ClientCertificateFile}\" -c http.sslkey=\"{agentCert.ClientCertificatePrivateKeyFile}\"");
                    }
                }
#if OS_WINDOWS
                if (schannelSslBackend)
                {
                    executionContext.Debug("Use SChannel SslBackend for git fetch.");
                    additionalFetchArgs.Add("-c http.sslbackend=\"schannel\"");
                    additionalLfsFetchArgs.Add("-c http.sslbackend=\"schannel\"");
                }
#endif
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

                    bool lfsSupportAuthHeader = GitLfsSupportUseAuthHeader(executionContext, gitCommandManager);
                    if (lfsSupportAuthHeader)
                    {
                        string authorityUrl = repositoryUrl.AbsoluteUri.Replace(repositoryUrl.PathAndQuery, string.Empty);
                        additionalLfsFetchArgs.Add($"-c http.{authorityUrl}.extraheader=\"AUTHORIZATION: {GenerateAuthHeader(executionContext, username, password)}\"");
                    }
                    else
                    {
                        // Inject credential into lfs fetch/push url
                        executionContext.Debug("Inject credential into git-lfs remote url.");
                        ArgUtil.NotNull(gitLfsUrlWithCred, nameof(gitLfsUrlWithCred));

                        // inject credential into fetch url
                        executionContext.Debug("Inject credential into git-lfs remote fetch url.");
                        configModifications["remote.origin.lfsurl"] = gitLfsUrlWithCred.AbsoluteUri;
                        int exitCode_configlfsurl = await gitCommandManager.GitConfig(executionContext, targetPath, "remote.origin.lfsurl", gitLfsUrlWithCred.AbsoluteUri);
                        if (exitCode_configlfsurl != 0)
                        {
                            throw new InvalidOperationException($"Git config failed with exit code: {exitCode_configlfsurl}");
                        }

                        // inject credential into push url
                        executionContext.Debug("Inject credential into git-lfs remote push url.");
                        configModifications["remote.origin.lfspushurl"] = gitLfsUrlWithCred.AbsoluteUri;
                        int exitCode_configlfspushurl = await gitCommandManager.GitConfig(executionContext, targetPath, "remote.origin.lfspushurl", gitLfsUrlWithCred.AbsoluteUri);
                        if (exitCode_configlfspushurl != 0)
                        {
                            throw new InvalidOperationException($"Git config failed with exit code: {exitCode_configlfspushurl}");
                        }
                    }
                }
            }

            List<string> additionalFetchSpecs = new List<string>();
            if (IsPullRequest(sourceBranch))
            {
                // Build a 'fetch-by-commit' refspec iff the server allows us to do so in the shallow fetch scenario
                // Otherwise, fall back to fetch all branches and pull request ref
                if (fetchDepth > 0 && fetchByCommit && !string.IsNullOrEmpty(sourceVersion))
                {
                    additionalFetchSpecs.Add($"+{sourceVersion}:{_remoteRefsPrefix}{sourceVersion}");
                }
                else
                {
                    additionalFetchSpecs.Add("+refs/heads/*:refs/remotes/origin/*");
                    additionalFetchSpecs.Add($"+{sourceBranch}:{GetRemoteRefName(sourceBranch)}");
                }
            }
            else
            {
                // Build a refspec iff the server allows us to fetch a specific commit in the shallow fetch scenario
                // Otherwise, use the default fetch behavior (i.e. with no refspecs)
                if (fetchDepth > 0 && fetchByCommit && !string.IsNullOrEmpty(sourceVersion))
                {
                    additionalFetchSpecs.Add($"+{sourceVersion}:{_remoteRefsPrefix}{sourceVersion}");
                }
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
                        executionContext.Warning(StringUtil.Loc("ShallowLfsFetchFail", fetchDepth, sourcesToBuild));
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
                    executionContext.Warning(StringUtil.Loc("ShallowCheckoutFail", fetchDepth, sourcesToBuild));
                }

                throw new InvalidOperationException($"Git checkout failed with exit code: {exitCode_checkout}");
            }

            // Submodule update
            if (checkoutSubmodules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                executionContext.Progress(90, "Updating submodules...");

                int exitCode_submoduleSync = await gitCommandManager.GitSubmoduleSync(executionContext, targetPath, checkoutNestedSubmodules, cancellationToken);
                if (exitCode_submoduleSync != 0)
                {
                    throw new InvalidOperationException($"Git submodule sync failed with exit code: {exitCode_submoduleSync}");
                }

                List<string> additionalSubmoduleUpdateArgs = new List<string>();
                if (!selfManageGitCreds)
                {
                    if (gitSupportAuthHeader)
                    {
                        string authorityUrl = repositoryUrl.AbsoluteUri.Replace(repositoryUrl.PathAndQuery, string.Empty);
                        additionalSubmoduleUpdateArgs.Add($"-c http.{authorityUrl}.extraheader=\"AUTHORIZATION: {GenerateAuthHeader(executionContext, username, password)}\"");
                    }

                    // Prepare proxy config for submodule update.
                    if (agentProxy != null && !string.IsNullOrEmpty(agentProxy.ProxyAddress) && !agentProxy.WebProxy.IsBypassed(repositoryUrl))
                    {
                        executionContext.Debug($"Config proxy server '{agentProxy.ProxyAddress}' for git submodule update.");
                        ArgUtil.NotNullOrEmpty(proxyUrlWithCredString, nameof(proxyUrlWithCredString));
                        additionalSubmoduleUpdateArgs.Add($"-c http.proxy=\"{proxyUrlWithCredString}\"");
                    }

                    // Prepare ignore ssl cert error config for fetch.
                    if (acceptUntrustedCerts)
                    {
                        additionalSubmoduleUpdateArgs.Add($"-c http.sslVerify=false");
                    }

                    // Prepare self-signed CA cert config for submodule update.
                    if (useSelfSignedCACert)
                    {
                        executionContext.Debug($"Use self-signed CA certificate '{agentCert.CACertificateFile}' for git submodule update.");
                        string authorityUrl = repositoryUrl.AbsoluteUri.Replace(repositoryUrl.PathAndQuery, string.Empty);
                        additionalSubmoduleUpdateArgs.Add($"-c http.{authorityUrl}.sslcainfo=\"{agentCert.CACertificateFile}\"");
                    }

                    // Prepare client cert config for submodule update.
                    if (useClientCert)
                    {
                        executionContext.Debug($"Use client certificate '{agentCert.ClientCertificateFile}' for git submodule update.");
                        string authorityUrl = repositoryUrl.AbsoluteUri.Replace(repositoryUrl.PathAndQuery, string.Empty);

                        if (!string.IsNullOrEmpty(clientCertPrivateKeyAskPassFile))
                        {
                            additionalSubmoduleUpdateArgs.Add($"-c http.{authorityUrl}.sslcert=\"{agentCert.ClientCertificateFile}\" -c http.{authorityUrl}.sslkey=\"{agentCert.ClientCertificatePrivateKeyFile}\" -c http.{authorityUrl}.sslCertPasswordProtected=true -c core.askpass=\"{clientCertPrivateKeyAskPassFile}\"");
                        }
                        else
                        {
                            additionalSubmoduleUpdateArgs.Add($"-c http.{authorityUrl}.sslcert=\"{agentCert.ClientCertificateFile}\" -c http.{authorityUrl}.sslkey=\"{agentCert.ClientCertificatePrivateKeyFile}\"");
                        }
                    }
#if OS_WINDOWS
                    if (schannelSslBackend)
                    {
                        executionContext.Debug("Use SChannel SslBackend for git submodule update.");
                        additionalSubmoduleUpdateArgs.Add("-c http.sslbackend=\"schannel\"");
                    }
#endif                    
                }

                int exitCode_submoduleUpdate = await gitCommandManager.GitSubmoduleUpdate(executionContext, targetPath, fetchDepth, string.Join(" ", additionalSubmoduleUpdateArgs), checkoutNestedSubmodules, cancellationToken);
                if (exitCode_submoduleUpdate != 0)
                {
                    throw new InvalidOperationException($"Git submodule update failed with exit code: {exitCode_submoduleUpdate}");
                }
            }

            // handle expose creds, related to 'Allow Scripts to Access OAuth Token' option
            if (!selfManageGitCreds)
            {
                if (gitSupportAuthHeader && exposeCred)
                {
                    string configKey = $"http.{repositoryUrl.AbsoluteUri}.extraheader";
                    string configValue = $"\"AUTHORIZATION: {GenerateAuthHeader(executionContext, username, password)}\"";
                    configModifications[configKey] = configValue.Trim('\"');
                    int exitCode_config = await gitCommandManager.GitConfig(executionContext, targetPath, configKey, configValue);
                    if (exitCode_config != 0)
                    {
                        throw new InvalidOperationException($"Git config failed with exit code: {exitCode_config}");
                    }
                }

                if (!gitSupportAuthHeader && !exposeCred)
                {
                    // remove cached credential from origin's fetch/push url.
                    await RemoveCachedCredential(executionContext, gitCommandManager, repositoryUrlWithCred, targetPath, repositoryUrl, "origin");
                }

                if (exposeCred)
                {
                    // save proxy setting to git config.
                    if (agentProxy != null && !string.IsNullOrEmpty(agentProxy.ProxyAddress) && !agentProxy.WebProxy.IsBypassed(repositoryUrl))
                    {
                        executionContext.Debug($"Save proxy config for proxy server '{agentProxy.ProxyAddress}' into git config.");
                        ArgUtil.NotNullOrEmpty(proxyUrlWithCredString, nameof(proxyUrlWithCredString));

                        string proxyConfigKey = "http.proxy";
                        string proxyConfigValue = $"\"{proxyUrlWithCredString}\"";
                        configModifications[proxyConfigKey] = proxyConfigValue.Trim('\"');

                        int exitCode_proxyconfig = await gitCommandManager.GitConfig(executionContext, targetPath, proxyConfigKey, proxyConfigValue);
                        if (exitCode_proxyconfig != 0)
                        {
                            throw new InvalidOperationException($"Git config failed with exit code: {exitCode_proxyconfig}");
                        }
                    }

                    // save ignore ssl cert error setting to git config.
                    if (acceptUntrustedCerts)
                    {
                        executionContext.Debug($"Save ignore ssl cert error config into git config.");
                        string sslVerifyConfigKey = "http.sslVerify";
                        string sslVerifyConfigValue = "\"false\"";
                        configModifications[sslVerifyConfigKey] = sslVerifyConfigValue.Trim('\"');

                        int exitCode_sslconfig = await gitCommandManager.GitConfig(executionContext, targetPath, sslVerifyConfigKey, sslVerifyConfigValue);
                        if (exitCode_sslconfig != 0)
                        {
                            throw new InvalidOperationException($"Git config failed with exit code: {exitCode_sslconfig}");
                        }
                    }

                    // save CA cert setting to git config.
                    if (useSelfSignedCACert)
                    {
                        executionContext.Debug($"Save CA cert config into git config.");
                        string sslCaInfoConfigKey = "http.sslcainfo";
                        string sslCaInfoConfigValue = $"\"{agentCert.CACertificateFile}\"";
                        configModifications[sslCaInfoConfigKey] = sslCaInfoConfigValue.Trim('\"');

                        int exitCode_sslconfig = await gitCommandManager.GitConfig(executionContext, targetPath, sslCaInfoConfigKey, sslCaInfoConfigValue);
                        if (exitCode_sslconfig != 0)
                        {
                            throw new InvalidOperationException($"Git config failed with exit code: {exitCode_sslconfig}");
                        }
                    }

                    // save client cert setting to git config.
                    if (useClientCert)
                    {
                        executionContext.Debug($"Save client cert config into git config.");
                        string sslCertConfigKey = "http.sslcert";
                        string sslCertConfigValue = $"\"{agentCert.ClientCertificateFile}\"";
                        string sslKeyConfigKey = "http.sslkey";
                        string sslKeyConfigValue = $"\"{agentCert.CACertificateFile}\"";
                        configModifications[sslCertConfigKey] = sslCertConfigValue.Trim('\"');
                        configModifications[sslKeyConfigKey] = sslKeyConfigValue.Trim('\"');

                        int exitCode_sslconfig = await gitCommandManager.GitConfig(executionContext, targetPath, sslCertConfigKey, sslCertConfigValue);
                        if (exitCode_sslconfig != 0)
                        {
                            throw new InvalidOperationException($"Git config failed with exit code: {exitCode_sslconfig}");
                        }

                        exitCode_sslconfig = await gitCommandManager.GitConfig(executionContext, targetPath, sslKeyConfigKey, sslKeyConfigValue);
                        if (exitCode_sslconfig != 0)
                        {
                            throw new InvalidOperationException($"Git config failed with exit code: {exitCode_sslconfig}");
                        }

                        // the client private key has a password
                        if (!string.IsNullOrEmpty(clientCertPrivateKeyAskPassFile))
                        {
                            string sslCertPasswordProtectedConfigKey = "http.sslcertpasswordprotected";
                            string sslCertPasswordProtectedConfigValue = "true";
                            string askPassConfigKey = "core.askpass";
                            string askPassConfigValue = $"\"{clientCertPrivateKeyAskPassFile}\"";
                            configModifications[sslCertPasswordProtectedConfigKey] = sslCertPasswordProtectedConfigValue.Trim('\"');
                            configModifications[askPassConfigKey] = askPassConfigValue.Trim('\"');

                            exitCode_sslconfig = await gitCommandManager.GitConfig(executionContext, targetPath, sslCertPasswordProtectedConfigKey, sslCertPasswordProtectedConfigValue);
                            if (exitCode_sslconfig != 0)
                            {
                                throw new InvalidOperationException($"Git config failed with exit code: {exitCode_sslconfig}");
                            }

                            exitCode_sslconfig = await gitCommandManager.GitConfig(executionContext, targetPath, askPassConfigKey, askPassConfigValue);
                            if (exitCode_sslconfig != 0)
                            {
                                throw new InvalidOperationException($"Git config failed with exit code: {exitCode_sslconfig}");
                            }
                        }
                    }
                }

                if (gitLfsSupport)
                {
                    bool lfsSupportAuthHeader = GitLfsSupportUseAuthHeader(executionContext, gitCommandManager);
                    if (lfsSupportAuthHeader && exposeCred)
                    {
                        string configKey = $"http.{repositoryUrl.AbsoluteUri}.extraheader";
                        string configValue = $"\"AUTHORIZATION: {GenerateAuthHeader(executionContext, username, password)}\"";
                        configModifications[configKey] = configValue.Trim('\"');
                        int exitCode_config = await gitCommandManager.GitConfig(executionContext, targetPath, configKey, configValue);
                        if (exitCode_config != 0)
                        {
                            throw new InvalidOperationException($"Git config failed with exit code: {exitCode_config}");
                        }
                    }

                    if (!lfsSupportAuthHeader && !exposeCred)
                    {
                        // remove cached credential from origin's lfs fetch/push url.
                        executionContext.Debug("Remove git-lfs fetch and push url setting from git config.");
                        await RemoveGitConfig(executionContext, gitCommandManager, targetPath, "remote.origin.lfsurl", gitLfsUrlWithCred.AbsoluteUri);
                        configModifications.Remove("remote.origin.lfsurl");
                        await RemoveGitConfig(executionContext, gitCommandManager, targetPath, "remote.origin.lfspushurl", gitLfsUrlWithCred.AbsoluteUri);
                        configModifications.Remove("remote.origin.lfspushurl");
                    }
                }

                if (useClientCert && !string.IsNullOrEmpty(clientCertPrivateKeyAskPassFile) && !exposeCred)
                {
                    executionContext.Debug("Remove git.sslkey askpass file.");
                    IOUtil.DeleteFile(clientCertPrivateKeyAskPassFile);
                }
            }

            // Set intra-task variable for post job cleanup
            executionContext.SetTaskVariable("repository", repository.Alias);

            if (selfManageGitCreds)
            {
                // no needs to cleanup creds, since customer choose to manage creds themselves.
                executionContext.SetTaskVariable("cleanupcreds", "false");
            }
            else if (exposeCred)
            {
                executionContext.SetTaskVariable("cleanupcreds", "true");
            }

            if (preferGitFromPath)
            {
                // use git from PATH
                executionContext.SetTaskVariable("preferPath", "true");
            }

            if (repositoryUrlWithCred != null)
            {
                executionContext.SetTaskVariable("repoUrlWithCred", repositoryUrlWithCred.AbsoluteUri, true);
            }

            if (configModifications.Count > 0)
            {
                executionContext.SetTaskVariable("modifiedgitconfig", JsonUtility.ToString(configModifications), true);
            }

            if (useClientCert && !string.IsNullOrEmpty(clientCertPrivateKeyAskPassFile))
            {
                executionContext.SetTaskVariable("clientCertAskPass", clientCertPrivateKeyAskPassFile);
            }
        }

        public async Task PostJobCleanupAsync(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository)
        {
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(repository, nameof(repository));

            executionContext.Output($"Cleaning any cached credential from repository: {repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Name)} ({repository.Type})");

            Uri repositoryUrl = repository.Url;
            string targetPath = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);

            executionContext.Debug($"Repository url={repositoryUrl}");
            executionContext.Debug($"targetPath={targetPath}");

            bool cleanupCreds = StringUtil.ConvertToBoolean(executionContext.TaskVariables.GetValueOrDefault("cleanupcreds")?.Value);
            if (cleanupCreds)
            {
                bool preferGitFromPath = StringUtil.ConvertToBoolean(executionContext.TaskVariables.GetValueOrDefault("preferPath")?.Value);

                // Initialize git command manager
                GitCliManager gitCommandManager = new GitCliManager();
                await gitCommandManager.LoadGitExecutionInfo(executionContext, useBuiltInGit: !preferGitFromPath);

                executionContext.Debug("Remove any extraheader, proxy and client cert setting from git config.");
                var configModifications = JsonUtility.FromString<Dictionary<string, string>>(executionContext.TaskVariables.GetValueOrDefault("modifiedgitconfig")?.Value);
                if (configModifications != null && configModifications.Count > 0)
                {
                    foreach (var config in configModifications)
                    {
                        await RemoveGitConfig(executionContext, gitCommandManager, targetPath, config.Key, config.Value);
                    }
                }

                var repositoryUrlWithCred = executionContext.TaskVariables.GetValueOrDefault("repoUrlWithCred")?.Value;
                if (string.IsNullOrEmpty(repositoryUrlWithCred))
                {
                    await RemoveCachedCredential(executionContext, gitCommandManager, new Uri(repositoryUrlWithCred), targetPath, repositoryUrl, "origin");
                }
            }

            // delete client cert askpass file.
            string clientCertPrivateKeyAskPassFile = executionContext.TaskVariables.GetValueOrDefault("clientCertAskPass")?.Value;
            if (!string.IsNullOrEmpty(clientCertPrivateKeyAskPassFile))
            {
                IOUtil.DeleteFile(clientCertPrivateKeyAskPassFile);
            }
        }

        private async Task<bool> IsRepositoryOriginUrlMatch(AgentTaskPluginExecutionContext context, GitCliManager gitCommandManager, string repositoryPath, Uri expectedRepositoryOriginUrl)
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

        private async Task RemoveGitConfig(AgentTaskPluginExecutionContext executionContext, GitCliManager gitCommandManager, string targetPath, string configKey, string configValue)
        {
            int exitCode_configUnset = await gitCommandManager.GitConfigUnset(executionContext, targetPath, configKey);
            if (exitCode_configUnset != 0)
            {
                // if unable to use git.exe unset http.extraheader, http.proxy or core.askpass, modify git config file on disk. make sure we don't left credential.
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

                            setting = $"askpass = {configValue}";
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

        private async Task RemoveCachedCredential(AgentTaskPluginExecutionContext context, GitCliManager gitCommandManager, Uri repositoryUrlWithCred, string repositoryPath, Uri repositoryUrl, string remoteName)
        {
            // there is nothing cached in repository Url.
            if (repositoryUrlWithCred == null)
            {
                return;
            }

            //remove credential from fetch url
            context.Debug("Remove injected credential from git remote fetch url.");
            int exitCode_seturl = await gitCommandManager.GitRemoteSetUrl(context, repositoryPath, remoteName, repositoryUrl.AbsoluteUri);

            context.Debug("Remove injected credential from git remote push url.");
            int exitCode_setpushurl = await gitCommandManager.GitRemoteSetPushUrl(context, repositoryPath, remoteName, repositoryUrl.AbsoluteUri);

            if (exitCode_seturl != 0 || exitCode_setpushurl != 0)
            {
                // if unable to use git.exe set fetch url back, modify git config file on disk. make sure we don't left credential.
                context.Warning("Unable to use git.exe remove injected credential from git remote fetch url, modify git config file on disk to remove injected credential.");
                string gitConfig = Path.Combine(repositoryPath, ".git/config");
                if (File.Exists(gitConfig))
                {
                    string gitConfigContent = File.ReadAllText(Path.Combine(repositoryPath, ".git", "config"));
                    gitConfigContent = gitConfigContent.Replace(repositoryUrlWithCred.AbsoluteUri, repositoryUrl.AbsoluteUri);
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