using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        Task ConfigureServiceAsync(AgentSettings settings, Dictionary<string, string> args, bool enforceSupplied);
    }

    public abstract class ServiceControlManager : AgentService, IServiceControlManager
    {
        protected ITerminal _term;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _term = hostContext.GetService<ITerminal>();
        }

        public abstract Task ConfigureServiceAsync(AgentSettings settings, Dictionary<string, string> args, bool enforceSupplied);
    }
}