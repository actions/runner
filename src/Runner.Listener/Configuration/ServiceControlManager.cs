using System;
using System.Linq;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Configuration
{
#if OS_WINDOWS
    [ServiceLocator(Default = typeof(WindowsServiceControlManager))]
    public interface IWindowsServiceControlManager : IRunnerService
    {
        void ConfigureService(RunnerSettings settings, CommandSettings command);

        void UnconfigureService();
    }
#endif

#if !OS_WINDOWS

#if OS_LINUX
    [ServiceLocator(Default = typeof(SystemDControlManager))]
#elif OS_OSX
    [ServiceLocator(Default = typeof(OsxServiceControlManager))]
#endif
    public interface ILinuxServiceControlManager : IRunnerService
    {
        void GenerateScripts(RunnerSettings settings);
    }
#endif

    public class ServiceControlManager : RunnerService
    {
        public void CalculateServiceName(RunnerSettings settings, string serviceNamePattern, string serviceDisplayNamePattern, out string serviceName, out string serviceDisplayName)
        {
            Trace.Entering();
            serviceName = string.Empty;
            serviceDisplayName = string.Empty;

            Uri accountUri = new Uri(settings.ServerUrl);
            string accountName = string.Empty;

            if (accountUri.Host.EndsWith(".githubusercontent.com", StringComparison.OrdinalIgnoreCase))
            {
                accountName = accountUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            }
            else
            {
                accountName = accountUri.Host.Split('.').FirstOrDefault();
            }

            if (string.IsNullOrEmpty(accountName))
            {
                throw new InvalidOperationException($"Cannot find GitHub organization name from server url: '{settings.ServerUrl}'");
            }

            serviceName = StringUtil.Format(serviceNamePattern, accountName, settings.PoolName, settings.AgentName);

            if (serviceName.Length > 80)
            {
                Trace.Verbose($"Calculated service name is too long (> 80 chars). Trying again by calculating a shorter name.");

                int exceededCharLength = serviceName.Length - 80;
                string accountNameSubstring = StringUtil.SubstringPrefix(accountName, 25);

                exceededCharLength -= accountName.Length - accountNameSubstring.Length;

                string poolNameSubstring = StringUtil.SubstringPrefix(settings.PoolName, 25);

                exceededCharLength -= settings.PoolName.Length - poolNameSubstring.Length;

                string runnerNameSubstring = settings.AgentName;

                // Only trim runner name if it's really necessary
                if (exceededCharLength > 0)
                {
                    runnerNameSubstring = StringUtil.SubstringPrefix(settings.AgentName, settings.AgentName.Length - exceededCharLength);
                }

                serviceName = StringUtil.Format(serviceNamePattern, accountNameSubstring, poolNameSubstring, runnerNameSubstring);
            }

            serviceDisplayName = StringUtil.Format(serviceDisplayNamePattern, accountName, settings.PoolName, settings.AgentName);

            Trace.Info($"Service name '{serviceName}' display name '{serviceDisplayName}' will be used for service configuration.");
        }
    }
}
