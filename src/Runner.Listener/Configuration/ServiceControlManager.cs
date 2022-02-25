using System;
using System.Linq;
using System.Text.RegularExpressions;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener.Configuration
{
    [ServiceLocator(Default = typeof(WindowsServiceControlManager))]
    public interface IWindowsServiceControlManager : IRunnerService
    {
        void ConfigureService(RunnerSettings settings, CommandSettings command);

        void UnconfigureService();
    }

    [ServiceLocator(Default = typeof(SystemDControlManager), OSX = typeof(OsxServiceControlManager))]
    public interface ILinuxServiceControlManager : IRunnerService
    {
        void GenerateScripts(RunnerSettings settings);
    }

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
            if (serviceName.Length > MaxServiceNameLength)
            {
                Trace.Verbose($"Calculated service name is too long (> {MaxServiceNameLength} chars). Trying again by calculating a shorter name.");
                // Add 5 to add -xxxx random number on the end
                int exceededCharLength = serviceName.Length - MaxServiceNameLength + 5;
                string repoOrOrgNameSubstring = StringUtil.SubstringPrefix(repoOrOrgName, MaxRepoOrgCharacters);

                exceededCharLength -= repoOrOrgName.Length - repoOrOrgNameSubstring.Length;

                string runnerNameSubstring = settings.AgentName;

                // Only trim runner name if it's really necessary
                if (exceededCharLength > 0)
                {
                    runnerNameSubstring = StringUtil.SubstringPrefix(settings.AgentName, settings.AgentName.Length - exceededCharLength);
                }

                // Lets add a suffix with a random number to reduce the chance of collisions between runner names once we truncate
                var random = new Random();
                var num = random.Next(1000, 9999).ToString();
                runnerNameSubstring +=$"-{num}";
                serviceName = StringUtil.Format(serviceNamePattern, repoOrOrgNameSubstring, runnerNameSubstring);
            }

            serviceDisplayName = StringUtil.Format(serviceDisplayNamePattern, repoOrOrgName, settings.AgentName);

            Trace.Info($"Service name '{serviceName}' display name '{serviceDisplayName}' will be used for service configuration.");
        }
        #if (OS_LINUX || OS_OSX)
            const int MaxServiceNameLength = 150;
            const int MaxRepoOrgCharacters = 70;
        #elif OS_WINDOWS
            const int MaxServiceNameLength = 80;
            const int MaxRepoOrgCharacters = 45;
        #endif
    }
}
