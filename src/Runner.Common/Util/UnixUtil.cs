using System;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Util
{
    [ServiceLocator(Default = typeof(UnixUtil))]
    public interface IUnixUtil : IRunnerService
    {
        Task ExecAsync(string workingDirectory, string toolName, string argLine);
        Task ChmodAsync(string mode, string file);
        Task ChownAsync(string owner, string group, string file);
    }

    public sealed class UnixUtil : RunnerService, IUnixUtil
    {
        private ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
        }

        public async Task ChmodAsync(string mode, string file)
        {
            Trace.Entering();
            await ExecAsync(HostContext.GetDirectory(WellKnownDirectory.Root), "chmod", $"{mode} \"{file}\"");
        }

        public async Task ChownAsync(string owner, string group, string file)
        {
            Trace.Entering();
            await ExecAsync(HostContext.GetDirectory(WellKnownDirectory.Root), "chown", $"{owner}:{group} \"{file}\"");
        }

        public async Task ExecAsync(string workingDirectory, string toolName, string argLine)
        {
            Trace.Entering();

            string toolPath = WhichUtil.Which(toolName, trace: Trace);
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

        private void OnOutputDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _term.WriteLine(e.Data);
            }
        }

        private void OnErrorDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                _term.WriteLine(e.Data);
            }
        }
    }
}
