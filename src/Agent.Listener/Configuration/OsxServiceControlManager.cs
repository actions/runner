using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    public class OsxServiceControlManager : ServiceControlManager
    {
        public override Task ConfigureServiceAsync(AgentSettings settings, Dictionary<string, string> args, bool enforceSupplied)
        {
            throw new NotImplementedException();
        }
    }
}