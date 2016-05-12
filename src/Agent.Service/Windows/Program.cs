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
            if (args != null && args.Length == 1 && args[0].Equals("init", StringComparison.InvariantCultureIgnoreCase))
            {                
                System.Diagnostics.EventLog.WriteEntry(AgentService.EventSourceName, "create event log trace source for vsts-agent service", System.Diagnostics.EventLogEntryType.Information, 100);
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
