using System;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public static class Program
    {
        public static Int32 Main(string[] args)
        {
            int resultCode = 0;
            using (HostContext hc = new HostContext("Worker"))
            {
                TraceSource trace = hc.GetTrace(nameof(Program));
                try
                {
                    if (null != args && 3 == args.Length && "spawnclient".Equals(args[0].ToLower()))
                    {
                        var worker = hc.GetService<IWorker>();
                        resultCode = worker.RunAsync(args[1], args[2], hc.CancellationTokenSource).GetAwaiter().GetResult();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore OperationCanceledException and TaskCanceledException exceptions
                }
                catch (AggregateException errors)
                {
                    // Ignore OperationCanceledException and TaskCanceledException exceptions
                    errors.Handle(e => e is OperationCanceledException);
                }
                catch (Exception ex)
                {
                    trace.Error(ex);
                    resultCode = 1;
                }
            }
            return resultCode;
        }
    }
}
