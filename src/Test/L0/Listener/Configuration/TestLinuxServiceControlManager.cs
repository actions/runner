using System.IO;

using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener.Configuration
{
    public class TestLinuxServiceControlManager : LinuxServiceControlManager
    {
        protected override string GetUnitFile(string serviceName)
        {
            return Path.Combine(IOUtil.GetBinPath(), serviceName);
        }

        protected override bool CheckIfSystemdExists()
        {
            return true;
        }
    }
}