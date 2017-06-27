using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    [ServiceLocator(Default = typeof(GitCommandManager))]
    public interface IGitCommandManager : IAgentService
    {
        bool EnsureGitVersion(Version requiredVersion, bool throwOnNotMatch);

        // setup git execution info, git location, version, useragent, execpath
        Task LoadGitExecutionInfo(IExecutionContext context, bool useBuiltInGit);

        // git init <LocalDir>
        Task<int> GitInit(IExecutionContext context, string repositoryPath);

        // git fetch --tags --prune --progress --no-recurse-submodules [--depth=15] origin [+refs/pull/*:refs/remote/pull/*]
        Task<int> GitFetch(IExecutionContext context, string repositoryPath, string remoteName, int fetchDepth, List<string> refSpec, string additionalCommandLine, CancellationToken cancellationToken);

        // git lfs fetch origin [ref]
        Task<int> GitLFSFetch(IExecutionContext context, string repositoryPath, string remoteName, string refSpec, string additionalCommandLine, CancellationToken cancellationToken);

        // git checkout -f --progress <commitId/branch>
        Task<int> GitCheckout(IExecutionContext context, string repositoryPath, string committishOrBranchSpec, CancellationToken cancellationToken);

        // git clean -fdx
        Task<int> GitClean(IExecutionContext context, string repositoryPath);

        // git reset --hard HEAD
        Task<int> GitReset(IExecutionContext context, string repositoryPath);

        // get remote add <origin> <url>
        Task<int> GitRemoteAdd(IExecutionContext context, string repositoryPath, string remoteName, string remoteUrl);

        // get remote set-url <origin> <url>
        Task<int> GitRemoteSetUrl(IExecutionContext context, string repositoryPath, string remoteName, string remoteUrl);

        // get remote set-url --push <origin> <url>
        Task<int> GitRemoteSetPushUrl(IExecutionContext context, string repositoryPath, string remoteName, string remoteUrl);

        // git submodule foreach git clean -fdx
        Task<int> GitSubmoduleClean(IExecutionContext context, string repositoryPath);

        // git submodule foreach git reset --hard HEAD
        Task<int> GitSubmoduleReset(IExecutionContext context, string repositoryPath);

        // git submodule update --init --force [--recursive]
        Task<int> GitSubmoduleUpdate(IExecutionContext context, string repositoryPath, string additionalCommandLine, bool recursive, CancellationToken cancellationToken);

        // git config --get remote.origin.url
        Task<Uri> GitGetFetchUrl(IExecutionContext context, string repositoryPath);

        // git config <key> <value>
        Task<int> GitConfig(IExecutionContext context, string repositoryPath, string configKey, string configValue);

        // git config --get-all <key>
        Task<bool> GitConfigExist(IExecutionContext context, string repositoryPath, string configKey);

        // git config --unset-all <key>
        Task<int> GitConfigUnset(IExecutionContext context, string repositoryPath, string configKey);

        // git config gc.auto 0
        Task<int> GitDisableAutoGC(IExecutionContext context, string repositoryPath);

        // git lfs version
        Task<int> GitLFSVersion(IExecutionContext context, string repositoryPath);

        // git lfs install --local
        Task<int> GitLFSInstall(IExecutionContext context, string repositoryPath);

        // git lfs logs last
        Task<int> GitLFSLogs(IExecutionContext context, string repositoryPath);

        // git version
        Task<Version> GitVersion(IExecutionContext context);
    }

    public class GitCommandManager : AgentService, IGitCommandManager
    {
#if OS_WINDOWS
        private static readonly Encoding s_encoding = Encoding.UTF8;
#else
        private static readonly Encoding s_encoding = null;
#endif
        private string _gitHttpUserAgentEnv = null;
        private string _gitPath = null;
        private Version _version = null;

        public bool EnsureGitVersion(Version requiredVersion, bool throwOnNotMatch)
        {
            ArgUtil.NotNull(_gitPath, nameof(_gitPath));
            ArgUtil.NotNull(_version, nameof(_version));

            if (_version < requiredVersion && throwOnNotMatch)
            {
                throw new NotSupportedException(StringUtil.Loc("MinRequiredGitVersion", requiredVersion, _gitPath, _version));
            }

            return _version >= requiredVersion;
        }

        public async Task LoadGitExecutionInfo(IExecutionContext context, bool useBuiltInGit)
        {
            // Resolve the location of git.
            if (useBuiltInGit)
            {
#if OS_WINDOWS
                _gitPath = Path.Combine(IOUtil.GetExternalsPath(), "git", "cmd", $"git{IOUtil.ExeExtension}");

                // Prepend the PATH.
                context.Output(StringUtil.Loc("Prepending0WithDirectoryContaining1", Constants.PathVariable, Path.GetFileName(_gitPath)));
                var varUtil = HostContext.GetService<IVarUtil>();
                varUtil.PrependPath(Path.GetDirectoryName(_gitPath));
                context.Debug($"{Constants.PathVariable}: '{Environment.GetEnvironmentVariable(Constants.PathVariable)}'");
#else
                // There is no built-in git for OSX/Linux
                _gitPath = null;
#endif
            }
            else
            {
                var whichUtil = HostContext.GetService<IWhichUtil>();
                _gitPath = whichUtil.Which("git", require: true);
            }

            ArgUtil.File(_gitPath, nameof(_gitPath));

            // Get the Git version.    
            _version = await GitVersion(context);
            ArgUtil.NotNull(_version, nameof(_version));
            context.Debug($"Detect git version: {_version.ToString()}.");

            // required 2.0, all git operation commandline args need min git version 2.0
            Version minRequiredGitVersion = new Version(2, 0);
            EnsureGitVersion(minRequiredGitVersion, throwOnNotMatch: true);

            // suggest user upgrade to 2.9 for better git experience
            Version recommendGitVersion = new Version(2, 9);
            if (!EnsureGitVersion(recommendGitVersion, throwOnNotMatch: false))
            {
                context.Output(StringUtil.Loc("UpgradeToLatestGit", recommendGitVersion, _version));
            }

            // Set the user agent.
            _gitHttpUserAgentEnv = $"git/{_version.ToString()} (vsts-agent-git/{Constants.Agent.Version})";
            context.Debug($"Set git useragent to: {_gitHttpUserAgentEnv}.");
        }

        // git init <LocalDir>
        public async Task<int> GitInit(IExecutionContext context, string repositoryPath)
        {
            context.Debug($"Init git repository at: {repositoryPath}.");
            string repoRootEscapeSpace = StringUtil.Format(@"""{0}""", repositoryPath.Replace(@"""", @"\"""));
            return await ExecuteGitCommandAsync(context, repositoryPath, "init", StringUtil.Format($"{repoRootEscapeSpace}"));
        }

        // git fetch --tags --prune --progress --no-recurse-submodules [--depth=15] origin [+refs/pull/*:refs/remote/pull/*]
        public async Task<int> GitFetch(IExecutionContext context, string repositoryPath, string remoteName, int fetchDepth, List<string> refSpec, string additionalCommandLine, CancellationToken cancellationToken)
        {
            context.Debug($"Fetch git repository at: {repositoryPath} remote: {remoteName}.");
            if (refSpec != null && refSpec.Count > 0)
            {
                refSpec = refSpec.Where(r => !string.IsNullOrEmpty(r)).ToList();
            }

            // default options for git fetch.
            string options = StringUtil.Format($"--tags --prune --progress --no-recurse-submodules {remoteName} {string.Join(" ", refSpec)}");

            // If shallow fetch add --depth arg
            // If the local repository is shallowed but there is no fetch depth provide for this build,
            // add --unshallow to convert the shallow repository to a complete repository
            if (fetchDepth > 0)
            {
                options = StringUtil.Format($"--tags --prune --progress --no-recurse-submodules --depth={fetchDepth} {remoteName} {string.Join(" ", refSpec)}");
            }
            else
            {
                if (File.Exists(Path.Combine(repositoryPath, ".git", "shallow")))
                {
                    options = StringUtil.Format($"--tags --prune --progress --no-recurse-submodules --unshallow {remoteName} {string.Join(" ", refSpec)}");
                }
            }

            return await ExecuteGitCommandAsync(context, repositoryPath, "fetch", options, additionalCommandLine, cancellationToken);
        }

        // git lfs fetch origin [ref]
        public async Task<int> GitLFSFetch(IExecutionContext context, string repositoryPath, string remoteName, string refSpec, string additionalCommandLine, CancellationToken cancellationToken)
        {
            context.Debug($"Fetch LFS objects for git repository at: {repositoryPath} remote: {remoteName}.");

            // default options for git lfs fetch.
            string options = StringUtil.Format($"fetch origin {refSpec}");
            return await ExecuteGitCommandAsync(context, repositoryPath, "lfs", options, additionalCommandLine, cancellationToken);
        }

        // git checkout -f --progress <commitId/branch>
        public async Task<int> GitCheckout(IExecutionContext context, string repositoryPath, string committishOrBranchSpec, CancellationToken cancellationToken)
        {
            context.Debug($"Checkout {committishOrBranchSpec}.");

            // Git 2.7 support report checkout progress to stderr during stdout/err redirect.
            string options;
            if (_version >= new Version(2, 7))
            {
                options = StringUtil.Format("--progress --force {0}", committishOrBranchSpec);
            }
            else
            {
                options = StringUtil.Format("--force {0}", committishOrBranchSpec);
            }

            return await ExecuteGitCommandAsync(context, repositoryPath, "checkout", options, cancellationToken);
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
        public async Task<int> GitRemoteAdd(IExecutionContext context, string repositoryPath, string remoteName, string remoteUrl)
        {
            context.Debug($"Add git remote: {remoteName} to url: {remoteUrl} for repository under: {repositoryPath}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "remote", StringUtil.Format($"add {remoteName} {remoteUrl}"));
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

        // git submodule foreach git clean -fdx
        public async Task<int> GitSubmoduleClean(IExecutionContext context, string repositoryPath)
        {
            context.Debug($"Delete untracked files/folders for submodules at {repositoryPath}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "submodule", "foreach git clean -fdx");
        }

        // git submodule foreach git reset --hard HEAD
        public async Task<int> GitSubmoduleReset(IExecutionContext context, string repositoryPath)
        {
            context.Debug($"Undo any changes to tracked files in the working tree for submodules at {repositoryPath}.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "submodule", "foreach git reset --hard HEAD");
        }

        // git submodule update --init --force [--recursive]
        public async Task<int> GitSubmoduleUpdate(IExecutionContext context, string repositoryPath, string additionalCommandLine, bool recursive, CancellationToken cancellationToken)
        {
            context.Debug("Update the registered git submodules.");
            string options = "update --init --force";
            if (recursive)
            {
                options = options + " --recursive";
            }

            return await ExecuteGitCommandAsync(context, repositoryPath, "submodule", options, additionalCommandLine, cancellationToken);
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

        // git config <key> <value>
        public async Task<int> GitConfig(IExecutionContext context, string repositoryPath, string configKey, string configValue)
        {
            context.Debug($"Set git config {configKey} {configValue}");
            return await ExecuteGitCommandAsync(context, repositoryPath, "config", StringUtil.Format($"{configKey} {configValue}"));
        }

        // git config --get-all <key>
        public async Task<bool> GitConfigExist(IExecutionContext context, string repositoryPath, string configKey)
        {
            // git config --get-all {configKey} will return 0 and print the value if the config exist.
            context.Debug($"Checking git config {configKey} exist or not");

            // ignore any outputs by redirect them into a string list, since the output might contains secrets.
            List<string> outputStrings = new List<string>();
            int exitcode = await ExecuteGitCommandAsync(context, repositoryPath, "config", StringUtil.Format($"--get-all {configKey}"), outputStrings);

            return exitcode == 0;
        }

        // git config --unset-all <key>
        public async Task<int> GitConfigUnset(IExecutionContext context, string repositoryPath, string configKey)
        {
            context.Debug($"Unset git config --unset-all {configKey}");
            return await ExecuteGitCommandAsync(context, repositoryPath, "config", StringUtil.Format($"--unset-all {configKey}"));
        }

        // git config gc.auto 0
        public async Task<int> GitDisableAutoGC(IExecutionContext context, string repositoryPath)
        {
            context.Debug("Disable git auto garbage collection.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "config", "gc.auto 0");
        }

        // git lfs version
        public async Task<int> GitLFSVersion(IExecutionContext context, string repositoryPath)
        {
            context.Debug("Get git-lfs version.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "lfs", "version");
        }

        // git lfs install --local
        public async Task<int> GitLFSInstall(IExecutionContext context, string repositoryPath)
        {
            context.Debug("Ensure git-lfs installed.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "lfs", "install --local");
        }

        // git lfs logs last
        public async Task<int> GitLFSLogs(IExecutionContext context, string repositoryPath)
        {
            context.Debug("Get git-lfs logs.");
            return await ExecuteGitCommandAsync(context, repositoryPath, "lfs", "logs last");
        }

        // git version
        public async Task<Version> GitVersion(IExecutionContext context)
        {
            context.Debug("Get git version.");
            Version version = null;
            List<string> outputStrings = new List<string>();
            int exitCode = await ExecuteGitCommandAsync(context, IOUtil.GetWorkPath(HostContext), "version", null, outputStrings);
            context.Output($"{string.Join(Environment.NewLine, outputStrings)}");
            if (exitCode == 0)
            {
                // remove any empty line.
                outputStrings = outputStrings.Where(o => !string.IsNullOrEmpty(o)).ToList();
                if (outputStrings.Count == 1 && !string.IsNullOrEmpty(outputStrings.First()))
                {
                    string verString = outputStrings.First();
                    // we interested about major.minor.patch version
                    Regex verRegex = new Regex("\\d+\\.\\d+(\\.\\d+)?", RegexOptions.IgnoreCase);
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

        private async Task<int> ExecuteGitCommandAsync(IExecutionContext context, string repoRoot, string command, string options, CancellationToken cancellationToken = default(CancellationToken))
        {
            string arg = StringUtil.Format($"{command} {options}").Trim();
            context.Command($"git {arg}");

            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            return await processInvoker.ExecuteAsync(
                workingDirectory: repoRoot,
                fileName: _gitPath,
                arguments: arg,
                environment: GetGitEnvironmentVariables(context),
                requireExitCodeZero: false,
                outputEncoding: s_encoding,
                cancellationToken: cancellationToken);
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
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                lock (outputLock)
                {
                    output.Add(message.Data);
                }
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                lock (outputLock)
                {
                    output.Add(message.Data);
                }
            };

            return await processInvoker.ExecuteAsync(
                workingDirectory: repoRoot,
                fileName: _gitPath,
                arguments: arg,
                environment: GetGitEnvironmentVariables(context),
                requireExitCodeZero: false,
                outputEncoding: s_encoding,
                cancellationToken: default(CancellationToken));
        }

        private async Task<int> ExecuteGitCommandAsync(IExecutionContext context, string repoRoot, string command, string options, string additionalCommandLine, CancellationToken cancellationToken)
        {
            string arg = StringUtil.Format($"{additionalCommandLine} {command} {options}").Trim();
            context.Command($"git {arg}");

            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            processInvoker.ErrorDataReceived += delegate (object sender, ProcessDataReceivedEventArgs message)
            {
                context.Output(message.Data);
            };

            return await processInvoker.ExecuteAsync(
                workingDirectory: repoRoot,
                fileName: _gitPath,
                arguments: arg,
                environment: GetGitEnvironmentVariables(context),
                requireExitCodeZero: false,
                outputEncoding: s_encoding,
                cancellationToken: cancellationToken);
        }

        private IDictionary<string, string> GetGitEnvironmentVariables(IExecutionContext context)
        {
            Dictionary<string, string> gitEnv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "GIT_TERMINAL_PROMPT", "0" },
            };

            if (!string.IsNullOrEmpty(_gitHttpUserAgentEnv))
            {
                gitEnv["GIT_HTTP_USER_AGENT"] = _gitHttpUserAgentEnv;
            }

            // Add the public variables.
            foreach (KeyValuePair<string, string> pair in context.Variables.Public)
            {
                // Add the variable using the formatted name.
                string formattedKey = (pair.Key ?? string.Empty).Replace('.', '_').Replace(' ', '_').ToUpperInvariant();

                // Skip any GIT_TRACE variable since GIT_TRACE will affect ouput from every git command.
                // This will fail the parse logic for detect git version, remote url, etc.
                // Ex. 
                //      SET GIT_TRACE=true
                //      git version 
                //      11:39:58.295959 git.c:371               trace: built-in: git 'version'
                //      git version 2.11.1.windows.1
                if (formattedKey == "GIT_TRACE" || formattedKey.StartsWith("GIT_TRACE_"))
                {
                    continue;
                }

                gitEnv[formattedKey] = pair.Value ?? string.Empty;
            }

            return gitEnv;
        }
    }
}