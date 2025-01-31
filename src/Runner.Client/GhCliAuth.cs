using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;

namespace Runner.Client
{
    public class GhCliAuth {
        public static async Task<string> GetToken(string workingDirectory, ITraceWriter trace, CancellationToken token) {
            string line = null;
            void handleOutput(object s, ProcessDataReceivedEventArgs e)
            {
                line ??= e.Data;
            }
            try {
                var gh = WhichUtil.Which("gh", require: false, trace: trace);
                if (string.IsNullOrWhiteSpace(gh))
                {
                    return null;
                }
                var ghInvoker = new ProcessInvoker(trace);
                ghInvoker.OutputDataReceived += handleOutput;
                await ghInvoker.ExecuteAsync(workingDirectory, gh, "auth token", new Dictionary<string, string>(), token);
                return line;
            } catch {
                return null;
            }
        }
    }
}