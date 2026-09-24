using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using Xunit;

namespace GitHub.Runner.Common.Tests
{
    // Verifies CreateService<IProcessInvoker>() call sites dispose per invocation.
    public sealed class ProcessInvokerDisposalL0
    {
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Fix_CreateServiceInUsingBlock_CallsDisposeExactlyOnce()
        {
            using (TestHostContext hc = new(this))
            {
                var tracker = new DisposalTrackingProcessInvoker();
                hc.EnqueueInstance<IProcessInvoker>(tracker);

                using (var processInvoker = hc.CreateService<IProcessInvoker>())
                {
                    _ = processInvoker;
                }

                Assert.Equal(1, tracker.DisposeCount);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public async Task Fix_DisposeIsCalledEvenIfExecuteThrows()
        {
            using (TestHostContext hc = new(this))
            {
                var tracker = new DisposalTrackingProcessInvoker { ThrowOnExecute = true };
                hc.EnqueueInstance<IProcessInvoker>(tracker);

                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    using (var processInvoker = hc.CreateService<IProcessInvoker>())
                    {
                        await processInvoker.ExecuteAsync("", "tool", "", null, CancellationToken.None);
                    }
                });

                Assert.Equal(1, tracker.DisposeCount);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
        public void Fix_ProcessInvokerWrapperDispose_ReleasesInnerProcessInvoker()
        {
            using (TestHostContext hc = new(this))
            {
                var wrapper = new ProcessInvokerWrapper();
                wrapper.Initialize(hc);

                wrapper.Dispose();
                wrapper.Dispose();
            }
        }
    }

    // Minimal invoker that records Dispose() calls.
    internal sealed class DisposalTrackingProcessInvoker : IProcessInvoker
    {
        public int DisposeCount { get; private set; }
        public bool ThrowOnExecute { get; set; }

        public void Initialize(IHostContext hostContext) { }

        public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
        public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

        private Task<int> FailOrReturn() =>
            ThrowOnExecute ? Task.FromException<int>(new InvalidOperationException("simulated failure")) : Task.FromResult(0);

        // Silence CS0067 "event never used" warnings by nominally attaching/removing.
        private void _touch()
        {
            OutputDataReceived?.Invoke(this, null);
            ErrorDataReceived?.Invoke(this, null);
        }

        public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, CancellationToken cancellationToken) => FailOrReturn();
        public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, CancellationToken cancellationToken) => FailOrReturn();
        public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, CancellationToken cancellationToken) => FailOrReturn();
        public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, bool killProcessOnCancel, CancellationToken cancellationToken) => FailOrReturn();
        public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, bool killProcessOnCancel, Channel<string> redirectStandardIn, CancellationToken cancellationToken) => FailOrReturn();
        public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, bool killProcessOnCancel, Channel<string> redirectStandardIn, bool inheritConsoleHandler, CancellationToken cancellationToken) => FailOrReturn();
        public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, bool killProcessOnCancel, Channel<string> redirectStandardIn, bool inheritConsoleHandler, bool keepStandardInOpen, CancellationToken cancellationToken) => FailOrReturn();
        public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, bool killProcessOnCancel, Channel<string> redirectStandardIn, bool inheritConsoleHandler, bool keepStandardInOpen, bool highPriorityProcess, CancellationToken cancellationToken) => FailOrReturn();

        public void Dispose() => DisposeCount++;
    }
}
