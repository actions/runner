using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    [ServiceLocator(Default = typeof(UnixUtil))]
    public interface IUnixUtil : IAgentService
    {
        Task Exec(string workingDirectory, string toolName, string argLine);
        Task Chmod(string mode, string file);
        Task Chown(string owner, string group, string file);
    }

    public sealed class UnixUtil : AgentService, IUnixUtil
    {
        private ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
        }

        public async Task Chmod(string mode, string file)
        {
            Trace.Entering();
            await Exec(IOUtil.GetRootPath(), "chmod", $"{mode} {file}");
        }

        public async Task Chown(string owner, string group, string file)
        {
            Trace.Entering();
            await Exec(IOUtil.GetRootPath(), "chown", $"{owner}:{group} {file}");
        }

        public async Task Exec(string workingDirectory, string toolName, string argLine)
        {
            Trace.Entering();

            var whichUtil = HostContext.GetService<IWhichUtil>();
            string toolPath = whichUtil.Which(toolName);
            Trace.Info($"Running {toolPath} {argLine}");

            var processInvoker = HostContext.CreateService<IProcessInvoker>();
            processInvoker.OutputDataReceived += OnOutputDataReceived;
            processInvoker.ErrorDataReceived += OnErrorDataReceived;

            try
            {
                using (var cs = new CancellationTokenSource(TimeSpan.FromSeconds(45)))
                {
                    await processInvoker.ExecuteAsync(workingDirectory, toolPath, argLine, null, true, cs.Token);
                }
            }
            finally
            {
                processInvoker.OutputDataReceived -= OnOutputDataReceived;
                processInvoker.ErrorDataReceived -= OnErrorDataReceived;
            }
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _term.WriteLine(e.Data);
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _term.WriteLine(e.Data);
            }
        }
    }
}
