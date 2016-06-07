using System;
using System.Linq;
using Microsoft.VisualStudio.Services.Agent.Util;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
#if OS_WINDOWS
    [ServiceLocator(Default = typeof(WindowsServiceControlManager))]
#elif OS_LINUX
    [ServiceLocator(Default = typeof(SystemDControlManager))]
#elif OS_OSX
    [ServiceLocator(Default = typeof(OsxServiceControlManager))]
#endif
    // TODO: If this pattern repeats, avoid having this conditions and create WindowsServiceLocator/LinuxServiceLocator attribute
    public interface IServiceControlManager : IAgentService
    {
        bool ConfigureService(AgentSettings settings, CommandSettings command);

        void UnconfigureService();

        void StartService();

        void StopService();

        bool CheckServiceExists(string serviceName);
    }

    public abstract class ServiceControlManager : AgentService, IServiceControlManager
    {
        public string ServiceName { get; set; }
        public string ServiceDisplayName { get; set; }

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
                // TODO: Localize this error message:
                throw new InvalidOperationException("CannotFindHostName");
            }

            ServiceName = StringUtil.Format(serviceNamePattern, accountName, settings.AgentName);
            ServiceDisplayName = StringUtil.Format(serviceDisplayNamePattern, accountName, settings.AgentName);
        }

        protected void SaveServiceSettings()
        {
            string serviceConfigPath = IOUtil.GetServiceConfigFilePath();
            File.WriteAllText(serviceConfigPath, ServiceName, new UTF8Encoding(false));
            File.SetAttributes(serviceConfigPath, File.GetAttributes(serviceConfigPath) | FileAttributes.Hidden);
        }

        public abstract bool ConfigureService(AgentSettings settings, CommandSettings command);

        public abstract void UnconfigureService();

        public abstract void StartService();

        public abstract void StopService();

        public abstract bool CheckServiceExists(string serviceName);
    }
}