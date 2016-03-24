using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace AgentService
{
    public partial class AgentService : ServiceBase
    {
        private Process AgentListener { get; set; }
        private bool Stopping { get; set; }
        private object ServiceLock { get; set; }
        private Task RunningLoop { get; set; }

        public AgentService(string serviceName)
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
                            WriteInfo("Starting VSTS Agent Service");
                            TimeSpan timeBetweenRetries = TimeSpan.FromSeconds(5);

                            lock (ServiceLock)
                            {
                                stopping = Stopping;
                            }

                            while (!stopping)
                            {
                                WriteInfo("Starting VSTS Agent listener");
                                lock (ServiceLock)
                                {
                                    AgentListener = CreateAgentListener();
                                    AgentListener.Start();
                                }

                                AgentListener.WaitForExit();
                                int exitCode = AgentListener.ExitCode;

                                // Handle error code 1? 
                                // If agent fails (because its not configured) also returns error code 1, in such case we dont want to run the service
                                // Killing a process also returns error code 1, but we want to restart the process here.
                                // TODO: change the error code for run method if agent is not configured?

                                if (exitCode == 2)
                                {
                                    // Agent wants to stop the service as well
                                    Stopping = true;
                                    WriteInfo(Resource.ServiceRequestedToStop);
                                    ExitCode = exitCode;
                                    Stop();
                                }
                                else
                                {
                                    // wait for few seconds before restarting the process
                                    Thread.Sleep(timeBetweenRetries);
                                }
                            }

                            lock (ServiceLock)
                            {
                                AgentListener.Dispose();
                                AgentListener = null;
                            }
                        }
                        catch (Exception exception)
                        {
                            WriteException(exception);
                            ExitCode = 1;
                            Stop();
                        }
                    });
        }

        private Process CreateAgentListener()
        {
            string exeLocation = Assembly.GetEntryAssembly().Location;
            string agentExeLocation = Path.Combine(Path.GetDirectoryName(exeLocation), "Agent.Listener.exe");
            Process newProcess = new Process();
            newProcess.StartInfo = new ProcessStartInfo(agentExeLocation, "run");
            newProcess.StartInfo.CreateNoWindow = true;
            newProcess.StartInfo.UseShellExecute = false;
            newProcess.StartInfo.RedirectStandardInput = true;
            return newProcess;
        }

        protected override void OnStop()
        {
            lock (ServiceLock)
            {
                Stopping = true;

                // TODO If agent service is killed make sure AgentListener also is killed
                try
                {
                    if (AgentListener != null && !AgentListener.HasExited)
                    {
                        // Try to let the agent process know that we are stopping
                        // TODO: This is not working, fix it
                        AgentListener.StandardInput.WriteLine("\x03");

                        // Wait for the process to finish (give it up to 30 seconds)
                        AgentListener.WaitForExit(30000);
                        // if agent is still running, kill it
                        if (!AgentListener.HasExited)
                        {
                            AgentListener.Kill();
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
        }

        private static void WriteToEventLog(string eventText, EventLogEntryType entryType)
        {
            String source = "vstsAgentService";
            EventLog.WriteEntry(source, eventText, entryType, 100);
        }

        private static string GetDiagnosticFolderPath()
        {
            return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)), "_diag");
        }

        private void WriteError(int exitCode)
        {
            String diagFolder = GetDiagnosticFolderPath();
            String eventText = String.Format(
                CultureInfo.InvariantCulture,
                "The AgentListener process failed to start successfully. It exited with code {0}. Check the latest Agent log files in {1} for more information.",
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
    }
}
