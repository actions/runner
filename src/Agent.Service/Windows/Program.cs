using System;
using System.ServiceProcess;

namespace AgentService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(String[] args)
        {
            if (args != null && args.Length == 2 && args[0].Equals("init", StringComparison.InvariantCultureIgnoreCase))
            {
                string source = "VstsAgentService";
                System.Diagnostics.EventLog.WriteEntry(source, $"create event log trace source for service {args[1]}", System.Diagnostics.EventLogEntryType.Information, 100);
                return;
            }            
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new AgentService(args.Length > 0 ? args[0] : "VstsAgentService")
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
