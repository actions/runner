using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace RunnerService
{
    public partial class RunnerService : ServiceBase
    {
        public const string EventSourceName = "ActionsRunnerService";
        private const int CTRL_C_EVENT = 0;
        private const int CTRL_BREAK_EVENT = 1;
        private bool _restart = false;
        private Process RunnerListener { get; set; }
        private bool Stopping { get; set; }
        private object ServiceLock { get; set; }
        private Task RunningLoop { get; set; }

        public RunnerService(string serviceName)
        {
            ServiceLock = new Object();
            InitializeComponent();
            base.ServiceName = serviceName;
        }

        protected override void OnStart(string[] args)
        {
            RunningLoop = Task.Run(
                () =>
                    {
                        try
                        {
                            bool stopping;
                            WriteInfo("Starting Actions Runner Service");
                            TimeSpan timeBetweenRetries = TimeSpan.FromSeconds(5);

                            lock (ServiceLock)
                            {
                                stopping = Stopping;
                            }

                            while (!stopping)
                            {
                                WriteInfo("Starting Actions Runner listener");
                                lock (ServiceLock)
                                {
                                    RunnerListener = CreateRunnerListener();
                                    RunnerListener.OutputDataReceived += RunnerListener_OutputDataReceived;
                                    RunnerListener.ErrorDataReceived += RunnerListener_ErrorDataReceived;
                                    RunnerListener.Start();
                                    RunnerListener.BeginOutputReadLine();
                                    RunnerListener.BeginErrorReadLine();
                                }

                                RunnerListener.WaitForExit();
                                int exitCode = RunnerListener.ExitCode;

                                // exit code 0 and 1 need stop service
                                // exit code 2 and 3 need restart runner
                                switch (exitCode)
                                {
                                    case 0:
                                        Stopping = true;
                                        WriteInfo(Resource.RunnerExitWithoutError);
                                        break;
                                    case 1:
                                        Stopping = true;
                                        WriteInfo(Resource.RunnerExitWithTerminatedError);
                                        break;
                                    case 2:
                                        WriteInfo(Resource.RunnerExitWithError);
                                        break;
                                    case 3:
                                        WriteInfo(Resource.RunnerUpdateInProcess);
                                        var updateResult = HandleRunnerUpdate();
                                        if (updateResult == RunnerUpdateResult.Succeed)
                                        {
                                            WriteInfo(Resource.RunnerUpdateSucceed);
                                        }
                                        else if (updateResult == RunnerUpdateResult.Failed)
                                        {
                                            WriteInfo(Resource.RunnerUpdateFailed);
                                            Stopping = true;
                                        }
                                        else if (updateResult == RunnerUpdateResult.SucceedNeedRestart)
                                        {
                                            WriteInfo(Resource.RunnerUpdateRestartNeeded);
                                            _restart = true;
                                            ExitCode = int.MaxValue;
                                            Stop();
                                        }
                                        break;
                                    default:
                                        WriteInfo(Resource.RunnerExitWithUndefinedReturnCode);
                                        break;
                                }

                                if (Stopping)
                                {
                                    ExitCode = exitCode;
                                    Stop();
                                }
                                else
                                {
                                    // wait for few seconds before restarting the process
                                    Thread.Sleep(timeBetweenRetries);
                                }

                                lock (ServiceLock)
                                {
                                    RunnerListener.OutputDataReceived -= RunnerListener_OutputDataReceived;
                                    RunnerListener.ErrorDataReceived -= RunnerListener_ErrorDataReceived;
                                    RunnerListener.Dispose();
                                    RunnerListener = null;
                                    stopping = Stopping;
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            WriteException(exception);
                            ExitCode = 99;
                            Stop();
                        }
                    });
        }

        private void RunnerListener_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                WriteToEventLog(e.Data, EventLogEntryType.Error);
            }
        }

        private void RunnerListener_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                WriteToEventLog(e.Data, EventLogEntryType.Information);
            }
        }

        private Process CreateRunnerListener()
        {
            string exeLocation = Assembly.GetEntryAssembly().Location;
            string runnerExeLocation = Path.Combine(Path.GetDirectoryName(exeLocation), "Runner.Listener.exe");
            Process newProcess = new Process();
            newProcess.StartInfo = new ProcessStartInfo(runnerExeLocation, "run --startuptype service");
            newProcess.StartInfo.CreateNoWindow = true;
            newProcess.StartInfo.UseShellExecute = false;
            newProcess.StartInfo.RedirectStandardInput = true;
            newProcess.StartInfo.RedirectStandardOutput = true;
            newProcess.StartInfo.RedirectStandardError = true;
            return newProcess;
        }

        protected override void OnShutdown()
        {
            SendCtrlSignalToRunnerListener(CTRL_BREAK_EVENT);
            base.OnShutdown();
        }

        protected override void OnStop()
        {
            lock (ServiceLock)
            {
                Stopping = true;

                // throw exception during OnStop() will make SCM think the service crash and trigger recovery option.
                // in this way we can self-update the service host.
                if (_restart)
                {
                    throw new Exception(Resource.CrashServiceHost);
                }

                SendCtrlSignalToRunnerListener(CTRL_C_EVENT);
            }
        }

        // this will send either Ctrl-C or Ctrl-Break to runner.listener
        // Ctrl-C will be used for OnStop()
        // Ctrl-Break will be used for OnShutdown()
        private void SendCtrlSignalToRunnerListener(uint signal)
        {
            try
            {
                if (RunnerListener != null && !RunnerListener.HasExited)
                {
                    // Try to let the runner process know that we are stopping
                    //Attach service process to console of Runner.Listener process. This is needed,
                    //because windows service doesn't use its own console.
                    if (AttachConsole((uint)RunnerListener.Id))
                    {
                        //Prevent main service process from stopping because of Ctrl + C event with SetConsoleCtrlHandler
                        SetConsoleCtrlHandler(null, true);
                        try
                        {
                            //Generate console event for current console with GenerateConsoleCtrlEvent (processGroupId should be zero)
                            GenerateConsoleCtrlEvent(signal, 0);
                            //Wait for the process to finish (give it up to 30 seconds)
                            RunnerListener.WaitForExit(30000);
                        }
                        finally
                        {
                            //Disconnect from console and restore Ctrl+C handling by main process
                            FreeConsole();
                            SetConsoleCtrlHandler(null, false);
                        }
                    }

                    // if runner is still running, kill it
                    if (!RunnerListener.HasExited)
                    {
                        RunnerListener.Kill();
                    }
                }
            }
            catch (Exception exception)
            {
                // InvalidOperationException is thrown when there is no process associated to the process object. 
                // There is no process to kill, Log the exception and shutdown the service. 
                // If we don't handle this here, the service get into a state where it can neither be stoped nor restarted (Error 1061)
                WriteException(exception);
            }
        }

        private RunnerUpdateResult HandleRunnerUpdate()
        {
            // sleep 5 seconds wait for upgrade script to finish
            Thread.Sleep(5000);

            // looking update result record under _diag folder (the log file itself will indicate the result)
            // SelfUpdate-20160711-160300.log.succeed or SelfUpdate-20160711-160300.log.fail
            // Find the latest upgrade log, make sure the log is created less than 15 seconds.
            // When log file named as SelfUpdate-20160711-160300.log.succeedneedrestart, Exit(int.max), during Exit() throw Exception, this will trigger SCM to recovery the service by restart it
            // since SCM cache the ServiceHost in memory, sometime we need update the servicehost as well, in this way we can upgrade the ServiceHost as well.

            DirectoryInfo dirInfo = new DirectoryInfo(GetDiagnosticFolderPath());
            FileInfo[] updateLogs = dirInfo.GetFiles("SelfUpdate-*-*.log.*") ?? new FileInfo[0];
            if (updateLogs.Length == 0)
            {
                // totally wrong, we are not even get a update log.
                return RunnerUpdateResult.Failed;
            }
            else
            {
                FileInfo latestLogFile = null;
                DateTime latestLogTimestamp = DateTime.MinValue;
                foreach (var logFile in updateLogs)
                {
                    int timestampStartIndex = logFile.Name.IndexOf("-") + 1;
                    int timestampEndIndex = logFile.Name.LastIndexOf(".log") - 1;
                    string timestamp = logFile.Name.Substring(timestampStartIndex, timestampEndIndex - timestampStartIndex + 1);
                    DateTime updateTime;
                    if (DateTime.TryParseExact(timestamp, "yyyyMMdd-HHmmss", null, DateTimeStyles.None, out updateTime) &&
                        updateTime > latestLogTimestamp)
                    {
                        latestLogFile = logFile;
                        latestLogTimestamp = updateTime;
                    }
                }

                if (latestLogFile == null || latestLogTimestamp == DateTime.MinValue)
                {
                    // we can't find update log with expected naming convention.
                    return RunnerUpdateResult.Failed;
                }

                latestLogFile.Refresh();
                if (DateTime.UtcNow - latestLogFile.LastWriteTimeUtc > TimeSpan.FromSeconds(15))
                {
                    // the latest update log we find is more than 15 sec old, the update process is busted.
                    return RunnerUpdateResult.Failed;
                }
                else
                {
                    string resultString = Path.GetExtension(latestLogFile.Name).TrimStart('.');
                    RunnerUpdateResult result;
                    if (Enum.TryParse<RunnerUpdateResult>(resultString, true, out result))
                    {
                        // return the result indicated by the update log.
                        return result;
                    }
                    else
                    {
                        // can't convert the result string, return failed to stop the service.
                        return RunnerUpdateResult.Failed;
                    }
                }
            }
        }

        private void WriteToEventLog(string eventText, EventLogEntryType entryType)
        {
            EventLog.WriteEntry(EventSourceName, eventText, entryType, 100);
        }

        private string GetDiagnosticFolderPath()
        {
            return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)), "_diag");
        }

        private void WriteError(int exitCode)
        {
            String diagFolder = GetDiagnosticFolderPath();
            String eventText = String.Format(
                CultureInfo.InvariantCulture,
                "The Runner.Listener process failed to start successfully. It exited with code {0}. Check the latest Runner log files in {1} for more information.",
                exitCode,
                diagFolder);

            WriteToEventLog(eventText, EventLogEntryType.Error);
        }

        private void WriteInfo(string message)
        {
            WriteToEventLog(message, EventLogEntryType.Information);
        }

        private void WriteException(Exception exception)
        {
            WriteToEventLog(exception.ToString(), EventLogEntryType.Error);
        }

        private enum RunnerUpdateResult
        {
            Succeed,
            Failed,
            SucceedNeedRestart,
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        // Delegate type to be used as the Handler Routine for SetConsoleCtrlHandler
        delegate Boolean ConsoleCtrlDelegate(uint CtrlType);
    }
}
