using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent
{
    public static class ProcessInvoker
    {
        public static int RunExe(IHostContext hostContext, string filename, string arguments)
        {
            TraceSource _trace = hostContext.GetTrace("ProcessInvoker");
            _trace.Info("Starting process {0} {1}", filename, arguments);

            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = filename,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            object syncObject = new object();
            using (Process proc = new Process())
            {
                bool processExited = false;

                proc.StartInfo = processStartInfo;
                proc.EnableRaisingEvents = true;

                proc.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
                {
                    // at the end of the process, the event fires one last time with null
                    if (e.Data != null)
                    {
                        lock (syncObject)
                        {
                            _trace.Info(e.Data);
                        }
                    }
                };
                proc.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
                {
                    // at the end of the process, the event fires one last time with null
                    if (e.Data != null)
                    {
                        lock (syncObject)
                        {
                            _trace.Info(e.Data);
                        }
                    }
                };

                proc.Exited += delegate (object sender, System.EventArgs e)
                {
                    processExited = true;
                };

                Stopwatch stopwatch = Stopwatch.StartNew();
                bool newProcessStarted = proc.Start();
                if (!newProcessStarted)
                {
                    _trace.Verbose("Used existing process instead of starting new one for " + filename);
                }
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                int seconds = 0;
                while (!proc.WaitForExit(1000))
                {
                    seconds++;
                    if ((seconds % 30) == 0)
                    {
                        _trace.Info(
                            "Waiting on process {0} ({1} seconds elapsed)",
                                proc.Id,
                                seconds);
                    }

                    if (processExited)
                    {
                        break;
                    }
                }

                // Wait for process to exit without hard timeout, which will 
                // ensure that we've read everything from the stdout and stderr.
                proc.WaitForExit();
                stopwatch.Stop();

                _trace.Info("Process finished: fileName={0} arguments={1} exitCode={2} in {3} ms", filename, arguments, proc.ExitCode, stopwatch.ElapsedMilliseconds);

                return proc.ExitCode;
            }
        }

    }
}
