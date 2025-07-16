using GitHub.Runner.Common.Tests;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Container;
using GitHub.Runner.Worker.Handlers;
using Moq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace GitHub.Runner.Common.Tests.Worker
{
    public sealed class StepHostNodeVersionL0
    {
        private Mock<IExecutionContext> _ec;
        private DefaultStepHost _defaultStepHost;

        public StepHostNodeVersionL0()
        {
            _ec = new Mock<IExecutionContext>();
            _defaultStepHost = new DefaultStepHost();
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_Node24OnArm32Linux()
        {
            // Test via NodeUtil directly
            string preferredVersion = "node24";
            var (nodeVersion, warningMessage) = GitHub.Runner.Common.Util.NodeUtil.CheckNodeVersionForLinuxArm32(preferredVersion);

            // On ARM32 Linux, we should fall back to node20
            bool isArm32 = RuntimeInformation.ProcessArchitecture == Architecture.Arm ||
                          Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE")?.Contains("ARM") == true;
            bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isArm32 && isLinux)
            {
                // Should downgrade to node20 on ARM32 Linux
                Assert.Equal("node20", nodeVersion);
                Assert.NotNull(warningMessage);
                Assert.Contains("Node 24 is not supported on Linux ARM32 platforms", warningMessage);
            }
            else
            {
                // On non-ARM32 platforms, should pass through the version unmodified
                Assert.Equal("node24", nodeVersion);
                Assert.Null(warningMessage);
            }
        }

        [Fact]
        [Trait("Level", "L0")]
        [Trait("Category", "Worker")]
        public void CheckNodeVersionForArm32_PassThroughNonNode24Versions()
        {
            string preferredVersion = "node20";
            var (nodeVersion, warningMessage) = GitHub.Runner.Common.Util.NodeUtil.CheckNodeVersionForLinuxArm32(preferredVersion);

            // Should never modify the version for non-node24 inputs
            Assert.Equal("node20", nodeVersion);
            Assert.Null(warningMessage);
        }
    }
}
