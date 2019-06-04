using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace GitHub.Runner.Common
{
    [ServiceLocator(Default = typeof(ProcessInvokerWrapper))]
    public interface IProcessInvoker : IDisposable, IRunnerService
    {
        event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            CancellationToken cancellationToken);

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            CancellationToken cancellationToken);

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            CancellationToken cancellationToken);

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            CancellationToken cancellationToken);

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            CancellationToken cancellationToken);

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            bool inheritConsoleHandler,
            CancellationToken cancellationToken);

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            bool inheritConsoleHandler,
            bool keepStandardInOpen,
            CancellationToken cancellationToken);

        Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            bool inheritConsoleHandler,
            bool keepStandardInOpen,
            bool highPriorityProcess,
            CancellationToken cancellationToken);
    }

    // The implementation of the process invoker does not hook up DataReceivedEvent and ErrorReceivedEvent of Process,
    // instead, we read both STDOUT and STDERR stream manually on seperate thread.
    // The reason is we find a huge perf issue about process STDOUT/STDERR with those events.
    //
    // Missing functionalities:
    //       1. Cancel/Kill process tree
    //       2. Make sure STDOUT and STDERR not process out of order
    public sealed class ProcessInvokerWrapper : RunnerService, IProcessInvoker
    {
        private ProcessInvoker _invoker;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _invoker = new ProcessInvoker(Trace);
        }

        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: false,
                cancellationToken: cancellationToken);
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: null,
                cancellationToken: cancellationToken);
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: outputEncoding,
                killProcessOnCancel: false,
                cancellationToken: cancellationToken);
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: outputEncoding,
                killProcessOnCancel: killProcessOnCancel,
                redirectStandardIn: null,
                cancellationToken: cancellationToken);
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: outputEncoding,
                killProcessOnCancel: killProcessOnCancel,
                redirectStandardIn: redirectStandardIn,
                inheritConsoleHandler: false,
                cancellationToken: cancellationToken
            );
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            bool inheritConsoleHandler,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: outputEncoding,
                killProcessOnCancel: killProcessOnCancel,
                redirectStandardIn: redirectStandardIn,
                inheritConsoleHandler: inheritConsoleHandler,
                keepStandardInOpen: false,
                cancellationToken: cancellationToken
            );
        }

        public Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            bool inheritConsoleHandler,
            bool keepStandardInOpen,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync(
                workingDirectory: workingDirectory,
                fileName: fileName,
                arguments: arguments,
                environment: environment,
                requireExitCodeZero: requireExitCodeZero,
                outputEncoding: outputEncoding,
                killProcessOnCancel: killProcessOnCancel,
                redirectStandardIn: redirectStandardIn,
                inheritConsoleHandler: inheritConsoleHandler,
                keepStandardInOpen: keepStandardInOpen,
                highPriorityProcess: false,
                cancellationToken: cancellationToken
            );
        }

        public async Task<int> ExecuteAsync(
            string workingDirectory,
            string fileName,
            string arguments,
            IDictionary<string, string> environment,
            bool requireExitCodeZero,
            Encoding outputEncoding,
            bool killProcessOnCancel,
            Channel<string> redirectStandardIn,
            bool inheritConsoleHandler,
            bool keepStandardInOpen,
            bool highPriorityProcess,
            CancellationToken cancellationToken)
        {
            _invoker.ErrorDataReceived += this.ErrorDataReceived;
            _invoker.OutputDataReceived += this.OutputDataReceived;
            return await _invoker.ExecuteAsync(
                workingDirectory,
                fileName,
                arguments,
                environment,
                requireExitCodeZero,
                outputEncoding,
                killProcessOnCancel,
                redirectStandardIn,
                inheritConsoleHandler,
                keepStandardInOpen,
                highPriorityProcess,
                cancellationToken);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_invoker != null)
                {
                    _invoker.Dispose();
                    _invoker = null;
                }
            }
        }
    }
}
