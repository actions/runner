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
        private const string LinuxServiceName = "vsts.agent.server.agent.service";

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
            _serviceHelper.Setup(x => x.GetUnitFile(LinuxServiceName)).Returns(Path.Combine(IOUtil.GetBinPath(), LinuxServiceName));
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
                                            ServerUrl = "http://server.name"
                                        };
                string unitFileTemplatePath = Path.Combine(IOUtil.GetBinPath(), "vsts.agent.service.template");
                File.WriteAllText(unitFileTemplatePath, "{User}-{Description}");

                controlManager.ConfigureService(agentSettings, null);

                
                Assert.Equal(controlManager.ServiceName, LinuxServiceName);
                Assert.Equal(controlManager.ServiceDisplayName, "VSTS Agent (server.agent)");

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
        public void VerifyLinuxServiceConfigurationFileContent()
        {
            using (var tc = CreateTestContext())
            {
                var controlManager = new LinuxServiceControlManager();
                controlManager.Initialize(tc);
                tc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
                tc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);
                tc.EnqueueInstance<IProcessInvoker>(_processInvoker.Object);

                var testUser = "testuser";
                Environment.SetEnvironmentVariable("SUDO_USER", testUser);
                string expectedUnitContent = string.Format(@"[Unit]
Description=VSTS Agent (server.agent)
After=network.target

[Service]
ExecStart={0}/runsvc.sh
User={1}
WorkingDirectory={0}
KillMode=process
KillSignal=SIGTERM
TimeoutStopSec=5min

[Install]
WantedBy=multi-user.target
", IOUtil.GetRootPath(), testUser);

                var agentSettings = new AgentSettings
                                        {
                                            AgentName = "agent",
                                            ServerUrl = "http://server.name"
                                        };
                string unitFileTemplatePath = Path.Combine(TestUtil.GetSrcPath(), "Misc", "layoutbin", "vsts.agent.service.template");
                string unitFilePath = _serviceHelper.Object.GetUnitFile(LinuxServiceName);
                File.Copy(unitFileTemplatePath, IOUtil.GetBinPath(), true);
                _serviceHelper.Setup(x => x.CheckIfSystemdExists()).Returns(true);

                controlManager.ConfigureService(agentSettings, null);

                Assert.Equal(File.ReadAllText(unitFilePath), expectedUnitContent);
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
                                            ServerUrl = "http://server.name"
                                        };

                // Tests dont run with sudo permission
                Environment.SetEnvironmentVariable("SUDO_USER", Environment.GetEnvironmentVariable("USER"));
                controlManager.ServiceName = "testservice";
                controlManager.StartService();

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
