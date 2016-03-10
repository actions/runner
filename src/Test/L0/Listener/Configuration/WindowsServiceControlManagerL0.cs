using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener.Configuration
{
    public sealed class WindowsServiceControlManagerL0
    {
        private Mock<IConsoleWizard> _reader;
        private Mock<IProcessInvoker> _processInvoker;
        private string _expectedLogonAccount = "NT AUTHORITY\\LOCAL SERVICE";
        private string _expectedLogonPassword = "test";

        public WindowsServiceControlManagerL0()
        {
            _reader = new Mock<IConsoleWizard>();
            _processInvoker = new Mock<IProcessInvoker>();

            _processInvoker.Setup(
                x =>
                x.Execute(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>()));

            _processInvoker.Setup(x => x.WaitForExit(It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));

            _reader.Setup(x => x.ReadValue(
                    "windowslogonaccount",
                    It.IsAny<string>(), // description
                    It.IsAny<bool>(),   // secret
                    It.IsAny<string>(), // defaultValue
                    It.IsAny<Func<string, bool>>(), // validator
                    It.IsAny<Dictionary<string, string>>(), //args
                    It.IsAny<bool>() // unattended
                )).Returns(_expectedLogonAccount);

            _reader.Setup(x => x.ReadValue(
                    "windowslogonpassword",
                    It.IsAny<string>(), // description
                    It.IsAny<bool>(),   // secret
                    It.IsAny<string>(), // defaultValue
                    It.IsAny<Func<string, bool>>(), // validator
                    It.IsAny<Dictionary<string, string>>(), //args
                    It.IsAny<bool>() // unattended
                )).Returns(_expectedLogonPassword);
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(this, testName);
            tc.SetSingleton<IConsoleWizard>(_reader.Object);
            tc.SetSingleton<IProcessInvoker>(_processInvoker.Object);
            return tc;
        }

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
#endif
        public async Task WindowsServiceControlManagerShouldInstallService()
        {
            using (var tc = CreateTestContext())
            {
                var serviceControlManager = new WindowsServiceControlManager();
                serviceControlManager.Initialize(tc);
                var agentSettings = new AgentSettings { ServerUrl = "http://server.name", AgentName = "myagent"};
                    await 
                    serviceControlManager.ConfigureServiceAsync(
                        agentSettings,
                        new Dictionary<string, string>
                            {
                                { "windowslogonaccount", "NT AUTHORITY\\LOCAL SERVICE" },
                                { "windowslogonpassword", "test" }
                            },
                        true);

                Assert.Equal("vstsagent.server.myagent", agentSettings.WindowsServiceName);
                Assert.Equal("VSTS Agent (server.myagent)", agentSettings.WindowsServiceDisplayName);
            }
        }

#if OS_WINDOWS
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
#endif
        public async Task PromptForPasswordForNonDefaultServiceUserAccount()
        {
            using (var tc = this.CreateTestContext())
            {
                var serviceControlManager = new WindowsServiceControlManager();
                serviceControlManager.Initialize(tc);
                var agentSettings = new AgentSettings { ServerUrl = "http://server.name", AgentName = "myagent" };

                _reader.Setup(
                    x => x.ReadValue(
                        "windowslogonaccount",
                        It.IsAny<string>(), // description
                        It.IsAny<bool>(), // secret
                        It.IsAny<string>(), // defaultValue
                        It.IsAny<Func<string, bool>>(), // validator
                        It.IsAny<Dictionary<string, string>>(), // args
                        It.IsAny<bool>())).Returns("domain\\randomuser");

                await serviceControlManager.ConfigureServiceAsync(agentSettings, null, true);

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
    }
}