using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.VisualStudio.Services.Agent.Tests.Listener.Configuration
{
    public sealed class LinuxServiceControlManagerL0
    {
        private Mock<IProcessInvoker> _processInvoker;
        private Mock<INativeLinuxServiceHelper> _serviceHelper;

        public LinuxServiceControlManagerL0()
        {
            _processInvoker = new Mock<IProcessInvoker>();
            _serviceHelper = new Mock<INativeLinuxServiceHelper>();

            _processInvoker.Setup(
                x =>
                x.ExecuteAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<CancellationToken>())).Returns(Task.FromResult(0));
            _serviceHelper.Setup(x => x.CheckIfSystemdExists()).Returns(true);
            _serviceHelper.Setup(x => x.GetUnitFile("vsts.agent.server.agent.service")).Returns(Path.Combine(IOUtil.GetBinPath(), "vsts.agent.server.agent.service"));
        }

        private TestHostContext CreateTestContext([CallerMemberName] String testName = "")
        {
            TestHostContext tc = new TestHostContext(this, testName);
            tc.SetSingleton<IProcessInvoker>(_processInvoker.Object);
            tc.SetSingleton<INativeLinuxServiceHelper>(_serviceHelper.Object);
            return tc;
        }

#if OS_LINUX
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
#endif
        public void LinuxServiceControlManagerShouldConfigureCorrectly()
        {
            using (var tc = CreateTestContext())
            {
                var controlManager = new LinuxServiceControlManager();
                controlManager.Initialize(tc);
                tc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
                tc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
                tc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);

                var agentSettings = new AgentSettings
                                        {
                                            AgentName = "agent",
                                            ServiceName = "testservice",
                                            ServiceDisplayName = "testservice",
                                            ServerUrl = "http://server.name"
                                        };
                string unitFileTemplatePath = Path.Combine(IOUtil.GetBinPath(), "vsts.agent.service.template");
                File.WriteAllText(unitFileTemplatePath, "{User}-{Description}");

                controlManager.ConfigureService(agentSettings, null);
                
                Assert.Equal(agentSettings.ServiceName, "vsts.agent.server.agent.service");
                Assert.Equal(agentSettings.ServiceDisplayName, "VSTS Agent (server.agent)");

                _processInvoker.Verify(
                    x => x.ExecuteAsync("/usr/bin", "systemctl", "daemon-reload", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
                    Times.Once);

                _processInvoker.Verify(
                    x =>
                    x.ExecuteAsync("/usr/bin", "systemctl", "enable vsts.agent.server.agent.service", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }


#if OS_LINUX
        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Common")]
#endif
        public void LinuxServiceControlManagerShouldStartServiceCorrectly()
        {
            using (var tc = CreateTestContext())
            {
                var controlManager = new LinuxServiceControlManager();
                controlManager.Initialize(tc);

                tc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
                tc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
                var agentSettings = new AgentSettings
                                        {
                                            AgentName = "agent",
                                            ServiceName = "testservice",
                                            ServiceDisplayName = "testservice",
                                            ServerUrl = "http://server.name"
                                        };

                // Tests dont run with sudo permission
                Environment.SetEnvironmentVariable("SUDO_USER", Environment.GetEnvironmentVariable("USER"));
                controlManager.StartService(agentSettings.ServiceName);

                _processInvoker.Verify(
                    x => x.ExecuteAsync("/usr/bin", "systemctl", "daemon-reload", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
                    Times.Once);

                _processInvoker.Verify(
                    x => x.ExecuteAsync("/usr/bin", "systemctl", "start testservice", It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()),
                    Times.Once);
            }
        }
    }
}