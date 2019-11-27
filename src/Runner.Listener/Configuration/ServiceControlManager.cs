using System;
using System.Linq;
using System.Text.RegularExpressions;
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

            if (string.IsNullOrEmpty(settings.RepoOrOrgName))
            {
                throw new InvalidOperationException($"Cannot find GitHub repository/organization name from server url: '{settings.ServerUrl}'");
            }

            // For the service name, replace any characters outside of the alpha-numeric set and ".", "_", "-" with "-"
            Regex regex = new Regex(@"[^0-9a-zA-Z._\-]");
            string repoOrOrgName = regex.Replace(settings.RepoOrOrgName, "-");

            serviceName = StringUtil.Format(serviceNamePattern, repoOrOrgName, settings.AgentName);

            if (serviceName.Length > 80)
            {
                Trace.Verbose($"Calculated service name is too long (> 80 chars). Trying again by calculating a shorter name.");

                int exceededCharLength = serviceName.Length - 80;
                string repoOrOrgNameSubstring = StringUtil.SubstringPrefix(repoOrOrgName, 45);

                exceededCharLength -= repoOrOrgName.Length - repoOrOrgNameSubstring.Length;

                string runnerNameSubstring = settings.AgentName;

                // Only trim runner name if it's really necessary
                if (exceededCharLength > 0)
                {
                    runnerNameSubstring = StringUtil.SubstringPrefix(settings.AgentName, settings.AgentName.Length - exceededCharLength);
                }

                serviceName = StringUtil.Format(serviceNamePattern, repoOrOrgNameSubstring, runnerNameSubstring);
            }

            serviceDisplayName = StringUtil.Format(serviceDisplayNamePattern, repoOrOrgName, settings.AgentName);

            Trace.Info($"Service name '{serviceName}' display name '{serviceDisplayName}' will be used for service configuration.");
        }
    }
}
