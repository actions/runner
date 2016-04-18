using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class OsxServiceControlManager : ServiceControlManager
    {
        public override bool ConfigureService(AgentSettings settings, CommandSettings command)
        {
            throw new NotImplementedException();
        }

        public override void StartService(string serviceName)
        {
            throw new NotImplementedException();
        }

        public override void StopService(string serviceName)
        {
            throw new NotImplementedException();
        }

        public override bool CheckServiceExists(string serviceName)
        {
            throw new NotImplementedException();
        }
    }
}