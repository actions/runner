using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
#if OS_WINDOWS
    [ServiceLocator(Default = typeof(WindowsServiceControlManager))]
#elif OS_LINUX
    [ServiceLocator(Default = typeof(LinuxServiceControlManager))]
#elif OS_OSX
    [ServiceLocator(Default = typeof(OsxServiceControlManager))]
#endif
    // TODO: If this pattern repeats, avoid having this conditions and create WindowsServiceLocator/LinuxServiceLocator attribute
    public interface IServiceControlManager : IAgentService
    {
        void ConfigureService(AgentSettings settings, Dictionary<string, string> args, bool enforceSupplied);

        void StartService(string serviceName);
    }

    public abstract class ServiceControlManager : AgentService, IServiceControlManager
    {
        protected ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
        }

        protected void CalculateServiceName(AgentSettings settings, string serviceNamePattern, string serviceDisplayNamePattern)
        {
            Trace.Info(nameof(this.CalculateServiceName));
            var accountName = new Uri(settings.ServerUrl).Host.Split('.').FirstOrDefault();

            if (string.IsNullOrEmpty(accountName))
            {
                throw new InvalidOperationException(StringUtil.Loc("CannotFindHostName"));
            }

            settings.ServiceName = StringUtil.Format(serviceNamePattern, accountName, settings.AgentName);
            settings.ServiceDisplayName = StringUtil.Format(serviceDisplayNamePattern, accountName, settings.AgentName);
        }

        public abstract void ConfigureService(AgentSettings settings, Dictionary<string, string> args, bool enforceSupplied);

        public abstract void StartService(string serviceName);
    }
}