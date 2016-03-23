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
            using (var hc = new HostContext("Worker"))
            {
                return RunAsync(args, hc).GetAwaiter().GetResult();
            }
        }

        public static async Task<int> RunAsync(
            string[] args,
            IHostContext hc)
        {
            Tracing trace = hc.GetTrace(nameof(Program));
            try
            {
                trace.Info($"Version: {Constants.Agent.Version}");
                trace.Info($"Commit: {BuildConstants.Source.CommitHash}");

                // Validate args.
                ArgUtil.NotNull(args, nameof(args));
                ArgUtil.Equal(3, args.Length, nameof(args.Length));
                ArgUtil.NotNullOrEmpty(args[0], $"{nameof(args)}[0]");
                ArgUtil.Equal("spawnclient", args[0].ToLowerInvariant(), $"{nameof(args)}[0]");
                ArgUtil.NotNullOrEmpty(args[1], $"{nameof(args)}[1]");
                ArgUtil.NotNullOrEmpty(args[2], $"{nameof(args)}[2]");
                var worker = hc.GetService<IWorker>();

                // Run the worker.
                return await worker.RunAsync(
                    pipeIn: args[1],
                    pipeOut: args[2]);
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
