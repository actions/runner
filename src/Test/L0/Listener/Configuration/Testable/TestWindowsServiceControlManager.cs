using System.ServiceProcess;

using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;

namespace Test.L0.Listener.Configuration.Testable
{
    public class TestWindowsServiceControlManager : WindowsServiceControlManager
    {
        public ServiceController SelectedServiceController { get; set; }

        protected override void InstallService(
            string serviceName,
            string serviceDisplayName,
            string logonAccount,
            string logonPassword)

        {
            // This method itself does not need to be unittested as it only makes pinvoke calls.
        }

        protected override ServiceController TryGetServiceController(string serviceName)
        {
            // TODO ServiceController is not mockable, create tesable for ServiceController instead of this implementation
            // Or create a wrapper for ServiceController
            return SelectedServiceController;
        }
    }
}