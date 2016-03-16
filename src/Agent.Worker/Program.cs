using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var tokenSource = new CancellationTokenSource();
            var hc = new HostContext("Worker", tokenSource.Token);
            return RunAsync(args, hc, tokenSource).GetAwaiter().GetResult();
        }

        public static async Task<int> RunAsync(
            string[] args,
            IHostContext hc,
            CancellationTokenSource tokenSource)
        {
            TraceSource trace = hc.GetTrace(nameof(Program));
            try
            {
                // Validate args.
                ArgUtil.NotNull(args, nameof(args));
                ArgUtil.Equal(3, args.Length, nameof(args.Length));
                ArgUtil.NotNullOrEmpty(args[0], $"{nameof(args)}[0]");
                ArgUtil.Equal("spawnclient", args[0].ToLowerInvariant(), $"{nameof(args)}[0]");
                ArgUtil.NotNullOrEmpty(args[1], $"{nameof(args)}[1]");
                ArgUtil.NotNullOrEmpty(args[2], $"{nameof(args)}[2]");
                ArgUtil.NotNull(tokenSource, nameof(tokenSource));
                var worker = hc.GetService<IWorker>();

                // Run the worker.
                return await worker.RunAsync(
                    pipeIn: args[1],
                    pipeOut: args[2],
                    hostTokenSource: tokenSource);
            }
            catch (Exception ex)
            {
                trace.Error(ex);
            }
            finally
            {
                hc.Dispose();
            }

            return 1;
        }
    }
}
