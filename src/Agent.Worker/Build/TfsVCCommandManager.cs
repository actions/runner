using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
#if OS_WINDOWS
    [ServiceLocator(Default = typeof(TFCommandManager))]
#else
    [ServiceLocator(Default = typeof(TeeCommandManager))]
#endif
    public interface ITfsVCCommandManager : IAgentService
    {
        CancellationToken CancellationToken { set; }
        ServiceEndpoint Endpoint { set; }
        IExecutionContext ExecutionContext { set; }
        TfsVCFeatures Features { get; }

        Task EulaAsync();
        Task GetAsync(string localPath);
        string ResolvePath(string serverPath);
        Task ScorchAsync();
        Task ShelveAsync(string shelveset, string commentFile);
        Task<ITfsVCShelveset> ShelvesetsAsync(string shelveset);
        Task<ITfsVCStatus> StatusAsync(string localPath);
        bool TestEulaAccepted();
        Task<bool> TryWorkspaceDeleteAsync(ITfsVCWorkspace workspace);
        Task UndoAsync(string localPath);
        Task UnshelveAsync(string shelveset);
        Task WorkfoldCloakAsync(string serverPath);
        Task WorkfoldMapAsync(string serverPath, string localPath);
        Task WorkfoldUnmapAsync(string serverPath);
        Task WorkspaceNewAsync();
        Task<ITfsVCWorkspace[]> WorkspacesAsync();
        Task WorkspacesRemoveAsync(ITfsVCWorkspace workspace);
    }

    public abstract class TfsVCCommandManager : AgentService
    {
        public CancellationToken CancellationToken { protected get; set; }
        public ServiceEndpoint Endpoint { protected get; set; }
        public IExecutionContext ExecutionContext { protected get; set; }
        public abstract TfsVCFeatures Features { get; }

        protected string SourceVersion
        {
            get
            {
                string version = ExecutionContext.Variables.Build_SourceVersion;
                ArgUtil.NotNullOrEmpty(version, nameof(version));
                return version;
            }
        }

        protected string SourcesDirectory
        {
            get
            {
                string sourcesDirectory = ExecutionContext.Variables.Build_SourcesDirectory;
                ArgUtil.NotNullOrEmpty(sourcesDirectory, nameof(sourcesDirectory));
                return sourcesDirectory;
            }
        }

        protected abstract string Switch { get; }

        protected abstract string TF { get; }

        protected string WorkspaceName
        {
            get
            {
                string workspace = ExecutionContext.Variables.Build_RepoTfvcWorkspace;
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
            using(var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                var outputLock = new object();
                processInvoker.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    lock (outputLock)
                    {
                        ExecutionContext.Output(e.Data);
                    }
                };
                processInvoker.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    lock (outputLock)
                    {
                        ExecutionContext.Output(e.Data);
                    }
                };
                string arguments = FormatArguments(formatFlags, args);
                ExecutionContext.Command($@"{TF} {arguments}");
                await processInvoker.ExecuteAsync(
                    workingDirectory: SourcesDirectory,
                    fileName: TF,
                    arguments: arguments,
                    environment: null,
                    requireExitCodeZero: true,
                    cancellationToken: CancellationToken);
            }
        }

        protected Task<string> RunPorcelainCommandAsync(params string[] args)
        {
            return RunPorcelainCommandAsync(FormatFlags.None, args);
        }

        protected async Task<string> RunPorcelainCommandAsync(FormatFlags formatFlags, params string[] args)
        {
            // Validation.
            ArgUtil.NotNull(args, nameof(args));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));

            // Invoke tf.
            using(var processInvoker = HostContext.CreateService<IProcessInvoker>())
            {
                var output = new List<string>();
                var outputLock = new object();
                processInvoker.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    lock (outputLock)
                    {
                        ExecutionContext.Debug(e.Data);
                        output.Add(e.Data);
                    }
                };
                processInvoker.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    lock (outputLock)
                    {
                        ExecutionContext.Debug(e.Data);
                        output.Add(e.Data);
                    }
                };
                string arguments = FormatArguments(formatFlags, args);
                ExecutionContext.Debug($@"{TF} {arguments}");
                // TODO: Test whether the output encoding needs to be specified on a non-Latin OS.
                try
                {
                    await processInvoker.ExecuteAsync(
                        workingDirectory: SourcesDirectory,
                        fileName: TF,
                        arguments: arguments,
                        environment: null,
                        requireExitCodeZero: true,
                        cancellationToken: CancellationToken);
                }
                catch (ProcessExitCodeException)
                {
                    // The command failed. Dump the output and throw.
                    output.ForEach(x => ExecutionContext.Output(x ?? string.Empty));
                    throw;
                }

                // Note, string.join gracefully handles a null element within the IEnumerable<string>.
                return string.Join(Environment.NewLine, output);
            }
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
            ArgUtil.NotNull(Endpoint.Url, nameof(Endpoint.Url));

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
                formattedArgs.Add($"{Switch}collection:{Endpoint.Url.AbsoluteUri}");
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

        // Indicates whether the "eula" subcommand is supported.
        Eula = 2,

        // Indicates whether the "get" and "undo" subcommands will correctly resolve
        // the workspace from an unmapped root folder. For example, if a workspace
        // contains only two mappings, $/foo -> $(build.sourcesDirectory)\foo and
        // $/bar -> $(build.sourcesDirectory)\bar, then "tf get $(build.sourcesDirectory)"
        // will not be able to resolve the workspace unless this feature is supported.
        GetFromUnmappedRoot = 4,

        // Indicates whether the "loginType" parameter is supported.
        LoginType = 8,

        // Indicates whether the "scorch" subcommand is supported.
        Scorch = 16,
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