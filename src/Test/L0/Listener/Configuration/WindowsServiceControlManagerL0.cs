using Microsoft.VisualStudio.Services.Agent.Listener;
using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TeamFoundation;

using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener.Configuration
{
    public sealed class WindowsServiceControlManagerL0
    {
        private Mock<IProcessInvoker> _processInvoker;
        private Mock<IPromptManager> _promptManager;
        private Mock<INativeWindowsServiceHelper> _windowsServiceHelper;
        private string _expectedLogonAccount = "NT AUTHORITY\\LOCAL SERVICE";
        private string _expectedLogonPassword = "test";

        public WindowsServiceControlManagerL0()
        {
            _processInvoker = new Mock<IProcessInvoker>();
            _promptManager = new Mock<IPromptManager>();
            _windowsServiceHelper = new Mock<INativeWindowsServiceHelper>();

            _processInvoker.Setup(
                x =>
                x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            _windowsServiceHelper.Setup(x => x.GetDefaultServiceAccount()).Returns(new NTAccount(_expectedLogonAccount));
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(this, testName);
            tc.SetSingleton<IProcessInvoker>(_processInvoker.Object);
            tc.SetSingleton<IPromptManager>(_promptManager.Object);
            tc.SetSingleton<INativeWindowsServiceHelper>(_windowsServiceHelper.Object);
            return tc;
        }

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
#endif
        public void WindowsServiceControlManagerShouldInstallService()
        {
            using (var tc = CreateTestContext())
            {
                var serviceControlManager = new WindowsServiceControlManager();
                serviceControlManager.Initialize(tc);
                var agentSettings = new AgentSettings { ServerUrl = "http://server.name", AgentName = "myagent" };
                var command = new CommandSettings(
                    tc,
                    new[]
                    {
                        "--windowslogonaccount", _expectedLogonAccount,
                        "--windowslogonpassword", _expectedLogonPassword,
                        "--unattended"
                    });
                serviceControlManager.ConfigureService(agentSettings, command);
                Assert.Equal("vstsagent.server.myagent", serviceControlManager.ServiceName);
                Assert.Equal("VSTS Agent (server.myagent)", serviceControlManager.ServiceDisplayName);
            }
        }

/*
#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
#endif
        public void PromptForPasswordForNonDefaultServiceUserAccount()
        {
            using (var tc = CreateTestContext())
            {
                var serviceControlManager = new WindowsServiceControlManager();
                serviceControlManager.Initialize(tc);
                var agentSettings = new AgentSettings { ServerUrl = "http://server.name", AgentName = "myagent" };
                var command = new CommandSettings(
                    tc,
                    new[]
                    {
                        "--windowslogonaccount", "domain\\randomuser",
                        "--windowslogonpassword", _expectedLogonPassword,
                        "--unattended"
                    });

                serviceControlManager.ConfigureService(agentSettings, command);

                _windowsServiceHelper.Verify(x => x.IsValidCredential("domain", "randomuser", _expectedLogonPassword));
            }
        }
*/

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
#endif
        public void CheckServiceExistsShouldWorkCorrectly()
        {
            using (var tc = CreateTestContext())
            {
                var serviceControlManager = new WindowsServiceControlManager();
                serviceControlManager.Initialize(tc);

                Assert.Equal(serviceControlManager.CheckServiceExists("NoService" + Guid.NewGuid()), false);
                // TODO: qvoid creating testable and write a wrapper for ServiceController as it can't be mocked 
                _windowsServiceHelper.Setup(x => x.TryGetServiceController("test"))
                    .Returns(new ServiceController("test"));

                Assert.Equal(serviceControlManager.CheckServiceExists("test"), true);
            }
        }
    }
}