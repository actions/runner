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

using Test.L0.Listener.Configuration.Testable;

using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener.Configuration
{
    public sealed class WindowsServiceControlManagerL0
    {
        private Mock<IConsoleWizard> _reader;

        private Mock<IProcessInvoker> _processInvoker;

        private Mock<IWindowsSecurityManager> _securityManager;

        private string _expectedLogonAccount = "NT AUTHORITY\\LOCAL SERVICE";

        private string _expectedLogonPassword = "test";

        public WindowsServiceControlManagerL0()
        {
            _reader = new Mock<IConsoleWizard>();
            _processInvoker = new Mock<IProcessInvoker>();
            _securityManager = new Mock<IWindowsSecurityManager>();

            _processInvoker.Setup(
                x =>
                x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            _reader.Setup(
                x => x.ReadValue(
                    "windowslogonaccount",
                    It.IsAny<string>(),
                         // description
                    It.IsAny<bool>(),
                         // secret
                    It.IsAny<string>(),
                         // defaultValue
                    It.IsAny<Func<string, bool>>(),
                         // validator
                    It.IsAny<Dictionary<string, string>>(),
                         //args
                    It.IsAny<bool>() // unattended
                         )).Returns(_expectedLogonAccount);

            _reader.Setup(
                x => x.ReadValue(
                    "windowslogonpassword",
                    It.IsAny<string>(),
                         // description
                    It.IsAny<bool>(),
                         // secret
                    It.IsAny<string>(),
                         // defaultValue
                    It.IsAny<Func<string, bool>>(),
                         // validator
                    It.IsAny<Dictionary<string, string>>(),
                         //args
                    It.IsAny<bool>() // unattended
                         )).Returns(_expectedLogonPassword);


            _securityManager.Setup(x => x.GetDefaultServiceAccount()).Returns(new NTAccount(_expectedLogonAccount));
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(this, testName);
            tc.SetSingleton<IConsoleWizard>(_reader.Object);
            tc.SetSingleton<IProcessInvoker>(_processInvoker.Object);
            tc.SetSingleton<IWindowsSecurityManager>(_securityManager.Object);
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
                var serviceControlManager = new TestWindowsServiceControlManager();
                serviceControlManager.Initialize(tc);
                var agentSettings = new AgentSettings { ServerUrl = "http://server.name", AgentName = "myagent" };
                serviceControlManager.ConfigureService(
                    agentSettings,
                    new Dictionary<string, string>
                        {
                            { "windowslogonaccount", "NT AUTHORITY\\LOCAL SERVICE" },
                            { "windowslogonpassword", "test" }
                        },
                    true);

                Assert.Equal("vstsagent.server.myagent", agentSettings.ServiceName);
                Assert.Equal("VSTS Agent (server.myagent)", agentSettings.ServiceDisplayName);
            }
        }

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
#endif
        public void PromptForPasswordForNonDefaultServiceUserAccount()
        {
            using (var tc = CreateTestContext())
            {
                var serviceControlManager = new TestWindowsServiceControlManager();
                serviceControlManager.Initialize(tc);
                var agentSettings = new AgentSettings { ServerUrl = "http://server.name", AgentName = "myagent" };

                _reader.Setup(
                    x => x.ReadValue(
                        "windowslogonaccount",
                        It.IsAny<string>(),
                             // description
                        It.IsAny<bool>(),
                             // secret
                        It.IsAny<string>(),
                             // defaultValue
                        It.IsAny<Func<string, bool>>(),
                             // validator
                        It.IsAny<Dictionary<string, string>>(),
                             // args
                        It.IsAny<bool>())).Returns("domain\\randomuser");

                serviceControlManager.ConfigureService(agentSettings, null, true);

                _reader.Verify(
                    x =>
                    x.ReadValue(
                        "windowslogonpassword",
                        It.IsAny<string>(),
                        true,
                        It.IsAny<string>(),
                        It.IsAny<Func<string, bool>>(),
                        It.IsAny<Dictionary<string, string>>(),
                        It.IsAny<bool>()),
                    Times.Once);
            }
        }


#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
#endif
        public void CheckServiceExistsShouldWorkCorrectly()
        {
            using (var tc = CreateTestContext())
            {
                var serviceControlManager = new TestWindowsServiceControlManager();
                serviceControlManager.Initialize(tc);

                Assert.Equal(serviceControlManager.CheckServiceExists("NoService" + Guid.NewGuid()), false);
                // TODO: qvoid creating testable and write a wrapper for ServiceController as it can't be mocked 
                serviceControlManager.SelectedServiceController = new ServiceController("test");

                Assert.Equal(serviceControlManager.CheckServiceExists("test"), true);
            }
        }
    }
}