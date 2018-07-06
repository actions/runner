using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Agent.Sdk;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Agent.Plugins.Repository
{
    public abstract class TfsVCCliManager
    {
        public readonly Dictionary<string, string> AdditionalEnvironmentVariables = new Dictionary<string, string>();

        public CancellationToken CancellationToken { protected get; set; }

        public ServiceEndpoint Endpoint { protected get; set; }

        public Pipelines.RepositoryResource Repository { protected get; set; }

        public AgentTaskPluginExecutionContext ExecutionContext { protected get; set; }

        public abstract TfsVCFeatures Features { get; }

        public abstract Task<bool> TryWorkspaceDeleteAsync(ITfsVCWorkspace workspace);
        public abstract Task WorkspacesRemoveAsync(ITfsVCWorkspace workspace);

        protected virtual Encoding OutputEncoding => null;

        protected string SourceVersion
        {
            get
            {
                string version = Repository.Version;
                ArgUtil.NotNullOrEmpty(version, nameof(version));
                return version;
            }
        }

        protected string SourcesDirectory
        {
            get
            {
                string sourcesDirectory = Repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
                ArgUtil.NotNullOrEmpty(sourcesDirectory, nameof(sourcesDirectory));
                return sourcesDirectory;
            }
        }

        protected abstract string Switch { get; }

        protected string WorkspaceName
        {
            get
            {
                string workspace = ExecutionContext.Variables.GetValueOrDefault("build.repository.tfvc.workspace")?.Value;
                ArgUtil.NotNullOrEmpty(workspace, nameof(workspace));
                return workspace;
            }
        }

        protected Task RunCommandAsync(params string[] args)
        {
            return RunCommandAsync(FormatFlags.None, args);
        }

        protected async Task RunCommandAsync(FormatFlags formatFlags, params string[] args)
        {
            // Validation.
            ArgUtil.NotNull(args, nameof(args));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));

            // Invoke tf.
            var processInvoker = new ProcessInvoker(ExecutionContext);
            var outputLock = new object();
            processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
            {
                lock (outputLock)
                {
                    ExecutionContext.Output(e.Data);
                }
            };
            processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
            {
                lock (outputLock)
                {
                    ExecutionContext.Output(e.Data);
                }
            };
            string arguments = FormatArguments(formatFlags, args);
            ExecutionContext.Command($@"tf {arguments}");
            await processInvoker.ExecuteAsync(
                workingDirectory: SourcesDirectory,
                fileName: "tf",
                arguments: arguments,
                environment: AdditionalEnvironmentVariables,
                requireExitCodeZero: true,
                outputEncoding: OutputEncoding,
                cancellationToken: CancellationToken);

        }

        protected Task<string> RunPorcelainCommandAsync(params string[] args)
        {
            return RunPorcelainCommandAsync(FormatFlags.None, args);
        }

        protected async Task<string> RunPorcelainCommandAsync(FormatFlags formatFlags, params string[] args)
        {
            // Run the command.
            TfsVCPorcelainCommandResult result = await TryRunPorcelainCommandAsync(formatFlags, args);
            ArgUtil.NotNull(result, nameof(result));
            if (result.Exception != null)
            {
                // The command failed. Dump the output and throw.
                result.Output?.ForEach(x => ExecutionContext.Output(x ?? string.Empty));
                throw result.Exception;
            }

            // Return the output.
            // Note, string.join gracefully handles a null element within the IEnumerable<string>.
            return string.Join(Environment.NewLine, result.Output ?? new List<string>());
        }

        protected async Task<TfsVCPorcelainCommandResult> TryRunPorcelainCommandAsync(FormatFlags formatFlags, params string[] args)
        {
            // Validation.
            ArgUtil.NotNull(args, nameof(args));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));

            // Invoke tf.
            var processInvoker = new ProcessInvoker(ExecutionContext);
            var result = new TfsVCPorcelainCommandResult();
            var outputLock = new object();
            processInvoker.OutputDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
            {
                lock (outputLock)
                {
                    ExecutionContext.Debug(e.Data);
                    result.Output.Add(e.Data);
                }
            };
            processInvoker.ErrorDataReceived += (object sender, ProcessDataReceivedEventArgs e) =>
            {
                lock (outputLock)
                {
                    ExecutionContext.Debug(e.Data);
                    result.Output.Add(e.Data);
                }
            };
            string arguments = FormatArguments(formatFlags, args);
            ExecutionContext.Debug($@"tf {arguments}");
            // TODO: Test whether the output encoding needs to be specified on a non-Latin OS.
            try
            {
                await processInvoker.ExecuteAsync(
                    workingDirectory: SourcesDirectory,
                    fileName: "tf",
                    arguments: arguments,
                    environment: AdditionalEnvironmentVariables,
                    requireExitCodeZero: true,
                    outputEncoding: OutputEncoding,
                    cancellationToken: CancellationToken);
            }
            catch (ProcessExitCodeException ex)
            {
                result.Exception = ex;
            }

            return result;

        }

        private string FormatArguments(FormatFlags formatFlags, params string[] args)
        {
            // Validation.
            ArgUtil.NotNull(args, nameof(args));
            ArgUtil.NotNull(Endpoint, nameof(Endpoint));
            ArgUtil.NotNull(Endpoint.Authorization, nameof(Endpoint.Authorization));
            ArgUtil.NotNull(Endpoint.Authorization.Parameters, nameof(Endpoint.Authorization.Parameters));
            ArgUtil.Equal(EndpointAuthorizationSchemes.OAuth, Endpoint.Authorization.Scheme, nameof(Endpoint.Authorization.Scheme));
            string accessToken = Endpoint.Authorization.Parameters.TryGetValue(EndpointAuthorizationParameters.AccessToken, out accessToken) ? accessToken : null;
            ArgUtil.NotNullOrEmpty(accessToken, EndpointAuthorizationParameters.AccessToken);
            ArgUtil.NotNull(Repository.Url, nameof(Repository.Url));

            // Format each arg.
            var formattedArgs = new List<string>();
            foreach (string arg in args ?? new string[0])
            {
                // Validate the arg.
                if (!string.IsNullOrEmpty(arg) && arg.IndexOfAny(new char[] { '"', '\r', '\n' }) >= 0)
                {
                    throw new Exception(StringUtil.Loc("InvalidCommandArg", arg));
                }

                // Add the arg.
                formattedArgs.Add(arg != null && arg.Contains(" ") ? $@"""{arg}""" : $"{arg}");
            }

            // Add the common parameters.
            if (!formatFlags.HasFlag(FormatFlags.OmitCollectionUrl))
            {
                if (Features.HasFlag(TfsVCFeatures.EscapedUrl))
                {
                    formattedArgs.Add($"{Switch}collection:{Repository.Url.AbsoluteUri}");
                }
                else
                {
                    // TEE CLC expects the URL in unescaped form.
                    string url;
                    try
                    {
                        url = Uri.UnescapeDataString(Repository.Url.AbsoluteUri);
                    }
                    catch (Exception ex)
                    {
                        // Unlikely (impossible?), but don't fail if encountered. If we don't hear complaints
                        // about this warning then it is likely OK to remove the try/catch altogether and have
                        // faith that UnescapeDataString won't throw for this scenario.
                        url = Repository.Url.AbsoluteUri;
                        ExecutionContext.Warning($"{ex.Message} ({url})");
                    }

                    formattedArgs.Add($"\"{Switch}collection:{url}\"");
                }
            }

            if (!formatFlags.HasFlag(FormatFlags.OmitLogin))
            {
                if (Features.HasFlag(TfsVCFeatures.LoginType))
                {
                    formattedArgs.Add($"{Switch}loginType:OAuth");
                    formattedArgs.Add($"{Switch}login:.,{accessToken}");
                }
                else
                {
                    formattedArgs.Add($"{Switch}jwt:{accessToken}");
                }
            }

            if (!formatFlags.HasFlag(FormatFlags.OmitNoPrompt))
            {
                formattedArgs.Add($"{Switch}noprompt");
            }

            return string.Join(" ", formattedArgs);
        }

        [Flags]
        protected enum FormatFlags
        {
            None = 0,
            OmitCollectionUrl = 1,
            OmitLogin = 2,
            OmitNoPrompt = 4,
            All = OmitCollectionUrl | OmitLogin | OmitNoPrompt,
        }
    }

    [Flags]
    public enum TfsVCFeatures
    {
        None = 0,

        // Indicates whether "workspace /new" adds a default mapping.
        DefaultWorkfoldMap = 1,

        // Indicates whether the CLI accepts the collection URL in escaped form.
        EscapedUrl = 2,

        // Indicates whether the "eula" subcommand is supported.
        Eula = 4,

        // Indicates whether the "get" and "undo" subcommands will correctly resolve
        // the workspace from an unmapped root folder. For example, if a workspace
        // contains only two mappings, $/foo -> $(build.sourcesDirectory)\foo and
        // $/bar -> $(build.sourcesDirectory)\bar, then "tf get $(build.sourcesDirectory)"
        // will not be able to resolve the workspace unless this feature is supported.
        GetFromUnmappedRoot = 8,

        // Indicates whether the "loginType" parameter is supported.
        LoginType = 16,

        // Indicates whether the "scorch" subcommand is supported.
        Scorch = 32,
    }

    public sealed class TfsVCPorcelainCommandResult
    {
        public TfsVCPorcelainCommandResult()
        {
            Output = new List<string>();
        }

        public ProcessExitCodeException Exception { get; set; }

        public List<string> Output { get; }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // tf shelvesets interfaces.
    ////////////////////////////////////////////////////////////////////////////////
    public interface ITfsVCShelveset
    {
        string Comment { get; }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // tf status interfaces.
    ////////////////////////////////////////////////////////////////////////////////
    public interface ITfsVCStatus
    {
        IEnumerable<ITfsVCPendingChange> AllAdds { get; }
        bool HasPendingChanges { get; }
    }

    public interface ITfsVCPendingChange
    {
        string LocalItem { get; }
    }

    ////////////////////////////////////////////////////////////////////////////////
    // tf workspaces interfaces.
    ////////////////////////////////////////////////////////////////////////////////
    public interface ITfsVCWorkspace
    {
        string Computer { get; set; }

        string Name { get; }

        string Owner { get; }

        ITfsVCMapping[] Mappings { get; }
    }

    public interface ITfsVCMapping
    {
        bool Cloak { get; }

        string LocalPath { get; }

        bool Recursive { get; }

        string ServerPath { get; }
    }
}