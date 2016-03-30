using System;
using System.IO;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    [ServiceLocator(Default = typeof(NativeLinuxServiceHelper))]
    public interface INativeLinuxServiceHelper : IAgentService
    {
        string GetUnitFile(string serviceName);

        bool CheckIfSystemdExists();
    }

    public class NativeLinuxServiceHelper : AgentService, INativeLinuxServiceHelper
    {
        public const string SystemdPathPrefix = "/etc/systemd/system";
        private const string InitFileCommandLocation = "/proc/1/comm";
        private ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
        }

        public string GetUnitFile(string serviceName)
        {
            return Path.Combine(SystemdPathPrefix, serviceName);
        }

        public bool CheckIfSystemdExists()
        {
            Trace.Entering();
            try
            {
                var commName = File.ReadAllText(InitFileCommandLocation).Trim();
                return commName.Equals("systemd", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                _term.WriteError(StringUtil.Loc("CanNotFindSystemd"));

                return false;
            }
        }


    }
}