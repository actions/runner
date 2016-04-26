using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    [ServiceLocator(Default = typeof(GitCommandManager))]
    public interface IGitCommandManager : IAgentService
    {
        string GitPath { get; set; }

        Version Version { get; set; }

        string GitHttpUserAgent { get; set; }

        // git clone --progress --no-checkout <URL> <LocalDir>
        Task<int> GitClone(IExecutionContext context, string repositoryPath, Uri repositoryUrl, string username, string password, bool exposeCred, CancellationToken cancellationToken);

        // git fetch --tags --prune --progress origin [+refs/pull/*:refs/remote/pull/*]
        Task<int> GitFetch(IExecutionContext context, string repositoryPath, string remoteName, List<string> refSpec, string username, string password, bool exposeCred, CancellationToken cancellationToken);

        // git checkout -f --progress <commitId/branch>
        Task<int> GitCheckout(IExecutionContext context, string repositoryPath, string committishOrBranchSpec, CancellationToken cancellationToken);

        // git clean -fdx
        Task<int> GitClean(IExecutionContext context, string repositoryPath);

        // git reset --hard HEAD
        Task<int> GitReset(IExecutionContext context, string repositoryPath);

        // get remote set-url <origin> <url>
        Task<int> GitRemoteSetUrl(IExecutionContext context, string repositoryPath, string remoteName, string remoteUrl);

        // get remote set-url --push <origin> <url>
        Task<int> GitRemoteSetPushUrl(IExecutionContext context, string repositoryPath, string remoteName, string remoteUrl);

        // git submodule init
        Task<int> GitSubmoduleInit(IExecutionContext context, string repositoryPath);

        // git submodule update -f
        Task<int> GitSubmoduleUpdate(IExecutionContext context, string repositoryPath, CancellationToken cancellationToken);

        // git config --get remote.origin.url
        Task<Uri> GitGetFetchUrl(IExecutionContext context, string repositoryPath);

        // git config --get-regexp submodule.*.url
        Task<Dictionary<string, Uri>> GitGetSubmoduleUrls(IExecutionContext context, string repoRoot);

        // git config <key> <value>
        Task<int> GitUpdateSubmoduleUrls(IExecutionContext context, string repositoryPath, Dictionary<string, Uri> updateSubmoduleUrls);

        // git config gc.auto 0
        Task<int> GitDisableAutoGC(IExecutionContext context, string repositoryPath);

        // git version
        Task<Version> GitVersion(IExecutionContext context);
    }

    public class GitCommandManager : AgentService, IGitCommandManager
    {
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

        public string GitPath { get; set; }
        public Version Version { get; set; }
        public string GitHttpUserAgent { get; set; }

        // git clone --progress --no-checkout <URL> <LocalDir>
        public async Task<int> GitClone(IExecutionContext context, string repositoryPath, Uri repositoryUrl, string username, string password, bool exposeCred, CancellationToken cancellationToken)
        {
            context.Debug($"Clone git repository: {repositoryUrl.AbsoluteUri} into: {repositoryPath}.");
            string repoRootEscapeSpace = StringUtil.Format(@"""{0}""", repositoryPath.Replace(@"""", @"\"""));
            return await ExecuteGitCommandAsync(context, repositoryPath, "clone", StringUtil.Format($"--progress --no-checkout {repositoryUrl.AbsoluteUri} {repoRootEscapeSpace}"), cancellationToken);
        }

        // git fetch --tags --prune --progress origin [+refs/pull/*:refs/remote/pull/*]
        public async Task<int> GitFetch(IExecutionContext context, string repositoryPath, string remoteName, List<string> refSpec, string username, string password, bool exposeCred, CancellationToken cancellationToken)
        {
            context.Debug($"Fetch git repository at: {repositoryPath} remote: {remoteName}.");
            if (refSpec != null && refSpec.Count > 0)
            {
                refSpec = refSpec.Where(r => !string.IsNullOrEmpty(r)).ToList();
            }

            return await ExecuteGitCommandAsync(context, repositoryPath, "fetch", StringUtil.Format($"--tags --prune --progress {remoteName} {string.Join(" ", refSpec)}"), cancellationToken);
        }

        // git checkout -f --progress <commitId/branch>
        public async Task<int> GitCheckout(IExecutionContext context, string repositoryPath, string committishOrBranchSpec, CancellationToken cancellationToken = default(CancellationToken))
        {
            context.Debug($"Checkout {committishOrBranchSpec}.");
            string checkoutOption = GetCommandOption("checkout");
            return await ExecuteGitCommandAsync(context, repositoryPath, "checkout", StringUtil.Format(checkoutOption, committishOrBranchSpec), cancellationToken);
        }

        // git clean -fdx
        public async Task<int> GitClean(IExecutionContext context, string repositoryPath)
        {
            context.Debug($"Delete untracked files/folders for repository at {repositoryPath}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "clean", "-fdx");
        }

        // git reset --hard HEAD
        public async Task<int> GitReset(IExecutionContext context, string repositoryPath)
        {
            context.Debug($"Undo any changes to tracked files in the working tree for repository at {repositoryPath}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "reset", "--hard HEAD");
        }

        // get remote set-url <origin> <url>
        public async Task<int> GitRemoteSetUrl(IExecutionContext context, string repositoryPath, string remoteName, string remoteUrl)
        {
            context.Debug($"Set git fetch url to: {remoteUrl} for remote: {remoteName}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "remote", StringUtil.Format($"set-url {remoteName} {remoteUrl}"));
        }

        // get remote set-url --push <origin> <url>
        public async Task<int> GitRemoteSetPushUrl(IExecutionContext context, string repositoryPath, string remoteName, string remoteUrl)
        {
            context.Debug($"Set git push url to: {remoteUrl} for remote: {remoteName}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "remote", StringUtil.Format($"set-url --push {remoteName} {remoteUrl}"));
        }

        // git submodule init
        public async Task<int> GitSubmoduleInit(IExecutionContext context, string repositoryPath)
        {
            context.Debug("Initialize the git submodules.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "submodule", "init");
        }

        // git submodule update -f
        public async Task<int> GitSubmoduleUpdate(IExecutionContext context, string repositoryPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            context.Debug("Update the registered git submodules.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "submodule", "update -f", cancellationToken);
        }

        // git config --get remote.origin.url
        public async Task<Uri> GitGetFetchUrl(IExecutionContext context, string repositoryPath)
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
        public async Task<Dictionary<string, Uri>> GitGetSubmoduleUrls(IExecutionContext context, string repoRoot)
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
        public async Task<int> GitUpdateSubmoduleUrls(IExecutionContext context, string repositoryPath, Dictionary<string, Uri> updateSubmoduleUrls)
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
        public async Task<int> GitDisableAutoGC(IExecutionContext context, string repositoryPath)
        {
            context.Debug("Disable git auto garbage collection.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "config", "gc.auto 0");
        }

        // git version
        public async Task<Version> GitVersion(IExecutionContext context)
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

        private string GetCommandOption(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentNullException("command");
            }

            if (!_gitCommands.ContainsKey(command))
            {
                throw new NotSupportedException($"Unsupported git command: {command}");
            }

            Dictionary<Version, string> options = _gitCommands[command];
            foreach (var versionOption in options.OrderByDescending(o => o.Key))
            {
                if (Version >= versionOption.Key)
                {
                    return versionOption.Value;
                }
            }

            var earliestVersion = options.OrderByDescending(o => o.Key).Last();
            Trace.Info($"Fallback to version {earliestVersion.Key.ToString()} command option for git {command}.");
            return earliestVersion.Value;
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

            Dictionary<string, string> _userAgentEnv = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(GitHttpUserAgent))
            {
                _userAgentEnv["GIT_HTTP_USER_AGENT"] = GitHttpUserAgent;
            }

            return await processInvoker.ExecuteAsync(repoRoot, GitPath, arg, _userAgentEnv, cancellationToken);
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
                lock (outputLock)
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

            Dictionary<string, string> _userAgentEnv = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(GitHttpUserAgent))
            {
                _userAgentEnv["GIT_HTTP_USER_AGENT"] = GitHttpUserAgent;
            }

            return await processInvoker.ExecuteAsync(repoRoot, GitPath, arg, _userAgentEnv, default(CancellationToken));
        }
    }
}