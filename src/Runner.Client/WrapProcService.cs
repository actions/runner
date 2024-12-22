using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using GitHub.Runner.Common;

namespace Runner.Client
{
    partial class Program
    {
        private class WrapProcService : IProcessInvoker
        {
            public WrapProcService() {
                org = new ProcessInvokerWrapper();
                org.OutputDataReceived += (s,e) => OutputDataReceived?.Invoke(s, e);
                org.ErrorDataReceived += (s,e) => ErrorDataReceived?.Invoke(s, e);
            }

            private IProcessInvoker org;
            private IHostContext _context;

            public event EventHandler<ProcessDataReceivedEventArgs> OutputDataReceived;
            public event EventHandler<ProcessDataReceivedEventArgs> ErrorDataReceived;

            public void Dispose()
            {
                org.Dispose();
            }

            public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, bool killProcessOnCancel, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, bool killProcessOnCancel, Channel<string> redirectStandardIn, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, bool killProcessOnCancel, Channel<string> redirectStandardIn, bool inheritConsoleHandler, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, bool killProcessOnCancel, Channel<string> redirectStandardIn, bool inheritConsoleHandler, bool keepStandardInOpen, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public Task<int> ExecuteAsync(string workingDirectory, string fileName, string arguments, IDictionary<string, string> environment, bool requireExitCodeZero, Encoding outputEncoding, bool killProcessOnCancel, Channel<string> redirectStandardIn, bool inheritConsoleHandler, bool keepStandardInOpen, bool highPriorityProcess, CancellationToken cancellationToken)
            {
                try {
                    var queue = _context.GetService<ExternalQueueService>();
                    int i = arguments.IndexOf("spawnclient");
                    if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    {
                        var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                        fileName = Path.Join(_context.GetDirectory(WellKnownDirectory.ConfigRoot), "bin", $"{queue.Prefix}.Worker{queue.Suffix}");
                        arguments = i == -1 ? arguments : arguments.Substring(i);
                        if(string.IsNullOrWhiteSpace(binpath)) {
                            arguments = $"spawn \"{fileName}\" {arguments}";
                            fileName = Environment.ProcessPath;
                        } else {
#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                            arguments = $"\"{Path.Join(binpath, "Runner.Client.dll")}\" spawn \"{fileName}\" {arguments}";
                            fileName = Sdk.Utils.DotNetMuxer.MuxerPath ?? WhichUtil.Which("dotnet", true);
#else
                            arguments = $"spawn \"{fileName}\" {arguments}";
                            fileName = Path.Join(binpath, $"Runner.Client{IOUtil.ExeExtension}");
#endif
                        }
                        return org.ExecuteAsync(workingDirectory, fileName, arguments, environment, requireExitCodeZero, outputEncoding, killProcessOnCancel, redirectStandardIn, inheritConsoleHandler, keepStandardInOpen, highPriorityProcess, cancellationToken);
                    } else {
                        return org.ExecuteAsync(workingDirectory, Path.Join(_context.GetDirectory(WellKnownDirectory.ConfigRoot), "bin", $"{queue.Prefix}.Worker{queue.Suffix}"), i == -1 ? arguments : arguments.Substring(i), environment, requireExitCodeZero, outputEncoding, killProcessOnCancel, redirectStandardIn, inheritConsoleHandler, keepStandardInOpen, highPriorityProcess, cancellationToken);
                    }
                } catch {}
                if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    var binpath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    if(string.IsNullOrWhiteSpace(binpath)) {
                        arguments = $"spawn \"{fileName}\" {arguments}";
                        fileName = Environment.ProcessPath;
                    } else {
#if !OS_LINUX && !OS_WINDOWS && !OS_OSX && !X64 && !X86 && !ARM && !ARM64
                        arguments = $"\"{Path.Join(binpath, "Runner.Client.dll")}\" spawn \"{fileName}\" {arguments}";
                        fileName = Sdk.Utils.DotNetMuxer.MuxerPath ?? WhichUtil.Which("dotnet", true);
#else
                        arguments = $"spawn \"{fileName}\" {arguments}";
                        fileName = Path.Join(binpath, $"Runner.Client{IOUtil.ExeExtension}");
#endif
                    }
                    return org.ExecuteAsync(workingDirectory, fileName, arguments, new Dictionary<string, string>() { {"RUNNER_SERVER_CONFIG_ROOT", _context.GetDirectory(WellKnownDirectory.ConfigRoot)} }, requireExitCodeZero, outputEncoding, killProcessOnCancel, redirectStandardIn, inheritConsoleHandler, keepStandardInOpen, highPriorityProcess, cancellationToken);
                }
                return org.ExecuteAsync(workingDirectory, fileName, arguments, new Dictionary<string, string>() { {"RUNNER_SERVER_CONFIG_ROOT", _context.GetDirectory(WellKnownDirectory.ConfigRoot)} }, requireExitCodeZero, outputEncoding, killProcessOnCancel, redirectStandardIn, inheritConsoleHandler, keepStandardInOpen, highPriorityProcess, cancellationToken);
            }

            public void Initialize(IHostContext context)
            {
                org.Initialize(context);
                this._context = context;
            }
        }
    }
}
